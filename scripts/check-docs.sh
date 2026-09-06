#!/usr/bin/env bash

# Salvo — verificación ejecutable de lo que el README afirma sobre este repositorio.
#
# Un documento que describe un repositorio se desactualiza en silencio. Una ruta se renombra, un
# test se reescribe con otro nombre, y el README sigue diciendo lo mismo con la misma seguridad. Ese
# es el modo de fallo que este proyecto ya vio tres veces en dos días, y la respuesta es la de
# `OpenApiDriftTests`: la deriva se detecta, no se promete.
#
# Comprueba dos clases de afirmación, las dos únicas que se pueden comprobar mecánicamente:
#
#   1. Cada ruta que el README cita entre acentos graves existe en el árbol de trabajo.
#   2. Cada destino de un enlace Markdown relativo existe. El mapa de documentación es justamente
#      la parte de un README que se pudre sin que nadie la lea.
#   3. Cada test que el README nombra existe en `backend/tests` o en `frontend/src`.
#
# Lo que NO comprueba, y conviene tener presente al leer un resultado verde: que una afirmación de
# comportamiento sea cierta. Para eso está la columna «test que lo afirma» de la tabla de
# verificación del handoff, y el test en sí.
#
# Reglas que este script se impone:
#
#   - No escribe nada, no toca la red y no necesita ningún proceso levantado. Corre en la compuerta.
#   - Busca solo en fuentes: `bin/` y `obj/` quedan fuera. No es cosmética. Con los artefactos de
#     compilación adentro, la búsqueda de veinte nombres de test pasa de menos de un segundo a más
#     de un minuto, y un paso así no entra en una compuerta.
#   - Falla nombrando exactamente lo que no encontró, con la línea del README donde aparece.
#   - Bash para el recorrido y el informe; Node solo para extraer los tokens, porque hay que saltear
#     los bloques de código cercados y eso con `grep` es adivinanza.
#
#   ./scripts/check-docs.sh

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

document="${1:-README.md}"

[[ -f "$document" ]] || { echo "ERROR: no existe el documento «${document}»." >&2; exit 1; }
command -v node >/dev/null || { echo "ERROR: hace falta node." >&2; exit 1; }

checks_run=0
checks_failed=0

report() {
  local status="$1" kind="$2" token="$3" line="$4" detail="${5:-}"

  checks_run=$((checks_run + 1))
  if [[ "$status" == "ok" ]]; then
    printf '  ok     %-8s %s\n' "$kind" "$token"
  else
    checks_failed=$((checks_failed + 1))
    printf '  FALLA  %-8s %s\n' "$kind" "$token"
    printf '         %s:%s — %s\n' "$document" "$line" "$detail"
  fi
}

echo "Salvo — verificación de ${document}"
echo

# ---------------------------------------------------------------------------- extracción
#
# Un token entre acentos graves es una ruta si tiene una barra y no es otra cosa: una ruta HTTP
# (empieza con `/`), una URL, una asignación de variable de entorno, o una frase con espacios.
# Es un nombre de test si termina en `Tests`, si es `Clase.Metodo` con la clase terminada en
# `Tests`, o si es un archivo `.test.ts`/`.test.tsx`. Un enlace Markdown aporta su destino cuando
# es relativo; un ancla o una URL no dicen nada sobre este árbol de trabajo.

extract() {
  node - "$document" "$1" <<'NODEEOF'
const { readFileSync } = require("node:fs");

const [documentPath, wanted] = process.argv.slice(2);
const lines = readFileSync(documentPath, "utf8").split("\n");

const isRoute = (token) =>
  token.includes("/")
  && !token.startsWith("/")
  && !/\s/.test(token)
  && !token.includes("=")
  && !/^[a-z]+:\/\//.test(token)
  && /^[A-Za-z0-9_.@-]+(\/[A-Za-z0-9_.@-]*)+$/.test(token);

const isTest = (token) =>
  /^[A-Za-z0-9_]+Tests(\.[A-Za-z0-9_]+)?$/.test(token) || /^[A-Za-z0-9_.-]+\.test\.tsx?$/.test(token);

const seen = new Set();
let fenced = false;

lines.forEach((line, index) => {
  if (line.trimStart().startsWith("```")) {
    fenced = !fenced;
    return;
  }
  if (fenced) {
    return;
  }

  const found = [];

  for (const match of line.matchAll(/`([^`]+)`/g)) {
    const token = match[1];
    found.push([isRoute(token) ? "route" : isTest(token) ? "test" : null, token]);
  }

  for (const match of line.matchAll(/\]\(([^)\s]+)\)/g)) {
    const target = match[1];
    const relative = !target.startsWith("#") && !/^[a-z]+:/.test(target);
    found.push([relative ? "link" : null, target]);
  }

  for (const [kind, token] of found) {
    if (kind !== wanted || seen.has(token)) {
      continue;
    }

    seen.add(token);
    process.stdout.write(`${String(index + 1)}\t${token}\n`);
  }
});
NODEEOF
}

# ---------------------------------------------------------------------------- rutas

echo "Rutas citadas:"
while IFS=$'\t' read -r line token; do
  [[ -n "$token" ]] || continue
  if [[ -e "$token" ]]; then
    report ok "ruta" "$token" "$line"
  else
    report falla "ruta" "$token" "$line" "no existe en el árbol de trabajo."
  fi
done < <(extract route)

# ---------------------------------------------------------------------------- enlaces

echo
echo "Enlaces relativos:"
while IFS=$'\t' read -r line token; do
  [[ -n "$token" ]] || continue
  if [[ -e "${token%%#*}" ]]; then
    report ok "enlace" "$token" "$line"
  else
    report falla "enlace" "$token" "$line" "el destino del enlace no existe."
  fi
done < <(extract link)

# ---------------------------------------------------------------------------- tests

echo
echo "Tests nombrados:"
while IFS=$'\t' read -r line token; do
  [[ -n "$token" ]] || continue

  # De `Clase.Metodo` se busca el método, que es el identificador que de verdad tiene que existir:
  # una clase presente con el método renombrado es exactamente la deriva que hay que cazar.
  needle="${token##*.}"
  [[ "$token" == *.test.ts || "$token" == *.test.tsx ]] && needle="$token"

  if grep -rqF --exclude-dir=bin --exclude-dir=obj -- "$needle" backend/tests frontend/src 2>/dev/null; then
    report ok "test" "$token" "$line"
  else
    report falla "test" "$token" "$line" "«${needle}» no aparece en backend/tests ni en frontend/src."
  fi
done < <(extract test)

# ---------------------------------------------------------------------------- resultado

echo
if ((checks_failed > 0)); then
  echo "${checks_run} comprobaciones, ${checks_failed} fallas."
  exit 1
fi

echo "${checks_run} comprobaciones, 0 fallas."
