#!/usr/bin/env bash

# Salvo — recorrido ejecutable de la consola.
#
# El Blueprint exige para la Etapa 5 «recorrido completo con datos, sin datos y con errores», y la
# compuerta no puede verificarlo: `check.sh` corre Vitest en jsdom y un build, y ahí no se ejercitan
# ni un componente de servidor asíncrono real, ni una acción de servidor, ni `revalidatePath`. Esto
# levanta la API y `next start` de verdad y le pide sus cinco rutas —`/`, `/import`, `/alerts`,
# `/alerts/[id]` y `/dashboard`— a un servidor HTTP, buscando textos fijos que distinguen un
# escenario del otro.
#
# Los tres escenarios del Blueprint, más los que agregaron las Etapas 6 y 7. Seis en total:
#
#   1.  Con datos             — corpus demo cargado y puntuado.
#   1b. Evaluación externa    — el mismo corpus, con la opinión del proveedor pedida y entregada.
#   1c. Con explicación       — el mismo corpus, con la evaluación del snapshot puesta en palabras.
#   1d. Plantilla anterior    — una explicación que escribió una plantilla que ya no es la vigente.
#   1e. Interruptor de idioma — la misma base y el mismo `next start`, con la API en portugués.
#   1f. Idioma desconocido    — `SALVO_LANGUAGE=fr`: la API no arranca.
#   2.  Base vacía            — la misma API contra una base migrada y sin un solo pedido.
#   3.  API apagada           — el proceso de la API muerto, la consola en pie y en castellano.
#
# El 1b y el 1c van después del 1 y no antes: el bloque externo y el de explicación tienen dos
# estados en pantalla cada uno —sin pedir y respondido— y comprobar el segundo destruye el primero.
# El 1d va sobre una alerta distinta por la misma razón: el 1c dejó la suya escrita con la vigente.
# El 1e va al final de la serie porque necesita que el 1c ya haya escrito la explicación castellana:
# lo que comprueba es que el despliegue en portugués escribe la suya al lado y no la pisa.
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

# El segundo argumento es el idioma del despliegue. Se pasa a la API y a nadie más: la consola lo
# lee de `GET /api/system/capabilities`, así que el mismo `next start` sirve los dos idiomas sin
# reconstruirse. Que este script no necesite tocar el proceso de Next para cambiar de idioma es
# justamente la propiedad que el origen único compra.
start_api() {
  local database="$1" language="${2:-}"
  local environment=(
    "ASPNETCORE_URLS=${api_base}"
    "ConnectionStrings__SalvoDb=Data Source=${database}"
    "DemoData__Enabled=true"
  )

  # Un arreglo y `env`, y no un prefijo `${language:+VAR=valor}` delante del comando: bash expande
  # ese prefijo *después* de decidir cuál es la palabra del comando, así que la asignación termina
  # siendo el comando y falla con «command not found». Verificado rompiéndolo.
  if [[ -n "$language" ]]; then
    environment+=("SALVO_LANGUAGE=${language}")
  fi

  env "${environment[@]}" \
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
# `e3-v2`: el motor emite los campos de cada señal y la consola compone la frase.
#
# Se comprueba sobre `ORD_000011`, no sobre la alerta que la cola devuelva primero: es un pedido que
# la fixture pone a propósito y que dispara `amount_anomaly`, así que la frase que tiene que aparecer
# se sabe de antemano. Comprobar solo que la prosa inglesa **no** está pasaría igual si la frase
# saliera vacía, que es justamente la manera en que esto se rompería.
anomaly_alert_id="$(
  curl -sS --max-time 30 "${api_base}/api/alerts?status=OPEN&pageSize=100" \
    | node -e 'let raw="";process.stdin.on("data",c=>raw+=c).on("end",()=>{const hit=JSON.parse(raw).items.find(i=>i.merchantReferenceId==="ORD_000011");process.stdout.write(hit ? hit.id : "");})'
)"
[[ -n "$anomaly_alert_id" ]] || fail "ORD_000011 no tiene alerta abierta: la fixture cambió y esta comprobación quedó sin sujeto."
expect_text "/alerts/${anomaly_alert_id}" "veces la mediana del comercio"
expect_text "/alerts/${anomaly_alert_id}" "pedidos previos"
# Y nada de la prosa que escribía `e3-v1`, que es lo que un analista leía antes.
expect_no_text "/alerts/${anomaly_alert_id}" "cents is"
expect_no_text "/alerts/${anomaly_alert_id}" "prior orders"
expect_no_text "/alerts/${anomaly_alert_id}" "differs from habitual"
# El tercer bloque de la Etapa 6, en su estado inicial: nadie pidió todavía la opinión del proveedor.
expect_text "/alerts/${alert_id}" "Evaluación externa"
expect_text "/alerts/${alert_id}" "Solicitar evaluación externa"
# El cuarto bloque de la Etapa 7, en su estado inicial: nadie pidió todavía el texto.
expect_text "/alerts/${alert_id}" "Explicación"
expect_text "/alerts/${alert_id}" "Todavía no se pidió una explicación"
expect_text "/alerts/${alert_id}" "Explicar esta evaluación"
expect_text "/import" "Proveedor antifraude externo"
expect_text "/dashboard" "Monto en riesgo"
expect_text "/dashboard" "Fraude reportado"
expect_text "/dashboard" "Pedidos y denegados por semana"
# La sección de calidad existe porque esta API arranca con DemoData__Enabled=true.
expect_text "/dashboard" "Calidad del criterio"
expect_text "/dashboard" "F1 es un parámetro elegido y no un resultado"
# La suma de las tres monedas del corpus demo. Es una cifra sin unidad y no puede estar en pantalla.
expect_no_text "/dashboard" "3.731.104"
# El panel de la Etapa 9: existe siempre, y sin evaluaciones externas dice por qué está vacío.
expect_text "/dashboard" "Denegados por el proveedor sin alerta local"
expect_text "/dashboard" "nadie pidió todavía la evaluación externa"

# ------------------------------------------------------------------- 1b. con evaluación externa
#
# El recorrido de la Etapa 6, sobre el mismo corpus: pedir la opinión del proveedor, entregar sus
# callbacks y comprobar que el veredicto y su procedencia aparecen en pantalla. Es lo único que
# ejercita el bloque externo con datos reales, y no puede hacerse antes porque hasta acá ninguna
# evaluación externa existe.

scenario "con evaluación externa"

echo "Solicitando la evaluación externa del corpus y entregando los callbacks…"
curl -sS -X POST --max-time 120 "${api_base}/api/demo-data/external-evaluations:request" >/dev/null \
  || fail "falló la solicitud de evaluaciones externas del corpus."
curl -sS -X POST --max-time 120 -H 'Content-Type: application/json' -d '{}' \
  "${api_base}/api/demo-data/external-callbacks:deliver" >/dev/null \
  || fail "falló la entrega de callbacks."

# La opinión del proveedor, con su procedencia. Y el vocabulario: ningún «pendiente» a secas, y los
# dos scores nunca puestos uno al lado del otro.
expect_text "/alerts/${alert_id}" "por el proveedor"
expect_text "/alerts/${alert_id}" "no se compara con el score local"
expect_no_text "/alerts/${alert_id}" "Solicitar evaluación externa"

# Y el panel del dashboard, que es la única pantalla donde aparece un pedido sin alerta. Los tres
# arquetipos de fraude que el motor no puede ver llevan referencias que el proveedor deniega, así
# que tienen que estar acá: `ORD_000275` es el fraude amigo y `ORD_000277` la cuenta tomada.
expect_text "/dashboard" "ORD_000275"
expect_text "/dashboard" "ORD_000277"

# El callback autenticado falla cerrado: esta API arranca sin secreto configurado, así que no entra
# ninguna petición, con cabecera o sin ella. Se comprueba contra la API y no contra la consola,
# porque la consola no envía callbacks nunca y no tiene el secreto.
check_callback_closed() {
  local label="$1" status
  shift

  checks_run=$((checks_run + 1))
  status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 30 -X POST \
    -H 'Content-Type: application/json' "$@" \
    -d '{"externalEvaluationId":"MOCK-SMOKE","status":"APPROVED"}' \
    "${api_base}/api/external-callbacks/EXTERNAL_MOCK" || true)"

  if [[ "$status" == "401" ]]; then
    printf '  ok     %-28s %s → 401\n' "callback cerrado" "$label"
  else
    checks_failed=$((checks_failed + 1))
    printf '  FALLA  %-28s %s → HTTP %s (se esperaba 401)\n' "callback cerrado" "$label" "${status:-sin respuesta}"
  fi
}

check_callback_closed "sin cabecera"
check_callback_closed "con un secreto cualquiera" -H 'X-Salvo-Callback-Secret: cualquier-cosa'

# ------------------------------------------------------------------------ 1c. con explicación
#
# El recorrido de la Etapa 7. Va después del externo por la misma razón por la que el externo va
# después del primero: el bloque tiene dos estados en pantalla —sin pedir y escrito— y comprobar el
# segundo destruye el primero.
#
# El texto lo compone la plantilla determinista de esta instalación, y la API verifica cada cifra y
# cada regla contra la evaluación antes de guardarlo. Que la petición devuelva un resumen y no un
# fallo es, por eso, una comprobación de la verificación y no solo del transporte.

scenario "con explicación"

echo "Pidiendo la explicación de la evaluación del snapshot…"
explanation_id="$(
  curl -sS -X POST --max-time 60 -H 'Content-Type: application/json' -d '{}' \
    "${api_base}/api/alerts/${alert_id}/explanation" \
    | node -e 'let raw="";process.stdin.on("data",c=>raw+=c).on("end",()=>{const body=JSON.parse(raw);process.stdout.write(body.explanation.status === "READY" ? body.explanation.id : "");})'
)"
[[ -n "$explanation_id" ]] || fail "la explicación no quedó escrita: la petición no devolvió una fila READY."
echo "Explicación de ejemplo: ${explanation_id}"

# La apertura de la plantilla, que es la única frase que escribe en todos los casos.
expect_text "/alerts/${alert_id}" "puntos sobre un umbral de"
# Quién la escribió, dicho en la pantalla: en esta instalación no hay modelo detrás.
expect_text "/alerts/${alert_id}" "no por un modelo"
expect_text "/alerts/${alert_id}" "Reglas citadas"
# Una explicación escrita no se regenera, así que el botón desaparece.
expect_no_text "/alerts/${alert_id}" "Explicar esta evaluación"
# D10: el formulario de revisión lleva el id de la explicación que está en pantalla.
expect_text "/alerts/${alert_id}" "value=\"${explanation_id}\""

# ------------------------------------------------------- 1d. explicación de otra plantilla
#
# Lo que la Etapa 7 dejó abierto y `E7D` cierra. Subir la versión de la plantilla es lo único que
# hace que un arreglo de redacción alcance a una evaluación ya explicada —la API no reescribe un
# texto escrito—, pero eso solo sirve si la consola ofrece pedirlo. Antes de `E7D` no lo ofrecía: la
# lectura devolvía la fila vieja, el bloque la veía escrita, y no había botón que pudiera pedir nada.
#
# Va sobre una alerta distinta de la del 1c, que ya tiene su explicación de la plantilla vigente. La
# fila se inserta en la base porque la plantilla anterior ya no existe: ningún proveedor de esta
# instalación puede volver a escribir `e7-v1`. Se escribe con `node:sqlite`, que viene con el Node
# que este repositorio fija, y sobre la base temporal del escenario — nunca sobre `salvo.db`.

scenario "explicación de otra plantilla"

older_alert_id="$(
  curl -sS --max-time 30 "${api_base}/api/alerts?status=OPEN&pageSize=2" \
    | node -e 'let raw="";process.stdin.on("data",c=>raw+=c).on("end",()=>{const second=JSON.parse(raw).items[1];process.stdout.write(second ? second.id : "");})'
)"
[[ -n "$older_alert_id" ]] || fail "hace falta una segunda alerta abierta para el escenario de plantilla anterior."
echo "Segunda alerta: ${older_alert_id}"

older_wording="El pedido obtuvo 100 puntos sobre un umbral de 60. Coincidieron 4 reglas."

curl -sS --max-time 30 "${api_base}/api/alerts/${older_alert_id}" \
  | SMOKE_DB="$db_with_data" SMOKE_ALERT="$older_alert_id" SMOKE_WORDING="$older_wording" node -e '
      let raw = "";
      process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
        const detail = JSON.parse(raw);
        const { DatabaseSync } = require("node:sqlite");
        const db = new DatabaseSync(process.env.SMOKE_DB);
        // Cada valor va ligado: en SQLite las comillas dobles son un identificador, no un texto.
        db.prepare(`
          INSERT INTO alert_explanations (
            id, risk_evaluation_id, provider, template_version, alert_policy_version,
            language, requested_from_alert_id, status, summary, referenced_rules_json,
            attempt_count, requested_at_utc, settled_at_utc, row_version)
          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, ?, ?, 1)
        `).run(
          crypto.randomUUID(),
          detail.snapshot.evaluationId,
          "MOCK",
          "e7-v1",
          detail.alertPolicyVersion,
          // El texto de abajo es castellano, así que la fila se marca como tal: es el idioma en el
          // que fue escrita, no un relleno.
          "es",
          process.env.SMOKE_ALERT,
          "READY",
          process.env.SMOKE_WORDING,
          JSON.stringify(detail.snapshot.signals.map((signal) => signal.rule)),
          "2026-09-05T00:00:00.000Z",
          "2026-09-05T00:00:01.000Z",
        );
        db.close();
      });
    ' || fail "no se pudo escribir la explicación de la plantilla anterior."

# El texto viejo sigue en pantalla: es el registro de lo que se pudo leer al decidir.
expect_text "/alerts/${older_alert_id}" "Coincidieron 4 reglas"
# Y la letra chica dice con qué plantilla se escribió, que es lo que vuelve legible la oferta.
expect_text "/alerts/${older_alert_id}" "plantilla determinista (e7-v1)"
# La oferta: redactar con la vigente, que escribe al lado y no reemplaza nada.
expect_text "/alerts/${older_alert_id}" "Redactar con la plantilla vigente"
# Sin alarma. Un cambio de redacción no invalida lo que el párrafo dice del pedido.
expect_no_text "/alerts/${older_alert_id}" "desactualizada"
# Y sin confundirlo con un fallo: no hay nada que reintentar.
expect_no_text "/alerts/${older_alert_id}" "Volver a intentar la explicación"
# La alerta del 1c, escrita por la plantilla vigente, no ofrece nada.
expect_no_text "/alerts/${alert_id}" "Redactar con la plantilla vigente"

# ------------------------------------------------------------------- 1e. el interruptor de idioma
#
# La consola en portugués, sobre la misma base y el mismo `next start`. Es lo que convierte «el
# idioma es conmutable» en una afirmación verificada de punta a punta: el diccionario puede estar
# completo y compilar, y la consola seguir sirviendo castellano porque el idioma nunca llegó a
# atravesar la API.
#
# No se duplican las 41 anclas del recorrido castellano. Se comprueba una pantalla de cada tipo —la
# raíz, la cola, el detalle, la importación y el panel—, el `<html lang>`, y sobre todo lo único
# que ningún diccionario puede dar: que la explicación se vuelve a escribir en el idioma nuevo y la
# castellana queda intacta.

scenario "el interruptor de idioma"

stop_api
start_api "$db_with_data" "pt"

# La API declara el idioma, y es de donde la consola lo toma.
checks_run=$((checks_run + 1))
if curl -sS --max-time 30 "${api_base}/api/system/capabilities" | grep -qF '"language":"pt"'; then
  printf '  ok     %-28s la API declara «pt»\n' "capabilities"
else
  checks_failed=$((checks_failed + 1))
  printf '  FALLA  %-28s la API no declara «pt»\n' "capabilities"
fi

# Una pantalla de cada tipo, en portugués.
expect_text "/" "Console antifraude"
expect_text "/alerts" "Fila de alertas"
expect_text "/import" "Importação e scoring"
expect_text "/dashboard" "Painel"
expect_text "/alerts/${alert_id}" "Snapshot que abriu o alerta"
expect_text "/alerts/${alert_id}" "Avaliação vigente"
# Y nada del castellano alrededor, que es el fallo que una traducción a medias produce.
expect_no_text "/alerts" "Cola de alertas"
expect_no_text "/dashboard" "Monto en riesgo"

# `<html lang>`. No es cosmético: sin él un lector de pantalla pronuncia el portugués con fonética
# castellana, que es donde el idioma del despliegue y la accesibilidad se tocan.
expect_text "/" '<html lang="pt"'

# La frase de una señal, compuesta desde los campos tipados en el idioma nuevo.
expect_text "/alerts/${anomaly_alert_id}" "vezes a mediana do estabelecimento"

# ---- La comprobación que vale más que todas las demás juntas ----
#
# La alerta del escenario 1c tiene su explicación escrita en castellano. Un despliegue en portugués
# tiene que ver que para él no hay ninguna, escribir la suya al lado, y dejar la castellana como
# estaba. Sin el idioma en la identidad de la fila, encuentra la castellana, la da por buena, y no
# escribe nunca la portuguesa: el defecto de `E7D` con otra columna en el mismo lugar.

# Antes de pedir nada: para este despliegue esta evaluación no está explicada.
expect_text "/alerts/${alert_id}" "Ainda não foi pedida uma explicação"
expect_text "/alerts/${alert_id}" "Explicar esta avaliação"
expect_no_text "/alerts/${alert_id}" "puntos sobre un umbral de"

echo "Pidiendo la explicación de la misma evaluación, ahora en portugués…"
portuguese_explanation_id="$(
  curl -sS -X POST --max-time 60 -H 'Content-Type: application/json' -d '{}' \
    "${api_base}/api/alerts/${alert_id}/explanation" \
    | node -e 'let raw="";process.stdin.on("data",c=>raw+=c).on("end",()=>{const body=JSON.parse(raw);process.stdout.write(body.applied && body.explanation.status === "READY" ? body.explanation.id : "");})'
)"
[[ -n "$portuguese_explanation_id" ]] \
  || fail "el despliegue en portugués no escribió una explicación nueva: o devolvió la castellana, o falló."
[[ "$portuguese_explanation_id" != "$explanation_id" ]] \
  || fail "el despliegue en portugués devolvió la misma fila que el castellano: el idioma no está en la identidad."
echo "Explicación portuguesa: ${portuguese_explanation_id}"

expect_text "/alerts/${alert_id}" "O pedido obteve"
expect_text "/alerts/${alert_id}" "não por um modelo de linguagem"

# Y las dos filas conviven en la base, con su idioma y su texto. Se lee con `node:sqlite`, sobre la
# base temporal de este script.
checks_run=$((checks_run + 1))
rows="$(
  SMOKE_DB="$db_with_data" SMOKE_ES="$explanation_id" SMOKE_PT="$portuguese_explanation_id" node -e '
    const { DatabaseSync } = require("node:sqlite");
    const db = new DatabaseSync(process.env.SMOKE_DB, { readOnly: true });
    const read = (id) => db.prepare(
      "SELECT language, substr(summary, 1, 20) AS opening FROM alert_explanations WHERE id = ?",
    ).get(id);
    const spanish = read(process.env.SMOKE_ES);
    const portuguese = read(process.env.SMOKE_PT);
    db.close();
    const ok =
      spanish?.language === "es"
      && portuguese?.language === "pt"
      && spanish.opening.startsWith("El pedido obtuvo")
      && portuguese.opening.startsWith("O pedido obteve");
    process.stdout.write(ok ? "ok" : `es=${JSON.stringify(spanish)} pt=${JSON.stringify(portuguese)}`);
  ' || true
)"
if [[ "$rows" == "ok" ]]; then
  printf '  ok     %-28s dos filas, «es» intacta y «pt» nueva\n' "identidad por idioma"
else
  checks_failed=$((checks_failed + 1))
  printf '  FALLA  %-28s %s\n' "identidad por idioma" "$rows"
fi

# ------------------------------------------------------------- 1f. un idioma que este build no habla
#
# Falla al arrancar, con el molde de `AI_PROVIDER`. Caer al castellano en silencio dejaría a un
# despliegue que quiso portugués sirviendo castellano sin que nada lo reporte — y escribiendo filas
# marcadas `es` que el despliegue corregido no puede reutilizar.

scenario "un idioma que este build no habla"

stop_api

checks_run=$((checks_run + 1))
unknown_language_log="${work_dir}/api-idioma-desconocido.log"
# En un subshell y con su stderr apagado: el proceso aborta a propósito, y bash anunciaría el
# «Abort trap» en medio de un informe verde como si algo hubiera salido mal.
if (
  env "ASPNETCORE_URLS=${api_base}" \
      "ConnectionStrings__SalvoDb=Data Source=${db_with_data}" \
      "SALVO_LANGUAGE=fr" \
    dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll >"$unknown_language_log" 2>&1
) 2>/dev/null
then
  checks_failed=$((checks_failed + 1))
  printf '  FALLA  %-28s la API arrancó con SALVO_LANGUAGE=fr\n' "arranque"
elif grep -qF "SALVO_LANGUAGE='fr'" "$unknown_language_log" \
  && grep -qF "es, pt" "$unknown_language_log"; then
  printf '  ok     %-28s no arranca, y dice cuáles valen\n' "SALVO_LANGUAGE=fr"
else
  checks_failed=$((checks_failed + 1))
  printf '  FALLA  %-28s no arrancó, pero sin nombrar el valor ni los válidos\n' "SALVO_LANGUAGE=fr"
fi
api_pid=""
wait_until_port_free "$api_port"

# ---------------------------------------------------------------------------- 2. base vacía
#
# Vuelve al castellano, que es lo que un despliegue sin `SALVO_LANGUAGE` sirve.

scenario "base vacía"

start_api "$db_empty"

expect_text "/import" "todavía no hay ninguno en la base"
expect_text "/alerts" "Todavía no hay pedidos"
expect_text "/dashboard" "Todavía no hay pedidos"
# Un identificador con forma válida y sin fila detrás: el 404 de la API tiene su propio texto.
expect_text "/alerts/00000000-0000-4000-8000-000000000000" "Esta alerta ya no existe"

# ---------------------------------------------------------------------------- 3. API apagada

scenario "API apagada"

stop_api

# Sin capacidades legibles no hay idioma que leer, y la consola se compone en castellano: el idioma
# es una preocupación de presentación y un fallo ahí no puede dejar la página en blanco.
expect_text "/" "Consola antifraude"
expect_text "/" '<html lang="es"'
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
