#!/usr/bin/env bash

# Salvo — la corrida real: un modelo de Anthropic redacta la explicación de cada alerta del corpus.
#
# Es el script de `E11C`, y lo corre el coordinador con su clave. **Ningún test, ni la compuerta, ni
# el smoke, ni la integración continua lo corren**, y ningún agente lo corre contra Anthropic: todo
# lo demás del proyecto se prueba contra un transporte simulado.
#
# Lo que hace: levanta la API sobre una base nueva y sembrada, como `demo.sh`, con
# `AI_PROVIDER=anthropic`; pide la explicación de cada alerta; reintenta las que fallaron dentro del
# tope de tres intentos de la fila, y nunca más; y escribe en `docs/explicaciones-modelo/AAAA-MM-DD/`
# lo que la decisión de publicación de la etapa permite (D16) y nada más:
#
#   - los textos aceptados, con el modelo que respondió, la versión del prompt y la fecha;
#   - de cada rechazo, el código y `FailureDetail` —el token ofensor o la clasificación del
#     proveedor—, **nunca el texto**, que no se guarda en ningún lado;
#   - los tokens de cada fila, leídos de la base con `node:sqlite`, acumulados entre intentos;
#   - el costo, calculado con los precios que imprime **junto a la fecha en que se leyeron**.
#
# **Es el único script del repositorio que carga un archivo de entorno**, y no lo hace sin que se lo
# pidan: el archivo va en `SALVO_ENV_FILE` y no tiene valor por omisión. Correrlo sin argumentos se
# niega, así que nadie —tampoco un agente— carga `.env` por accidente. Del archivo lee dos valores,
# `ANTHROPIC_API_KEY` y `ANTHROPIC_MODEL`, dentro de un proceso aparte y sin heredar el entorno de la
# terminal; no exporta ninguno a este script, y la clave solo llega al entorno del proceso de la API.
# **Nunca imprime el valor de la clave**: un error nombra la variable, no su contenido.
#
#   SALVO_ENV_FILE=.env ./scripts/explicar-con-anthropic.sh
#
# Variables: SALVO_ENV_FILE (obligatoria), EXPLICAR_API_PORT (5399),
# EXPLICAR_TIMEOUT_SECONDS (120). Con un modelo que no sea `claude-sonnet-5-5`, también
# EXPLICAR_PRECIO_ENTRADA, EXPLICAR_PRECIO_SALIDA (US$ por millón de tokens) y
# EXPLICAR_PRECIO_LEIDO (la fecha en que se leyeron): un costo sin fecha es lo que la decisión 69
# prohíbe, y un precio de otro modelo sería un número falso.
#
# Reglas que este script se impone, las mismas que `demo.sh`:
#
#   - No borra nada. Estrena una base con fecha y hora, y se niega a escribir sobre una carpeta de
#     salida que ya tenga archivos.
#   - Se niega a arrancar si el puerto ya está ocupado, antes que matar el proceso de otro.
#   - Libera la API al salir, también si falla y también si lo interrumpen.

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

# El proveedor queda fijo en `mock` para todo lo que este script lance salvo la API de la corrida:
# la migración de la base construye el host de la API y, sin esta línea, heredaría el proveedor de
# la terminal. Solo el proceso de la API recibe `anthropic`, y lo recibe por su cuenta, más abajo.
export AI_PROVIDER=mock
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# ---------------------------------------------------------------------------- el archivo de entorno

env_file="${SALVO_ENV_FILE:-}"
[[ -n "$env_file" ]] \
  || fail "falta SALVO_ENV_FILE: el archivo de entorno con la clave no tiene valor por omisión, a propósito. Uso: SALVO_ENV_FILE=.env ./scripts/explicar-con-anthropic.sh"
[[ -f "$env_file" && -r "$env_file" ]] \
  || fail "SALVO_ENV_FILE no nombra un archivo legible."

# Un valor del archivo, leído en un proceso aparte con el entorno vacío: lo que la terminal tenga
# exportado no cuenta, y nada de lo que el archivo declare se exporta a este script.
read_from_env_file() {
  env -i /bin/bash -c 'set -a; . "$1"; set +a; printf "%s" "${!2:-}"' _ "$env_file" "$1"
}

api_key="$(read_from_env_file ANTHROPIC_API_KEY)" || fail "no se pudo leer SALVO_ENV_FILE."
model="$(read_from_env_file ANTHROPIC_MODEL)" || fail "no se pudo leer SALVO_ENV_FILE."

[[ -n "$api_key" ]] || fail "falta ANTHROPIC_API_KEY en el archivo de SALVO_ENV_FILE."
[[ -n "$model" ]] || fail "falta ANTHROPIC_MODEL en el archivo de SALVO_ENV_FILE."

# ---------------------------------------------------------------------------- los precios

# Leídos en https://platform.claude.com/docs/en/about-claude/pricing el 2026-10-01, para
# `claude-sonnet-5-5`: US$ 2 por millón de tokens de entrada y US$ 10 por millón de salida. El
# razonamiento, si lo hubiera, se cobra como salida; el adaptador lo apaga por adelantado.
if [[ "$model" == "claude-sonnet-5-5" ]]; then
  price_input="${EXPLICAR_PRECIO_ENTRADA:-2}"
  price_output="${EXPLICAR_PRECIO_SALIDA:-10}"
  price_read_on="${EXPLICAR_PRECIO_LEIDO:-2026-10-01}"
else
  price_input="${EXPLICAR_PRECIO_ENTRADA:-}"
  price_output="${EXPLICAR_PRECIO_SALIDA:-}"
  price_read_on="${EXPLICAR_PRECIO_LEIDO:-}"
  [[ -n "$price_input" && -n "$price_output" && -n "$price_read_on" ]] \
    || fail "el modelo de ANTHROPIC_MODEL no es claude-sonnet-5-5, cuyos precios trae este script. Declará EXPLICAR_PRECIO_ENTRADA, EXPLICAR_PRECIO_SALIDA y EXPLICAR_PRECIO_LEIDO."
fi

# ---------------------------------------------------------------------------- la corrida

api_port="${EXPLICAR_API_PORT:-5399}"
timeout_seconds="${EXPLICAR_TIMEOUT_SECONDS:-120}"
api_base="http://127.0.0.1:${api_port}"
run_date="$(date +%Y-%m-%d)"
output_dir="${repository_root}/docs/explicaciones-modelo/${run_date}"
# Absoluta, por el mismo motivo que en `demo.sh`: `dotnet ef database update --connection` resuelve
# una ruta relativa contra el proyecto de arranque.
database="${repository_root}/backend/src/Salvo.Api/salvo-anthropic-$(date +%Y%m%d-%H%M%S).db"
log_dir="$(mktemp -d "${TMPDIR:-/tmp}/salvo-explicar-XXXXXX")"
api_log="${log_dir}/api.log"
api_pid=""

cleanup() {
  local status=$?
  set +e

  if [[ -n "$api_pid" ]] && kill -0 "$api_pid" 2>/dev/null; then
    kill "$api_pid" 2>/dev/null
    for _ in $(seq 1 50); do
      kill -0 "$api_pid" 2>/dev/null || break
      sleep 0.2
    done
    kill -0 "$api_pid" 2>/dev/null && kill -9 "$api_pid" 2>/dev/null
  fi

  echo
  echo "La base de la corrida queda donde está: ${database}"
  echo "Registro de la API: ${api_log}"

  exit "$status"
}
trap cleanup EXIT INT TERM

command -v curl >/dev/null || fail "hace falta curl."
command -v lsof >/dev/null || fail "hace falta lsof para comprobar el puerto."
command -v node >/dev/null || fail "hace falta node."

if lsof -nP -iTCP:"$api_port" -sTCP:LISTEN >/dev/null 2>&1; then
  fail "el puerto ${api_port} ya está ocupado. Elegí otro con EXPLICAR_API_PORT."
fi

if [[ -d "$output_dir" ]] && [[ -n "$(ls -A "$output_dir")" ]]; then
  fail "${output_dir} ya tiene archivos, y este script no sobrescribe nada."
fi

echo "Salvo — explicaciones redactadas por ${model}"
echo "Precios: US\$ ${price_input} / ${price_output} por millón de tokens de entrada / salida, leídos el ${price_read_on}."
echo

echo "Compilando la API…"
dotnet build Salvo.slnx --configuration Release >>"$api_log" 2>&1 \
  || fail "no compila la solución. Registro: ${api_log}"
dotnet tool restore >>"$api_log" 2>&1 || fail "no se pudo restaurar el tooling de EF."

echo "Creando y migrando la base: ${database}"
dotnet ef database update \
  --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
  --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
  --configuration Release \
  --no-build \
  --connection "Data Source=${database}" >>"$api_log" 2>&1 \
  || fail "falló la migración. Registro: ${api_log}"

echo "Levantando la API con AI_PROVIDER=anthropic…"
# La clave entra al entorno de este subproceso y de ningún otro, y nunca a una línea de comando, que
# cualquiera con `ps` podría leer.
(
  export ANTHROPIC_API_KEY="$api_key"
  export ANTHROPIC_MODEL="$model"
  export AI_PROVIDER=anthropic
  export ASPNETCORE_URLS="$api_base"
  export ConnectionStrings__SalvoDb="Data Source=${database}"
  export DemoData__Enabled=true
  exec dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll
) >>"$api_log" 2>&1 &
api_pid=$!
unset api_key

deadline=$((SECONDS + timeout_seconds))
until curl -sS -o /dev/null --max-time 5 "${api_base}/health" 2>/dev/null; do
  kill -0 "$api_pid" 2>/dev/null || fail "la API murió durante el arranque. Registro: ${api_log}"
  ((SECONDS < deadline)) || fail "la API no respondió en ${timeout_seconds} s. Registro: ${api_log}"
  sleep 0.25
done

mkdir -p "$output_dir"

SALVO_API="$api_base" \
SALVO_DB="$database" \
SALVO_OUT="$output_dir" \
SALVO_DATE="$run_date" \
SALVO_PRICE_INPUT="$price_input" \
SALVO_PRICE_OUTPUT="$price_output" \
SALVO_PRICE_READ_ON="$price_read_on" \
  node --no-warnings --input-type=module -e '
    import { writeFile } from "node:fs/promises";
    import { DatabaseSync } from "node:sqlite";

    const api = process.env.SALVO_API;
    const call = async (method, path, body) => {
      const response = await fetch(new URL(path, api), {
        method,
        headers: body === undefined ? {} : { "content-type": "application/json" },
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: AbortSignal.timeout(180_000),
      });
      const text = await response.text();
      return { status: response.status, body: text.length === 0 ? null : JSON.parse(text) };
    };

    const seed = await call("POST", "/api/demo-data/seed");
    if (seed.status !== 200) throw new Error(`La carga del corpus respondió ${seed.status}.`);
    const run = await call("POST", "/api/risk-evaluations:run");
    if (run.status !== 200) throw new Error(`La corrida de scoring respondió ${run.status}.`);

    const alerts = (await call("GET", "/api/alerts?pageSize=200")).body.items;
    console.log(`Alertas: ${alerts.length}. Pidiendo una explicación por alerta, con hasta tres intentos cada una.`);

    const results = [];
    let calls = 0;
    for (const alert of alerts) {
      // Una petición por intento; los reintentos son los de la fila, y su tope lo pone la API.
      let answer = await call("POST", `/api/alerts/${alert.id}/explanation`, { regenerate: false });
      calls += 1;
      while (
        answer.status === 200
        && answer.body.explanation.status === "FAILED"
        && !answer.body.explanation.attemptsExhausted
      ) {
        answer = await call("POST", `/api/alerts/${alert.id}/explanation`, { regenerate: true });
        calls += 1;
      }

      const explanation = answer.status === 200 ? answer.body.explanation : null;
      results.push({ reference: alert.merchantReferenceId, httpStatus: answer.status, explanationId: explanation?.id ?? null });
      console.log(`  ${alert.merchantReferenceId}: ${explanation === null ? `HTTP ${answer.status}` : `${explanation.status}${explanation.failureCode === null ? "" : ` ${explanation.failureCode}`} en ${explanation.attemptCount} intento(s)`}`);
    }

    // Los tokens, de la base: es lo que la fila acumuló entre intentos, rechazos incluidos.
    const db = new DatabaseSync(process.env.SALVO_DB, { readOnly: true });
    const read = db.prepare(`
      SELECT provider, template_version, provider_version, status, summary, referenced_rules_json,
             failure_code, failure_detail, attempt_count, input_tokens, output_tokens, settled_at_utc
      FROM alert_explanations WHERE id = ?`);

    const priceInput = Number(process.env.SALVO_PRICE_INPUT);
    const priceOutput = Number(process.env.SALVO_PRICE_OUTPUT);
    const costOf = (input, output) => ((input ?? 0) * priceInput + (output ?? 0) * priceOutput) / 1_000_000;

    const rows = results.map((result) => {
      const row = result.explanationId === null ? null : read.get(result.explanationId);
      if (row === undefined || row === null) {
        return { reference: result.reference, httpStatus: result.httpStatus };
      }
      const ready = row.status === "READY";
      return {
        reference: result.reference,
        status: row.status,
        provider: row.provider,
        promptVersion: row.template_version,
        model: row.provider_version,
        settledAt: row.settled_at_utc,
        // D16: el texto solo si se aceptó. De un rechazo, el código y el detalle, nunca la frase.
        summary: ready ? row.summary : undefined,
        referencedRules: ready ? JSON.parse(row.referenced_rules_json) : undefined,
        failureCode: ready ? undefined : row.failure_code,
        failureDetail: ready ? undefined : row.failure_detail,
        attempts: row.attempt_count,
        inputTokens: row.input_tokens,
        outputTokens: row.output_tokens,
        costUsd: Number(costOf(row.input_tokens, row.output_tokens).toFixed(6)),
      };
    });
    db.close();

    const sum = (field) => rows.reduce((total, row) => total + (row[field] ?? 0), 0);
    const summary = {
      date: process.env.SALVO_DATE,
      prices: { inputUsdPerMillion: priceInput, outputUsdPerMillion: priceOutput, readOn: process.env.SALVO_PRICE_READ_ON },
      alerts: rows.length,
      calls,
      ready: rows.filter((row) => row.status === "READY").length,
      failed: rows.filter((row) => row.status === "FAILED").length,
      inputTokens: sum("inputTokens"),
      outputTokens: sum("outputTokens"),
      costUsd: Number(costOf(sum("inputTokens"), sum("outputTokens")).toFixed(6)),
      note: "Es una tirada, no un dorado: un modelo no es determinista, y nada se compara contra esto en un test.",
    };

    const out = process.env.SALVO_OUT;
    await writeFile(`${out}/explicaciones.json`, `${JSON.stringify({ summary, rows }, null, 2)}\n`, { flag: "wx" });

    const lines = [
      `# Explicaciones redactadas por un modelo — ${summary.date}`,
      "",
      `Una tirada sobre el corpus de demostración, con \`AI_PROVIDER=anthropic\`. ${summary.ready} de ${summary.alerts} alertas con texto aceptado, ${summary.failed} rechazadas, en ${summary.calls} llamadas.`,
      "",
      `Tokens: ${summary.inputTokens} de entrada y ${summary.outputTokens} de salida. Costo: US$ ${summary.costUsd.toFixed(4)}, con US$ ${priceInput} / ${priceOutput} por millón de tokens de entrada / salida, precios leídos el ${summary.prices.readOn}.`,
      "",
      "De un texto rechazado se publica el código y el detalle —el token ofensor o lo que dijo el proveedor—, nunca el texto: no se guarda en ningún lado.",
      "",
    ];
    for (const row of rows) {
      lines.push(`## ${row.reference}`, "");
      if (row.status === undefined) {
        lines.push(`Sin explicación: HTTP ${row.httpStatus}.`, "");
        continue;
      }
      lines.push(`- Estado: ${row.status}, en ${row.attempts} intento(s). Modelo: ${row.model ?? "sin dato"}. Prompt: ${row.promptVersion}.`);
      lines.push(`- Tokens: ${row.inputTokens ?? "sin dato"} / ${row.outputTokens ?? "sin dato"}. Costo: US$ ${row.costUsd.toFixed(4)}.`);
      if (row.status === "READY") {
        lines.push(`- Reglas citadas: ${row.referencedRules.join(", ")}.`, "", `> ${row.summary}`, "");
      } else {
        lines.push(`- Código: ${row.failureCode ?? "sin código"}. Detalle: ${row.failureDetail ?? "sin detalle"}.`, "");
      }
    }
    await writeFile(`${out}/explicaciones.md`, lines.join("\n"), { flag: "wx" });

    console.log("");
    console.log(`Aceptadas: ${summary.ready}. Rechazadas: ${summary.failed}. Llamadas: ${summary.calls}.`);
    console.log(`Costo: US$ ${summary.costUsd.toFixed(4)} (precios leídos el ${summary.prices.readOn}).`);
    console.log(`Escrito en ${out}`);
  ' || fail "la corrida no terminó. Registro de la API: ${api_log}"
