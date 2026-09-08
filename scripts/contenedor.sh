#!/usr/bin/env bash

# Salvo — construir, correr y medir el contenedor con la forma de un tier gratuito.
#
# La Etapa 10 quiere publicar Salvo donde cualquiera lo abra sin instalar nada, y el riesgo central
# no es la memoria: es **el camino frío**. Las tres plataformas gratuitas candidatas duermen la
# instancia cuando nadie la usa —Koyeb tras una hora, Render a los quince minutos, SnapDeploy
# también—, así que cada visitante que llega después de un silencio espera a que el contenedor
# arranque los dos procesos, migre una base vacía, siembre 300 pedidos y corra el scoring. Este
# script mide eso, por partes y contra los umbrales que ya existen en `scripts/smoke-ui.sh`.
#
#   ./scripts/contenedor.sh construir     construye la imagen
#   ./scripts/contenedor.sh correr        la levanta con los límites y espera a que responda
#   ./scripts/contenedor.sh medir         camino frío y camino tibio, cronometrados
#   ./scripts/contenedor.sh parar         detiene el contenedor de `correr`
#
# Reglas que este script se impone, heredadas de `demo.sh` y `smoke-ui.sh`:
#
#   - **No borra nada.** Ni imágenes, ni contenedores, ni volúmenes: los deja y los nombra al
#     terminar para que los borre una persona. `rm` está denegado en este repositorio por decisión
#     del usuario, y la regla se respeta también del lado de Docker.
#   - Cada medición del camino frío estrena un contenedor, porque `/data` vive en su capa escribible
#     y un contenedor nuevo es la única forma honesta de tener una base vacía.
#   - Se niega a arrancar si el puerto ya está ocupado, antes que matar el proceso de otro.
#   - Espera activamente a que cada cosa responda, en vez de dormir un tiempo fijo.
#
# Variables: SALVO_IMAGE (salvo:e10a), SALVO_PORT (3210), SALVO_MEMORY (512m), SALVO_CPUS (0.5),
# DOCKER (ruta al binario; por defecto `docker`, y si no está en el PATH prueba ~/.docker/bin).

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

image="${SALVO_IMAGE:-salvo:e10a}"
port="${SALVO_PORT:-3210}"
memory="${SALVO_MEMORY:-512m}"
cpus="${SALVO_CPUS:-0.5}"
container="${SALVO_CONTAINER:-salvo-e10a}"
timeout_seconds="${SALVO_TIMEOUT_SECONDS:-180}"

# Docker Desktop instala su binario en ~/.docker/bin, que no siempre está en el PATH de un shell no
# interactivo. Se busca ahí antes de rendirse, porque «no está en el PATH» y «no está instalado» son
# dos problemas distintos y solo uno de los dos justifica parar.
docker_bin="${DOCKER:-}"
if [[ -z "$docker_bin" ]]; then
  if command -v docker >/dev/null 2>&1; then
    docker_bin="$(command -v docker)"
  elif [[ -x "${HOME}/.docker/bin/docker" ]]; then
    docker_bin="${HOME}/.docker/bin/docker"
  else
    echo "ERROR: no se encontró el binario de docker. Instalalo, o pasá DOCKER=/ruta/a/docker." >&2
    exit 1
  fi
fi

docker() { "$docker_bin" "$@"; }

fail() { echo "ERROR: $*" >&2; exit 1; }

port_in_use() { lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1; }

# Milisegundos desde la época. `date +%s%3N` no existe en el `date` de macOS, así que lo da Node,
# que hace falta igual para leer las respuestas de la API.
now_ms() { node -e 'process.stdout.write(String(Date.now()))'; }

human() { node -e 'const ms=Number(process.argv[1]); process.stdout.write((ms/1000).toFixed(2)+" s")' "$1"; }

# ------------------------------------------------------------------ construir

construir() {
  echo "Construyendo ${image}…"
  local started elapsed
  started="$(now_ms)"
  docker build -t "$image" . || fail "falló la construcción de la imagen."
  elapsed=$(( $(now_ms) - started ))
  echo "Listo en $(human "$elapsed"). Tamaño: $(docker images "$image" --format '{{.Size}}')"
}

# ------------------------------------------------------------------ correr

# Arranca un contenedor y devuelve su nombre por stdout. El llamador decide si esperarlo.
arrancar() {
  local name="$1"
  docker run -d --name "$name" \
    --memory="$memory" --memory-swap="$memory" --cpus="$cpus" \
    -p "${port}:3000" \
    "$image" >/dev/null || fail "no se pudo arrancar el contenedor ${name}."
}

esperar_consola() {
  local name="$1"
  local deadline=$((SECONDS + timeout_seconds))

  while ((SECONDS < deadline)); do
    if [[ "$(docker inspect -f '{{.State.Running}}' "$name" 2>/dev/null)" != "true" ]]; then
      echo "--- registros ---" >&2
      docker logs "$name" 2>&1 | tail -30 >&2
      fail "el contenedor ${name} terminó durante el arranque."
    fi
    if curl -fsS -o /dev/null --max-time 5 "http://127.0.0.1:${port}/" 2>/dev/null; then
      return 0
    fi
    sleep 0.2
  done

  fail "el contenedor ${name} no respondió en ${timeout_seconds} s."
}

correr() {
  port_in_use "$port" && fail "el puerto ${port} ya está ocupado. Elegí otro con SALVO_PORT."
  docker inspect "$container" >/dev/null 2>&1 && \
    fail "ya existe un contenedor llamado ${container}. Paralo con './scripts/contenedor.sh parar', o elegí otro nombre con SALVO_CONTAINER. Este script no borra contenedores."

  echo "Levantando ${container} con ${memory} de RAM y ${cpus} vCPU, en el puerto ${port}…"
  arrancar "$container"
  esperar_consola "$container"

  cat <<LISTO

────────────────────────────────────────────────────────────────────────
  La consola responde en http://127.0.0.1:${port}

  La API **no** está publicada: escucha en 127.0.0.1 dentro del
  contenedor y ningún rewrite la reexpone. Para hablarle:
    ${docker_bin} exec ${container} node -e "fetch('http://127.0.0.1:5100/health').then(r=>r.text()).then(console.log)"

  Registros:  ${docker_bin} logs -f ${container}
  Detener:    ./scripts/contenedor.sh parar
────────────────────────────────────────────────────────────────────────

LISTO
}

parar() {
  docker inspect "$container" >/dev/null 2>&1 || fail "no hay ningún contenedor llamado ${container}."
  docker stop "$container" >/dev/null && echo "Detenido: ${container}."
  echo "No se borra: queda para inspección. Para borrarlo, '${docker_bin} rm ${container}'."
}

# ------------------------------------------------------------------ medir

# Una petición a la API desde dentro del contenedor, porque desde fuera no es alcanzable —que es
# justamente la propiedad que esta imagen tiene que cumplir—. Devuelve el cuerpo por stdout.
api() {
  local name="$1" method="$2" path="$3"
  docker exec "$name" node -e '
    const [method, path] = process.argv.slice(1);
    fetch("http://127.0.0.1:5100" + path, { method })
      .then(async (response) => {
        process.stdout.write(await response.text());
        process.exit(response.ok ? 0 : 1);
      })
      .catch((error) => { console.error(String(error)); process.exit(1); });
  ' "$method" "$path"
}

memoria_mib() {
  docker stats --no-stream --format '{{.MemUsage}}' "$1" | awk '{print $1}'
}

# Cronometra un comando y escribe una fila de la tabla.
fila() {
  local etiqueta="$1" umbral="$2"; shift 2
  local started elapsed salida archivo
  started="$(now_ms)"
  salida="$("$@")" || { printf '  %-46s %-11s FALLÓ\n' "$etiqueta" "$umbral"; return 1; }
  elapsed=$(( $(now_ms) - started ))
  printf '  %-46s %-11s %s\n' "$etiqueta" "$umbral" "$(human "$elapsed")"
  # La barra de una ruta como «/alerts» no puede ir en un nombre de archivo: sin esto la escritura
  # falla con «No such file or directory» y la medición sigue sin guardar la respuesta.
  archivo="$(printf '%s' "$etiqueta" | tr ' /' '__')"
  printf '%s' "$salida" > "${medicion_dir}/${archivo}.txt" 2>/dev/null || true
}

render() {
  curl -fsS -o /dev/null --max-time 30 "http://127.0.0.1:${port}$1"
}

medir() {
  local name="${container}-medicion-$(date +%Y%m%d-%H%M%S)"
  medicion_dir="$(mktemp -d "${TMPDIR:-/tmp}/salvo-medicion-XXXXXX")"

  port_in_use "$port" && fail "el puerto ${port} ya está ocupado. Elegí otro con SALVO_PORT."

  echo "Salvo — medición del camino frío"
  echo "Imagen ${image} · ${memory} de RAM · ${cpus} vCPU · puerto ${port}"
  echo "Contenedor: ${name} (nuevo, con /data vacío)"
  echo

  local t_arranque started
  started="$(now_ms)"
  arrancar "$name"
  esperar_consola "$name"
  t_arranque=$(( $(now_ms) - started ))

  echo "CAMINO FRÍO — contenedor nuevo, base vacía"
  printf '  %-46s %-11s %s\n' "Qué se mide" "Umbral" "Medido"
  printf '  %-46s %-11s %s\n' "$(printf '%.0s-' {1..46})" "-----------" "----------"
  printf '  %-46s %-11s %s\n' "Arranque: dos procesos + migración + primer 200" "90 s" "$(human "$t_arranque")"

  # La migración por separado, de los propios registros con marca de tiempo: entre la línea que
  # anuncia que va a migrar y la que dice que la API responde.
  local t_migracion
  t_migracion="$(docker logs -t "$name" 2>&1 | node -e '
    let raw = "";
    process.stdin.on("data", (d) => (raw += d)).on("end", () => {
      const lines = raw.split("\n");
      const stamp = (needle) => {
        const line = lines.find((l) => l.includes(needle));
        return line ? Date.parse(line.slice(0, line.indexOf(" "))) : null;
      };
      const from = stamp("applying pending migrations");
      const to = stamp("la API responde en");
      process.stdout.write(from && to ? String(to - from) : "");
    });
  ')"
  if [[ -n "$t_migracion" ]]; then
    printf '  %-46s %-11s %s\n' "  de los cuales, migrar la base vacía" "sin umbral" "$(human "$t_migracion")"
  fi

  printf '  %-46s %-11s %s\n' "RAM en reposo" "512 MiB" "$(memoria_mib "$name")"

  fila "Sembrado de los 300 pedidos" "60 s" api "$name" POST /api/demo-data/seed
  fila "Corrida de scoring sobre los 300" "120 s" api "$name" POST /api/risk-evaluations:run
  printf '  %-46s %-11s %s\n' "RAM tras la corrida" "512 MiB" "$(memoria_mib "$name")"

  fila "Primer render de /" "30 s" render "/"
  fila "Primer render de /alerts" "30 s" render "/alerts"
  fila "Primer render de /dashboard" "30 s" render "/dashboard"
  fila "Primer render de /import" "30 s" render "/import"

  echo
  echo "CAMINO TIBIO — el mismo contenedor, con la base ya sembrada"
  printf '  %-46s %-11s %s\n' "Qué se mide" "Umbral" "Medido"
  printf '  %-46s %-11s %s\n' "$(printf '%.0s-' {1..46})" "-----------" "----------"
  fila "Sembrado repetido (idempotente)" "60 s" api "$name" POST /api/demo-data/seed
  fila "Corrida repetida (reusa evaluaciones)" "120 s" api "$name" POST /api/risk-evaluations:run
  fila "Render de /alerts, ya caliente" "30 s" render "/alerts"
  fila "Render de /dashboard, ya caliente" "30 s" render "/dashboard"
  printf '  %-46s %-11s %s\n' "RAM al terminar" "512 MiB" "$(memoria_mib "$name")"

  echo
  echo "Respuestas guardadas en ${medicion_dir}"
  echo "El contenedor ${name} queda en pie y **no se borra**: paralo y borralo vos con"
  echo "  ${docker_bin} stop ${name} && ${docker_bin} rm ${name}"
}

# ------------------------------------------------------------------ despacho

case "${1:-correr}" in
  construir) construir ;;
  correr)    correr ;;
  medir)     medir ;;
  parar)     parar ;;
  *) fail "uso: $0 [construir|correr|medir|parar]" ;;
esac
