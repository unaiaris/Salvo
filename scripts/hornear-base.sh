#!/usr/bin/env bash

# Salvo — deja la base de la instancia pública lista **durante la construcción de la imagen**.
#
# `E10A` midió lo que cuesta hacer esto al arrancar, a 0,1 vCPU: sembrar los 300 pedidos son 24,68 s
# y puntuarlos 17,50 s, encima de los 77,5 s que ya tarda el arranque. El primer visitante después
# de un silencio esperaría cerca de dos minutos frente a una consola vacía. Se hace acá, una vez,
# con la CPU de quien construye, y el arranque solo copia el archivo a su lugar de trabajo.
#
# **Qué deja montado, y por qué cada cosa.** No alcanza con el corpus:
#
#   1. Los 300 pedidos y la corrida de scoring, que son las 23 alertas de la cola.
#   2. Las evaluaciones externas pedidas y sus callbacks entregados. Sin esto el panel de denegados
#      por el proveedor sin alerta local aparece vacío, y es la superficie que sostiene el argumento
#      de la Etapa 9: el fraude que las reglas no vieron.
#   3. Una explicación escrita, sobre la alerta de `ORD_000011`. Para que un visitante lea una sin
#      tener que pedirla y esperar a que se redacte.
#
# Los pasos 2 y 3 son los de la receta que la fase 1 de `E9C2` dejó en su handoff para el recorrido
# con lector de pantalla. **Su primer paso no se reutiliza**: importaba tres pedidos más para
# fabricar una divergencia entre el snapshot y la evaluación vigente, y una instancia pública con
# 303 pedidos contradiría al README, que publica 300 pedidos, 28 fraudes y 23 alertas de una corrida
# fechada. Lo que se pierde con eso queda dicho en el brief de `E10B`.
#
# **Reproducible en lo que se observa, no byte a byte.** El corpus, el scoring, el proveedor mock y
# la plantilla de explicación son todos deterministas, así que dos construcciones producen la misma
# consola. Los identificadores y los instantes de creación no: son un GUID y un reloj. Por eso lo
# que este script afirma son las cantidades, no un hash del archivo.
#
# Se ejecuta dentro del `Dockerfile`, en una etapa que tiene la API publicada y Node. Node hace de
# cliente HTTP y de lector de JSON por el mismo motivo que en `contenedor-entrypoint.sh`: es lo que
# la imagen garantiza tener.
#
# Uso:  hornear-base.sh <ruta-de-la-base> <directorio-de-la-api>

set -euo pipefail

db_path="${1:?falta la ruta de la base a hornear}"
api_dir="${2:?falta el directorio de la API publicada}"

api_url="http://127.0.0.1:5199"
startup_timeout="${SALVO_BAKE_STARTUP_TIMEOUT_SECONDS:-300}"

# Lo que la base tiene que contener al terminar. Las tres primeras son las cifras que el README
# publica en su bloque de corpus; si una construcción produce otra cosa, la instancia pública
# mostraría números que contradicen al documento que el visitante acaba de leer.
expected_orders=300
expected_fraud_labels=28
expected_alerts=23

log() { printf '[hornear] %s\n' "$*"; }
fail() { printf '[hornear] ERROR: %s\n' "$*" >&2; exit 1; }

# ------------------------------------------------------------------ la API, en segundo plano

mkdir -p "$(dirname "$db_path")"

log "levantando la API sobre ${db_path}"
ConnectionStrings__SalvoDb="Data Source=${db_path}" \
ASPNETCORE_URLS="$api_url" \
Database__MigrateOnStartup=true \
DemoData__Enabled=true \
DemoData__SeedEnabled=true \
SALVO_LANGUAGE="${SALVO_LANGUAGE:-es}" \
Logging__LogLevel__Default=Warning \
Logging__LogLevel__Microsoft=Warning \
DOTNET_NOLOGO=1 \
  dotnet "${api_dir}/Salvo.Api.dll" &
api_pid=$!

detener_api() {
  if [[ -n "${api_pid:-}" ]] && kill -0 "$api_pid" 2>/dev/null; then
    # SIGTERM y espera, nunca SIGKILL: un apagado limpio es lo que cierra el archivo de SQLite y
    # deja sus diarios consolidados. Matarlo a la fuerza es la manera de hornear una base que
    # arranca pidiendo recuperación.
    kill -TERM "$api_pid" 2>/dev/null || true
    wait "$api_pid" 2>/dev/null || true
  fi
}
trap detener_api EXIT

# `node` en lugar de `curl` por lo mismo que el punto de entrada: la imagen de runtime no trae
# `curl`, y esta etapa comparte el script con ella.
peticion() {
  local method="$1" path="$2" body="${3:-}"
  node -e '
    const [method, path, body] = process.argv.slice(1);
    const init = { method };
    if (body) {
      init.body = body;
      init.headers = { "content-type": "application/json" };
    }
    fetch(process.env.SALVO_BAKE_API + path, init)
      .then(async (response) => {
        const text = await response.text();
        if (!response.ok) {
          process.stderr.write(`${method} ${path} -> ${response.status}: ${text.slice(0, 400)}\n`);
          process.exit(1);
        }
        process.stdout.write(text);
      })
      .catch((error) => { process.stderr.write(String(error) + "\n"); process.exit(1); });
  ' "$method" "$path" "$body"
}

# Un dato de una respuesta JSON. El argumento es el **cuerpo** de una función que recibe `body` y
# devuelve: sentencias incluidas, no solo una expresión. La primera versión interpolaba el argumento
# dentro de un `return (...)` y una búsqueda con `const` intermedio no compilaba — el horneado moría
# sin decir por qué, porque el error de Node se perdía entre los registros de la API.
campo() {
  node -e '
    let raw = "";
    process.stdin.on("data", (chunk) => (raw += chunk)).on("end", () => {
      try {
        const body = JSON.parse(raw);
        const value = new Function("body", process.argv[1])(body);
        process.stdout.write(value === undefined || value === null ? "" : String(value));
      } catch (error) {
        process.stderr.write(`[hornear] no se pudo leer la respuesta: ${error}\n`);
        process.exit(1);
      }
    });
  ' "$1"
}

esperar_api() {
  local deadline=$((SECONDS + startup_timeout))

  while ((SECONDS < deadline)); do
    if ! kill -0 "$api_pid" 2>/dev/null; then
      fail "la API murió durante el arranque del horneado."
    fi
    if SALVO_BAKE_API="$api_url" peticion GET /health >/dev/null 2>&1; then
      return 0
    fi
    sleep 0.25
  done

  fail "la API no respondió en ${startup_timeout} s."
}

export SALVO_BAKE_API="$api_url"

esperar_api
log "la API responde"

# ------------------------------------------------------------------ 1. el corpus y su corrida

log "sembrando el corpus"
seed="$(peticion POST /api/demo-data/seed)" || fail "falló el sembrado."
seeded_orders="$(printf '%s' "$seed" | campo 'return body.totalOrders;')"
seeded_frauds="$(printf '%s' "$seed" | campo 'return body.fraudLabelCount;')"

[[ "$seeded_orders" == "$expected_orders" ]] \
  || fail "el sembrado dejó ${seeded_orders} pedidos y se esperaban ${expected_orders}."
[[ "$seeded_frauds" == "$expected_fraud_labels" ]] \
  || fail "el sembrado dejó ${seeded_frauds} etiquetas de fraude y se esperaban ${expected_fraud_labels}."

log "corriendo el scoring"
peticion POST /api/risk-evaluations:run >/dev/null || fail "falló la corrida de scoring."

alerts="$(peticion GET '/api/alerts?status=OPEN&pageSize=1')" || fail "no se pudo leer la cola."
open_alerts="$(printf '%s' "$alerts" | campo 'return body.totalCount;')"
[[ "$open_alerts" == "$expected_alerts" ]] \
  || fail "la corrida dejó ${open_alerts} alertas abiertas y se esperaban ${expected_alerts}."

# ------------------------------------------------------------------ 2. la segunda opinión

log "pidiendo las evaluaciones externas del corpus"
peticion POST /api/demo-data/external-evaluations:request >/dev/null \
  || fail "falló el pedido de evaluaciones externas."

log "entregando los callbacks pendientes"
peticion POST /api/demo-data/external-callbacks:deliver >/dev/null \
  || fail "falló la entrega de callbacks."

dashboard="$(peticion GET /api/dashboard)" || fail "no se pudo leer el dashboard."
denials="$(printf '%s' "$dashboard" | campo 'return body.externalDenialsWithoutAlert.total;')"
[[ -n "$denials" && "$denials" -gt 0 ]] \
  || fail "el panel de denegados sin alerta local quedó vacío: la instancia no tendría qué mostrar ahí."
log "denegados por el proveedor sin alerta local: ${denials}"

# ------------------------------------------------------------------ 3. una explicación escrita

# `ORD_000011` y no la primera alerta de la cola: es el pedido que la fixture pone a propósito para
# `amount_anomaly`, el mismo sobre el que `ExplanationGoldenTests` fija el texto, y el que el guion
# de demostración recorre. El desempate de la cola es un identificador aleatorio.
explanation_alert="$(
  peticion GET '/api/alerts?status=OPEN&pageSize=100' \
    | campo 'const hit = body.items.find((i) => i.merchantReferenceId === "ORD_000011"); return hit ? hit.id : "";'
)"
[[ -n "$explanation_alert" ]] \
  || fail "ORD_000011 no tiene alerta abierta: la fixture cambió y el horneado quedó sin sujeto."

log "redactando la explicación de ORD_000011"
explanation="$(peticion POST "/api/alerts/${explanation_alert}/explanation" '{}')" \
  || fail "falló el pedido de explicación."
status="$(printf '%s' "$explanation" | campo 'return body.explanation.status;')"
[[ "$status" == "READY" ]] \
  || fail "la explicación quedó en '${status}' y se esperaba READY."

# ------------------------------------------------------------------ el cierre

log "apagando la API para que SQLite cierre el archivo"
detener_api
api_pid=""

# Un `-wal` o un `-shm` sobrevivientes significan que el archivo quedó a medio consolidar, y la
# imagen llevaría una base que arranca recuperando. No se borra ninguno: se falla, porque su
# presencia dice que el apagado no fue el que este script cree haber hecho.
for sufijo in -wal -shm -journal; do
  [[ -e "${db_path}${sufijo}" ]] \
    && fail "quedó ${db_path}${sufijo}: la base no se cerró limpiamente."
done

log "base horneada en ${db_path} ($(wc -c < "$db_path") bytes)"
log "  ${expected_orders} pedidos · ${expected_alerts} alertas abiertas · ${denials} denegados sin alerta · 1 explicación"
