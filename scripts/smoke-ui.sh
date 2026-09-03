#!/usr/bin/env bash

# Salvo — recorrido ejecutable de la consola.
#
# El Blueprint exige para la Etapa 5 «recorrido completo con datos, sin datos y con errores», y la
# compuerta no puede verificarlo: `check.sh` corre Vitest en jsdom y un build, y ahí no se ejercitan
# ni un componente de servidor asíncrono real, ni una acción de servidor, ni `revalidatePath`. Esto
# levanta la API y `next start` de verdad y le pide las cuatro rutas a un servidor HTTP, buscando
# textos fijos que distinguen un escenario del otro.
#
# Los tres escenarios son los del Blueprint:
#
#   1. Con datos      — corpus demo cargado y puntuado.
#   2. Base vacía     — la misma API contra una base migrada y sin un solo pedido.
#   3. API apagada    — el proceso de la API muerto, la consola en pie.
#
# Reglas que este script se impone:
#
#   - No toca `salvo.db`. Usa bases propias en un directorio temporal y lo dice al terminar.
#   - No borra nada. Deja su directorio temporal donde el sistema operativo lo recoja.
#   - Espera activamente a que cada proceso responda, en vez de dormir un tiempo fijo.
#   - Libera procesos y puertos al salir, también si falla y también si lo interrumpen: un proceso
#     colgado en el puerto rompe la ejecución siguiente.
#   - Se niega a arrancar si los puertos ya están ocupados, antes que matar el proceso de otro.
#
# No forma parte de `scripts/check.sh`. Integrarlo a la compuerta se decide al cerrar la etapa.
#
#   ./scripts/smoke-ui.sh
#
# Variables: SMOKE_API_PORT (5199), SMOKE_WEB_PORT (3199), SMOKE_TIMEOUT_SECONDS (90).

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

api_port="${SMOKE_API_PORT:-5199}"
web_port="${SMOKE_WEB_PORT:-3199}"
timeout_seconds="${SMOKE_TIMEOUT_SECONDS:-90}"

api_base="http://127.0.0.1:${api_port}"
web_base="http://127.0.0.1:${web_port}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/salvo-smoke-XXXXXX")"
db_with_data="${work_dir}/with-data.db"
db_empty="${work_dir}/empty.db"
api_log="${work_dir}/api.log"
web_log="${work_dir}/web.log"

api_pid=""
web_pid=""
checks_run=0
checks_failed=0

# ---------------------------------------------------------------------------- salida y limpieza

# `trap ... EXIT` cubre el camino de éxito, el `set -e` y el Ctrl-C, que es exactamente lo que hace
# falta: un `next start` sobreviviente deja el puerto tomado para la corrida siguiente.
#
# Un detalle de bash que conviene saber antes de depurar este script: un `kill -INT` dirigido solo al
# pid de este proceso no aborta la corrida. Bash difiere una señal atrapada hasta que termina el
# comando en primer plano y, como el hijo no murió, sigue adelante. Ctrl-C en una terminal no tiene
# ese problema porque señaliza al grupo entero: mueren también la API y `next start`, este script lo
# detecta y sale por el trap. Verificado de las dos maneras.
cleanup() {
  local status=$?
  set +e

  stop_process "API" "$api_pid"
  stop_process "consola" "$web_pid"

  echo
  echo "Directorio de trabajo (no se borra nada): ${work_dir}"
  echo "  base con datos: ${db_with_data}"
  echo "  base vacía:     ${db_empty}"
  echo "  registros:      ${api_log}, ${web_log}"

  report_port_state "$api_port"
  report_port_state "$web_port"

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

port_in_use() {
  lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1
}

report_port_state() {
  if port_in_use "$1"; then
    echo "AVISO: el puerto $1 sigue ocupado."
  else
    echo "Puerto $1 libre."
  fi
}

# ---------------------------------------------------------------------------- utilidades

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

# Espera a que una URL responda, sondeándola. Nada de `sleep 5` y cruzar los dedos: el arranque de
# la API y el de `next start` no tardan lo mismo ni tardan siempre lo mismo.
wait_until_responding() {
  local label="$1" url="$2" pid="$3"
  local deadline=$((SECONDS + timeout_seconds))

  while ((SECONDS < deadline)); do
    if ! kill -0 "$pid" 2>/dev/null; then
      fail "el proceso de ${label} murió durante el arranque. Registro: ${work_dir}"
    fi

    if curl -sS -o /dev/null --max-time 5 "$url" 2>/dev/null; then
      echo "Listo: ${label} responde en ${url}"
      return 0
    fi

    sleep 0.25
  done

  fail "${label} no respondió en ${timeout_seconds} s. Registro: ${work_dir}"
}

# Espera a que el puerto quede libre antes de volver a usarlo.
wait_until_port_free() {
  local port="$1"
  local deadline=$((SECONDS + 30))

  while ((SECONDS < deadline)); do
    port_in_use "$port" || return 0
    sleep 0.2
  done

  fail "el puerto ${port} sigue ocupado después de detener el proceso."
}

# Una comprobación: pedir una ruta y exigir un texto exacto en el HTML.
#
# Se busca texto fijo y no un código de estado, porque los tres escenarios devuelven 200: la
# diferencia entre «no hay pedidos», «no hay corrida» y «no se pudo contactar a la API» está en lo
# que dice la página, que es justamente lo que hay que verificar.
expect_text() {
  local route="$1" needle="$2"
  local body status

  checks_run=$((checks_run + 1))
  body="$(curl -sS --max-time 30 -w $'\n%{http_code}' "${web_base}${route}" 2>/dev/null || true)"
  status="$(printf '%s' "$body" | tail -n 1)"

  if [[ "$status" != "200" ]]; then
    checks_failed=$((checks_failed + 1))
    printf '  FALLA  %-28s HTTP %s (se esperaba 200)\n' "$route" "${status:-sin respuesta}"
    return 0
  fi

  if printf '%s' "$body" | grep -qF -- "$needle"; then
    printf '  ok     %-28s «%s»\n' "$route" "$needle"
  else
    checks_failed=$((checks_failed + 1))
    printf '  FALLA  %-28s no contiene «%s»\n' "$route" "$needle"
  fi
}

# Exige que un texto NO esté. Se usa para la única cifra que esta consola no puede publicar.
expect_no_text() {
  local route="$1" needle="$2"

  checks_run=$((checks_run + 1))
  if curl -sS --max-time 30 "${web_base}${route}" 2>/dev/null | grep -qF -- "$needle"; then
    checks_failed=$((checks_failed + 1))
    printf '  FALLA  %-28s contiene «%s» y no debería\n' "$route" "$needle"
  else
    printf '  ok     %-28s sin «%s»\n' "$route" "$needle"
  fi
}

scenario() {
  echo
  echo "── Escenario: $1"
}

# ---------------------------------------------------------------------------- arranque

start_api() {
  local database="$1"

  ASPNETCORE_URLS="$api_base" \
  ConnectionStrings__SalvoDb="Data Source=${database}" \
  DemoData__Enabled=true \
    dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll >>"$api_log" 2>&1 &
  api_pid=$!

  wait_until_responding "API" "${api_base}/health" "$api_pid"
}

stop_api() {
  stop_process "API" "$api_pid"
  api_pid=""
  wait_until_port_free "$api_port"
}

migrate() {
  dotnet ef database update \
    --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
    --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
    --configuration Release \
    --no-build \
    --connection "Data Source=$1" >>"$api_log" 2>&1
}

# ---------------------------------------------------------------------------- preparación

echo "Salvo — recorrido de la consola"
echo "API ${api_base} · consola ${web_base}"

for port in "$api_port" "$web_port"; do
  if port_in_use "$port"; then
    fail "el puerto ${port} ya está ocupado. Detené ese proceso o elegí otro puerto con SMOKE_API_PORT / SMOKE_WEB_PORT."
  fi
done

command -v curl >/dev/null || fail "hace falta curl."
command -v lsof >/dev/null || fail "hace falta lsof para comprobar los puertos."

echo
echo "Compilando la API y la consola…"
dotnet build Salvo.slnx --configuration Release >>"$api_log" 2>&1 \
  || fail "no compila la solución. Registro: ${api_log}"
dotnet tool restore >>"$api_log" 2>&1 || fail "no se pudo restaurar el tooling de EF."

# La consola se construye apuntando a la API de este script, que en el build ni siquiera hace falta
# que exista: las rutas de datos son dinámicas y no se prerenderizan.
SALVO_API_BASE_URL="$api_base" npm run build --prefix frontend >>"$web_log" 2>&1 \
  || fail "no compila la consola. Registro: ${web_log}"

echo "Migrando las dos bases temporales…"
migrate "$db_with_data" || fail "falló la migración de la base con datos."
migrate "$db_empty" || fail "falló la migración de la base vacía."

echo "Levantando la consola…"
SALVO_API_BASE_URL="$api_base" PORT="$web_port" \
  npm run start --prefix frontend >>"$web_log" 2>&1 &
web_pid=$!

# ---------------------------------------------------------------------------- 1. con datos

scenario "con datos"

start_api "$db_with_data"
wait_until_responding "consola" "$web_base" "$web_pid"

echo "Cargando el corpus de demostración y ejecutando la corrida…"
curl -sS -X POST --max-time 60 "${api_base}/api/demo-data/seed" >/dev/null \
  || fail "falló la carga del corpus de demostración."
curl -sS -X POST --max-time 120 "${api_base}/api/risk-evaluations:run" >/dev/null \
  || fail "falló la corrida de scoring."

alert_id="$(
  curl -sS --max-time 30 "${api_base}/api/alerts?status=OPEN&pageSize=1" \
    | node -e 'let raw="";process.stdin.on("data",c=>raw+=c).on("end",()=>{const first=JSON.parse(raw).items[0];process.stdout.write(first ? first.id : "");})'
)"
[[ -n "$alert_id" ]] || fail "la corrida no dejó ninguna alerta abierta: el escenario con datos no tiene qué mostrar."
echo "Alerta de ejemplo: ${alert_id}"

expect_text "/" "Consola antifraude"
expect_text "/import" "Importación y scoring"
expect_text "/import" "Todos los pedidos de la base están cubiertos"
expect_text "/alerts" "Cola de alertas"
expect_text "/alerts/${alert_id}" "Snapshot que abrió la alerta"
expect_text "/alerts/${alert_id}" "Evaluación vigente"
expect_text "/dashboard" "Monto en riesgo"
expect_text "/dashboard" "Fraude reportado"
expect_text "/dashboard" "Pedidos y denegados por semana"
# La sección de calidad existe porque esta API arranca con DemoData__Enabled=true.
expect_text "/dashboard" "Calidad del criterio"
expect_text "/dashboard" "no la calidad del criterio de detección"
# La suma de las tres monedas del corpus demo. Es una cifra sin unidad y no puede estar en pantalla.
expect_no_text "/dashboard" "3.942.246"

# ---------------------------------------------------------------------------- 2. base vacía

scenario "base vacía"

stop_api
start_api "$db_empty"

expect_text "/import" "todavía no hay ninguno en la base"
expect_text "/alerts" "Todavía no hay pedidos"
expect_text "/dashboard" "Todavía no hay pedidos"
# Un identificador con forma válida y sin fila detrás: el 404 de la API tiene su propio texto.
expect_text "/alerts/00000000-0000-4000-8000-000000000000" "Esta alerta ya no existe"

# ---------------------------------------------------------------------------- 3. API apagada

scenario "API apagada"

stop_api

expect_text "/" "Consola antifraude"
expect_text "/import" "No se pudo contactar a la API"
expect_text "/alerts" "No se pudo contactar a la API"
expect_text "/alerts/00000000-0000-4000-8000-000000000000" "No se pudo contactar a la API"
expect_text "/dashboard" "No se pudo contactar a la API"

# ---------------------------------------------------------------------------- resultado

echo
if ((checks_failed == 0)); then
  echo "Recorrido verde: ${checks_run} comprobaciones, 0 fallas."
else
  echo "Recorrido en rojo: ${checks_failed} de ${checks_run} comprobaciones fallaron."
  exit 1
fi
