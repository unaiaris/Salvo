#!/usr/bin/env bash

# Salvo — un ensayo de la demo, sobre una base nueva.
#
# El guion de `docs/guion-demo.md` es destructivo por diseño y no se puede repetir sobre la misma
# base: el veredicto de una alerta es terminal (decisión 35), una explicación escrita no se
# regenera, y el seed es idempotente, así que volver a sembrar no devuelve nada a cero. Cada ensayo
# consume el estado del anterior.
#
# El reset correcto no es borrar `salvo.db` —`rm` está denegado en este repositorio y es una regla
# del usuario—, sino el que ya usa el smoke: apuntar la API a **otra** base con
# `ConnectionStrings__SalvoDb` y migrarla con `dotnet ef database update --connection`. Este script
# hace eso con una base nueva cuyo nombre lleva la fecha y la hora, la deja poblada y puntuada, y
# levanta la API y la consola listas para el minuto 0 del guion.
#
# Reglas que este script se impone:
#
#   - **No borra nada.** Ni bases, ni registros, ni las bases de ensayos anteriores. La base que
#     estuviera en uso queda exactamente donde estaba, con su contenido intacto.
#   - Cada corrida estrena una base. Dos ensayos seguidos no comparten estado.
#   - Se niega a arrancar si los puertos ya están ocupados, antes que matar el proceso de otro.
#   - Espera activamente a que cada proceso responda, en vez de dormir un tiempo fijo.
#   - Libera los dos procesos al salir, también con Ctrl-C: un `next start` sobreviviente deja el
#     puerto tomado para el ensayo siguiente.
#
# Se queda en primer plano hasta que se lo interrumpa con Ctrl-C. Es lo que se espera de algo que
# levanta una demo: mientras corre, la demo está en pie.
#
#   ./scripts/demo.sh
#
# Variables: DEMO_API_PORT (5100), DEMO_WEB_PORT (3000), DEMO_TIMEOUT_SECONDS (120).

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

api_port="${DEMO_API_PORT:-5100}"
web_port="${DEMO_WEB_PORT:-3000}"
timeout_seconds="${DEMO_TIMEOUT_SECONDS:-120}"

api_base="http://127.0.0.1:${api_port}"
web_base="http://localhost:${web_port}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# La base vive junto a la que usa `dotnet run`, para que se vean todas juntas y se note cuántos
# ensayos hubo. `*.db` está en `.gitignore`, así que ninguna de ellas se versiona.
#
# La ruta es **absoluta a propósito**: `dotnet ef database update --connection` resuelve una ruta
# relativa contra el directorio del proyecto de arranque y no contra la raíz del repositorio, así
# que una ruta relativa termina en `backend/src/Salvo.Api/backend/src/Salvo.Api/…` y la migración
# falla con «unable to open database file». El smoke no tropieza con esto porque `mktemp -d` ya le
# devuelve una ruta absoluta.
database="${repository_root}/backend/src/Salvo.Api/salvo-demo-$(date +%Y%m%d-%H%M%S).db"

log_dir="$(mktemp -d "${TMPDIR:-/tmp}/salvo-demo-XXXXXX")"
api_log="${log_dir}/api.log"
web_log="${log_dir}/web.log"

api_pid=""
web_pid=""

# ---------------------------------------------------------------------------- salida y limpieza

cleanup() {
  local status=$?
  set +e

  echo
  stop_process "consola" "$web_pid"
  stop_process "API" "$api_pid"

  echo
  echo "La base del ensayo queda donde está, con todo lo que hiciste dentro:"
  echo "  ${database}"
  echo "  registros: ${api_log}, ${web_log}"
  echo
  echo "Para otro ensayo desde cero, volvé a correr este script: estrena otra base y no toca esta."

  exit "$status"
}
trap cleanup EXIT INT TERM

# Termina un proceso y espera a que muera de verdad. Un `kill` sin espera devuelve el control antes
# de que el sistema libere el puerto, y el arranque siguiente falla por «address already in use».
stop_process() {
  local label="$1" pid="$2"

  if [[ -z "$pid" ]] || ! kill -0 "$pid" 2>/dev/null; then
    return 0
  fi

  kill "$pid" 2>/dev/null
  for _ in $(seq 1 50); do
    kill -0 "$pid" 2>/dev/null || { echo "Detenido: ${label} (pid ${pid})"; return 0; }
    sleep 0.2
  done

  echo "El proceso de ${label} (pid ${pid}) ignoró SIGTERM; se envía SIGKILL."
  kill -9 "$pid" 2>/dev/null
  wait "$pid" 2>/dev/null
}

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

port_in_use() {
  lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1
}

wait_until_responding() {
  local label="$1" url="$2" pid="$3"
  local deadline=$((SECONDS + timeout_seconds))

  while ((SECONDS < deadline)); do
    if ! kill -0 "$pid" 2>/dev/null; then
      fail "el proceso de ${label} murió durante el arranque. Registro: ${log_dir}"
    fi

    if curl -sS -o /dev/null --max-time 5 "$url" 2>/dev/null; then
      echo "Listo: ${label} responde en ${url}"
      return 0
    fi

    sleep 0.25
  done

  fail "${label} no respondió en ${timeout_seconds} s. Registro: ${log_dir}"
}

# ---------------------------------------------------------------------------- preparación

echo "Salvo — ensayo de la demo"
echo

command -v curl >/dev/null || fail "hace falta curl."
command -v lsof >/dev/null || fail "hace falta lsof para comprobar los puertos."

for port in "$api_port" "$web_port"; do
  if port_in_use "$port"; then
    fail "el puerto ${port} ya está ocupado. Detené ese proceso, o elegí otros con DEMO_API_PORT / DEMO_WEB_PORT."
  fi
done

[[ -e "$database" ]] && fail "ya existe ${database}. Esperá un segundo y repetí: el nombre lleva la hora."

echo "Compilando la API y la consola…"
dotnet build Salvo.slnx --configuration Release >>"$api_log" 2>&1 \
  || fail "no compila la solución. Registro: ${api_log}"
dotnet tool restore >>"$api_log" 2>&1 || fail "no se pudo restaurar el tooling de EF."

SALVO_API_BASE_URL="$api_base" npm run build --prefix frontend >>"$web_log" 2>&1 \
  || fail "no compila la consola. Registro: ${web_log}"

echo "Creando y migrando la base del ensayo: ${database}"
dotnet ef database update \
  --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
  --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
  --configuration Release \
  --no-build \
  --connection "Data Source=${database}" >>"$api_log" 2>&1 \
  || fail "falló la migración de la base del ensayo. Registro: ${api_log}"

# ---------------------------------------------------------------------------- arranque

echo "Levantando la API…"
# `DemoData__Enabled=true` es la única bandera que enciende la carga del corpus, los dos botones del
# proveedor externo y la sección de calidad del criterio. Sin ella la API ni registra esas rutas.
ASPNETCORE_URLS="$api_base" \
ConnectionStrings__SalvoDb="Data Source=${database}" \
DemoData__Enabled=true \
  dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll >>"$api_log" 2>&1 &
api_pid=$!
wait_until_responding "API" "${api_base}/health" "$api_pid"

echo "Levantando la consola…"
SALVO_API_BASE_URL="$api_base" PORT="$web_port" \
  npm run start --prefix frontend >>"$web_log" 2>&1 &
web_pid=$!
wait_until_responding "consola" "$web_base" "$web_pid"

# ---------------------------------------------------------------------------- estado inicial

# La precondición del guion: corpus cargado y corrida ejecutada. Sin esto, el minuto 0 se gasta en
# clics de carga, y el feed y el dashboard arrancan diciendo que no hay pedidos.
echo
echo "Cargando el corpus de demostración…"
seed_summary="$(curl -sS -X POST --max-time 120 "${api_base}/api/demo-data/seed")" \
  || fail "falló la carga del corpus de demostración."

echo "Ejecutando la corrida de scoring…"
run_summary="$(curl -sS -X POST --max-time 180 "${api_base}/api/risk-evaluations:run")" \
  || fail "falló la corrida de scoring."

summarise() {
  node -e '
    let raw = "";
    process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
      const body = JSON.parse(raw);
      const fields = process.argv.slice(1);
      process.stdout.write(fields.map((field) => `${field}: ${String(body[field])}`).join(" · "));
    });
  ' "$@"
}

echo
echo "  Corpus:  $(printf '%s' "$seed_summary" | summarise totalOrders insertedOrders duplicateOrders)"
echo "  Corrida: $(printf '%s' "$run_summary" | summarise orderCount evaluationsCreated alertsCreated)"

# ---------------------------------------------------------------------------- listo

cat <<LISTO

────────────────────────────────────────────────────────────────────────
  La demo está en pie. El guion es docs/guion-demo.md.

    Consola   ${web_base}/alerts
    Dashboard ${web_base}/dashboard
    Importar  ${web_base}/import
    API       ${api_base}

  Base de este ensayo: ${database}
  Ctrl-C para bajar los dos procesos. No se borra nada.
────────────────────────────────────────────────────────────────────────

LISTO

# Se queda en pie hasta el Ctrl-C, y sale solo si alguno de los dos se muere: la consola sin API no
# es una demo, es la pantalla de «No se pudo contactar a la API».
#
# Se sondea en vez de usar `wait -n`, que devolvería en cuanto muriera cualquiera de los dos: el
# bash de macOS es el 3.2 —`/usr/bin/env bash` resuelve a 3.2.57 en esta máquina— y ahí `-n` no
# existe. `wait: -n: invalid option`, y el script se caía justo después de anunciar que estaba listo.
while kill -0 "$api_pid" 2>/dev/null && kill -0 "$web_pid" 2>/dev/null; do
  sleep 1
done

fail "uno de los dos procesos terminó por su cuenta. Registros: ${api_log}, ${web_log}"
