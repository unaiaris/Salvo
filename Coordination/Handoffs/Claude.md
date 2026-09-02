# Salvo — Handoffs de Claude

Registro append-only de entregas producidas por Claude. El estado canónico sigue en el Workboard y
`DesignAgent/Salvo-Progress.md`.

Para cada tarea, copiar la estructura de `ClaudeAgent/Claude-Handoff-Template.md` y agregarla al final
de este archivo. No sobrescribir entradas anteriores ni incluir secretos, PII o logs sin redactar.

## `E0-DOC-06` — Tooling de Claude Code: comandos, permisos y política de `.claude/`

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 0
- Rama/worktree: `claude/e0-doc-06-tooling`
- Commit base: `dbce667be64e5902e3f0faa8fed123f4a3ed162b`
- Commit final: `205bdd0fd6af95976b3367f34c3a514dfe325e19` (`docs: add E0-DOC-06 handoff entry`,
  incluye la implementación de `15b2b2a` y la entrada de handoff); esta corrección se agrega en un
  commit posterior sobre la misma rama
- Fecha: 2026-09-01

### Resultado

Se versionó el tooling de Claude Code para este repositorio: tres comandos de sesión
(`/gate`, `/handoff`, `/brief-check`) en `.claude/commands/`, permisos preautorizados en
`.claude/settings.json` que expresan sin ampliar la política de `AGENTS.md`, y una entrada de
`.gitignore` que ignora `.claude/settings.local.json` manteniendo versionados los comandos y
`settings.json`. `ClaudeAgent/README.md` quedó actualizado con el estado real del entorno (Claude
Code 2.1.257 instalado y verificado, smoke test de imports de `CLAUDE.md` ejecutado con éxito el
2026-09-01) y documenta los tres comandos nuevos. Ningún comportamiento de aplicación cambió;
`./scripts/check.sh` sigue verde.

### Archivos modificados

- `.claude/settings.json` (nuevo)
- `.claude/commands/gate.md` (nuevo)
- `.claude/commands/handoff.md` (nuevo)
- `.claude/commands/brief-check.md` (nuevo)
- `.gitignore` (agregada entrada para `.claude/settings.local.json`)
- `ClaudeAgent/README.md` (estado del entorno corregido; sección de comandos agregada)
- `Coordination/Handoffs/Claude.md` (esta entrada)

### Verificación

| Comando | Resultado |
| --- | --- |
| `git rev-parse HEAD` antes de empezar | `ac3f379ec349c0463086040b9a2384be93e09ce9` (contiene `dbce667` como ancestro; verificado con `git merge-base --is-ancestor dbce667 HEAD`) |
| `./scripts/check.sh` | Verde: restore locked, build Release 0 warnings/0 errores, sin cambios de modelo EF pendientes, 24 tests `Salvo.Domain.Tests` + 18 tests `Salvo.Api.IntegrationTests` (42 total, 0 fallos), `npm run check` (typecheck + ESLint + 3 tests Vitest) y `npm run build` (Next.js 16.3.3 webpack) sin errores |
| Consulta de documentación vigente de Claude Code | Agente de investigación confirmó en vivo el esquema de `permissions.allow`/`permissions.deny` (`ToolName(pattern)`, precedencia deny→ask→allow, `.claude/settings.local.json` como scope local documentado) y el formato de `.claude/commands/*.md` (frontmatter `description`/`allowed-tools`/`argument-hint`, `$ARGUMENTS`, prefijo `!` para bash) antes de escribir los archivos |
| `/gate`, `/handoff`, `/brief-check` como comandos de sesión | No pudieron invocarse como slash commands reales en esta misma sesión: el registro de comandos se carga al iniciar Claude Code y estos archivos se crearon durante la sesión activa. Se verificó su lógica ejecutando manualmente las acciones que cada comando orquesta (ver más abajo) |
| Verificación manual de `/gate` | `./scripts/check.sh` ejecutado directamente desde la raíz; resultado verde, igual al reportado arriba |
| Verificación manual de `/brief-check` sobre `Coordination/Tasks/E0-DOC-06.md` | Secciones completas contra la plantilla; `git show --stat dbce667` confirma que el commit base existe; `Coordination/Workboard.md` no registra otro trabajo activo que se solape con los paths reservados; sin contradicción con `AGENTS.md` — brief válido, sin faltantes |
| `git status --porcelain` | Solo `.gitignore`, `ClaudeAgent/README.md` y `.claude/` (nuevo) — ningún archivo fuera de los paths autorizados |
| Regla de denegación de lectura de `.env` | No probada de forma destructiva por diseño; verificada por inspección: `.claude/settings.json` deniega `Read(./.env)` y las variantes estándar de Next.js (`.local`, `.development`, `.production`, `.test`), sin denegar `Read(./.env.example)` |
| `/gate` (sesión nueva, invocado como skill) sobre `205bdd0` | Compuerta full-stack verde: restore NuGet locked, build Release 0 warnings/0 errores, sin cambios de modelo EF pendientes, 24 tests `Salvo.Domain.Tests` + 18 tests `Salvo.Api.IntegrationTests` (42 total, 0 fallos), `npm run check` (typecheck + ESLint + 3 tests Vitest) y build de producción Next.js 16.3.3 (webpack) sin errores |
| `/brief-check Coordination/Tasks/E0-DOC-06.md` (sesión nueva) | Brief válido: todas las secciones obligatorias presentes y completas, commit base `dbce667` verificado con `git show --stat`, sin solapamiento con otro trabajo activo en el Workboard, sin contradicción con `AGENTS.md` ni el Blueprint |
| `/brief-check` contra un brief incompleto de prueba (`/tmp/brief-incompleto-test.md`, fuera del repo) | Reportó inválido y se detuvo sin completar huecos: listó como faltantes Coordinador/Fecha/Rama/Commit base/Dependencias en Identificación, la sección Contexto obligatorio completa, "Fuera" y "Paths reservados" en Alcance, y las secciones Acciones autorizadas, Criterios de aceptación, Verificación y evidencia, Decisiones delegadas, Detenerse y consultar si, y Entrega requerida |

### Decisiones y supuestos

- Se consultó la documentación vigente de Claude Code (fetch en vivo, no memoria) antes de escribir
  `.claude/settings.json` y `.claude/commands/*.md`, según exige el brief.
- Para la regla "denegar lectura de `.env` y cualquier `.env.*` distinto de `.env.example`" se
  optó por enumerar las variantes estándar de Next.js (`.env`, `.env.local`, `.env.development[.local]`,
  `.env.production[.local]`, `.env.test[.local]`) en vez de un comodín `Read(./.env.*)`, porque la
  precedencia documentada es deny→ask→allow con el primer match ganando sin importar especificidad:
  un comodín amplio también habría bloqueado `Read(./.env.example)` sin posibilidad de excepción
  vía `allow`. Es una decisión delegada explícitamente por el brief.
- Además de `git push` y `git reset --hard` (listados explícitamente), se denegaron `git rebase`,
  `git commit --amend`, `git filter-branch`, `rm` y `git rm` como instancias concretas de
  "reescritura de historia" y "borrado de archivos", categorías que el brief menciona sin enumerar
  comandos exactos. Es un endurecimiento de `deny`, no una ampliación de `allow`, por lo que no
  amplía la política de `AGENTS.md`.
- `/gate`, `/handoff` y `/brief-check` no reimplementan la compuerta ni el ritual de handoff/brief:
  delegan explícitamente en `./scripts/check.sh`, `ClaudeAgent/Claude-Handoff-Template.md` y
  `Coordination/Task-Brief-Template.md` como fuente única.
- El coordinador relajó el criterio de bloqueo de lectura de `.env`: se acepta la verificación por
  inspección del patrón de `.claude/settings.json` en vez de una lectura real observada en runtime,
  porque no existe ningún `.env` en el repositorio y crear uno solo para probar el bloqueo sería peor
  que verificar el patrón declarado.

### Riesgos o pendientes

- Los tres comandos se probaron en una sesión nueva de Claude Code sobre esta rama (ver tabla de
  verificación arriba): `/gate` corrió la compuerta completa y reportó verde, `/brief-check` validó
  el brief real sin faltantes, y `/brief-check` sobre un brief incompleto de prueba reportó los
  faltantes exactos y se detuvo sin completarlos.
- La regla de denegación de lectura de `.env` se verificó por inspección del patrón, no intentando
  una lectura real y observando el bloqueo en runtime, para no ejecutar una acción fuera del alcance
  de esta tarea de forma innecesaria.

### Integración

- Orden sugerido: revisar el diff, confirmar en una sesión nueva que `/gate`, `/handoff` y
  `/brief-check` se reconocen y comportan como se describe arriba, luego commitear y fusionar sin
  pasos manuales adicionales.
- Migraciones o pasos manuales: ninguno.
- Posibles conflictos: ninguno esperado; los paths tocados no se solapan con otro trabajo activo
  según `Coordination/Workboard.md`.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` tras la integración,
  como exige el protocolo de `Coordination/README.md`.

## `E0-DOC-07` — Política de modelo y esfuerzo, e instrucciones del Proyecto de Claude.ai

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 0
- Rama/worktree: `claude/e0-doc-07-project-and-models`
- Commit base: `aa002e3` (`docs: assign E0-DOC-07 with coordinator drafts`)
- Commit final: `6afa4c478ff4acf029d6955474a67cb7e740308c` (`docs: connect E0-DOC-07 model policy
  and project instructions`)
- Fecha: 2026-09-02

### Resultado

Se revisaron los dos borradores del coordinador (`ClaudeAgent/Claude-Model-Policy.md` y
`DesignAgent/Salvo-Project-Instructions.md`) contra `AGENTS.md`, `Coordination/README.md` y el
Blueprint: no se encontró contradicción que requiriera corrección. La tabla de modelos y precios se
verificó contra la documentación oficial vigente de Anthropic (fetch en vivo, no memoria) y coincide
exactamente, así que no requirió cambios. Se conectaron ambos documentos con el resto del
repositorio: `Coordination/Task-Brief-Template.md` incorpora el campo `Modelo y esfuerzo acordados:`
en Identificación, `ClaudeAgent/README.md` ya no mantiene una segunda lista de adjuntos de
Claude.ai (remite a `Salvo-Project-Instructions.md` y agrega `Claude-Model-Policy.md` a su tabla de
archivos), y `DesignAgent/Salvo-MOC.md` indexa `Claude-Model-Policy.md`. Ningún comportamiento de
aplicación cambió; `./scripts/check.sh` sigue verde. No se tocaron `AGENTS.md`, `CLAUDE.md`, el
Blueprint, el Progress ni el Workboard.

### Archivos modificados

- `ClaudeAgent/README.md` (fila de `Claude-Model-Policy.md` en la tabla de archivos; sección «Uso
  con Claude.ai sin Claude Code» reemplazada por una remisión a `Salvo-Project-Instructions.md`)
- `Coordination/Task-Brief-Template.md` (campo `Modelo y esfuerzo acordados:` agregado en
  Identificación, entre `Commit base:` y `Dependencias:`)
- `DesignAgent/Salvo-MOC.md` (entrada nueva para `Claude-Model-Policy.md`)
- `Coordination/Handoffs/Claude.md` (esta entrada)
- Revisados sin cambios: `ClaudeAgent/Claude-Model-Policy.md`, `DesignAgent/Salvo-Project-Instructions.md`
  (ya estaban commiteados en `aa002e3` como borradores del coordinador; no contradicen `AGENTS.md`,
  `Coordination/README.md` ni el Blueprint)

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E0-DOC-07.md` | Brief válido: todas las secciones obligatorias presentes y completas, commit base `aa002e3` confirmado con `git log`, `E0-DOC-07` como única tarea activa en el Workboard sin solapamiento, sin contradicción con `AGENTS.md` ni el Blueprint |
| Consulta en vivo de `https://platform.claude.com/docs/en/about-claude/models/overview` (vía WebFetch, redirigido desde `docs.claude.com`) | Tabla de modelos, contexto y precios confirmada idéntica al borrador: Fable 5.1 $10/$50 · 1M · `claude-fable-5-1`; Opus 5 $5/$25 · 1M · `claude-opus-5`; Sonnet 5 $2/$10 · 1M · `claude-sonnet-5`; Haiku 4.5 $1/$5 · 200K · alias `claude-haiku-4-5` (ID pinneado `claude-haiku-4-5-20251001`); sin correcciones necesarias |
| `grep -n "Modelo y esfuerzo" Coordination/Task-Brief-Template.md` | Campo presente en Identificación, entre `Commit base:` y `Dependencias:` |
| `./scripts/check.sh` (`/gate`) sobre `6afa4c4` | Compuerta full-stack verde: restore NuGet actualizado (incluye `dotnet-ef` 10.0.11), build Release 0 advertencias/0 errores, sin cambios de modelo EF pendientes, 24 tests `Salvo.Domain.Tests` + 18 tests `Salvo.Api.IntegrationTests` (42 total, 0 fallos), `npm run check` (typecheck + ESLint 0 warnings + 3 tests Vitest) y build de producción Next.js 16.3.3 (webpack) sin errores |
| `git status --porcelain` (tras commit) | Limpio; solo los tres paths autorizados aparecieron modificados antes del commit |
| `git diff --check` | Pasa, sin marcadores de conflicto ni espacios en blanco problemáticos |

### Decisiones y supuestos

- La tabla de modelos del borrador ya coincidía con la documentación oficial vigente; se declara
  la verificación en vez de reescribir una tabla que no tenía errores, siguiendo el criterio del
  brief ("si un dato cambió, corregilo y declaralo" — en este caso no cambió).
- El texto exacto de la remisión que reemplaza «Uso con Claude.ai sin Claude Code» en
  `ClaudeAgent/README.md` y la ubicación de las filas nuevas en las tablas de `ClaudeAgent/README.md`
  y `Salvo-MOC.md` fueron decisiones delegadas explícitamente por el brief.
- No se modificó `ClaudeAgent/Claude-Model-Policy.md` ni `DesignAgent/Salvo-Project-Instructions.md`:
  tras revisarlos contra `AGENTS.md`, el Blueprint y `Coordination/README.md`, no se encontró
  contradicción que ameritara una corrección.

### Riesgos o pendientes

- Ninguno. Todos los criterios de aceptación del brief se verificaron con comandos reales.

### Integración

- Orden sugerido: revisar el diff, confirmar que `Claude-Model-Policy.md` y
  `Salvo-Project-Instructions.md` siguen sin contradicción tras cualquier cambio posterior del
  coordinador, luego fusionar sin pasos manuales adicionales.
- Migraciones o pasos manuales: ninguno.
- Posibles conflictos: ninguno esperado; los paths tocados no se solapan con otro trabajo activo
  según `Coordination/Workboard.md`. El coordinador debe agregar la fila de
  `Claude-Model-Policy.md` al §14 del Blueprint al integrar, según indica el brief.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` tras la integración,
  como exige el protocolo de `Coordination/README.md`.
