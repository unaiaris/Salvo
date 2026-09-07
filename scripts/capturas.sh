#!/usr/bin/env bash

# Salvo — regenera las seis capturas del README.
#
# Una captura es una afirmación sobre el producto, y una afirmación que nadie puede volver a
# comprobar no vale nada. Este script existe para que cualquiera que clone el repositorio pueda
# rehacerlas y ver si lo que muestran sigue siendo cierto.
#
# Reutiliza el diseño de `scripts/smoke-ui.sh`: base temporal migrada con `--connection`, API y
# consola en puertos propios, seed y corrida por `curl`. Lo que agrega es que el estado de cada toma
# se prepara por API **antes** de fotografiar, y que la sexta toma se hace con un navegador porque
# no tiene otra manera honesta de existir.
#
# Reglas que este script se impone:
#
#   - **No borra nada.** Escribe siempre los mismos seis nombres en `docs/capturas/` y sobrescribe.
#     Nunca toca `salvo.db`: usa una base temporal propia.
#   - La descarga del navegador es un **paso explícito**, no un efecto de instalar dependencias.
#     Verificado en `playwright@1.63.0`: ni él ni `playwright-core` declaran script de instalación
#     (`hasInstallScript: false` en el lockfile), así que `npm ci` no baja nada por su cuenta.
#   - No toca `frontend/package.json`, su lockfile, ni la compuerta. Playwright vive en su propio
#     paquete, en `tools/capturas/`, con su propio lockfile.
#   - Se niega a arrancar si los puertos ya están ocupados, antes que matar el proceso de otro.
#   - Libera los procesos al salir, también si falla y también si lo interrumpen.
#
# La primera vez descarga el Chromium que fija esa versión de Playwright: unos 240 MB entre el
# navegador y su shell headless, una sola vez por máquina, en la caché de Playwright del usuario.
#
#   ./scripts/capturas.sh
#
# Variables: CAPTURAS_API_PORT (5299), CAPTURAS_WEB_PORT (3299), CAPTURAS_TIMEOUT_SECONDS (120).

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

api_port="${CAPTURAS_API_PORT:-5299}"
web_port="${CAPTURAS_WEB_PORT:-3299}"
timeout_seconds="${CAPTURAS_TIMEOUT_SECONDS:-120}"

api_base="http://127.0.0.1:${api_port}"
web_base="http://127.0.0.1:${web_port}"

output_dir="${repository_root}/docs/capturas"
sample="${repository_root}/docs/muestras/import-con-errores.csv"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/salvo-capturas-XXXXXX")"
database="${work_dir}/capturas.db"
api_log="${work_dir}/api.log"
web_log="${work_dir}/web.log"

api_pid=""
web_pid=""

# ---------------------------------------------------------------------------- salida y limpieza

cleanup() {
  local status=$?
  set +e

  stop_process "consola" "$web_pid"
  stop_process "API" "$api_pid"

  echo
  echo "Directorio de trabajo (no se borra nada): ${work_dir}"
  echo "  base temporal: ${database}"
  echo "  registros:     ${api_log}, ${web_log}"

  exit "$status"
}
trap cleanup EXIT INT TERM

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

step() {
  echo
  echo "── $1"
}

# ---------------------------------------------------------------------------- preparación

echo "Salvo — capturas del README"
echo "API ${api_base} · consola ${web_base}"

command -v curl >/dev/null || fail "hace falta curl."
command -v lsof >/dev/null || fail "hace falta lsof para comprobar los puertos."
command -v node >/dev/null || fail "hace falta node."

[[ -f "$sample" ]] || fail "falta la muestra versionada ${sample}."

for port in "$api_port" "$web_port"; do
  if port_in_use "$port"; then
    fail "el puerto ${port} ya está ocupado. Detené ese proceso, o elegí otros con CAPTURAS_API_PORT / CAPTURAS_WEB_PORT."
  fi
done

step "Instalando Playwright y su navegador"

# Los dos pasos van separados a propósito: el primero no descarga navegadores, y el segundo es la
# descarga, dicha en voz alta.
npm ci --prefix tools/capturas >>"$web_log" 2>&1 \
  || fail "no se pudo instalar el paquete de capturas. Registro: ${web_log}"
npm exec --prefix tools/capturas -- playwright install chromium \
  || fail "no se pudo descargar el Chromium de Playwright."

step "Compilando la API y la consola"

dotnet build Salvo.slnx --configuration Release >>"$api_log" 2>&1 \
  || fail "no compila la solución. Registro: ${api_log}"
dotnet tool restore >>"$api_log" 2>&1 || fail "no se pudo restaurar el tooling de EF."

SALVO_API_BASE_URL="$api_base" npm run build --prefix frontend >>"$web_log" 2>&1 \
  || fail "no compila la consola. Registro: ${web_log}"

step "Migrando la base temporal"

dotnet ef database update \
  --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
  --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
  --configuration Release \
  --no-build \
  --connection "Data Source=${database}" >>"$api_log" 2>&1 \
  || fail "falló la migración de la base temporal. Registro: ${api_log}"

step "Levantando la API y la consola"

ASPNETCORE_URLS="$api_base" \
ConnectionStrings__SalvoDb="Data Source=${database}" \
DemoData__Enabled=true \
  dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll >>"$api_log" 2>&1 &
api_pid=$!
wait_until_responding "API" "${api_base}/health" "$api_pid"

SALVO_API_BASE_URL="$api_base" PORT="$web_port" \
  npm run start --prefix frontend >>"$web_log" 2>&1 &
web_pid=$!
wait_until_responding "consola" "$web_base" "$web_pid"

# ---------------------------------------------------------------------------- estado de las tomas

step "Preparando el estado que cada toma promete"

curl -sS -X POST --max-time 120 "${api_base}/api/demo-data/seed" >/dev/null \
  || fail "falló la carga del corpus de demostración."
curl -sS -X POST --max-time 180 "${api_base}/api/risk-evaluations:run" >/dev/null \
  || fail "falló la corrida de scoring."

# La alerta se resuelve **por el pedido**, nunca por posición ni por un id escrito a mano: el id es
# un `Guid.NewGuid()` y el desempate del orden de la cola también, así que «la primera fila» nombra
# una alerta distinta en cada regeneración.
#
# Se exige que la referencia identifique exactamente una alerta. En el corpus demo `ORD_000011` es
# único, pero la unicidad de un pedido es `(merchantId, merchantReferenceId)`: si alguna vez el
# corpus trajera dos comercios con esa misma numeración, este script tiene que detenerse en vez de
# elegir uno de los dos en silencio.
resolved="$(
  curl -sS --max-time 30 "${api_base}/api/alerts?status=OPEN&pageSize=200" \
    | node -e '
        let raw = "";
        process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
          const matches = JSON.parse(raw).items.filter(
            (item) => item.merchantReferenceId === "ORD_000011",
          );
          if (matches.length !== 1) {
            process.stderr.write(`ORD_000011 identifica ${String(matches.length)} alertas abiertas.\n`);
            process.exit(1);
          }
          process.stdout.write(`${matches[0].id} ${matches[0].orderId}`);
        });
      '
)" || fail "no se pudo resolver la alerta de ORD_000011."

alert_id="${resolved% *}"
order_id="${resolved#* }"
echo "Alerta de ORD_000011: ${alert_id}"

# La explicación de la toma 3. La API verifica cada cifra y cada regla del texto contra la
# evaluación antes de guardarlo, así que exigir que vuelva `READY` comprueba esa verificación y no
# solo el transporte.
curl -sS -X POST --max-time 60 -H 'Content-Type: application/json' -d '{}' \
  "${api_base}/api/alerts/${alert_id}/explanation" \
  | node -e '
      let raw = "";
      process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
        const status = JSON.parse(raw).explanation.status;
        if (status !== "READY") {
          process.stderr.write(`la explicación quedó en ${status}.\n`);
          process.exit(1);
        }
      });
    ' || fail "la explicación de la toma 3 no quedó escrita."

# La divergencia de criterio de la toma 6. El proveedor simulado decide por los dígitos finales de
# la referencia módulo cien y aprueba por debajo de 75: `ORD_000011` cae en 11, así que responde
# `APPROVED` en el acto, sin callback, mientras el motor local marcó el pedido. La divergencia es
# determinista, y se exige que lo sea: si el proveedor no aprobara, no hay toma 6 que sacar.
curl -sS -X POST --max-time 60 -H 'Content-Type: application/json' -d '{}' \
  "${api_base}/api/orders/${order_id}/external-evaluations" \
  | node -e '
      let raw = "";
      process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
        const evaluation = JSON.parse(raw).evaluation;
        if (evaluation.status !== "APPROVED") {
          process.stderr.write(`el proveedor respondió ${evaluation.status} y no APPROVED.\n`);
          process.exit(1);
        }
      });
    ' || fail "el proveedor no aprobó ORD_000011: sin eso no hay divergencia de criterio que fotografiar."

# El panel de denegados por el proveedor sin alerta local, que es la única pantalla donde aparece un
# pedido que las reglas nunca marcaron. Sin esta llamada el panel sale vacío en la toma 4, diciendo
# que nadie pidió todavía la evaluación externa, y la captura del dashboard se queda justamente sin
# el argumento que ese panel existe para sostener: hay fraude que el proveedor ve y el motor no.
#
# Va **después** del pedido individual a propósito: este endpoint solo pregunta por los pedidos que
# nunca se le consultaron a este proveedor, así que ORD_000011 queda intacto con su APPROVED y la
# toma 6 sigue teniendo su divergencia. No se entregan los callbacks: con las evaluaciones síncronas
# el panel ya tiene contenido, y dejar 21 pendientes es el estado más honesto para fotografiar.
curl -sS -X POST --max-time 600 "${api_base}/api/demo-data/external-evaluations:request" \
  | node -e '
      let raw = "";
      process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
        const summary = JSON.parse(raw);
        if (summary.settled < 1) {
          process.stderr.write(`ninguna evaluación externa del corpus se asentó.\n`);
          process.exit(1);
        }
        process.stdout.write(
          `Evaluación externa del corpus: ${String(summary.settled)} asentadas, `
          + `${String(summary.stillPending)} pendientes.\n`,
        );
      });
    ' || fail "falló la evaluación externa del corpus: sin ella el panel de denegados sale vacío."

# ---------------------------------------------------------------------------- las capturas

step "Fotografiando"

CAPTURAS_WEB_BASE="$web_base" \
CAPTURAS_ALERT_ID="$alert_id" \
CAPTURAS_OUTPUT_DIR="$output_dir" \
CAPTURAS_SAMPLE="$sample" \
  node tools/capturas/capturar.mjs \
  || fail "falló la captura. La consola sigue en ${web_base} mientras este script viva."

# ---------------------------------------------------------------------------- resultado

step "Resultado"

missing=0
for shot in 01-cola.png 02-detalle.png 03-explicacion.png 04-dashboard.png 05-import.png 06-divergencia.png; do
  if [[ -f "${output_dir}/${shot}" ]]; then
    printf '  ok     %-22s %s\n' "$shot" "$(du -h "${output_dir}/${shot}" | cut -f1 | tr -d ' ')"
  else
    printf '  FALTA  %s\n' "$shot"
    missing=$((missing + 1))
  fi
done

echo
if ((missing == 0)); then
  echo "Seis capturas en ${output_dir}."
else
  fail "faltan ${missing} capturas."
fi
