#!/usr/bin/env bash

# Salvo — el proceso 1 del contenedor: levanta la API y la consola, y muere si muere cualquiera.
#
# Un contenedor con dos procesos necesita que alguien responda una pregunta: qué pasa cuando uno de
# los dos se cae. Sin respuesta, el caso malo es silencioso y caro. Si la API muere y `server.js`
# sigue en pie, la consola contesta 200 en todas sus rutas —las páginas renderizan igual, con el
# aviso de que no se pudo contactar a la API, porque así está escrito a propósito en
# `frontend/src/lib/api/console.ts`— y la sonda de salud de la plataforma, que mira el puerto
# público, ve una instancia sana. Nadie se entera hasta que alguien la abre.
#
# La respuesta de este script: **la muerte de cualquiera de los dos termina el contenedor.** El
# supervisor de la plataforma lo reinicia, que es exactamente lo que se quiere de un servicio sin
# estado que se recrea al arrancar.
#
# Se usa `wait -n`, que devuelve en cuanto termina el primero de los hijos. `scripts/demo.sh` hace
# lo mismo con un bucle de sondeo y dice por qué: el bash de macOS es el 3.2 y ahí `wait -n` no
# existe. Acá la imagen es Ubuntu 24.04 con bash 5, así que se puede usar la primitiva buena y
# reaccionar al instante en vez de hasta un segundo después.

set -euo pipefail

api_port_url="${ASPNETCORE_URLS:-http://127.0.0.1:5100}"
health_url="${api_port_url%/}/health"
startup_timeout="${SALVO_API_STARTUP_TIMEOUT_SECONDS:-90}"

api_pid=""
web_pid=""
# Distingue las dos formas de terminar. Sin esto las dos se ven iguales desde afuera, y no lo son:
# que la plataforma pida detener el contenedor es un final normal, y que uno de los dos procesos se
# caiga solo no lo es nunca.
detencion_pedida=0

log() { printf '[entrypoint] %s\n' "$*"; }
fail() { log "ERROR: $*"; exit 1; }

# ------------------------------------------------------- la base, copiada de la imagen

# **Esto es el reinicio de la instancia pública.** La imagen lleva una base ya sembrada, puntuada,
# con las evaluaciones externas entregadas y una explicación escrita; copiarla encima de la de
# trabajo devuelve la instancia al estado prístino con una sola primitiva.
#
# Se hace acá y no con una cirugía sobre el archivo en caliente porque acá **todavía no hay nadie
# que lo tenga abierto**: la API arranca unas líneas más abajo. La revisión adversarial de la
# Etapa 10 marcó que borrar y recrear un SQLite bajo conexiones agrupadas no es gratis, y con la
# base horneada ese problema no llega a existir.
#
# Es incondicional, y esa es la propiedad. Un visitante que importó pedidos, emitió veredictos o
# escribió notas no le deja nada al siguiente, y nadie tiene que acordarse de limpiar — que es la
# tercera condición de la decisión 70.
copiar_base_horneada() {
  local baked="${SALVO_BAKED_DB:-}"

  if [[ -z "$baked" ]]; then
    log "SALVO_BAKED_DB vacío: la base de trabajo queda como esté"
    return 0
  fi

  [[ -f "$baked" ]] || fail "no está la base horneada en ${baked}."

  # La ruta de trabajo se saca de la cadena de conexión en vez de declararse aparte: dos
  # declaraciones de la misma ruta es una manera de que la API abra un archivo y el arranque
  # escriba otro, y eso se vería como una instancia que no reinicia nunca.
  local conn="${ConnectionStrings__SalvoDb:-}"
  [[ -n "$conn" ]] || fail "ConnectionStrings__SalvoDb no está definida y hay una base horneada que copiar."

  local work="${conn#*Data Source=}"
  work="${work%%;*}"
  work="${work#"${work%%[![:space:]]*}"}"
  work="${work%"${work##*[![:space:]]}"}"

  [[ "$work" = /* ]] || fail "la cadena de conexión no da una ruta absoluta: '${conn}'."

  # Los diarios de una ejecución anterior no se arrastran: describen transacciones de una base que
  # está por dejar de existir, y aplicarlos sobre la copia nueva sería reintroducir justo lo que el
  # reinicio quita. Se truncan en vez de borrarse porque `rm` está denegado en este repositorio.
  local sufijo
  for sufijo in -wal -shm -journal; do
    [[ -e "${work}${sufijo}" ]] && : > "${work}${sufijo}"
  done

  mkdir -p "$(dirname "$work")"
  cp "$baked" "$work"
  log "base restaurada desde ${baked} ($(wc -c < "$work" | tr -d ' ') bytes)"
}

copiar_base_horneada

# Baja a los dos hijos. Se llama tanto en la salida normal como ante SIGTERM/SIGINT: la plataforma
# manda SIGTERM al detener el contenedor y sin esto los hijos se quedarían hasta el SIGKILL.
shutdown() {
  local code=$?
  set +e
  trap - EXIT INT TERM

  for pid in "$web_pid" "$api_pid"; do
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null
    fi
  done
  wait 2>/dev/null

  exit "$code"
}

pedir_detencion() {
  detencion_pedida=1
  shutdown
}

trap shutdown EXIT
trap pedir_detencion INT TERM

# ------------------------------------------------------------------ la API

# Va primero y la consola espera a que responda. No es una preferencia de orden: el layout de la
# consola le pide el idioma del despliegue a `GET /api/system/capabilities` en cada render, así que
# una consola que abre antes que la API sirve sus primeras peticiones en el idioma por defecto y con
# el aviso de error puesto.
log "levantando la API en ${api_port_url}"
dotnet /app/api/Salvo.Api.dll &
api_pid=$!

# La sonda la hace Node y no `curl`, porque **`mcr.microsoft.com/dotnet/aspnet:10.0` no trae
# `curl`** —comprobado dentro de la imagen— y Node está garantizado: es el que corre la consola.
# Instalar `curl` para esto sería agregar un paquete a la imagen final para hacer lo que ya se
# puede hacer con lo que hay.
probe_health() {
  node -e '
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 5000);
    fetch(process.argv[1], { signal: controller.signal })
      .then((response) => process.exit(response.ok ? 0 : 1))
      .catch(() => process.exit(1))
      .finally(() => clearTimeout(timer));
  ' "$health_url" 2>/dev/null
}

deadline=$((SECONDS + startup_timeout))
until probe_health; do
  if ! kill -0 "$api_pid" 2>/dev/null; then
    log "la API murió durante el arranque"
    exit 1
  fi
  if ((SECONDS >= deadline)); then
    log "la API no respondió en ${startup_timeout} s"
    exit 1
  fi
  sleep 0.25
done
log "la API responde en ${health_url}"

# ------------------------------------------------------------------ la consola

log "levantando la consola en 0.0.0.0:${PORT:-3000}"
node /app/web/server.js &
web_pid=$!

# ------------------------------------------------------------------ la supervisión

# `wait -n` vuelve con el primero que termine, sea cual sea. A partir de ahí el contenedor está roto
# por definición: media aplicación no es la aplicación.
wait -n "$api_pid" "$web_pid"
first_exit=$?

if ((detencion_pedida)); then
  # Llegó un SIGTERM y el hijo terminó por eso. Es la salida ordenada.
  exit 0
fi

if kill -0 "$api_pid" 2>/dev/null; then
  log "la consola terminó (código ${first_exit}); se baja la API y termina el contenedor"
else
  log "la API terminó (código ${first_exit}); se baja la consola y termina el contenedor"
fi

# **Nunca se sale con 0 por acá**, y el motivo lo encontró una falsación de `E10A`. Matar la API
# dentro del contenedor la hacía terminar con código 0 —un apagado limpio de ASP.NET ante SIGTERM—,
# así que `wait -n` devolvía 0 y el contenedor terminaba anunciando éxito. Un supervisor de
# plataforma que distinga «terminó bien» de «se cayó» leería eso como un final deseado y podría no
# reiniciar. Que un hijo termine solo, con el código que sea, es siempre una falla de este
# contenedor: se traduce a 1 cuando el hijo no dio un código propio.
exit "$(( first_exit == 0 ? 1 : first_exit ))"
