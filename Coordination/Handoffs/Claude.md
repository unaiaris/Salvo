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

---

## `E4A-PERSISTENCIA` — Persistencia idempotente de evaluaciones locales y corridas de scoring

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 4
- Rama/worktree: `claude/e4a-persistencia`
- Commit base: `1215a2d` (`docs: approve E4 design and assign E4A-PERSISTENCIA`, cabeza de `main`
  y `merge-base` de la rama; es el commit que el brief describe en prosa como «el commit de `main`
  que incorpora las decisiones 28–36 del Blueprint y este brief»)
- Commit final: `73c1b17` (implementación completa; esta entrada de handoff se agrega en el commit
  siguiente, que solo toca `Coordination/Handoffs/Claude.md`)
- Fecha: 2026-09-02

### Resultado

El sistema persiste evaluaciones locales de riesgo y las corridas que las produjeron, de forma
idempotente y auditable, y expone la corrida y la lectura paginada de pedidos por HTTP.

`RiskEvaluation` es append-only y su identidad es un fingerprint SHA-256 de su contenido, acotado a
`source = 'LOCAL'` mediante un índice único **parcial**. La serialización canónica de señales vive
en `Salvo.Domain` y usa el orden de reglas que emite `TemporalRiskEngine`, no un orden alfabético;
la cadena que se hashea es exactamente la que se persiste en `signalsJson` y nunca se re-serializa.
`ScoringRun` y `RunEvaluation` hacen que la evaluación vigente de un pedido sea la que referenció la
última corrida, de modo que un score que rebota a un valor anterior (0 → 40 → 0) deja vigente la
evaluación correcta y no la intermedia. La corrida completa se escribe en un único
`SaveChangesAsync`, con preconsulta de fingerprints antes de insertar, e invoca
`TemporalRiskEngine.Score` directamente en lugar de `EvaluateLocalRiskHandler`, que exige etiquetas
que una importación no produce.

Los cinco hallazgos de origen quedaron cubiertos: 2 (`ScoringRun`/`RunEvaluation` y redefinición de
«vigente»), 5 (fingerprint e índice acotados a `LOCAL`), 6 (el run llama al motor, no al handler de
métricas), 7 (un solo `SaveChanges`, preconsulta de fingerprints, colisión de unicidad a `409`) y 8
(serialización canónica en Domain con el orden del motor y test dorado de fingerprints).

### Archivos modificados

Commit `73c1b17`, 44 archivos, todos dentro de los paths autorizados del brief.

**Dominio — `backend/src/Salvo.Domain/Risk/`** (nuevos salvo el último)

- `RiskEvaluation.cs`, `ScoringRun.cs`, `RunEvaluation.cs`
- `RiskEvaluationSource.cs`, `RiskEvaluationStatus.cs`, `RiskEvaluationWireNames.cs`
- `RiskSignalSerializer.cs`, `RiskEvaluationFingerprint.cs`
- `RiskRuleNames.cs` (modificado: agrega `CanonicalOrder` y `CanonicalIndexOf`)

**Aplicación — `backend/src/Salvo.Application/`** (todos nuevos)

- `Risk/RunScoringHandler.cs`, `Risk/IScoringRunStore.cs`, `Risk/ScoringRunSummary.cs`,
  `Risk/ScoringRunConflictException.cs`, `Risk/IRiskIdGenerator.cs`
- `Orders/ListOrdersHandler.cs`, `Orders/ListOrdersResult.cs`, `Orders/IOrderPageReader.cs`,
  `Orders/OrderPage.cs`

**Infraestructura — `backend/src/Salvo.Infrastructure/`**

- Nuevos: `Persistence/Configurations/{RiskEvaluation,ScoringRun,RunEvaluation}Configuration.cs`,
  `Persistence/EfScoringRunStore.cs`, `Persistence/EfOrderPageReader.cs`,
  `Persistence/RiskEvaluation{Source,Status}Converter.cs`, `SystemRiskIdGenerator.cs`,
  `Persistence/Migrations/20260902190108_RiskEvaluationPersistence.cs` (+ `.Designer.cs`)
- Modificados: `Persistence/SalvoDbContext.cs`, `DependencyInjection.cs`,
  `Persistence/Migrations/SalvoDbContextModelSnapshot.cs`

**API — `backend/src/Salvo.Api/`**

- Nuevo: `RiskEvaluationEndpoints.cs` (`POST /api/risk-evaluations:run`)
- Modificados: `OrderEndpoints.cs` (`GET /api/orders` paginado), `Program.cs` (composición)

**Tests — `backend/tests/`**

- Nuevos: `Salvo.Domain.Tests/{RiskSignalSerializerTests,RiskEvaluationIdentityTests,ScoringRunTests,CultureProbe}.cs`,
  `Salvo.Api.IntegrationTests/{ScoringRunPersistenceTests,OrderListingTests}.cs`
- Modificados: `Salvo.Domain.Tests/ArchitectureSmokeTests.cs`,
  `Salvo.Api.IntegrationTests/{FoundationTests,RiskEvaluationTests,SalvoApiFactory}.cs`

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E4A-PERSISTENCIA.md` | Brief válido: las 13 secciones obligatorias presentes y completas; commit base resuelto a `1215a2d` y confirmado con `git log`/`git merge-base`; sin solapamiento de paths en el Workboard (`E4A` es la única fila activa); sin contradicción con `AGENTS.md` ni el Blueprint |
| `dotnet ef migrations add RiskEvaluationPersistence` | Migración generada; el índice se emite como `CREATE UNIQUE INDEX … WHERE "source" = 'LOCAL'` |
| `dotnet ef migrations has-pending-model-changes` | `No changes have been made to the model since the last migration.` |
| `dotnet build -c Release` | 0 advertencias, 0 errores (incluye la corrección de `CA1861` en la migración, siguiendo el patrón de la migración de E2) |
| `dotnet test` | **71 tests, 0 fallos**: 39 en `Salvo.Domain.Tests` (antes 24) y 32 en `Salvo.Api.IntegrationTests` (antes 18) |
| Corrida doble sobre el corpus demo (`RepeatedRunOverTheSameCorpusAppendsNothingAndReferencesTheSameEvaluations`) | Primera corrida: `orderCount=300`, `evaluationsCreated=300`, `evaluationsReused=0`. Segunda: `evaluationsCreated=0`, `evaluationsReused=300`; 300 filas en `risk_evaluations`, 600 en `run_evaluations`, y las referencias de ambas corridas apuntan a las mismas evaluaciones |
| Escenario de rebote 0 → 40 → 0 (`AScoreThatReturnsToAnEarlierValueKeepsTheCurrentEvaluationCorrect`) | Tres corridas con importaciones retroactivas: `(seq, orders, creadas, reusadas)` = `(1,3,3,0)`, `(2,4,2,2)`, `(3,7,3,4)`. El pedido objetivo tiene exactamente 2 evaluaciones (scores 0 y 40) y la vigente tras la tercera corrida es la del score 0, con `status=APPROVED` y `signalsJson="[]"` |
| Test dorado de fingerprints (`DemoCorpusProducesTheGoldenFingerprintsOfTheApprovedRuleConfiguration`) | 300 fingerprints distintos; distribución de scores `{0:266, 20:16, 60:13, 90:5}` idéntica a la de E3; 18 evaluaciones `DENIED`; fingerprint fijo de `ORD_000001` y digest agregado del corpus fijados y reproducidos en dos procesos distintos |
| Estabilidad ante culturas (`SerializationIsStableAndIndependentOfTheAmbientCulture`, `FingerprintIsStableAcrossCultures`) | Serialización y fingerprint idénticos bajo `en-US`, `es-UY` y `de-DE`, ejecutados en hilos dedicados para no filtrar la cultura a otros tests |
| Fallo parcial (`AFailedWriteLeavesNeitherRunNorEvaluationsNorReferences`) | Una referencia a una evaluación inexistente hace fallar la escritura; `scoring_runs`, `risk_evaluations` y `run_evaluations` quedan en 0 filas verificadas desde un scope nuevo |
| Índice parcial (`TheFingerprintIndexIsUniqueOnlyForLocallyProducedEvaluations`) | El DDL en `sqlite_master` contiene `CREATE UNIQUE INDEX` y `WHERE "source" = 'LOCAL'`; dos evaluaciones locales con el mismo fingerprint son rechazadas y la violación se traduce a `ScoringRunConflictException` |
| Corrida sin etiquetas (`ScoringRunSucceedsForImportedOrdersThatHaveNoGroundTruthLabel`) | Importar un pedido sin etiqueta y correr el scoring responde `200`, con 0 filas en `order_evaluation_labels` y 1 en `risk_evaluations` |
| Conflicto de unicidad (`TwoRunsThatClaimTheSamePositionCollideInsteadOfBecomingAmbiguous`, `ARunConflictIsReportedAsConflictAndNotAsServerError`) | Dos corridas que reclaman la misma posición colisionan en el índice único y la segunda recibe `ScoringRunConflictException`; el endpoint responde `409` con `code = "SCORING_RUN_CONFLICT"`, nunca `500` |
| `GET /api/orders` sin etiquetas (`OrderListingNeverExposesGroundTruthLabels`, teoría con 5 variantes de query string) | Ninguna respuesta contiene `fraud` ni `label` en ningún campo, tampoco con `?isFraudLabel=true`, `?include=…`, `?fields=…&expand=…` ni `?sort=…`; `OrderListItem` no declara ninguna propiedad de etiqueta |
| Frontera de dominio (`DomainAssemblyCanBeLoadedWithoutFrameworkDependencies`) | `Salvo.Domain` no referencia ensamblados de ASP.NET Core, EF Core, `Microsoft.Data.Sqlite`, `Microsoft.Extensions`, CsvHelper ni Anthropic |
| `./scripts/check.sh` (`/gate`) | **Compuerta full-stack verde**, exit `0`: restore con `--locked-mode`, build Release 0/0, sin cambios de modelo EF pendientes, 71 tests .NET, `npm run check` (typecheck + ESLint `--max-warnings=0` + 3 tests Vitest) y build de producción Next.js 16.3.3 (webpack) |
| `git status --porcelain` | Limpio tras el commit; antes del commit solo aparecieron paths autorizados |
| `git diff --check` | Pasa |

### Decisiones y supuestos

- **Commit base declarado en prosa.** El brief no fija un hash. Resuelve sin ambigüedad a `1215a2d`
  (cabeza de `main`, `merge-base` de la rama y commit que introduce `E4-DISENO.md`,
  `E4-revision-adversarial.md`, el propio brief y las decisiones 28–36). Se asume ese commit como
  base; si el coordinador esperaba otro, detener la integración.
- **`status` de una evaluación local.** El Blueprint define `status: PENDING | APPROVED | DENIED |
  ERROR` pensando en el ciclo externo. Para `LOCAL` se deriva del veredicto determinista:
  `DENIED` cuando la evaluación está marcada, `APPROVED` cuando no. Así «marcada» queda persistida y
  estable aunque cambie el umbral de una futura `RuleConfig`, y `E4B` puede leer el predicado de
  creación de alertas directamente del estado guardado. `status` no entra en el fingerprint porque
  es función del score, que sí entra.
- **`ScoringRun.Sequence`, campo no listado en el brief.** El brief enumera `id`,
  `ruleConfigVersion`, `startedAt`, `completedAt`, `orderCount`, `evaluationsCreated` y
  `evaluationsReused`. Se agregó `run_sequence`, un entero único y creciente, porque sin un orden
  total la definición «la evaluación que referenció la última corrida» queda indefinida ante empates
  de timestamp —los timestamps se persisten con precisión de milisegundos— y el desempate por `id`
  aleatorio es exactamente el tipo de no-determinismo que la revisión adversarial objetó. Su índice
  único es además el mecanismo por el que dos corridas concurrentes colisionan y se mapean a `409`.
- **`RunEvaluation` con clave primaria compuesta `(run_id, order_id)`.** Es la restricción de
  unicidad que pide el brief, expresada como PK en vez de índice único adicional; SQLite crea su
  propio índice único para la PK. Se agregaron índices de apoyo sobre `order_id` y `evaluation_id`.
- **Índice único parcial en EF Core.** Se expresó con `HasIndex(...).IsUnique().HasFilter("source =
  'LOCAL'")`, sin SQL crudo. No hizo falta detenerse ni consultar: EF Core 10 lo traduce a
  `CREATE UNIQUE INDEX … WHERE "source" = 'LOCAL'` sobre SQLite, y el DDL resultante se verifica en
  un test contra `sqlite_master`.
- **Serialización canónica.** Formato JSON compacto `[{"rule":…,"weight":…,"detail":…}]` escrito con
  `Utf8JsonWriter` (BCL, sin el serializador por reflexión ni opciones por defecto), con los números
  emitidos de forma invariante. El serializador **ordena** por `RiskRuleNames.CanonicalOrder` en vez
  de confiar en el orden de entrada, y rechaza reglas desconocidas o repetidas, para que un
  recálculo desde la fila almacenada reproduzca su propio fingerprint. Un test verifica ese orden
  contra la salida real de `TemporalRiskEngine` y comprueba explícitamente que **no** es el orden
  alfabético.
- **Fingerprint.** `SHA-256(orderId | source | ruleConfigVersion | score | signalsCanonical)` en
  UTF-8, hex minúscula de 64 caracteres. Solo el último componente puede contener el separador, así
  que la codificación es inequívoca. `RiskEvaluationWireNames` centraliza los nombres textuales de
  `source`/`status`, de modo que lo hasheado y lo persistido no pueden divergir.
- **`RiskEvaluationSource`/`RiskEvaluationStatus` completos.** Se definen `EXTERNAL_MOCK`,
  `KOIN_SANDBOX`, `PENDING` y `ERROR` aunque E4A no los use, para no rehacer el schema en E6, tal
  como pide el brief. No hay fábrica de dominio para evaluaciones externas: crearla es E6.
- **`IScoringRunStore.GetCurrentEvaluationsAsync` no se expone todavía por HTTP.** Es la
  implementación concreta de la decisión 31 y el único modo de verificar el criterio de rebote;
  `E4B` la consume para el predicado de creación de alertas. Se prefirió definirla y testearla una
  vez, en Infraestructura, antes que rederivar la consulta en el test y otra vez en E4B.
- **`IRiskIdGenerator` propio.** Se siguió el patrón existente de `IOrderIdGenerator` en vez de
  llamar a `Guid.NewGuid()` dentro del caso de uso. No se tocó `IOrderIdGenerator`, que vive en
  `Orders/` y pertenece a otro concern.
- **Mapeo de `OrderChannel` a texto en `ListOrdersHandler`.** El nombre de transporte del canal se
  repite en Aplicación porque `Salvo.Domain/Orders/**` no está entre los paths autorizados y el
  conversor existente vive en Infraestructura. Es la única duplicación introducida; si el
  coordinador lo prefiere, moverla a Domain es un cambio de una línea en una tarea posterior.
- **Errores de EF Core no cruzan la frontera.** `EfScoringRunStore` traduce una violación de
  unicidad de SQLite (códigos extendidos 2067 y 1555) a `ScoringRunConflictException`, definida en
  Aplicación. Las demás fallas de persistencia se propagan como `DbUpdateException`, igual que en el
  `EfOrderDataStore` de E2.
- **`RuleConfig`, `TemporalRiskEngine`, `Order`, el importador y el seed no se tocaron.** Se
  consumen tal como están, como exige el brief. `Salvo.Domain/Evaluation/**` tampoco se modificó, así
  que no hubo que justificar su uso.

### Riesgos o pendientes

- **Un test previo de E3 cambió de premisa.** `RiskEvaluationTests`
  (`…WithoutPersistenceEffects`) afirmaba que no existe ninguna tabla cuyo nombre contenga «risk» o
  «alert», es decir, codificaba «la Etapa 4 todavía no ocurrió». E4A crea `risk_evaluations`, así que
  esa cláusula se reemplazó por una comprobación más fuerte y todavía vigente: el camino de métricas
  sigue sin escribir nada, con `risk_evaluations`, `scoring_runs` y `run_evaluations` en cero filas
  tras evaluar. La cláusula sobre «alert» se conserva intacta hasta E4B. Es el único test existente
  cuyo significado se ajustó.
- **`SalvoApiFactory` acepta ahora overrides de servicios de prueba** (`ConfigureTestServices`,
  aditivo y opcional). Ningún test previo cambia de comportamiento. Sigue compartiendo una única
  conexión `:memory:` por instancia de fábrica, así que la carrera de revisión que `E4B` necesita
  todavía requerirá una base en archivo o `mode=memory&cache=shared`, como anticipó el hallazgo 4.
- **El test dorado fija el texto de los `detail`.** Es deliberado: cambiar la redacción de una señal
  sin subir `ruleConfigVersion` rompe el test de forma ruidosa en vez de producir 300 evaluaciones
  falsamente nuevas en la siguiente corrida.
- **`POST /api/risk-evaluations:run` recalcula el corpus completo.** Con 300 pedidos es trivial; el
  diseño no define comportamiento para volúmenes donde deje de serlo. Riesgo ya declarado en
  `E4-DISENO.md`, no introducido aquí.
- **`AGENTS.md` §«Estado actual» está desactualizado**: sigue diciendo «No hay tareas activas; Etapa
  4 permanece pendiente de autorización explícita» y «No iniciar … persistencia de evaluaciones sin
  aprobación», mientras el Progress y el Workboard registran la Etapa 4 aprobada y `E4A` asignada. Se
  procedió con la autorización explícita del coordinador; corregir `AGENTS.md` corresponde al
  coordinador y está fuera de los paths autorizados de esta tarea.
- Sin dependencias nuevas, sin cambios en `Directory.Packages.props` ni en lockfiles, sin escrituras
  externas, sin `git push` y sin borrar ni recrear ninguna base local.

### Integración

- Orden sugerido: `E4A-PERSISTENCIA` primero y sola; `E4B-ALERTAS` depende de esta rama integrada.
- Migraciones o pasos manuales: la migración `20260902190108_RiskEvaluationPersistence` es
  puramente aditiva —crea `risk_evaluations`, `scoring_runs` y `run_evaluations` y no altera
  `orders` ni `order_evaluation_labels`—, así que una base local existente se actualiza con
  `dotnet ef database update` sin recrearse ni perder datos.
- Posibles conflictos: ninguno esperado. `E4B` tocará los mismos proyectos, por lo que conviene
  integrar esta rama antes de despachar `E4B`. El único archivo compartido de test que cambió es
  `SalvoApiFactory.cs`, de forma aditiva.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` y confirmar que el test
  dorado de fingerprints sigue verde sobre el estado integrado, según el protocolo de
  `Coordination/README.md`.

---

## `E4B-ALERTAS` — Alertas con escalada, revisión transaccional y control de concurrencia

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 4
- Rama/worktree: `claude/e4b-alertas`
- Commit base: `05ddb4a` (`chore: assign E4B-ALERTAS`, punta de `main`)
- Commit final: `c6e945b` (implementación completa; esta entrada de handoff se agrega en el commit
  siguiente, que solo toca `Coordination/Handoffs/Claude.md`)
- Fecha: 2026-09-02

### Resultado

Una evaluación local marcada produce una alerta operable. La corrida de scoring abre las alertas
dentro de su único `SaveChangesAsync`; el listado y el detalle las exponen junto con la evaluación
vigente y su divergencia de banda; y `POST /api/alerts/{id}/review` emite un veredicto terminal que
queda auditado. Dos revisiones simultáneas de la misma alerta terminan en un `200` y un `409`, con
un solo `AlertReview` persistido.

Piezas entregadas:

- **Dominio.** `AlertStatus` (`OPEN`, `CONFIRMED_SAFE`, `REPORTED_FRAUD`), `AlertSeverity`
  (`MEDIUM`, `HIGH`, `CRITICAL`), `AlertPolicy e4-v1` inmutable con bandas 60–69 / 70–89 / 90–100,
  `Alert` y `AlertReview`. La severidad es propiedad calculada, resuelta por la versión de política
  que la alerta guarda; no existe como columna.
- **Validación de arranque.** `AlertPolicy.Validate(RuleConfig)` exige que el piso de la banda más
  baja sea igual a `RuleConfig.FlagThreshold`, que las bandas cubran sin huecos ni solapes hasta
  `ScoreCap` y que la severidad crezca. `Program.Main` la invoca antes de construir el host.
- **Creación dentro de la corrida.** `RunScoringHandler` evalúa el predicado sobre estado
  persistido (`IScoringRunStore.GetAlertStatesAsync`) y entrega las alertas a `SaveRunAsync`, que
  las escribe en el mismo `SaveChangesAsync` de la corrida. `ScoringRun` gana `alertsCreated`,
  `alertsSkippedOpen` y `alertsSkippedReviewed`.
- **Escalada.** Sin alerta abierta, se crea alerta nueva si el pedido nunca fue alertado o si la
  banda vigente supera la de la última alerta; en ese caso enlaza con `supersedesAlertId`. Subir de
  puntos dentro de la misma banda no crea nada.
- **Revisión transaccional.** `ReviewAlertHandler` escribe alerta y auditoría en un solo
  `SaveChangesAsync`. `alerts.status` es token de concurrencia y `UNIQUE(alert_reviews.alert_id)` lo
  respalda a nivel base. Idempotencia y conflicto siguen exactamente la tabla de D7.
- **Divergencia.** El detalle devuelve snapshot, evaluación vigente y un bloque `divergence`;
  revisar con bandas divergentes exige `acknowledgedDivergence: true`.
- **Corrección de arrastre.** `SalvoDbContextFactory` ya no fija `salvo.design.db`: resuelve
  `ConnectionStrings__SalvoDb`, luego `appsettings.{Environment}.json`, luego `appsettings.json`, y
  sólo entonces cae a `salvo.design.db`.

### Archivos modificados

Dominio (`backend/src/Salvo.Domain/`):

- `Alerts/Alert.cs`, `Alerts/AlertReview.cs`, `Alerts/AlertPolicy.cs`, `Alerts/AlertSeverityBand.cs`
- `Alerts/AlertStatus.cs`, `Alerts/AlertSeverity.cs`, `Alerts/AlertWireNames.cs`,
  `Alerts/AlertTransitionException.cs`
- `Risk/ScoringRun.cs` (tres contadoras nuevas), `Risk/RiskSignalSerializer.cs` (`Deserialize`)

Aplicación (`backend/src/Salvo.Application/`):

- `Alerts/IAlertStore.cs`, `Alerts/IAlertIdGenerator.cs`, `Alerts/AlertContext.cs`,
  `Alerts/AlertPage.cs`, `Alerts/OrderAlertState.cs`
- `Alerts/AlertViews.cs`, `Alerts/AlertProjection.cs`
- `Alerts/ListAlertsHandler.cs`, `Alerts/GetAlertHandler.cs`, `Alerts/ReviewAlertHandler.cs`,
  `Alerts/ReviewAlertCommand.cs`
- `Alerts/AlertReviewConflictException.cs`, `Alerts/AlertReviewConflictReason.cs`
- `Risk/IScoringRunStore.cs`, `Risk/RunScoringHandler.cs`, `Risk/ScoringRunSummary.cs`

Infraestructura (`backend/src/Salvo.Infrastructure/`):

- `Persistence/Configurations/AlertConfiguration.cs`,
  `Persistence/Configurations/AlertReviewConfiguration.cs`,
  `Persistence/Configurations/ScoringRunConfiguration.cs`
- `Persistence/AlertStatusConverter.cs`, `Persistence/EfAlertStore.cs`,
  `Persistence/EfScoringRunStore.cs`, `Persistence/SalvoDbContext.cs`,
  `Persistence/SalvoDbContextFactory.cs`
- `Persistence/Migrations/20260902204944_AlertsAndReview.cs` y su `.Designer.cs`,
  `Persistence/Migrations/SalvoDbContextModelSnapshot.cs`
- `DependencyInjection.cs`, `SystemAlertIdGenerator.cs`

API (`backend/src/Salvo.Api/`):

- `AlertEndpoints.cs`, `Program.cs`

Tests (`backend/tests/`):

- `Salvo.Domain.Tests/AlertPolicyTests.cs`, `Salvo.Domain.Tests/AlertTests.cs`
- `Salvo.Domain.Tests/ArchitectureSmokeTests.cs`, `Salvo.Domain.Tests/ScoringRunTests.cs`
- `Salvo.Api.IntegrationTests/AlertCreationTests.cs`,
  `Salvo.Api.IntegrationTests/AlertReviewTests.cs`,
  `Salvo.Api.IntegrationTests/AlertSchemaTests.cs`,
  `Salvo.Api.IntegrationTests/AlertEndpointTests.cs`,
  `Salvo.Api.IntegrationTests/AlertTestCorpus.cs`,
  `Salvo.Api.IntegrationTests/SalvoDbContextFactoryTests.cs`
- `Salvo.Api.IntegrationTests/SalvoApiFactory.cs`,
  `Salvo.Api.IntegrationTests/ScoringRunPersistenceTests.cs`,
  `Salvo.Api.IntegrationTests/RiskEvaluationTests.cs`

No se tocó `Directory.Packages.props`, ningún lockfile, `frontend/`, el Blueprint, el Progress ni el
Workboard.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E4B-ALERTAS.md` | Brief válido: once secciones completas, commit base resuelto a `05ddb4a`, sin solapamiento de paths, sin contradicción con `AGENTS.md` ni el Blueprint |
| `dotnet ef migrations add AlertsAndReview` | Migración `20260902204944_AlertsAndReview` creada |
| `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration» |
| `dotnet test Salvo.slnx` | 55 tests de dominio + 54 de integración, todos verdes (antes: 40 + 32) |
| Test de carrera sobre base en archivo (`TwoConcurrentReviewsLeaveOneVerdictAndExactlyOneAudit`) | Un `200` y un `409`; exactamente un `AlertReview`; el estado final coincide con el veredicto ganador. Repetido cinco veces seguidas, verde en todas |
| Token de concurrencia aislado (`AStaleVerdictLosesToTheConcurrencyTokenEvenWithoutTheAuditIndex`) | El segundo `SaveChangesAsync`, que sólo actualiza la alerta, lanza `DbUpdateConcurrencyException` |
| Test de escalada (`AnEscalationAfterABackfillOpensANewAlertLinkedToTheReviewedOne`) | 60 `MEDIUM` revisada → 100 `CRITICAL` con `supersedesAlertId` apuntando a la anterior |
| Test de banda estable (`AHigherScoreInsideTheSameBandOpensNothing`) | 90 → 100, ambas `CRITICAL`: `alertsCreated = 0`, `alertsSkippedReviewed = 1` |
| Test de distribución del corpus demo | 18 alertas: 13 `MEDIUM`, 0 `HIGH`, 5 `CRITICAL` |
| Segunda corrida sin cambios | `alertsCreated = 0`, `alertsSkippedOpen = 18` |
| `GET /api/alerts` sin `isFraudLabel` | Ningún nombre de campo del listado ni del detalle contiene «label»; un parámetro desconocido `isFraudLabel=true` devuelve exactamente la misma respuesta |
| `./scripts/check.sh` | **Verde**, código de salida `0`: restore bloqueado, build Release con 0 advertencias y 0 errores, migraciones sin cambios pendientes, 109 tests .NET, `npm run check` y build de producción Next.js 16.3.3 |
| `git status --porcelain` | Limpio tras el commit; todos los archivos dentro de los paths autorizados |
| `git diff --check` | Pasa |

### Decisiones y supuestos

- **`AlertReview` no atribuye la decisión a nadie.** Sin autenticación no hay identidad de revisor,
  y fabricar un campo de identidad produciría un rastro de auditoría falso. Queda declarado, como
  pide el brief; el campo pertenece al trabajo post-MVP de autenticación.
- **`Alert.Severity` resuelve la política por versión, no por la política vigente.**
  `AlertPolicy.ForVersion(AlertPolicyVersion)` es lo que hace que `alertPolicyVersion` sirva para
  algo: una alerta abierta bajo `e4-v1` conserva su banda aunque exista una `e4-v2`. `AlertPolicy`
  expone un registro `All` con las versiones conocidas.
- **`AlertPolicy` tiene constructor público.** `RuleConfig` usa constructor privado, pero
  `Validate()` sólo es comprobable si se puede construir una política inválida. Las bandas
  aprobadas siguen viviendo en el singleton `AlertPolicy.E4V1` y la validación estructural ocurre en
  `Validate`, no en el constructor, para que el fallo sea el del arranque y no el de un
  `TypeInitializationException`.
- **El filtro por severidad se traduce a rangos de score.** Al no ser columna, `EfAlertStore`
  construye el predicado desde las bandas de cada `AlertPolicy` conocida, emparejando versión y
  rango. Con una sola política registrada degenera en un `WHERE` simple.
- **Orden del listado: `createdAt` descendente y luego `id`.** Es un orden determinista y estable
  para paginar. El orden del feed por score vigente es Etapa 5 y queda fuera; el listado ya devuelve
  ambos scores y el indicador de divergencia para que E5 pueda ordenarlo sin cambiar el contrato.
- **La divergencia se calcula contra la evaluación vigente del pedido.** Si un pedido no tuviera
  evaluación vigente —imposible una vez que existe una alerta, porque la corrida cubre todo el
  corpus— no se reporta divergencia y `currentScore` queda nulo, para no bloquear revisiones
  legítimas por ausencia de dato.
- **Sin `acknowledgedDivergence`, sólo se bloquea la transición desde `OPEN`.** Repetir el mismo
  veredicto con la misma nota sobre una alerta ya revisada devuelve `200` sin comprobar divergencia:
  la decisión ya está tomada y el reintento no la cambia.
- **Pedir un `newStatus` que no es veredicto es `400`, no `409`,** incluso sobre una alerta ya
  revisada. `OPEN` no es un veredicto y nada reabre una alerta.
- **`SalvoApiFactory` conserva un único constructor público.** xUnit exige exactamente uno en un
  class fixture, así que la base en archivo se obtiene con `SalvoApiFactory.WithFileDatabase()`. Los
  tests existentes siguen usando la conexión `:memory:` compartida y no cambian de comportamiento.
- **El test de carrera fuerza el entrelazado con un decorador de `IAlertStore`.** Ambas requests
  terminan de leer antes de que cualquiera escriba, y las escrituras se serializan. Sin eso el
  entrelazado dependería del planificador y el test sería inestable en vez de una prueba. Se hizo al
  primer intento; no fue necesario invocar la excepción de esfuerzo del brief.
- **`SalvoDbContextFactory` no incorpora `Microsoft.Extensions.Configuration`.** Ese paquete no está
  entre las dependencias del proyecto y añadirlo habría exigido consulta previa y regenerar
  lockfiles. La fábrica lee la variable de entorno y, si falta, el `appsettings` con
  `System.Text.Json`, que ya está en la BCL. La lógica vive en un método público y puro,
  `ResolveConnectionString`, cubierto por cinco tests, uno de los cuales resuelve contra el
  `appsettings.json` real de `Salvo.Api`.
- **`RiskSignalSerializer.Deserialize`.** El detalle presenta las señales del snapshot como objetos,
  no como texto crudo. La lectura se añadió junto a la escritura canónica para que ambas se
  mantengan juntas; nunca se usa para recalcular un fingerprint, que sigue derivándose del texto
  almacenado.
- **`RuleConfig`, `TemporalRiskEngine`, `Order`, el importador, el seed, las métricas de E3 y la
  migración de E4A no se tocaron.** Se consumen tal como están.

### Riesgos o pendientes

- **Dos tests previos cambiaron de premisa, ambos por obsolescencia declarada.**
  `RiskEvaluationTests` (`…WithoutPersistenceEffects`) afirmaba que no existía ninguna tabla cuyo
  nombre contuviera «alert» —es decir, codificaba «E4B todavía no ocurrió»—. Esa cláusula se
  reemplazó por una comprobación más fuerte y vigente: el camino de métricas sigue sin escribir
  nada, con `alerts` y `alert_reviews` en cero filas además de las tres tablas de E4A.
  `ScoringRunTests` y las llamadas directas a `SaveRunAsync` se ampliaron con los argumentos nuevos.
- **El token de concurrencia y el único de `alert_reviews` se solapan en la práctica.** Como los
  estados son terminales, toda carrera de revisión choca también con `UNIQUE(alert_reviews.alert_id)`,
  y EF ejecuta el `INSERT` antes del `UPDATE`, de modo que en el camino HTTP la excepción que llega
  es la de unicidad. Ambas se mapean a `409` y la transacción revierte el `UPDATE` de la alerta, así
  que el comportamiento observable es el prometido. El token se verifica de forma aislada en
  `AStaleVerdictLosesToTheConcurrencyTokenEvenWithoutTheAuditIndex`, donde no hay fila de auditoría
  en juego. Es defensa en profundidad deliberada, no redundancia accidental.
- **Los corpus sintéticos de los tests fijan scores exactos.** Están construidos para que sólo el
  pedido bajo prueba cruce el umbral, y cada helper documenta de qué reglas sale su score. Cambiar
  `RuleConfig` los rompería de forma ruidosa, que es lo deseable.
- **`POST /api/risk-evaluations:run` recalcula el corpus completo y ahora también relee el estado de
  alertas de los pedidos marcados.** Con 300 pedidos y 18 marcados es trivial. Riesgo de volumen ya
  declarado en `E4-DISENO.md`, no introducido aquí.
- **`amountAtRisk`, el orden del feed y toda la UI quedan fuera**, como fija el brief: son Etapa 5.
  Tampoco se crearon las columnas de explicabilidad de Etapa 7.
- **`AGENTS.md` §«Estado actual» describe `E4B-ALERTAS` como pendiente.** Actualizarlo corresponde
  al coordinador tras la integración; está fuera de los paths autorizados de esta tarea.
- Sin dependencias nuevas, sin cambios en `Directory.Packages.props` ni en lockfiles, sin escrituras
  externas, sin `git push`, sin PR y sin borrar ni recrear ninguna base local.

### Integración

- Orden sugerido: rama única; `E4A-PERSISTENCIA` ya está integrada en `main` y es su única
  dependencia.
- Migraciones o pasos manuales: `20260902204944_AlertsAndReview` es aditiva. Crea `alerts` y
  `alert_reviews` y añade `alerts_created`, `alerts_skipped_open` y `alerts_skipped_reviewed` a
  `scoring_runs` con `defaultValue: 0`, más el check `ck_scoring_runs_alert_counts`. Una base local
  existente se actualiza con `dotnet ef database update` sin recrearse ni perder datos. Tras la
  corrección de la fábrica de diseño, ese comando ya migra la base que la API abre
  (`ConnectionStrings:SalvoDb`, `salvo.db` por defecto) y no `salvo.design.db`; si alguien migró
  antes contra `salvo.design.db`, ese archivo queda huérfano y puede ignorarse.
- Posibles conflictos: ninguno esperado. La rama parte de la punta de `main` y no hay otra tarea
  activa sobre los mismos paths.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main`, y confirmar que el
  test dorado de fingerprints de E4A y la distribución 18 / 13 / 5 del corpus demo siguen verdes
  sobre el estado integrado, según el protocolo de `Coordination/README.md`.
