# Salvo — Task brief `E0-DOC-06`

## Identificación

- Work ID: `E0-DOC-06`
- Etapa: 0
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-01
- Rama/worktree: `claude/e0-doc-06-tooling`
- Commit base: `dbce667be64e5902e3f0faa8fed123f4a3ed162b`
- Dependencias: ninguna. No hay trabajo activo ni paths reservados en el Workboard.

## Resultado esperado

El repositorio versiona el tooling de Claude Code —comandos `/gate`, `/handoff` y `/brief-check`,
permisos preautorizados y una política explícita de `.claude/` en `.gitignore`— y
`ClaudeAgent/README.md` describe el estado real del entorno. Ningún comportamiento de aplicación
cambia y la compuerta full-stack sigue verde.

## Contexto obligatorio

- Sección del Blueprint: §14 Mapa de documentación. Esta tarea agrega una entrada de tooling; no
  altera producto ni arquitectura, por lo que no requiere entrada en la bitácora de decisiones.
- Entrada/checklist del Progress: Etapa 0 — Documentación e instrucciones. El estado canónico lo
  actualiza el coordinador después de integrar; esta tarea no lo toca.
- Tests, contratos o documentación relacionados:
  - `AGENTS.md`, sección «Autonomía y aprobaciones» — es la política que los permisos deben
    reflejar, no ampliar.
  - `CLAUDE.md` — directiva de no leer `.env` ni almacenes de credenciales.
  - `ClaudeAgent/README.md` — contiene dos afirmaciones hoy falsas.
  - `Coordination/README.md` y `Coordination/Task-Brief-Template.md` — definen el ritual que los
    comandos automatizan.
  - `ClaudeAgent/Claude-Handoff-Template.md` — estructura exacta que `/handoff` debe producir.
  - `scripts/check.sh` — compuerta que `/gate` debe invocar sin reimplementar.

Antes de escribir los archivos de configuración, consultar la documentación vigente de Claude Code
para el esquema real de `settings.json` y el formato de los comandos de `.claude/commands/`. No
inferir el esquema de memoria.

## Alcance

### Dentro

- Crear `.claude/commands/gate.md`: ejecuta `./scripts/check.sh` desde la raíz del repositorio y
  reporta el resultado paso a paso, distinguiendo con claridad verde de fallo. No reimplementa los
  comandos de la compuerta ni los ejecuta sueltos.
- Crear `.claude/commands/handoff.md`: arma una entrada siguiendo exactamente
  `ClaudeAgent/Claude-Handoff-Template.md`, con rama, commit base y commit final obtenidos de Git
  —nunca de memoria— y la agrega al final de `Coordination/Handoffs/Claude.md` sin sobrescribir
  entradas previas.
- Crear `.claude/commands/brief-check.md`: valida un task brief recibido contra
  `Coordination/Task-Brief-Template.md`. Verifica que estén todas las secciones obligatorias, que
  el commit base exista en el repositorio, que los paths autorizados no se solapen con trabajo
  activo del Workboard, y que el alcance no contradiga `AGENTS.md` ni el Blueprint. Ante cualquier
  falta, se detiene y la reporta; no completa huecos por su cuenta ni genera briefs.
- Crear `.claude/settings.json` con los permisos definidos más abajo.
- Actualizar `.gitignore` para ignorar `.claude/settings.local.json` manteniendo versionados
  `.claude/commands/` y `.claude/settings.json`.
- Actualizar `ClaudeAgent/README.md`: corregir el estado del entorno —Claude Code 2.1.257 instalado
  y verificado— y registrar que el smoke test de imports de `CLAUDE.md` se ejecutó con éxito el
  2026-09-01, cerrando el riesgo abierto en el handoff `E0-DOC-04`. Documentar los comandos nuevos.
- Registrar la entrega en `Coordination/Handoffs/Claude.md`.

### Fuera

- Modificar `CLAUDE.md` o `AGENTS.md`. Los permisos reflejan la política existente; no la cambian.
- Ejecutar `/init`.
- Cualquier archivo de `backend/`, `frontend/`, `scripts/` o configuración de build.
- Hooks, servidores MCP, subagentes o plugins. Fuera de alcance por decisión explícita: se evalúan
  después de que este tooling se use en una tarea real.
- Modificar `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` o el Blueprint. Son estado
  canónico y los actualiza el coordinador tras integrar y verificar.
- Instalar o actualizar dependencias de cualquier tipo.

### Paths autorizados

- `.claude/**`
- `.gitignore`
- `ClaudeAgent/README.md`
- `Coordination/Handoffs/Claude.md`

### Paths reservados por otros trabajos

- Ninguno. El Workboard no registra trabajo activo.

## Acciones autorizadas

- Ediciones locales permitidas: sí, únicamente en los paths autorizados.
- Instalación o actualización de dependencias: no autorizada.
- Escrituras externas: ninguna. No hacer `git push`, no abrir PR, no acceder a red.
- Acciones destructivas: ninguna. No borrar archivos, no reescribir historia, no `git reset --hard`.

Commits locales en la rama `claude/e0-doc-06-tooling`: autorizados.

## Permisos a configurar

Preautorizar solo comandos verificables y reversibles:

- `dotnet restore`, `dotnet tool restore`, `dotnet build`, `dotnet test`
- `dotnet ef migrations has-pending-model-changes`
- `npm ci`, `npm run check`, `npm run build`
- `./scripts/check.sh`
- `git status`, `git diff`, `git log`, `git show`, `git branch`

Denegar explícitamente:

- `git push` y cualquier escritura al remoto
- `git reset --hard` y reescritura de historia
- borrado de archivos
- lectura de `.env` y de cualquier `.env.*` distinto de `.env.example`

Todo lo no listado queda sujeto a la política por defecto de `AGENTS.md`: el silencio nunca
autoriza escrituras externas, acciones destructivas ni ampliaciones de alcance.

## Criterios de aceptación

- [ ] `.claude/commands/gate.md`, `handoff.md` y `brief-check.md` existen y son ejecutables como
      comandos de sesión.
- [ ] `/gate` ejecutado una vez sobre la rama corre la compuerta completa y reporta su resultado.
- [ ] `/brief-check` ejecutado contra este mismo brief lo valida sin reportar faltantes.
- [ ] `/brief-check` ejecutado contra un brief incompleto reporta qué falta y se detiene.
- [ ] `.claude/settings.json` refleja exactamente los permisos listados, sin ampliar la política de
      `AGENTS.md`.
- [ ] Un intento de leer `.env` queda bloqueado por la regla de denegación.
- [ ] `.gitignore` ignora `.claude/settings.local.json` y mantiene versionados `.claude/commands/` y
      `.claude/settings.json`.
- [ ] `ClaudeAgent/README.md` no contiene afirmaciones falsas sobre el entorno y documenta los
      comandos nuevos.
- [ ] `Coordination/Handoffs/Claude.md` contiene la entrada `E0-DOC-06` completa.
- [ ] Ningún archivo fuera de los paths autorizados aparece en `git status`.
- [ ] `./scripts/check.sh` sigue verde.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `git rev-parse HEAD` antes de empezar | `dbce667be64e5902e3f0faa8fed123f4a3ed162b` |
| `./scripts/check.sh` | Compuerta full-stack verde: 0 warnings, 42 tests .NET, sin cambios de modelo pendientes, `npm run check` y build de producción |
| `/memory` | `CLAUDE.md` y sus cinco imports cargados |
| `/gate` | Ejecuta la compuerta y reporta el resultado real, no uno asumido |
| `/brief-check` sobre este brief | Validación sin faltantes |
| `git status --porcelain` | Solo paths autorizados |
| `git diff --check` | Pasa |
| `git log --oneline` | Commits de la rama con base `dbce667` |

## Decisiones delegadas

- Redacción, estructura interna y nombres exactos de los archivos de comando.
- Formato del reporte que produce `/gate`.
- Esquema concreto de `.claude/settings.json` según la documentación vigente de Claude Code,
  siempre que exprese los permisos listados sin ampliarlos.
- Ubicación de la línea de `.claude/` dentro de `.gitignore`.
- Redacción de la actualización de `ClaudeAgent/README.md`.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- se necesita una credencial, escritura externa o acción destructiva no autorizada;
- aparece solapamiento con otra tarea o cambios ajenos en paths autorizados;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio;
- el esquema real de `settings.json` no permite expresar alguno de los permisos listados;
- `ClaudeAgent/README.md` resulta contradecir a `AGENTS.md` en algún punto no previsto aquí.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md` siguiendo
  `ClaudeAgent/Claude-Handoff-Template.md`.
