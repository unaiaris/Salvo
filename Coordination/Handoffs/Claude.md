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

## `E5A-API-LECTURA` — Superficie de lectura de la consola

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 5
- Rama/worktree: `claude/e5a-api-lectura`
- Commit base: `3081bb3` (`chore: assign E5A-API-LECTURA`, punta de `main`)
- Commit final: `513a980` (implementación completa; esta entrada de handoff se agrega en el
  commit siguiente, que queda como punta de la rama)
- Fecha: 2026-09-03

### Resultado

La API expone lo que la consola necesita leer. `GET /api/dashboard` devuelve la corrida vigente,
los pedidos pendientes de puntuar, las alertas abiertas por severidad derivada, el monto en riesgo
por moneda, el fraude reportado por pedido distinto, la tasa de marcado, el riesgo temporal en
cubetas semanales de `America/Montevideo` y las reglas detrás de la cola abierta; ningún campo sale
de `OrderEvaluationLabel`. `GET /api/evaluation-metrics` es la superficie de calidad, registrada
solo con `DemoData:Enabled`, con manejador propio sobre la corrida persistida, `unlabeledOrders` en
lugar de excepción y `409 METRICS_UNAVAILABLE` cuando no hay nada que medir.
`GET /api/system/capabilities` publica `demoDataEnabled`. `GET /api/alerts` gana `sort=SCORE_DESC`
—score local de la evaluación vigente, con el `JOIN` dentro de la consulta y antes de `Skip`/`Take`—,
más `scoringRunSequence` y `currentRun`; `AlertDetail` también rotula su corrida vigente.

### Archivos modificados

- `backend/src/Salvo.Api/DashboardEndpoints.cs`, `EvaluationMetricsEndpoints.cs`,
  `SystemEndpoints.cs` (nuevos); `AlertEndpoints.cs` y `Program.cs` (modificados).
- `backend/src/Salvo.Application/Dashboard/`: `IDashboardReader.cs`, `DashboardViews.cs`,
  `GetDashboardHandler.cs` (nuevos).
- `backend/src/Salvo.Application/Metrics/`: `IEvaluationMetricsReader.cs`,
  `EvaluationMetricsViews.cs`, `GetEvaluationMetricsHandler.cs`,
  `EvaluationMetricsUnavailableException.cs` (nuevos).
- `backend/src/Salvo.Application/Alerts/`: `AlertSortOrder.cs` y `ScoringRunReference.cs` (nuevos);
  `IAlertStore.cs`, `ListAlertsHandler.cs`, `AlertPage.cs`, `AlertContext.cs`, `AlertViews.cs` y
  `AlertProjection.cs` (modificados).
- `backend/src/Salvo.Infrastructure/Persistence/`: `EfDashboardReader.cs` y
  `EfEvaluationMetricsReader.cs` (nuevos); `EfAlertStore.cs` (modificado).
  `backend/src/Salvo.Infrastructure/DependencyInjection.cs` (modificado).
- `backend/tests/Salvo.Api.IntegrationTests/`: `DashboardEndpointTests.cs`,
  `EvaluationMetricsEndpointTests.cs`, `SystemCapabilitiesTests.cs`, `AlertFeedOrderTests.cs`
  (nuevos); `SalvoApiFactory.cs`, `FoundationTests.cs` y `AlertReviewTests.cs` (modificados).
- Sin cambios en `frontend/`, en migraciones, en `Directory.Packages.props` ni en lockfiles.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | Verde, exit `0`: restore bloqueado, build Release con 0 advertencias y 0 errores, `No changes have been made to the model since the last migration.`, 127 tests .NET, `npm run check` y `next build --webpack` |
| `dotnet test` | 127 verdes (55 dominio + 72 integración); los 109 previos siguen pasando, 18 nuevos |
| `TheDashboardIsIndependentOfGroundTruth` | Verde. Invierte las 300 `is_fraud_label` con `UPDATE order_evaluation_labels SET is_fraud_label = 1 - is_fraud_label` y exige respuesta idéntica; afirma además que se afectaron 300 filas para que no pase en vacío |
| Falsación del test diferencial | **Falla cuando debe.** Ver abajo |
| `AmountAtRiskIsPerCurrencyAndCarriesNoTotal` | Verde: `BRL`, `USD`, `UYU`, seis alertas cada una, y cada entrada tiene exactamente `currencyCode`, `amountCents`, `alertCount` |
| `ReportedFraudCountsAnEscalatedOrderOnce` | Verde: dos alertas `REPORTED_FRAUD` del mismo pedido, un solo importe y `orderCount = 1` |
| `OrdersWithoutGroundTruthAreCountedInsteadOfFailingTheRequest` | Verde: `200` con 304 puntuados, 300 etiquetados, `unlabeledOrders = 4` |
| `WithoutAScoringRunTheMetricsAreUnavailableRatherThanAServerError` y `WithoutGroundTruthAtAllTheMetricsAreUnavailable` | Verde: `409` con código `METRICS_UNAVAILABLE` |
| `WithoutDemoDataTheSeedAndTheMetricsDoNotExist` | Verde: con `DemoData:Enabled = false`, `404` en seed y métricas, `demoDataEnabled: false`, y `/api/dashboard` y `/api/alerts` siguen en `200` |
| `ScoreDescOrdersTheWholeFeedBeforeItIsPaged` | Verde: cuatro páginas de cinco reconstruyen exactamente el orden de la página única, sin solapamiento |
| Sonda temporal de SQL (eliminada) | El comando real de `SCORE_DESC` es `LEFT JOIN (…run_evaluations INNER JOIN risk_evaluations… WHERE run_id = @id) ON … ORDER BY "s"."score" DESC, "a"."created_at_utc" DESC, "a"."id" LIMIT @p OFFSET @p`: el `JOIN` precede al `LIMIT`/`OFFSET` |
| `git status --porcelain` | Limpio tras el commit; ningún path fuera de `backend/**` |

**Cómo se comprobó que el test diferencial falla cuando debe.** Se inyectó de forma temporal en
`EfDashboardReader.GetCurrentEvaluationsAsync` exactamente la fuga que describe el hallazgo 4: un
`join` con `dbContext.OrderEvaluationLabels` que hace `IsFlagged` dependiente de la etiqueta. Ningún
tipo de `Salvo.Application` cambia con esa edición. Resultado:

- `TheDashboardIsIndependentOfGroundTruth` **falló**, con el diff exacto
  `Expected: …"flagRate":0.06,… / Actual: …"flagRate":0,…` en la posición 497 de la respuesta.
- `TheDashboardContractNeverExposesGroundTruth` —el test de nombres de propiedad— **pasó igual** con
  la fuga presente, lo que confirma que el diferencial es el único de los dos que cierra la vía.

La fuga se revirtió desde una copia previa; `grep -rn "OrderEvaluationLabels" EfDashboardReader.cs`
devuelve cero coincidencias y la compuerta se ejecutó sobre el estado revertido.

### Decisiones y supuestos

- **`reportedFraud` usa `orderCount`, no `alertCount`.** El brief pide «misma forma» que
  `amountAtRisk`; se conservó la forma `{ currencyCode, amountCents, … }` pero se nombró el contador
  por lo que realmente cuenta. Llamarlo `alertCount` describiría mal el agregado que el propio
  criterio exige. El `DISTINCT` se hace en SQL sobre `order_id`.
- **`scoringRunSequence` y `currentRun.sequence` conviven aunque coincidan.** No es redundancia
  accidental: `scoringRunSequence` es el testigo con el que un cliente detecta que el corpus cambió
  entre dos páginas, y `currentRun` es la procedencia que toda pantalla de datos declara. El brief
  pide ambos de forma explícita.
- **`riskOverTime` emite semanas contiguas, rellenando con ceros las vacías**, y fija el lunes local
  como inicio de semana. Una serie con huecos haría que una semana sin pedidos se leyera como una
  barra más baja junto a su vecina en vez de como un hueco.
- **`openAlerts.bySeverity` publica las tres bandas de la política vigente, incluidas las de conteo
  cero** —el corpus demo tiene 0 `HIGH`—, ordenadas de mayor a menor severidad. La severidad se
  deriva siempre con la versión de política con la que se abrió cada alerta.
- **El barrido se colapsa al umbral más alto de cada matriz idéntica**, no al más bajo. Es la única
  dirección que garantiza que el umbral seleccionado esté entre las filas devueltas, porque
  `RiskMetricsEvaluator.SelectBest` desempata hacia el umbral mayor. El test lo afirma.
- **El manejador de métricas reutiliza `IEvaluationLabelReader`** en vez de crear un segundo puerto
  de etiquetas, y su propio puerto (`IEvaluationMetricsReader`) no las expone. `IDashboardReader` es
  deliberadamente un puerto aparte que no puede devolver verdad de campo.
- **`SalvoApiFactory` gana la propiedad `DemoDataEnabled`** en lugar de una segunda fábrica estática:
  xUnit exige un único constructor público para las fixtures, y el patrón sigue al de
  `ConfigureTestServices`, que ya se fija antes de resolver el primer cliente.
- **Los instantes persistidos se truncan a milisegundos** (ISO 8601 de 24 caracteres). El
  `ScoringRunSummary` que vuelve en memoria es más fino que lo que se relee de la base, así que el
  test compara `completedAt` truncado. No es un defecto del dashboard sino la precisión del
  almacenamiento, ya fijada en E2.
- Se añadió a `FoundationTests` la comprobación de que las tres rutas nuevas y `amountAtRisk`,
  `scoringRunSequence` y `currentRun` aparecen en `/openapi/v1.json`: `E5B` genera sus tipos desde
  ese documento y una ruta ausente allí sería una ruta que la UI no puede llamar.
- Sin dependencias nuevas, sin migraciones, sin escrituras externas, sin `git push` ni PR, y sin
  tocar `EvaluateLocalRiskHandler`, `RiskMetricsEvaluator`, `TemporalRiskEngine`, `RuleConfig`, el
  fingerprint ni la semántica de alertas de E4B.

### Riesgos o pendientes

- **`GET /api/dashboard` materializa las evaluaciones de la corrida vigente en memoria** —300 filas
  reducidas a `occurredAt` y un booleano— para calcular tasa de marcado y cubetas semanales. Es
  trivial en el volumen del MVP; con un corpus grande convendría agregar en SQL. Riesgo de volumen
  ya declarado, no introducido aquí.
- **El brief del Workboard no reserva `backend/tests/**`** aunque el brief de la tarea sí lo
  autoriza. No hubo colisión porque `E5A` es la única fila activa, pero conviene alinearlo antes de
  despachar `E5B` y `E5C`.
- **El *Contexto obligatorio* del brief no cita la entrada del Progress** («Etapa 5 — UI y
  dashboard»); `/brief-check` lo reportó como desviación menor, no bloqueante.
- **La corrección de §4.4 del Blueprint y de la línea de Recharts en `AGENTS.md`** que exige el
  diseño v2 sigue pendiente: es estado canónico, del coordinador.
- El rótulo obligatorio de la sección de calidad (D1) es de UI y corresponde a `E5C`; la API solo
  devuelve las cifras.

### Integración

- Orden sugerido: rama única, sin dependencias. `E5B-ALERTAS-UI` la requiere integrada.
- Migraciones o pasos manuales: ninguno. La tarea es de lectura;
  `dotnet ef migrations has-pending-model-changes` no reporta cambios y el modelo no se tocó.
- Posibles conflictos: la rama parte de la punta de `main` y no hay otra tarea activa. `E5A` toma
  `IAlertStore.GetPageAsync`, `ListAlertsHandler`, `AlertPage`, `AlertContext`, `AlertViews` y
  `AlertProjection`, que son código de E4B; el cambio es el que fija el diseño v2 (D6, D12) y está
  reservado en el brief.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` y confirmar que sigue
  verde el test diferencial de etiquetas y la distribución 18 / 13 / 5 del corpus demo, según el
  protocolo de `Coordination/README.md`.

## `E5B-ALERTAS-UI` — Cliente tipado, feed de alertas, detalle y revisión

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 5
- Rama/worktree: `claude/e5b-alertas-ui`
- Commit base: `35e6b14` («chore: assign E5B-ALERTAS-UI»), punta de `main` al empezar
- Commit final: `1774277`
- Fecha: 2026-09-03

### Resultado

Una analista abre `/alerts`, ve la cola de alertas abiertas ordenada por score local de la evaluación
vigente y con la corrida de procedencia en cabecera; entra a `/alerts/[id]`, lee el snapshot que abrió
la alerta y la evaluación vigente como dos bloques separados, cada uno con su propia procedencia; y
emite un veredicto que queda registrado, o recibe en castellano el motivo exacto por el que no se
pudo. Ningún campo que la API no proyecte llega al navegador, y `npm run build` pasa con la API
apagada dejando las rutas de datos como dinámicas.

Se ejecutó en dos commits: `6d087f2` dejó la capa de datos (captura de OpenAPI, tipos generados,
guardas, cliente `server-only`, catálogo de mensajes y formateadores) y `1774277` la superficie de
aplicación con todos los tests.

### Archivos modificados

Contrato y generación de tipos:

- `frontend/openapi/salvo-openapi.json` — documento OpenAPI capturado y versionado.
- `frontend/scripts/capture-openapi.mjs` — recaptura el documento desde una API levantada.
- `frontend/scripts/check-openapi-types.mjs` — detecta deriva sin abrir ningún puerto.
- `frontend/src/lib/api/schema.d.ts` — generado por `openapi-typescript`, versionado.
- `frontend/package.json`, `frontend/package-lock.json` — `openapi-typescript@7.13.0` fijada y
  scripts `api:capture`, `api:types`, `api:types:check`.
- `scripts/check.sh` — `api:types:check` como primer paso de la compuerta.

Capa de datos:

- `frontend/src/lib/api/contract.ts`, `failures.ts`, `guards.ts`, `server-client.ts`, `alerts.ts`,
  `messages.ts`, `taint.ts`
- `frontend/src/lib/format.ts`
- `frontend/next.config.ts` — `experimental.taint`.

Aplicación:

- `frontend/src/app/layout.tsx`, `page.tsx`, `frontend/src/components/*` (cabecera, badge de
  severidad, aviso de fallo, procedencia).
- `frontend/src/app/alerts/page.tsx`, `alert-table.tsx`, `empty-states.tsx`
- `frontend/src/app/alerts/[id]/page.tsx`, `evaluation-blocks.tsx`, `order-block.tsx`,
  `review-panel.tsx`, `review-form.tsx`, `review-action.ts`, `review-state.ts`, `divergence.ts`

Tests e infraestructura de test:

- `frontend/src/test/boundary.test.ts`, `server-tree.ts`, `fixtures.ts`, `server-only-stub.ts`
- `frontend/src/lib/api/guards.test.ts`, `messages.test.ts`, `server-client.test.ts`
- `frontend/src/app/alerts/page.test.tsx`, `frontend/src/app/alerts/[id]/*.test.ts(x)`
- `frontend/src/app/page.test.tsx`, `frontend/vitest.config.mts`, `frontend/vitest.setup.ts`

Sin cambios en `backend/`: `git diff --stat 35e6b14..HEAD` no lista ningún path bajo `backend/`.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E5B-ALERTAS-UI.md` | Brief válido: 11 secciones completas, base `35e6b14` existente, sin solapamiento de paths, sin contradicción con `AGENTS.md` ni el Blueprint |
| `SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build` | Pasa; `/` y `/_not-found` estáticas (`○`), `/alerts` y `/alerts/[id]` dinámicas (`ƒ`). Salida completa abajo |
| `npm run api:types:check` | «OpenAPI types are up to date». Falsado: con una línea agregada a mano en `schema.d.ts` sale con código 1 y el mensaje «does not match the captured OpenAPI document» |
| `npx vitest run src/test/boundary.test.ts` | 6/6. Falsación documentada abajo |
| `npx vitest run src/lib/api/guards.test.ts` | 21/21 |
| `npx vitest run src/lib/api/messages.test.ts` | 11/11 |
| `npx vitest run src/lib/api/server-client.test.ts` | 12/12 |
| `npx vitest run "src/app/alerts/[id]/review-form.test.tsx"` | 9/9. Falsado: sembrando los campos con `""` en vez de con lo enviado, fallan «la nota sobrevive a un 409» y «mantiene marcado el reconocimiento tras un conflicto» |
| `npm run check --prefix frontend` | Typecheck ✓, ESLint `--max-warnings=0` ✓, Vitest 97/97 en 11 archivos |
| `./scripts/check.sh` | **Verde**, exit `0`: restore bloqueado, build Release 0 advertencias / 0 errores, sin cambios de modelo pendientes, 127 tests .NET (55 dominio + 72 integración), `npm run check` y build de producción de Next.js |
| `git status --porcelain` | Limpio; ningún path de `backend/` tocado |

#### Salida de `next build` con la API apagada

Ejecutado con `SALVO_API_BASE_URL=http://127.0.0.1:9`, un puerto sin nada escuchando:

```
▲ Next.js 16.3.3 (webpack)
✓ Running next.config.ts took 55ms
- Experiments (use with caution):
  ✓ taint

  Creating an optimized production build ...
✓ Compiled successfully in 1328ms
  Running TypeScript ...
  Finished TypeScript in 1226ms ...
  Collecting page data using 6 workers ...
  Generating static pages using 6 workers (0/3) ...
✓ Generating static pages using 6 workers (3/3) in 253ms
  Finalizing page optimization ...
  Collecting build traces ...

Route (app)
┌ ○ /
├ ○ /_not-found
├ ƒ /alerts
└ ƒ /alerts/[id]


○  (Static)   prerendered as static content
ƒ  (Dynamic)  server-rendered on demand
```

Las dos rutas de datos aparecen como `ƒ`. La portada queda estática a propósito: no lee la API, y
que siga en `○` es la prueba de que `force-dynamic` está puesto donde hace falta y no en todas
partes.

#### Falsación del test de la frontera servidor–cliente

El test viejo —«el HTML no contiene `isFraudLabel`»— pasaba vacuamente, porque la API no emite ese
campo y ninguna fuente podía producir el texto. Se reemplazó por `src/test/boundary.test.ts`, que
recorre el árbol real de la página (`AlertDetailPage` y `AlertsPage`), invoca los componentes de
servidor, se detiene en los de cliente y afirma dos cosas sobre lo que cruza:

1. **Forma**: toda prop que recibe un componente cliente es primitiva.
2. **Contenido**: con `fetch` simulado devolviendo `isFraudLabel: true`, `internalNotes` y
   `labelSource` en cada objeto del payload, ninguno aparece ni en las props ni en el render.

El registro de componentes cliente se descubre leyendo `src/` en busca de módulos `"use client"`, no
se mantiene a mano, y un test aparte exige que el descubrimiento no venga vacío.

**Se comprobó que falla cuando debe**, en dos pasos:

- Pasando `detail={detail}` a `ReviewForm` (con la guarda intacta) falla la afirmación de forma:

  ```
  AssertionError: ReviewForm recibe la prop no primitiva «detail»: {"id":"2f2b7f3e-…","order":{…},
  "snapshot":{…},"currentEvaluation":{…},"divergence":{…},"review":null}: expected false to be true
  ```

  La afirmación de contenido **no** falla en este paso, y es correcto: la guarda ya había descartado
  los campos desconocidos. Es lo que separa las dos protecciones.

- Debilitando además `projectAlertDetail` a un `return { ...raw }` —una guarda-predicado como la de
  `health.ts`— falla también la de contenido:

  ```
  AssertionError: expected '[{"alertId":"2f2b7f3e-…' not to contain 'isFraudLabel'
  ```

Con el código restaurado, las seis vuelven a pasar. Es la misma técnica que el test diferencial de
`E5A`: no depende de saber por dónde entraría la fuga.

### Decisiones y supuestos

- **La deriva de tipos se verifica contra un documento capturado.** `frontend/openapi/salvo-openapi.json`
  se capturó de la API real con `DemoData__Enabled=true` —si no, `/api/demo-data/seed` y
  `/api/evaluation-metrics` no se mapean y el documento no sería el superconjunto que `E5C` necesita—
  y el `servers` se normaliza a `/` porque de otro modo arrastraría el puerto de la captura. La
  compuerta regenera y diffea, sin abrir ningún puerto.
- **Las vistas se derivan de los tipos generados**, no se escriben a mano: `ApiView<T>` toma
  `components["schemas"][…]` y estrecha a `number` los enteros. ASP.NET Core 10 declara todo entero
  como `["integer","string"]`, así que la forma de cadena decimal es parte del contrato y las guardas
  la aceptan y la normalizan. Como consecuencia, un campo que la API agregue o quite rompe la
  compilación en la guarda, que es donde conviene que rompa.
- **La cola muestra `status=OPEN`.** El feed es la cola de revisión y el tercer estado vacío está
  redactado como «sin alertas abiertas»; una alerta revisada sale de la cola y se sigue viendo por su
  detalle. No se agregó filtro de estado: el brief no lo pide.
- **`GET /api/orders` se consulta solo en el camino vacío**, con `pageSize=1`, para distinguir «sin
  pedidos» de «con pedidos y sin corrida». El feed de alertas no puede separarlos por sí solo.
- **Los campos del formulario se remontan por `submissionId`.** Controlarlos no alcanzaba: React 19
  resetea el `<form action>` al terminar la acción escribiendo el DOM directamente, y para un radio o
  una casilla React no vuelve a escribirlos porque su estado no cambió, de modo que la casilla queda
  visualmente vacía mientras el componente la cree marcada. Se verificó en jsdom antes de cambiar el
  enfoque.
- **La acción de servidor se importa en el componente cliente**, no se pasa como prop, para que las
  props sigan siendo primitivas; el id de la alerta viaja en un campo oculto.
- **`experimental.taint` quedó habilitado** y marca la respuesta cruda en `server-client.ts`. Se
  verificó primero con un build de prueba: la bandera cambia el canal de React del directorio `app`,
  y `experimental_taintObjectReference` se lee del namespace en vez de importarse por nombre, porque
  React 19.2.8 —el que resuelve Vitest— no lo exporta. Es respaldo, no el mecanismo: el mecanismo es
  que los componentes cliente reciben primitivas.
- **Formato con `es-UY`, zona `America/Montevideo` y hora de 24 h.** La zona es la misma
  `RuleConfig.BusinessTimeZone` con la que las reglas leen el día; las 24 h se fijaron explícitamente
  para no obligar a la analista a resolver «8:41 p. m.» contra un log de auditoría.
- **Sin dependencias nuevas más allá de `openapi-typescript@7.13.0`.** Los tests de formulario usan
  `fireEvent` en lugar de `@testing-library/user-event`, que no está instalado y no está autorizado.
- Vitest resuelve `server-only` a un stub para poder importar los módulos; a cambio,
  `boundary.test.ts` lee el código fuente y exige que `import "server-only";` siga presente en
  `server-client.ts` y `alerts.ts`, y que ningún componente cliente los importe.

### Riesgos o pendientes

- **El documento OpenAPI capturado puede quedar atrás respecto de la API.** La compuerta detecta la
  deriva entre el documento y los tipos, no entre la API y el documento. La red de contención es
  `tsc`, porque las guardas proyectan cada clave obligatoria por nombre, más `npm run api:capture` al
  cambiar el contrato. El cierre limpio sería un test de integración que compare el documento servido
  por `WebApplicationFactory` contra el archivo versionado, pero vive en `backend/tests/**`, que esta
  tarea no tiene autorizado; `E5C` sí lo reserva y es el lugar natural.
- **`SCORING_RUN_CONFLICT` y `METRICS_UNAVAILABLE` todavía caen en el mensaje genérico.** Son de D13
  pero pertenecen a endpoints de `/import` y `/dashboard`; agregarlos ahora sería código muerto sin
  pantalla que los produzca. `E5C` debe sumarlos a `messages.ts` y a `messages.test.ts`.
- **`frontend/src/lib/api/health.ts` quedó intacto** y ya no lo usa ninguna pantalla. Conserva el
  patrón de URL relativa y sigue sin timeout explícito, lo que roza la regla de `AGENTS.md` sobre red
  externa. Es evidencia de la Etapa 1 y borrarla o reescribirla excede este brief; conviene decidirlo
  al cerrar la Etapa 5.
- **La accesibilidad se verificó por estructura, no con un lector de pantalla**: encabezados de tabla
  con `scope`, `caption`, regiones rotuladas con `aria-labelledby`, `role="alert"` y `role="status"`
  en los avisos, foco visible en todo lo interactivo y ningún significado confiado solo al color. Una
  pasada real con lector de pantalla queda para la Etapa 8.
- **El recorrido completo sigue sin smoke HTTP.** Vitest resuelve el árbol de servidor a mano, que es
  fiel pero no es el framework: `revalidatePath` y la serialización RSC real no se ejercitan.
  `scripts/smoke-ui.sh` es criterio de aceptación de `E5C` (D15) y es lo que cierra ese hueco.
- **Siguen pendientes**, del coordinador: la corrección de §4.4 del Blueprint sobre dónde se dispara
  el scoring, y la línea de `AGENTS.md` que todavía lista Recharts como stack de UI pese a la
  decisión 43.

### Integración

- Orden sugerido: rama única. Depende de `E5A-API-LECTURA`, ya integrada en `5f48db0`.
  `E5C-IMPORT-DASHBOARD` la requiere integrada.
- Migraciones o pasos manuales: ninguno. `dotnet ef migrations has-pending-model-changes` no reporta
  cambios y no se tocó el modelo. `npm ci` en `frontend/` incorpora `openapi-typescript@7.13.0`.
- Posibles conflictos: la rama reserva `frontend/**` y `scripts/check.sh` completos y no hay otra
  tarea activa. `scripts/check.sh` gana un paso al principio; `E5C` lo tocará también, para
  `smoke-ui.sh`.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main`, y además el build con
  la API apagada (`SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build --prefix frontend`)
  confirmando que `/alerts` y `/alerts/[id]` siguen listadas como `ƒ`. Para una comprobación con
  datos reales: levantar la API con `DemoData__Enabled=true`, hacer seed y corrida, y recorrer
  `/alerts` y un detalle.

## `E5C-IMPORT-DASHBOARD` — Importación con corrida, dashboard, gráfico SVG y smoke de recorrido

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 5
- Rama/worktree: `claude/e5c-import-dashboard`
- Commit base: `bb4cf620373a7c7dc4870efd6219e7f67ba0337a` (`chore: assign E5C-IMPORT-DASHBOARD`),
  punta de `main` al empezar y el commit que incorpora el brief, como declara la Identificación del
  task brief
- Commit final: `2db4722327818e967025da6f0dc72f9e35aeea9f`
- Fecha: 2026-09-03

### Resultado

El recorrido queda cerrado de punta a punta. Una analista entra a `/import`, carga el corpus de
demostración o importa un archivo, lee registro por registro qué se rechazó y por qué, y **ejecuta
la corrida de scoring desde la propia consola**; entra a `/dashboard` y ve el estado operativo de la
corrida vigente, con el monto en riesgo desglosado por moneda, el riesgo temporal como SVG escrito
por el servidor y, solo si la instancia se declara de demostración, la calidad del criterio con su
rótulo fijo. Todo eso se verifica con `scripts/smoke-ui.sh`, que levanta la API y `next start` de
verdad y comprueba las cuatro rutas en los tres escenarios del Blueprint.

Se ejecutó en cinco commits, para no repetir lo que pasó en `E5B`, donde un error de servidor cortó
la sesión sin checkpoint: `514b3ed` la capa de datos, `115a2a5` la pantalla de importación,
`83edbda` el dashboard, `4d7f32a` el test de deriva de OpenAPI y `2db4722` el smoke.

Con esto la Etapa 5 tiene entregados todos sus ítems. Declararla completa es del coordinador.

### Archivos modificados

Capa de datos (`frontend/src/lib/`):

- `api/console.ts` (nuevo) — lecturas y escrituras de `/import` y `/dashboard`.
- `api/contract.ts`, `api/guards.ts`, `api/guards.test.ts` — tipos y guardas que proyectan para
  dashboard, métricas, importación, corrida, semilla y capacidades.
- `api/messages.ts`, `api/messages.test.ts` — diecisiete códigos nuevos.
- `api/server-client.ts` — cuerpos `FormData` para la importación multipart.
- `format.ts` — etiqueta neutral de `amount_anomaly` y formateadores de porcentaje, cantidad y fecha
  de calendario.
- `api/health.ts` y `api/health.test.ts` — **eliminados**.

Pantalla de importación (`frontend/src/app/import/`, todo nuevo):

- `page.tsx`, `page.test.tsx`, `actions.ts`, `actions.test.ts`, `action-state.ts`,
  `action-outcome.tsx`, `action-section.tsx`, `import-form.tsx`, `corpus-actions.tsx`

Dashboard (`frontend/src/app/dashboard/`, todo nuevo):

- `page.tsx`, `page.test.tsx`, `panels.tsx`, `risk-chart.tsx`, `quality-section.tsx`,
  `empty-states.tsx`

Verificación:

- `backend/tests/Salvo.Api.IntegrationTests/OpenApiDriftTests.cs` (nuevo) — único archivo fuera de
  `frontend/**` y `scripts/**`, y el único path de `backend/tests/**` que el brief autoriza.
- `scripts/smoke-ui.sh` (nuevo)
- `frontend/src/test/boundary.test.ts`, `frontend/src/test/fixtures.ts`

Otros:

- `frontend/package.json` — solo el script `dev`.

`git diff --stat bb4cf62..HEAD`: 30 archivos, 3953 inserciones, 87 supresiones. Ningún path bajo
`backend/src/`; `package-lock.json` y `Directory.Packages.props` intactos: **cero dependencias
nuevas**.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E5C-IMPORT-DASHBOARD.md` | Brief válido: 11 secciones completas, base `bb4cf62` existente, sin solapamiento de paths, sin contradicción con `AGENTS.md` ni el Blueprint |
| `./scripts/check.sh` | **Verde**, exit `0`. Salida por paso abajo |
| `./scripts/smoke-ui.sh` | **Verde**, exit `0`: 21 comprobaciones, 0 fallas. Salida completa abajo |
| Test de deriva de OpenAPI | 2/2 en verde; **falsado dos veces**, detalle abajo |
| `SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build` | Pasa. `/` y `/_not-found` estáticas (`○`); `/alerts`, `/alerts/[id]`, `/dashboard` e `/import` dinámicas (`ƒ`) |
| `npx vitest run src/test/boundary.test.ts` | 9/9, con `/import` y `/dashboard` cubiertos |
| `git status --porcelain` | Limpio |
| `git diff --name-only bb4cf62..HEAD \| grep '^backend/src/'` | Sin resultados |

#### `./scripts/check.sh`

| Paso | Resultado |
| --- | --- |
| `api:types:check` | «OpenAPI types are up to date with openapi/salvo-openapi.json.» |
| `dotnet restore --locked-mode` | Restauración correcta, 6 proyectos |
| `dotnet build --configuration Release` | Compilación correcta, **0 advertencias, 0 errores** |
| `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration.» |
| `dotnet test` | **129 tests**: 55 de dominio y 74 de integración, 0 fallos. Eran 127; los dos nuevos son los de deriva |
| `npm run check` | Typecheck ✓, ESLint `--max-warnings=0` ✓, **153 tests de Vitest** en 13 archivos. Eran 97 |
| `npm run build` | Next.js 16.3.3 con Webpack ✓ |

#### Salida de `scripts/smoke-ui.sh`

```
Salvo — recorrido de la consola
API http://127.0.0.1:5199 · consola http://127.0.0.1:3199

Compilando la API y la consola…
Migrando las dos bases temporales…
Levantando la consola…

── Escenario: con datos
Listo: API responde en http://127.0.0.1:5199/health
Listo: consola responde en http://127.0.0.1:3199
Cargando el corpus de demostración y ejecutando la corrida…
Alerta de ejemplo: 0367f5d3-ebca-474e-adbd-f87c3b8b83f3
  ok     /                            «Consola antifraude»
  ok     /import                      «Importación y scoring»
  ok     /import                      «Todos los pedidos de la base están cubiertos»
  ok     /alerts                      «Cola de alertas»
  ok     /alerts/0367f5d3-…-f87c3b8b83f3 «Snapshot que abrió la alerta»
  ok     /alerts/0367f5d3-…-f87c3b8b83f3 «Evaluación vigente»
  ok     /dashboard                   «Monto en riesgo»
  ok     /dashboard                   «Fraude reportado»
  ok     /dashboard                   «Pedidos y denegados por semana»
  ok     /dashboard                   «Calidad del criterio»
  ok     /dashboard                   «no la calidad del criterio de detección»
  ok     /dashboard                   sin «3.942.246»

── Escenario: base vacía
Detenido: API (pid 50166)
Listo: API responde en http://127.0.0.1:5199/health
  ok     /import                      «todavía no hay ninguno en la base»
  ok     /alerts                      «Todavía no hay pedidos»
  ok     /dashboard                   «Todavía no hay pedidos»
  ok     /alerts/00000000-0000-4000-8000-000000000000 «Esta alerta ya no existe»

── Escenario: API apagada
Detenido: API (pid 50275)
  ok     /                            «Consola antifraude»
  ok     /import                      «No se pudo contactar a la API»
  ok     /alerts                      «No se pudo contactar a la API»
  ok     /alerts/00000000-0000-4000-8000-000000000000 «No se pudo contactar a la API»
  ok     /dashboard                   «No se pudo contactar a la API»

Recorrido verde: 21 comprobaciones, 0 fallas.
Detenido: consola (pid 50165)

Directorio de trabajo (no se borra nada): …/salvo-smoke-WzwKah
  base con datos: …/salvo-smoke-WzwKah/with-data.db
  base vacía:     …/salvo-smoke-WzwKah/empty.db
  registros:      …/salvo-smoke-WzwKah/api.log, …/salvo-smoke-WzwKah/web.log
Puerto 5199 libre.
Puerto 3199 libre.
```

La liberación de procesos y puertos se comprobó de tres maneras, no se asumió:

1. **Corrida verde**: ambos puertos libres, sin procesos huérfanos, como muestra la salida.
2. **Corrida en rojo**: se rompió a propósito una expectativa (`Cola de alertas` cambiada por un
   texto inexistente). Salió con código `1`, imprimió «Recorrido en rojo: 1 de 21 comprobaciones
   fallaron», detuvo API y consola, y dejó ambos puertos libres.
3. **Ctrl-C**: simulado como lo hace una terminal de verdad, con `SIGINT` al **grupo de procesos**.
   Murió `next start`, el script lo detectó («el proceso de consola murió durante el arranque»),
   detuvo la API por el trap y dejó los dos puertos libres y ningún proceso vivo.

Un detalle que sí conviene saber y quedó escrito junto al trap: un `kill -INT` dirigido **solo** al
pid del script no aborta la corrida. Bash difiere una señal atrapada hasta que termina el comando en
primer plano y, como el hijo no murió, sigue adelante; el trap `EXIT` corre igual al final. Se aisló
en un script de seis líneas para confirmar que es comportamiento de bash y no del smoke. Ctrl-C real
no tiene ese problema porque señaliza al grupo entero.

#### Falsación del test de deriva de OpenAPI

Se comprobó que **falla al alterar el documento versionado**, dos veces, restaurando el archivo byte
a byte después de cada una (`shasum -a 256` idéntico y `git status` limpio):

1. **Campo renombrado.** En `DashboardAmountAtRiskView` se cambió `amountCents` por
   `amountMinorUnits`, que es exactamente la forma de la deriva que preocupa: alguien renombra un
   campo en la vista y no recaptura. Falló la comparación profunda con el mensaje que incluye las
   tres instrucciones de recaptura:

   ```
   The OpenAPI document this API serves is not the one committed at …/frontend/openapi/salvo-openapi.json.

   Re-capture it:
     DemoData__Enabled=true dotnet run --project backend/src/Salvo.Api
     npm run api:capture --prefix frontend
     npm run api:types --prefix frontend
   ```

2. **Ruta eliminada.** Se borró `/api/dashboard` del documento. Falló antes, en la comparación de
   nombres de rutas, que es la mitad legible del fallo:

   ```
   Assert.Equal() Failure: Collections differ
                                            ↓ (pos 3)
   Expected: [···, "/api/alerts/{id}/review", "/api/demo-data/seed", ···]
   Actual:   [···, "/api/alerts/{id}/review", "/api/dashboard", ···]
   ```

Restaurado el documento, los dos tests vuelven a pasar.

#### Falsación de dos garantías del dashboard

- **Monto sin total.** Se agregó a `AmountAtRiskPanel` una fila «Total» con la suma de las tres
  monedas: falla el test con `expected … not to contain '3.942.246'`. La cifra es la suma real del
  corpus demo y es una cifra sin unidad.
- **Compuerta de capacidades.** Se hizo que `/dashboard` pidiera las métricas sin consultar
  `/api/system/capabilities`: falla el test que exige que con `demoDataEnabled: false` no aparezca la
  sección de calidad ni se llame al endpoint.

### Decisiones y supuestos

- **Las cuatro correcciones de arrastre**, con su justificación:
  - **`format.ts`.** `amount_anomaly` pasó de «Monto atípico para el comprador» a **«Monto atípico»**.
    `TemporalRiskEngine` usa la mediana del comprador cuando hay historia suficiente y **cae a la del
    comercio** cuando no, y el `detail` de la señal nombra cuál de las dos usó
    (`…x the {scope} median…`, `TemporalRiskEngine.cs:108`). El título viejo contradecía a la frase
    que tiene debajo en todos los pedidos que caían al comercio. Hay un test que exige el título
    neutral y que el viejo no esté.
  - **`messages.ts`.** Se agregaron `SCORING_RUN_CONFLICT` y `METRICS_UNAVAILABLE`, que `E5B` dejó sin
    mensaje propio, y además los quince códigos de transporte y de documento que puede emitir la
    importación —`FILE_REQUIRED`, `FILE_TOO_LARGE`, `TOO_MANY_RECORDS`, `UNSUPPORTED_MEDIA_TYPE`,
    `UNSUPPORTED_FORMAT`, `EMPTY_FILE`, `INVALID_ENCODING`, `INVALID_CSV`, `INVALID_JSON`,
    `INVALID_JSON_ROOT`, `MISSING_HEADER`, `INVALID_HEADER`, `DUPLICATE_HEADER`, `UNKNOWN_HEADER`— y
    `DEMO_DATA_CONFLICT` de la semilla. El brief pide que 413, 415 y 400 tengan mensaje propio, y D13
    pide un texto por código. El test que exigía la lista exacta se reescribió: el código que usaba
    como ejemplo de «desconocido» era justamente `SCORING_RUN_CONFLICT`, y ahora usa uno de la Etapa 6.
  - **`package.json`.** `dev` pasó de `next dev` a **`next dev --webpack`**, alineado con `build`. En
    Next.js 16 `next dev` usa Turbopack por defecto y `--webpack` está documentado para `dev`
    (`node_modules/next/dist/docs/01-app/03-api-reference/06-cli/next.md`, línea 69). Desarrollar
    sobre un bundler y verificar el build sobre otro deja sin cubrir toda una clase de errores que
    solo aparecen al construir; la compuerta usa Webpack por la restricción de puertos que quedó
    registrada como riesgo de la Etapa 1.
  - **`health.ts`.** **Eliminado, con su test.** Era código muerto —ningún módulo lo importaba desde
    `E5B`; solo lo referenciaban su propio test y dos comentarios— y además modelaba la
    guarda-predicado que `guards.ts` documenta como antipatrón, con URL relativa (un `TypeError`
    dentro de un componente de servidor) y sin timeout explícito, contra la regla de red externa de
    `AGENTS.md`. El descubrimiento en tiempo de ejecución que podría justificarlo ya lo cubre
    `GET /api/system/capabilities` (decisión D10). Se ajustó el comentario de `server-client.ts` que
    lo mencionaba y el de `guards.ts`, que ahora describe el antipatrón sin apuntar a un módulo
    inexistente. El borrado lo ejecutó el usuario desde su terminal: `rm` y `git rm` están denegados
    por política en la sesión del agente.
- **Los errores por fila viajan como oraciones ya escritas.** `useActionState` serializa su estado
  entero dentro del payload RSC, igual que una prop, así que la regla de la decisión 42 le aplica: el
  estado de `/import` es plano y solo de primitivas, y el servidor traduce el código del importador
  antes de que el texto cruce. `isPrimitiveProp` acepta arreglos de primitivas, que es lo que se usa.
- **El dashboard no cruza la frontera ni una vez.** `boundary.test.ts` no afirma «las props son
  primitivas» sino que la lista de cruces es **vacía**: la pantalla entera, gráfico incluido, se
  renderiza en el servidor. Es la forma observable de la decisión 43, y una librería de gráficos la
  rompería con solo entrar, porque obligaría a que el componente del gráfico fuera de cliente.
- **El gráfico es SVG a mano, con tabla siempre visible.** `<title>` y `<desc>` en la raíz con
  `role="img"` y `aria-labelledby`, un `<title>` por barra, y la tabla equivalente **fuera** de un
  `<details>`: en un `<details>` cerrado la tabla queda fuera del árbol de accesibilidad, que es
  justo lo contrario de lo que pide el criterio.
- **Los tres estados vacíos del dashboard se distinguen sin una segunda consulta.** Sin corrida,
  `CountOrdersPendingScoringAsync(null, …)` cuenta *todos* los pedidos
  (`GetDashboardHandler.cs:24-36`), así que `ordersPendingScoring` separa «base vacía» de «sin
  corrida». El feed de `E5B` necesitaba `GET /api/orders` para lo mismo; el dashboard no.
- **Las bandas de severidad en cero se muestran igual.** La API solo informa las bandas que tienen
  alertas, y el corpus demo no tiene ninguna `HIGH`: omitir la fila haría leer «no existe la banda»
  donde la verdad es «hoy está vacía».
- **El rótulo de la fixture se renderiza antes que las cifras y no es plegable**, ni siquiera cuando
  las métricas fallan con `409`. Hay un test para ese caso concreto.
- **La sección de calidad muestra el barrido de umbrales en un `<details>`.** Es evidencia de una
  decisión ya tomada, no algo sobre lo que la analista actúe; `<details>` lo pliega sin JavaScript y
  por lo tanto sin convertir nada en componente cliente.
- **Timeouts propios para el trabajo pesado**: 60 s la importación y la semilla, 120 s la corrida. El
  timeout general de 5 s es correcto para una lectura que bloquea un render, pero abortar a los cinco
  segundos una corrida sobre trescientos pedidos reportaría un timeout de trabajo que la API sí
  terminó de escribir.
- **El smoke no borra nada.** Deja su directorio temporal y lo imprime. Es deliberado: un script de
  verificación que ejecuta `rm -rf` sobre una ruta calculada es un riesgo que no compensa el
  beneficio, y el sistema operativo recoge `TMPDIR`. Tampoco toca `salvo.db`: migra dos bases propias.
- **El smoke se niega a arrancar si los puertos ya están ocupados**, antes que matar el proceso de
  otro. Usa 5199 y 3199, no los de desarrollo, y ambos son configurables.
- **No se agregó `smoke-ui.sh` a `scripts/check.sh`**, como pide el brief: esa decisión es del cierre
  de etapa. Cuesta una compilación completa de las dos toolchains más dos arranques.

### Riesgos o pendientes

- **El smoke no está en la compuerta**, así que hoy depende de que alguien lo corra. Es la decisión
  que el brief difiere al cierre de la Etapa 5.
- **La accesibilidad se verificó por estructura, no con un lector de pantalla**: `role="img"` con
  `aria-labelledby` en el SVG, tabla equivalente con `caption` y `scope`, regiones rotuladas,
  `role="alert"` y `role="status"` en los avisos, foco visible y ningún significado confiado solo al
  color. La pasada con lector real sigue agendada para la Etapa 8.
- **El gráfico dibuja etiquetas de eje una de cada `ceil(n/6)` semanas.** Con las 17 semanas del
  corpus demo entran bien; con un corpus de años, el eje quedaría escaso y la tabla pasaría a ser la
  lectura principal. No es un problema hoy y no se optimizó por adelantado.
- **`describeRecordError` traduce cinco códigos de fila** —`REQUIRED`, `INVALID_FORMAT`,
  `OUT_OF_RANGE`, `UNSUPPORTED_VALUE`, `REFERENCE_CONFLICT`—, que son los que emiten hoy `Order.cs` y
  `ImportOrdersHandler.cs`. Un código nuevo se muestra tal cual, sin romper nada, pero en inglés.
- **Las señales del motor siguen en inglés dentro del `detail`**, como ya estaba registrado: la
  consola traduce el nombre de la regla y no el detalle. Estructurarlas es tarea de la Etapa 8 y ya
  está acordada como candidata en el Workboard.
- **Sigue pendiente, del coordinador**, cerrar los ítems de la Etapa 5 en `Salvo-Progress.md` y el
  Workboard. Se comprobó contra el archivo, como pide el brief, que §4.4 del Blueprint y la línea de
  Recharts de `AGENTS.md` **ya están corregidas** desde `c759d58`: no son pendientes.

### Integración

- Orden sugerido: rama única, sin dependencias. Requiere `E5A` y `E5B` integradas, y lo están.
- Migraciones o pasos manuales: **ninguno**. No se tocó `backend/src/**` ni el modelo;
  `dotnet ef migrations has-pending-model-changes` no reporta cambios. Tampoco hay dependencias
  nuevas, así que no hace falta `npm ci`.
- Posibles conflictos: la rama parte de la punta de `main` y no hay otra tarea activa. El único
  archivo fuera de `frontend/**` y `scripts/**` es `OpenApiDriftTests.cs`, en el path de
  `backend/tests/**` que el brief reserva para esta tarea.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` y, además,
  `./scripts/smoke-ui.sh`, que es la comprobación que la compuerta no hace. Conviene confirmar sobre
  el estado integrado que el dashboard sigue coincidiendo con `salvo.db`: 18 alertas abiertas,
  13 `MEDIUM` y 5 `CRITICAL`, y el monto en riesgo en tres filas —BRL, USD y UYU— sin ningún total.

## `E6A-PROVEEDOR` — Entidad externa, puerto, mock determinista, reserva en dos fases y reconciliación

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 6
- Rama/worktree: `claude/e6a-proveedor`
- Commit base: `8d5faf7` (`chore: assign E6A-PROVEEDOR`, punta de `main`)
- Commit final: `faa5d6a`
- Fecha: 2026-09-04

### Resultado

Salvo puede pedirle a un proveedor antifraude externo que evalúe un pedido, guarda esa evaluación
como una entidad separada de la local, sobrevive a que el proveedor falle o tarde sin perder nada, y
cierra las pendientes con una reconciliación explícita. La evaluación local, las alertas, el
dashboard y las métricas no cambian: hay un test que lo exige byte a byte.

Cinco commits, uno por concern: dominio, migración, aplicación e infraestructura, API y contrato,
tests.

### Archivos modificados

61 archivos, +5176/−62. Todos dentro de los paths autorizados.

- **Dominio nuevo**, `backend/src/Salvo.Domain/External/`: `ExternalEvaluation`, `ExternalProvider`,
  `ExternalEvaluationStatus`, `ExternalEvaluationErrorCode`, `ExternalSettlementSource`,
  `ExternalEvaluationWireNames`, `ExternalEvaluationReference`,
  `ExternalEvaluationTransitionException`.
- **Dominio acotado**: `Risk/RiskEvaluation.cs` pierde `ExternalEvaluationId` y `ErrorCode`;
  `RiskEvaluationSource` queda en `Local`; `RiskEvaluationStatus` queda en `Approved` y `Denied`;
  `RiskEvaluationWireNames` acompaña.
- **Aplicación nueva**, `backend/src/Salvo.Application/External/`: `IAntifraudProvider`,
  `IAntifraudProviderRegistry`, `IExternalEvaluationStore`, `IExternalEvaluationIdGenerator`,
  `ExternalEvaluationInput/Lookup/Result`, `ExternalProviderOutcome`, `ExternalProviderExchange`,
  `ExternalEvaluationOptions`, `ExternalEvaluationProjection`, `ExternalEvaluationViews`, las dos
  excepciones y los cuatro handlers.
- **Infraestructura**: `External/MockAntifraudProvider`, `MockAntifraudProviderOptions`,
  `AntifraudProviderRegistry`; `Persistence/EfExternalEvaluationStore`, los cuatro convertidores,
  `Configurations/ExternalEvaluationConfiguration`, `RiskEvaluationConfiguration` endurecida,
  `SalvoDbContext`, `SystemExternalEvaluationIdGenerator`, `DependencyInjection`.
- **Migración**: `20260904150633_ExternalEvaluations` y el snapshot del modelo.
- **API**: `ExternalEvaluationEndpoints` y una línea en `Program.cs`.
- **Tests**: `Salvo.Domain.Tests/ExternalEvaluationTests`;
  `Salvo.Api.IntegrationTests/ExternalEvaluation{TestCorpus,SchemaTests,RequestTests,ConcurrencyTests,ReconciliationTests,IsolationTests}`;
  `SalvoApiFactory` gana un diccionario `Settings`; `ArchitectureSmokeTests` incluye la entidad nueva
  y `RiskEvaluationIdentityTests` pierde las dos aserciones que la migración invalida.
- **Contrato**: `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`,
  recapturado y regenerado. Solo agregan; ningún otro archivo de `frontend/` se tocó.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E6A-PROVEEDOR.md` | Válido: las 11 secciones completas, commit base existente, sin solapamiento, sin contradicción con `AGENTS.md` ni el Blueprint |
| Copia de `salvo.db` antes de migrar | Tomada antes de `database update` y movida fuera del árbol del repositorio, a `salvo.db.pre-e6a.bak` en el scratchpad de la sesión |
| `dotnet ef database update` sobre `backend/src/Salvo.Api/salvo.db` | Aplicada. EF advirtió, como estaba previsto, que `PRAGMA foreign_keys = 0` corre fuera de transacción |
| Fingerprints tras la migración | **328 antes y 328 después, byte a byte idénticos**: mismo SHA-256 del volcado ordenado, `a25156ab2e0e864a44922cde02bea790b561ee45d551bcb4c3ec3b8039106e75` |
| Integridad de la base migrada | `PRAGMA integrity_check` = ok; `PRAGMA foreign_key_check` sin filas; 328 evaluaciones, 300 pedidos, 21 alertas, 2141 vínculos de corrida; el índice de fingerprint sigue con `WHERE source = 'LOCAL'` |
| Test de concurrencia | Un `200`, un `409 EXTERNAL_EVALUATION_PENDING` y **una** invocación al proveedor. Verde cinco veces seguidas |
| **Falsación del test de concurrencia** | Invirtiendo las dos fases en `RequestExternalEvaluationHandler` —llamar al proveedor antes de reservar— **falla**, tres veces de tres: `Assert.Equal(1, counter.Count)` obtiene 2, en la línea 72. Las aserciones anteriores, incluida la del `409`, siguen pasando: el contador es lo único que distingue los dos órdenes |
| Test de timeout + reconciliación | Una sola fila, en `DENIED`, `settledBy = RECONCILIATION`, `attemptCount = 1`, y el lookup usó la referencia porque el identificador nunca llegó |
| Test diferencial | `GET /api/dashboard`, `GET /api/alerts?sort=SCORE_DESC` y `GET /api/evaluation-metrics` idénticos antes y después de crear evaluaciones externas en los cuatro estados, con la aserción que exige que los cuatro se hayan alcanzado |
| **Falsación del test diferencial** | Sumando `dbContext.ExternalEvaluations.Any(...)` al conteo de `CountOrdersPendingScoringAsync` de `EfDashboardReader`, **falla**: `"ordersPendingScoring":0` contra `"ordersPendingScoring":4` |
| Conteos del mock sobre el corpus demo | 225 aprobados, 45 denegados, 21 pendientes, 9 con error |
| `KOIN_MODE=sandbox` | `InvalidOperationException` al construir el host, citando §5.3. `KOIN_MODE=mock` arranca normalmente |
| `dotnet test Salvo.slnx` | 66 de dominio + 94 de integración = **160**, 0 fallas, repetido tres veces |
| `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration» |
| `npm run api:types:check --prefix frontend` | Tipos al día con el documento capturado |
| `/gate` (`./scripts/check.sh`) | **Verde**, salida `0`: restore bloqueado, build Release con 0 advertencias y 0 errores, sin cambios de modelo pendientes, 160 tests .NET, typecheck, ESLint `--max-warnings=0`, 153 tests de frontend y build de producción de Next.js con las cuatro rutas de datos dinámicas |
| `git status --porcelain` | Vacío |

### Decisiones y supuestos

- **Tipos propios de punta a punta.** `Salvo.Domain/External/` no referencia `Salvo.Domain/Risk/`, y
  un test lo afirma por reflexión sobre los tipos de las propiedades además de comparar los dos
  pares de enumeraciones. Compartir la enumeración habría reintroducido a nivel de tipo la mezcla
  que la separación de tablas combate.
- **La taxonomía de fallo vive en un solo lugar.** `ExternalProviderExchange` es el único punto donde
  se llama a un proveedor y el único donde su respuesta se convierte en transición, para que la
  solicitud y la reconciliación no puedan divergir. El `errorCode` de `UNREACHABLE` y
  `PROVIDER_REJECTED` lo fija el outcome, no el adaptador: así ningún adaptador puede declarar «no
  se envió» nombrando un timeout.
- **Una excepción inesperada del proveedor se clasifica como transitoria**, nunca como fallo que
  cierra. Una vez que la solicitud pudo haber salido del proceso, la lectura segura del silencio es
  «no se sabe». Es lo que exige D4 y lo que hace que el veredicto real no se pierda.
- **Código nuevo `EXTERNAL_EVALUATION_SETTLED`, `409`.** El brief pide refutar «solicitud nueva» con
  una terminal vigente que no sea `ERROR`, y la tabla de códigos no cubría ese caso;
  `EXTERNAL_EVALUATION_PENDING` habría mentido. `messages.ts` no se tocó —es `frontend/` y es de
  `E6B`—: hoy cae en el mensaje genérico, igual que el test de `E5` ya anticipaba para los códigos
  de esta etapa.
- **`RECONCILIATION_CONFLICT` se emite cuando el barrido examinó filas y las perdió todas.** El
  diseño listaba el código sin fijar su disparador. Una pérdida parcial es normal y va en el
  resumen; una pérdida total significa que otro escritor tiene el trabajo entero y lo útil es
  decirlo, no devolver un resumen de ceros.
- **La banda pendiente del mock se resuelve por la paridad del resto**: par aprueba, impar deniega.
  «El estado terminal de la misma función» necesitaba una regla concreta, y esta mantiene el control
  de la distribución en manos de quien escribe la fixture.
- **Una referencia sin sufijo numérico cae a los primeros cuatro bytes de su SHA-256.** El hash
  administrado de `string` no servía: está aleatorizado por proceso, así que el mismo pedido caería
  en bandas distintas en cada arranque. La fixture nunca produce ese caso; una importación sí puede.
- **El registro de proveedores resuelve por última registración**, como cualquier reemplazo de
  servicio en este contenedor. Fallar habría convertido un override de test en un `500` en la
  primera solicitud.
- **`SalvoApiFactory` gana un diccionario `Settings`** porque `KOIN_MODE` se lee al componer la
  colección de servicios y puede impedir el arranque; `ConfigureTestServices` corre después y no
  alcanza esa ruta.
- **La compuerta del test de concurrencia tiene dos barreras, no una.** La primera fuerza que ambas
  solicitudes lean antes de que ninguna reserve; la segunda, que ambas reserven antes de que ninguna
  cierre. Sin la segunda el test era intermitente —el ganador cerraba su fila y el único parcial, que
  solo cubre las pendientes, dejaba de tener con qué chocar—. Se detectó porque falló en la corrida
  completa de la suite.
- **El índice parcial de fingerprint y la nulabilidad de `RiskEvaluation` quedaron como estaban**,
  según D13. El filtro `WHERE source = 'LOCAL'` es hoy redundante con el check endurecido y se
  conserva a propósito.

### Riesgos o pendientes

- **Ventana estrecha de duplicación entre dos solicitudes concurrentes.** El único parcial solo cubre
  las filas `PENDING`, así que si la solicitud A completa su ciclo entero —reservar, llamar, cerrar—
  entre que B lee «no hay evaluación» y B inserta, B reserva sin chocar y el pedido termina con dos
  evaluaciones. Es la ventana que hizo intermitente el test antes de agregar la segunda barrera. No
  se cerró porque cerrarla exige un único total sobre `(order_id, provider)`, que contradice §7 —un
  pedido acumula evaluaciones externas a lo largo del tiempo— y el alcance que el brief autoriza. La
  reserva sigue eliminando el caso que importa, el de dos solicitudes realmente simultáneas; queda
  registrado para decidirlo en `E6B` o en la Etapa 8.
- **La vinculación tardía de recibos `UNMATCHED` no está**, y es de `E6B` por diseño. Hasta entonces,
  el único camino que cierra una fila que quedó entre las dos fases es la reconciliación.
- **`attemptCount` no tiene tope.** D4 dice que una `PENDING` pasa a `ERROR` «tras N reconciliaciones
  infructuosas o por acción explícita», y ni el umbral ni la acción explícita están implementados:
  hoy una fila puede sondearse indefinidamente. Con el mock no ocurre, porque `GetStatusAsync`
  siempre resuelve. Falta decidir el N.
- **La reconciliación es secuencial y sin paginado.** Con 21 pendientes del corpus demo sobra; con
  decenas de miles, el barrido entero se hace en una sola solicitud HTTP.
- **`EXTERNAL_EVALUATION_SETTLED`, `EXTERNAL_EVALUATION_PENDING`, `PROVIDER_NOT_REGISTERED`,
  `RECONCILIATION_CONFLICT`, `ORDER_NOT_FOUND` e `INVALID_PROVIDER` no están traducidos** en
  `messages.ts`. Es `frontend/`, fuera del alcance de esta tarea, y le toca a `E6B`.
- **La copia de seguridad de `salvo.db` quedó fuera del repositorio**, en el scratchpad de la sesión.
  Si se quiere conservar más allá de la sesión, hay que moverla a un lugar propio.
- **El coordinador tiene pendiente** cerrar `E6A` en `Salvo-Progress.md` y el Workboard; esta rama no
  toca el estado canónico, como corresponde al modo paralelo.

### Integración

- Orden sugerido: rama única, sin dependencias. `E6B-CALLBACK-UI` depende de que esta quede
  integrada.
- Migraciones o pasos manuales: **sí**. `20260904150633_ExternalEvaluations` reconstruye
  `risk_evaluations` y crea `external_evaluations`. **Copiar `backend/src/Salvo.Api/salvo.db` antes
  de aplicarla**: EF ejecuta `PRAGMA foreign_keys = 0` fuera de transacción y lo advierte. En esta
  rama ya se aplicó sobre la base local y se verificó que los 328 fingerprints quedaron idénticos. No
  hay dependencias nuevas, así que no hace falta `npm ci`.
- Posibles conflictos: la rama parte de la punta de `main` y no hay otra tarea activa. Los dos
  archivos fuera de `backend/**` son el documento OpenAPI y `schema.d.ts`, ambos autorizados y ambos
  cambian solo por agregado.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` y, además,
  `./scripts/smoke-ui.sh`, que la compuerta no ejecuta. El smoke no conoce las rutas nuevas, así que
  debería seguir pasando sin cambios; si falla, es señal de regresión en la superficie existente.
  Conviene además confirmar contra `salvo.db` que tras la migración siguen las 328 evaluaciones y las
  21 alertas de la Etapa 5, y que `external_evaluations` arranca vacía.

---

## `E6B-CALLBACK-UI` — Recibos, callback autenticado, vinculación tardía, disparador de demo y superficie en la consola

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 6
- Rama/worktree: `claude/e6b-callback-ui`
- Commit base: `d5da100` (`chore: assign E6B-CALLBACK-UI`, punta de `main`)
- Commit final: `d8d13fc`
- Fecha: 2026-09-04

### Resultado

Una evaluación externa puede cerrarse por callback, y ese callback es seguro. Un duplicado no repite
efectos, un mensaje fuera de orden no retrocede, una contradicción del proveedor no se traga en
silencio, y sin secreto configurado no entra ninguna petición. La analista pide la evaluación externa
desde el detalle de una alerta, ve la opinión del proveedor junto a la propia y entiende cuándo
discrepan. Con esto la Etapa 6 queda completa.

Seis commits, uno por concern: entidad y migración, capa de aplicación, superficie HTTP, tests del
backend con la recaptura del contrato, consola, y smoke.

### Archivos modificados

70 archivos, +6511 / −25. Por área:

**Dominio** — `External/CallbackReceipt.cs`, `CallbackReceiptStatus.cs`, `CallbackReceiptWireNames.cs`,
`CallbackDeduplicationKey.cs`.

**Aplicación** — nuevos: `ApplyExternalCallbackHandler.cs`, `LinkUnmatchedCallbacksHandler.cs`,
`ExternalCallbackTransition.cs`, `IExternalCallbackStore.cs`, `ExternalCallbackMessage.cs`,
`ExternalCallbackViews.cs`, `ExternalCallbackUnavailableException.cs`,
`ICallbackReceiptIdGenerator.cs`, `DeliverPendingCallbacksHandler.cs`,
`RequestCorpusExternalEvaluationsHandler.cs`. Tocados: `RequestExternalEvaluationHandler.cs` y
`ReconcileExternalEvaluationsHandler.cs` (vinculación tardía), `ExternalEvaluationViews.cs`
(`Linked` en el resumen), `IExternalEvaluationStore.cs`, y `Alerts/AlertContext.cs`,
`AlertViews.cs`, `AlertProjection.cs` para el sub-objeto `externalEvaluation`.

**Infraestructura** — `EfExternalCallbackStore.cs`, `CallbackReceiptConfiguration.cs`,
`CallbackReceiptStatusConverter.cs`, `SystemCallbackReceiptIdGenerator.cs`, migración
`20260904191800_CallbackReceipts`, `SalvoDbContext.cs`, `DependencyInjection.cs`,
`EfExternalEvaluationStore.cs`, `EfAlertStore.cs`.

**API** — `ExternalCallbackEndpoints.cs`, `ExternalDemoEndpoints.cs`, `Program.cs`,
`SystemEndpoints.cs`.

**Tests backend** — `CallbackReceiptTests.cs`, `CallbackReceiptSchemaTests.cs`,
`ExternalCallbackTransitionTests.cs`, `ExternalCallbackRaceTests.cs`,
`ExternalCallbackEndpointTests.cs`, `ExternalCallbackTestCorpus.cs`; tocados `SalvoApiFactory.cs`,
`ArchitectureSmokeTests.cs`, `ExternalEvaluationIsolationTests.cs`,
`ExternalEvaluationConcurrencyTests.cs`, `ExternalEvaluationReconciliationTests.cs`.

**Consola** — `alerts/[id]/external-block.tsx`, `external-actions.tsx`, `external-action.ts`,
`external-state.ts`, `external-block.test.tsx`, `page.tsx`; `import/actions.ts`,
`corpus-actions.tsx`, `page.tsx`, `actions.test.ts`; `lib/api/external.ts`, `contract.ts`,
`guards.ts`, `messages.ts`, `schema.d.ts` y sus tests; `lib/format.ts`; `test/fixtures.ts`,
`test/boundary.test.ts`; `openapi/salvo-openapi.json`.

**Otros** — `.env.example` (`SALVO_CALLBACK_SHARED_SECRET` y `KOIN_CALLBACK_SHARED_SECRET`),
`scripts/smoke-ui.sh`.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E6B-CALLBACK-UI.md` | Válido: las 11 secciones completas, commit base `d5da100` existente en `main` y en la rama, sin solapamiento en el Workboard, sin contradicción con `AGENTS.md` ni el Blueprint |
| Copia de `salvo.db` antes de migrar | Hecha; SHA-256 idéntico al original antes de aplicar la migración |
| `dotnet ef database update` sobre `salvo.db` | Aplicada. `PRAGMA integrity_check` → `ok`, `PRAGMA foreign_key_check` sin filas, 328 evaluaciones, 328 pedidos y 21 alertas intactos |
| Test de duplicado exacto | `200`, **una** transición, **un** recibo, `replayCount` = 1 |
| Tests de `SUPERSEDED` y `CONFLICTING` | Fila intacta en `APPROVED`, recibos distintos, `settledBy = CALLBACK` |
| Mismo terminal con otro instante | `NO_OP`, `200`, no duplicado (la clave difiere) |
| Callback correlacionado solo por referencia | `APPLIED`, encuentra la fila reservada |
| Test del callback antes del commit | Verde. **Falsado**: comentando la llamada a `linker.HandleAsync` en `RequestExternalEvaluationHandler` (con `_ = linker;` para que compile), el test falla con `Expected: "DENIED" / Actual: "PENDING"`. El recibo queda `UNMATCHED` y el veredicto se pierde. La reconciliación no lo rescata: el proveedor de prueba responde `PENDING` a todo sondeo. Restaurado y reverificado |
| Carrera callback–reconciliación sobre base en archivo | Una transición (`DENIED`, `settledBy = CALLBACK`, `attemptCount = 0`), un recibo. El barrido perdió su única fila y responde `409 RECONCILIATION_CONFLICT`, el disparador que fijó `E6A` |
| Test del secreto | `401` y **cero** recibos, para tres formas de «no configurado» (ausente, vacío, en blanco) × cuatro secretos presentados, y para cinco secretos incorrectos incluidos uno más corto y uno más largo |
| Proveedor desconocido en la ruta | `404 PROVIDER_NOT_REGISTERED` con secreto válido; `401` sin él, así que un llamador no autenticado no aprende qué proveedores existen |
| Payload con campos desconocidos | Aceptado, `202`, recibo escrito |
| Cuerpo por encima del límite | `413`, cero recibos |
| Disparador de demo sin `DemoData:Enabled` | `404` en las dos rutas; `externalCallbackTriggerEnabled` es `false` |
| El disparador no acepta un estado del cliente | El contrato tiene un solo miembro, `ExternalEvaluationId`; un `status` enviado igual se ignora y la evaluación cierra como el proveedor decide |
| Test diferencial | `GET /api/dashboard`, `GET /api/alerts?sort=SCORE_DESC` y `GET /api/evaluation-metrics` byte a byte idénticos antes y después de entregar los callbacks del corpus entero, con la aserción de que hubo recibos y que no quedó ninguna evaluación esperando al proveedor |
| `boundary.test.ts` | Cubre el bloque nuevo. **Falsado**: pasándole el objeto externo a `ExternalActions` falla con «ExternalActions recibe la prop no primitiva «leaked»». Restaurado |
| `SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build` | Pasa; las cuatro rutas de datos siguen `ƒ` |
| `npm run api:types:check --prefix frontend` | Tipos al día con el documento recapturado |
| `./scripts/smoke-ui.sh` | **Verde: 29 comprobaciones, 0 fallas** (eran 21). Salida completa abajo |
| `/gate` (`./scripts/check.sh`) | **Verde**, salida `0`: restore bloqueado, build Release con 0 advertencias y 0 errores, sin cambios de modelo pendientes, 196 tests .NET (74 dominio + 122 integración), typecheck, ESLint `--max-warnings=0`, 172 tests de frontend y build de producción de Next.js |
| `git status --porcelain` | Vacío |

Salida de `./scripts/smoke-ui.sh`:

```
API http://127.0.0.1:5199 · consola http://127.0.0.1:3199

Compilando la API y la consola…
Migrando las dos bases temporales…
Levantando la consola…

── Escenario: con datos
Listo: API responde en http://127.0.0.1:5199/health
Listo: consola responde en http://127.0.0.1:3199
Cargando el corpus de demostración y ejecutando la corrida…
Alerta de ejemplo: 02844db1-5668-40e7-96aa-4b438535b65a
  ok     /                            «Consola antifraude»
  ok     /import                      «Importación y scoring»
  ok     /import                      «Todos los pedidos de la base están cubiertos»
  ok     /alerts                      «Cola de alertas»
  ok     /alerts/02844db1-… «Snapshot que abrió la alerta»
  ok     /alerts/02844db1-… «Evaluación vigente»
  ok     /alerts/02844db1-… «Evaluación externa»
  ok     /alerts/02844db1-… «Solicitar evaluación externa»
  ok     /import                      «Proveedor antifraude externo»
  ok     /dashboard                   «Monto en riesgo»
  ok     /dashboard                   «Fraude reportado»
  ok     /dashboard                   «Pedidos y denegados por semana»
  ok     /dashboard                   «Calidad del criterio»
  ok     /dashboard                   «no la calidad del criterio de detección»
  ok     /dashboard                   sin «3.942.246»

── Escenario: con evaluación externa
Solicitando la evaluación externa del corpus y entregando los callbacks…
  ok     /alerts/02844db1-… «por el proveedor»
  ok     /alerts/02844db1-… «no se compara con el score local»
  ok     /alerts/02844db1-… sin «Solicitar evaluación externa»
  ok     callback cerrado             sin cabecera → 401
  ok     callback cerrado             con un secreto cualquiera → 401

── Escenario: base vacía
Detenido: API (pid 63463)
Listo: API responde en http://127.0.0.1:5199/health
  ok     /import                      «todavía no hay ninguno en la base»
  ok     /alerts                      «Todavía no hay pedidos»
  ok     /dashboard                   «Todavía no hay pedidos»
  ok     /alerts/00000000-0000-4000-8000-000000000000 «Esta alerta ya no existe»

── Escenario: API apagada
Detenido: API (pid 63615)
  ok     /                            «Consola antifraude»
  ok     /import                      «No se pudo contactar a la API»
  ok     /alerts                      «No se pudo contactar a la API»
  ok     /alerts/00000000-0000-4000-8000-000000000000 «No se pudo contactar a la API»
  ok     /dashboard                   «No se pudo contactar a la API»

Recorrido verde: 29 comprobaciones, 0 fallas.
Detenido: consola (pid 63462)
```

(Los identificadores de alerta van abreviados en esta transcripción; en la salida real aparecen
completos.)

### Decisiones y supuestos

- **Recibo y transición comparten puerto, no solo transacción.** `IExternalCallbackStore` es un solo
  puerto porque son una sola unidad de trabajo. Dos puertos sobre el mismo `DbContext` habrían
  funcionado por accidente de composición y no por diseño, y ese acoplamiento implícito es
  exactamente el que se rompe cuando alguien cambia el ciclo de vida de un servicio.
- **El recibo persiste lo que el mensaje dijo, no solo con qué correlacionarlo.** §7 lista campos de
  correlación; la vinculación tardía es imposible sin el veredicto reportado, porque aplica un recibo
  cuya fila no existía. Se agregaron `reported_status`, `reported_score` y `provider_instant_utc`.
  No es payload y no es PII: el estado y el instante ya viajan dentro de la clave de deduplicación,
  y el score es una cifra del proveedor.
- **La clave de deduplicación cae a la referencia cuando no hay identificador.** El diseño la fija
  como `provider|externalEvaluationId|status|providerInstant` suponiendo que el identificador existe.
  Sin él, el hueco vacío daría a todo mensaje sin identificador del mismo estado e instante la misma
  clave, y un callback sobre un pedido se descartaría como duplicado de otro. El slot lleva
  `ref:{referenceId}`, etiquetado para que los dos no puedan confundirse. La forma de cuatro partes
  se conserva y el caso normal produce exactamente el texto que el diseño fija; un test de dominio lo
  clava.
- **Un callback `ERROR` cierra la evaluación como `PROVIDER_REJECTED`.** Un proveedor que se toma el
  trabajo de mandar un callback diciendo que falló ya decidió; eso no es el silencio de un timeout,
  que es el único caso para el que existe el camino pendiente. La transición pasa por
  `ExternalProviderExchange`, así que el `errorCode` lo fija el outcome y no el mensaje.
- **Una excepción de transición del dominio se clasifica como `CONFLICTING`.** Un mensaje que
  correlaciona con una fila pero contradice un invariante suyo —un segundo identificador de proveedor
  distinto, por ejemplo— es el proveedor discrepando consigo mismo, no un error del servidor.
- **Endpoint extra no listado en el brief: `POST /api/demo-data/external-evaluations:request`.** El
  brief exige la acción en `/import` que pide evaluación externa del corpus vigente, y su sección de
  API solo lista el callback y el disparador de entrega. Sin este endpoint la única alternativa era
  que la acción de servidor hiciera trescientas llamadas HTTP desde Next. Está bajo `DemoData:Enabled`
  con el mismo patrón que el seed, en un path autorizado, y no es una ruta de la consola: `/orders`
  sigue siendo candidata de Etapa 8, como pide el brief.
- **El disparador estampa el instante del proveedor con `requestedAt` de la evaluación, no con el
  reloj.** Así reentregar la misma evaluación produce la misma clave y se reconoce como la reentrega
  que es. Con el reloj, cada pulsación habría acuñado un mensaje nuevo que la tabla clasifica como
  `NO_OP`, y el camino del duplicado no se ejercitaría nunca en la demo.
- **La ruta de callback se mapea siempre y responde `401` sin secreto**, en vez de no mapearse. Las
  dos opciones que el brief admite son equivalentes en seguridad, pero no mapearla haría que el
  documento OpenAPI publicado dependiera del entorno, y `OpenApiDriftTests` compara ese documento.
- **`externalCallbackTriggerEnabled` es un campo propio aunque hoy valga lo mismo que
  `demoDataEnabled`.** La consola necesita saber si puede ofrecer ese botón, y esa no es la misma
  pregunta que si se puede sembrar un corpus. Separarlo ahora evita que una división futura mienta.
- **El feed no lee la evaluación externa.** `ComposeAsync` recibe una bandera: el detalle la pide y
  el listado no. Pagar dos consultas por página por una columna que nadie renderiza no tenía razón.
- **La grilla del detalle no se volvió de tres columnas.** El snapshot y la evaluación vigente son dos
  momentos del mismo criterio y siguen emparejados; la opinión del proveedor es otro criterio y ocupa
  su propia fila, para que no se lea como una versión más de lo mismo.
- **`AlertContext` recibió los dos miembros nuevos con valor por defecto**, así que el resto de sus
  usos no cambió. Es la forma menos invasiva de extender un `record` posicional con tantos llamadores.
- **Se tocó `.env.example`**, que no está en los paths autorizados pero sí lo exige la sección
  «Dentro» del brief. Se agregó también `KOIN_CALLBACK_SHARED_SECRET`, que §9 del Blueprint ya
  declara y el archivo no tenía.
- **La reconciliación vincula recibos después del bucle, no dentro.** La vinculación puede tener que
  vaciar el rastreador de cambios, y hacerlo a mitad del barrido desprendería las filas que a las
  iteraciones siguientes todavía les hacen falta.

### Riesgos o pendientes

- **El README no declara D8.** El diseño pide decir en el código y en el README que el secreto
  compartido no es el mecanismo real. En el código está —XML docs y la descripción del endpoint, que
  viaja al OpenAPI—; el README quedó fuera porque no está en los paths autorizados ni en el alcance
  del brief. Le toca al coordinador.
- **`ReconciliationSummary.Settled` no cuenta lo que la vinculación cerró.** El barrido vincula
  después de contar, así que una fila que cerró por un recibo tardío aparece en `Linked` y no en
  `Settled`. Es deliberado —son eventos distintos— pero los dos números no suman lo que un lector
  distraído esperaría.
- **La correlación de recibos con una evaluación es por referencia mientras la fila no tenga
  identificador.** Un pedido con varias evaluaciones externas a lo largo del tiempo, todas sin
  identificador, vería los recibos de las anteriores. En la práctica toda fila cerrada tiene
  identificador; queda anotado.
- **Sigue abierta la ventana estrecha de duplicación entre dos solicitudes** que `E6A` registró: el
  único parcial solo cubre las `PENDING`. No se cerró, por la misma razón que entonces —exigiría un
  único total sobre `(order_id, provider)`, que contradice §7—.
- **`attemptCount` sigue sin tope**, pendiente de `E6A`. Falta decidir el N tras el cual una
  `PENDING` pasa a `ERROR`.
- **La entrega de callbacks del corpus es secuencial y sin paginado**, como la reconciliación. Con 21
  pendientes del corpus demo sobra.
- **La copia de seguridad de `salvo.db` quedó en el scratchpad de la sesión**, fuera del repositorio.
  Si se quiere conservar, hay que moverla.
- **El coordinador tiene pendiente** cerrar `E6B` y la Etapa 6 en `Salvo-Progress.md` y el Workboard;
  esta rama no toca el estado canónico, como corresponde al modo paralelo.

### Integración

- Orden sugerido: rama única, sin dependencias. Es el último ítem de la Etapa 6.
- Migraciones o pasos manuales: **sí**. `20260904191800_CallbackReceipts` crea `callback_receipts`.
  Es puramente aditiva —un `CREATE TABLE` y cuatro índices, sin reconstrucción de tabla—, así que no
  tiene el problema de `PRAGMA foreign_keys = 0` que traía la de `E6A`. Aun así, **copiar
  `backend/src/Salvo.Api/salvo.db` antes de `database update`**, con la regla de no borrar ni recrear
  la base local. Ya está aplicada sobre la copia de trabajo de esta rama.
- Posibles conflictos: ninguno con otra tarea activa. Dentro de `main`, los archivos con más
  probabilidad de conflicto si algo más los tocó son `frontend/openapi/salvo-openapi.json`,
  `schema.d.ts`, `messages.ts`, `guards.ts` y `SalvoDbContextModelSnapshot.cs`.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main` y, además,
  `./scripts/smoke-ui.sh`, que la compuerta no ejecuta y que ahora hace 29 comprobaciones. Conviene
  confirmar contra `salvo.db` que tras la migración siguen las 328 evaluaciones y las 21 alertas, que
  `callback_receipts` arranca vacía, y hacer el recorrido a mano: pedir la evaluación externa de una
  alerta, entregar su callback y ver el veredicto con su procedencia.

## `E7A-EXPLICACIONES` — Explicaciones verificadas sobre la salida

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 7
- Rama/worktree: `claude/e7a-explicaciones`
- Commit base: `5318518` (`docs: add Etapa 7 task briefs`, punta de `main`)
- Commit final: `5565793`
- Fecha: 2026-09-05

### Resultado

Una analista puede pedir la explicación de una alerta. El sistema la genera con un proveedor
determinista, **verifica sobre el texto** que cada regla y cada cifra estén respaldadas por la
evaluación, y solo entonces la guarda. Si el proveedor falla, tarda o el navegador aborta, la fila
queda en un estado del que siempre se puede salir. El texto generado no puede cambiar ninguna
superficie de decisión, y hay cuatro tests que fallan si lo hiciera. El motor, el fingerprint, las
alertas, el dashboard y las métricas no cambian: los 328 fingerprints siguen byte a byte idénticos
tras la migración.

La explicación es una entidad propia, `alert_explanations`, y **su identidad es la evaluación**, no
la alerta: `(risk_evaluation_id, provider, template_version, alert_policy_version)`. La alerta viaja
como `requested_from_alert_id`, procedencia y no identidad, que es lo que hace que una escalada
sobre una evaluación ya explicada encuentre el párrafo escrito en vez de pagarlo de nuevo.

### Archivos modificados

**Dominio, nuevo** (`backend/src/Salvo.Domain/Explanations/`): `AlertExplanation`,
`ExplanationStatus`, `ExplanationProvider`, `ExplanationFailureCode`, `ExplanationWireNames`,
`ExplanationInput`, `ExplanationFacts`, `ExplanationGrounding`, `NumberTokenizer`, `SignalFacts`,
`SpanishNumberFormat`, `ReferencedRuleSerializer`, `ExplanationTransitionException`,
`SignalDetailNotRecognizedException`.

**Dominio, modificado**: `Alerts/Alert.cs` y `Alerts/AlertReview.cs` ganan `explanationId`.

**Aplicación, nuevo**: `Explanations/` con `IExplanationProvider`, `IExplanationStore`,
`IExplanationIdGenerator`, `ExplanationOptions`, `ExplanationExchange`, `ExplanationInputFactory`,
`ExplanationViews`, `ExplanationConflictException`, `RequestExplanationHandler`; y
`Providers/ProviderCall.cs`.

**Aplicación, modificado**: `External/ExternalProviderExchange.cs` pasa a usar `ProviderCall`;
`Alerts/` (`AlertContext`, `AlertProjection`, `AlertViews`, `ReviewAlertCommand`,
`ReviewAlertHandler`, `AlertReviewConflictReason`).

**Infraestructura**: `Explanations/DeterministicExplanationProvider.cs`,
`Persistence/EfExplanationStore.cs`, `Persistence/Configurations/AlertExplanationConfiguration.cs`,
los tres convertidores, `SystemExplanationIdGenerator`, la migración
`20260905013514_Explanations` con su designer y el snapshot; `DependencyInjection`,
`SalvoDbContext`, `EfAlertStore`, `AlertReviewConfiguration`.

**API**: `ExplanationEndpoints.cs` nuevo; `Program.cs` y `AlertEndpoints.cs` modificados.

**Tests**: `ExplanationFactsTests` (dominio); `ExplanationTestCorpus`, `ExplanationGroundingTests`,
`ExplanationEndpointTests`, `ExplanationIsolationTests`, `ExplanationGoldenTests` (integración);
`AlertSchemaTests`, `ArchitectureSmokeTests`, `SalvoApiFactory`, `AlertTestCorpus`, `AlertTests`
modificados.

**Frontend**, solo lo autorizado: `openapi/salvo-openapi.json` y `schema.d.ts` recapturados;
`guards.ts`, `guards.test.ts`, `test/fixtures.ts` y una clave en
`src/app/alerts/[id]/page.test.tsx`. Ningún componente ni ruta.

**Raíz**: `.gitignore` ignora las copias de la base local.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | **Verde**, salida 0 |
| `npm run api:types:check` | Tipos al día con el documento recapturado |
| `dotnet build` Release | 0 advertencias, 0 errores |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `dotnet test` | 94 de dominio + 145 de integración, 0 fallas |
| `npm run test` | 178 de frontend, 0 fallas |
| `next build --webpack` | Las cuatro rutas de datos siguen dinámicas (`ƒ`) |
| Migración sobre `salvo.db` | 328 fingerprints byte a byte idénticos (`cmp`), `integrity_check` ok, `foreign_key_check` sin filas, 21 alertas y 1 revisión intactas |
| `git status --porcelain` | Limpio |

**Falsaciones ejecutadas**, cada una revertida y con la suite verde después:

| Mutación | Resultado |
| --- | --- |
| Quitar `ExplanationGrounding.Verify` **del manejador** | 2 rojos. No hay nada que quitarle al proveedor: no valida nada, y el rechazo igual ocurría |
| Quitar el monto en unidades de `ExplanationFacts` | 1 rojo |
| Quitar el instante en hora de negocio | 1 rojo |
| Quitar redondeo y truncamiento | 3 rojos |
| Asentar con el token del llamador en vez de `CancellationToken.None` | 1 rojo: la fila queda `PENDING` |
| Que el almacén reescriba `alerts.signals_snapshot_json` | Interceptor rojo, y el diferencial rojo **solo en la línea 131**, que es el detalle de alerta. Dashboard, feed y métricas pasan con el snapshot reescrito: por eso el diferencial de tres superficies no habría visto nada |
| **Guarda, paso 1**: proyectar `summary` sin mirar `status` | 2 rojos en `guards.test.ts` |
| **Guarda, paso 2**: con esa mutación puesta | Los otros 176 tests de frontend siguen verdes, así que esos dos son lo único entre una respuesta que se contradice y un resumen renderizado |

El test dorado encontró un defecto real antes de que se integrara: la plantilla escribía el año como
`2.026`, porque el formateador agrupa miles y el año pasaba por él. El grounding lo aceptaba —`2.026`
se lee como 2026 bajo la regla de ambigüedad—, así que nada más lo habría notado.

### Decisiones y supuestos

- **La identidad es la evaluación.** Todo lo que el proveedor recibe es función de la evaluación y
  del pedido; lo único que aporta la alerta es la versión de política. Con clave por alerta, escalar
  duplicaba filas y pagaba dos veces la misma redacción.
- **La validación vive en el caso de uso**, entre el puerto y el almacén. La lógica es pura y está en
  el dominio; la *llamada* está en el manejador, que es lo que hace que ningún proveedor pueda
  saltarla. El test de mutación apunta ahí.
- **El tokenizador lee un token en todas sus interpretaciones defendibles.** `56.0` es decimal
  porque un grupo de miles tiene tres dígitos; `8.900,00` son ocho mil novecientos; `1.279` es
  genuinamente ambiguo y da las dos lecturas. Ser generoso acá es seguro: una cifra inventada no se
  vuelve real por tener dos grafías, y lo contrario rechaza un monto correcto.
- **`FAILED` no es terminal** y el reintento ocurre sobre la misma fila, que es lo que permite que el
  único total sobre la identidad no bloquee nada. Tope de 3 intentos; al agotarse, la fila queda con
  `ATTEMPT_LIMIT_REACHED` y `failure_detail` conserva el código real, que si no se perdería.
- **Una reserva anterior a `ahora − 2 × timeout` se retoma**, con o sin `regenerate`. Es lo que
  reemplaza a una reconciliación en una etapa que no la tiene: sin eso, un proceso que muere deja una
  fila que el único parcial defiende para siempre.
- **`ProviderCall` se comparte** con `ExternalProviderExchange` en vez de copiarlo. Los dos mapean
  distinto —un timeout antifraude deja la evaluación pendiente y una explicación que no llegó
  simplemente no llegó—, pero no pueden discrepar sobre *qué es* un timeout. El comportamiento
  externo quedó idéntico: la cancelación del llamador se vuelve a lanzar como antes.
- **Un `detail` que el extractor no reconoce es un fallo declarado**, no un silencio: cierra la fila
  con `PROVIDER_UNAVAILABLE` y el motivo. Es inalcanzable mientras el motor y sus fingerprints
  dorados coincidan, y deja de serlo en silencio si dejan de coincidir.
- **La plantilla no escribe el valor de la mediana**, porque el conjunto de hechos lleva el monto en
  unidades pero no la mediana dividida por cien, y agregarla habría ensanchado el conjunto por una
  razón estética. Dice el ámbito, el factor y sobre cuántos pedidos se calculó.
- **`row_version` es `INTEGER`, no `BLOB`** como nombra la tabla del diseño. EF Core sobre SQLite no
  tiene row version automática; el dominio lo incrementa en cada transición y la comprobación es la
  misma. El estado solo no alcanzaba: retomar una reserva vencida es `PENDING → PENDING`.
- **`ExplanationInput` vive en el dominio**, porque es el material del que se construyen los hechos y
  tiene que ser puro e inspeccionable. El puerto, en aplicación, lo referencia.
- **La proyección del frontend se autorizó a mitad de tarea.** El punto 8 del brief le da a E7A el
  sub-objeto en `AlertDetail`, y eso vuelve requeridos tres miembros del contrato, con lo que
  `guards.ts` deja de compilar. En la Etapa 6 no pasó porque `E6A` **no** agregó el sub-objeto al
  detalle: `AlertDetail.externalEvaluation` y su guarda entraron juntos en `E6B` (`6fbdb66`). El
  coordinador autorizó la proyección mínima; `page.test.tsx` y `guards.test.ts` se tocaron por
  arrastre —una clave de fixture y los tests de la guarda nueva—, sin componentes, rutas ni
  `messages.ts`.
- El commit de checkpoint se reescribió para sacar una copia de `salvo.db` que había quedado
  versionada; el `.gitignore` ahora cubre ese patrón.

### Riesgos o pendientes

- **`DesignAgent/Salvo-Portability.md:72` conserva `ANTHROPIC_MODEL="claude-sonnet-5"`.** El
  Blueprint §9 y `.env.example` ya están vacíos, como pide D11; ese archivo se salteó. Está fuera de
  los paths autorizados de esta tarea.
- **`SignalFacts` es la semilla de `e3-v2` y su extractor está para morir.** Cuando el motor emita
  campos tipados, se borra el extractor y la plantilla y los hechos quedan intactos. Está dicho en el
  archivo.
- **El conjunto de hechos es permisivo a propósito.** No es lo que hace fuerte a la comprobación: lo
  que la hace fuerte es que una cifra *inventada* no está ahí ni se alcanza redondeando. Las cifras
  escritas en letras no se validan, y se declara que no se validan.
- **No hay reconciliación**; la regla de pendiente vencida la reemplaza dentro del alcance. La
  ventana es `2 × timeout`, 30 segundos con la configuración por defecto.
- **`E7B` tiene por delante**: el bloque en el detalle, el aviso de desactualizada, los botones,
  `messages.ts` con los códigos nuevos, `boundary.test.ts` contaminando el sub-objeto —incluido un
  `summary` inyectado sobre una fila `FAILED`— y los textos de `smoke-ui.sh`. La constante
  `EXPLANATION_READY` quedó dentro de `guards.ts` y le corresponde moverla junto a los demás valores
  de cable.
- **`scripts/smoke-ui.sh` no se ejecutó**: no hay superficie de consola que recorrer todavía, y el
  script está reservado para `E7B`.
- Sin autenticación, el endpoint es alcanzable por el rewrite de Next. Con un proveedor de pago, la
  idempotencia y el tope de intentos son el único freno; queda declarado como lo declara el Blueprint
  para el resto de las rutas mutables.

### Integración

- Orden sugerido: rama única, sin dependencias. `E7B-EXPLICACIONES-UI` depende de esta integrada,
  con el OpenAPI ya recapturado: **`E7B` no vuelve a capturar el contrato**.
- Migraciones o pasos manuales: **sí**. `20260905013514_Explanations` crea `alert_explanations` y
  agrega `alert_reviews.explanation_id` con su clave foránea. Lo segundo obliga a EF a **reconstruir
  `alert_reviews`**, así que emite `PRAGMA foreign_keys = 0` fuera de transacción y lo advierte:
  **copiar `backend/src/Salvo.Api/salvo.db` antes de `database update`**, sin borrar ni recrear la
  base local. Ya está aplicada sobre la copia de trabajo de esta rama, con los 328 fingerprints
  verificados idénticos.
- Posibles conflictos: ninguno con otra tarea activa. Dentro de `main`, los archivos con más
  probabilidad de conflicto si algo más los tocó son `frontend/openapi/salvo-openapi.json`,
  `schema.d.ts`, `guards.ts`, `SalvoDbContextModelSnapshot.cs`, `EfAlertStore.cs` y
  `ExternalProviderExchange.cs`.
- Verificación posterior al merge: repetir `./scripts/check.sh` sobre `main`. Conviene además
  confirmar contra `salvo.db` que tras la migración siguen las 328 evaluaciones, las 21 alertas y la
  revisión, que `alert_explanations` arranca vacía, y pedir a mano la explicación de una alerta del
  corpus para leer el párrafo. `./scripts/smoke-ui.sh` queda para el cierre de la etapa, cuando `E7B`
  haya construido la superficie que recorrer.

## `E7B-EXPLICACIONES-UI` — La explicación, en la consola

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 7
- Rama/worktree: `claude/e7b-explicaciones-ui`
- Commit base: `af93f01` (`docs: complete the E7B brief with its base commit and Progress entry`),
  que es la punta actual de `main`. El brief declara `8951fa2` porque se escribió antes de que el
  coordinador integrara en `main` la corrección del propio brief; `af93f01` es su hijo directo y no
  toca código.
- Commit final: `1e8ddec`
- Fecha: 2026-09-05

### Resultado

La analista ve la explicación de la evaluación que abrió la alerta, puede pedirla cuando no existe y
volver a intentarla cuando falló con intentos disponibles, y lee un aviso —antes del texto, nunca en
lugar del texto— cuando la explicación describe una evaluación que ya no es la vigente. El bloque
dice quién la redactó: en esta instalación, una plantilla determinista y no un modelo.

Ningún texto que la verificación del backend no haya aprobado llega al navegador. La guarda rechaza
como `malformed` una respuesta con `summary` sobre un estado que no lo admite, y el detalle entero
cae con ella: una respuesta que se contradice no se renderiza a medias.

La revisión registra qué explicación había en pantalla. El id viaja como primitiva oculta junto al de
la alerta; la nota nunca se precarga con el resumen, y el formulario no podría hacerlo aunque
alguien se lo pidiera, porque recibe una cadena y no la explicación.

### Archivos modificados

**Bloque nuevo** (`frontend/src/app/alerts/[id]/`): `explanation-block.tsx`,
`explanation-actions.tsx`, `explanation-action.ts`, `explanation-state.ts` y
`explanation-block.test.tsx`.

**Detalle de alerta, modificado**: `page.tsx` monta el bloque; `review-panel.tsx` lee el id de la
explicación y se lo pasa al formulario; `review-form.tsx` lo lleva como campo oculto;
`review-action.ts` lo envía. Tests: `page.test.tsx`, `review-form.test.tsx`, `review-action.test.ts`.

**Cliente de la API** (`frontend/src/lib/api/`): `explanations.ts` nuevo; `contract.ts` gana los dos
tipos y `EXPLANATION_STATUS`; `guards.ts` pierde la constante inlineada y gana
`projectExplanationOutcome`; `alerts.ts` manda `explanationId`; `messages.ts` gana cuatro códigos.
Tests: `messages.test.ts`, `server-client.test.ts`.

**Rótulos**: `frontend/src/lib/format.ts` gana `explanationFailureLabel` y
`explanationProviderLabel`.

**Frontera**: `frontend/src/test/boundary.test.ts`.

**Recorrido**: `scripts/smoke-ui.sh`.

Nada fuera de `frontend/**` y `scripts/**`. El backend y el contrato OpenAPI no se tocaron: los
gobierna `E7A`.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | **Verde**, salida `0` |
| `dotnet build` Release | 0 advertencias, 0 errores |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `dotnet test` | 94 de dominio + 145 de integración, 0 fallas |
| `npm run check` | Typecheck, ESLint y 197 tests de frontend, 0 fallas |
| `next build --webpack` | Compilado con la API apagada; las cuatro rutas de datos siguen dinámicas (`ƒ`) |
| `npm run api:types:check` | Al día, sin recapturar |
| `./scripts/smoke-ui.sh` | **Verde**: 37 comprobaciones, 0 fallas, cinco escenarios |
| `git status --porcelain` | Limpio |

**Falsaciones ejecutadas**, cada una revertida y con la suite verde después:

| Mutación | Resultado |
| --- | --- |
| **Guarda**: proyectar `summary` sin mirar `status` | 3 rojos: los dos de `guards.test.ts` que dejó `E7A` y el nuevo de `boundary.test.ts`. La guarda es lo único entre una respuesta que se contradice y un párrafo renderizado |
| **Frontera, paso 1**: que el bloque le pase el objeto de la explicación a `ExplanationActions` | Rojo, nombrando la prop: «`ExplanationActions` recibe la prop no primitiva «explanation»» |
| **Frontera, paso 2**: con esa mutación puesta, el resto de la suite | 187 verdes. Ese test es lo único que separa el objeto del navegador |

El paso 1 falla por **forma** y no por contenido: la guarda ya había descartado las claves
desconocidas, así que `isFraudLabel` no llega ni con la mutación. Son dos defensas distintas y el
test las distingue, que es lo que se quería saber.

**El smoke encontró un defecto real en su primera pasada.** El rótulo del botón viajaba como prop a
un componente cliente, de modo que quedaba serializado en el payload RSC de todas las páginas,
hubiera botón o no: una alerta ya explicada seguía llevando «Explicar esta evaluación» en el HTML y
`expect_no_text` lo vio. Los dos rótulos pasaron a vivir dentro del control, que es donde
`ExternalActions` guarda los suyos, y la comprobación quedó en el script.

### Decisiones y supuestos

- **Los nueve `ExplanationFailureCode` no van a `messages.ts`.** El brief los enumera ahí junto a los
  conflictos, pero son valores de un campo dentro de un `200` y no rechazos de una petición:
  `describeFailure` nunca los recibiría, y `messages.test.ts` afirma que el catálogo es exactamente
  lo que los endpoints emiten como problema. Van a `format.ts` con `externalErrorLabel` como
  precedente exacto —los códigos de error externos son el mismo caso y viven ahí desde E6—. El brief
  delega «la redacción de los rótulos, coherente con `format.ts` y `messages.ts`», y esto es esa
  elección.
- **Un código de conflicto más de los tres que el brief lista.** `ExplanationConflictReason` tiene
  cuatro miembros y `ConcurrentUpdate` cae en la rama por defecto de `ExplanationEndpoints.ToCode`,
  que emite `EXPLANATION_CONFLICT`. Es alcanzable y tiene su texto, igual que
  `EXTERNAL_EVALUATION_CONFLICT`, que existe por lo mismo.
- **El botón sigue lo que la API acepta, no lo que la pantalla podría ofrecer.** Pedir cuando no hay
  nada; reintentar solo sobre un fallo con intentos disponibles. Sobre una explicación escrita y
  sobre un presupuesto agotado no hay botón, porque las dos peticiones terminan en `409`.
- **El resumen no viaja por el estado de la acción.** Ya está en la parte del bloque que renderiza el
  servidor; repetirlo en el payload sería una segunda copia que puede discrepar de la primera. El
  componente cliente no recibe una palabra del texto.
- **`isOutdated` llega calculado y no se recalcula.** El bloque lo lee del sub-objeto; la consola no
  compara identificadores de evaluación por su cuenta.
- **`currentExplanation` se usa en una sola frase**: el aviso de desactualizada dice si la evaluación
  vigente ya tiene la suya. No se muestra en lugar de la del snapshot, que es la premisa del
  veredicto.
- **`external.ts` entró a la lista de módulos `server-only` de `boundary.test.ts`.** Declaraba
  `import "server-only"` desde E6 y nada lo comprobaba; `explanations.ts` entró con él.
- **La ventana de la llamada es de 20 s**, contra los 15 s de `ExplanationOptions.Default`: con los
  5 s del cliente por defecto, la consola habría reportado un timeout que la API no tiene.

### Riesgos o pendientes

- **El estado `PENDING` no se refresca solo.** Con el proveedor determinista se asienta en la misma
  petición, así que es casi inalcanzable en pantalla; con un proveedor real haría falta decidir si la
  página se recarga sola o si la analista vuelve a entrar. La pantalla se lo dice explícitamente.
- **La fuga de rótulos por props no tiene test unitario.** La detecta el smoke, que es donde el HTML
  real existe; en Vitest no hay payload RSC que inspeccionar. Cualquier rótulo nuevo que se pase como
  prop a un componente cliente vuelve a filtrarse sin que la compuerta lo note.
- **`DesignAgent/Salvo-Portability.md:72` sigue con `ANTHROPIC_MODEL="claude-sonnet-5"`**, pendiente
  heredado de `E7A` y fuera de los paths de esta tarea.
- **Sin autenticación, el endpoint de explicación es alcanzable por el rewrite de Next.** Con un
  proveedor de pago, la idempotencia y el tope de tres intentos son el único freno. Queda declarado
  como lo declara el Blueprint para el resto de las rutas mutables.
- **Los rótulos de los nueve códigos de fallo no tienen test propio de `format.ts`**; se verifican
  desde el bloque, que afirma que ninguno se muestra crudo. Es una comprobación de comportamiento y
  no de tabla, deliberadamente.

### Integración

- Orden sugerido: rama única, sin dependencias. `E7A` ya está integrada y esta tarea no recaptura el
  contrato.
- Migraciones o pasos manuales: **ninguno**. No hay cambios de esquema ni de backend.
- Posibles conflictos: `guards.ts`, `contract.ts`, `messages.ts`, `format.ts`, `boundary.test.ts` y
  `scripts/smoke-ui.sh` si algo más los tocó en `main` desde `af93f01`.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre `main`. El
  smoke es lo único que ejercita el bloque con datos reales, y con esta tarea son 37 comprobaciones.
  Conviene además abrir a mano una alerta del corpus, pedirle la explicación y leer el párrafo antes
  de cerrar la etapa.

---

## `E7C-PULIDO-EXPLICACION` — Pulido del bloque de explicación

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 7
- Rama/worktree: `claude/e7c-pulido-explicacion`
- Commit base: `0e3faf1`, el declarado en el brief. El padre real de mi único commit es `0d05d0d`:
  entre uno y otro están `d0e1f78` y `0d05d0d`, los dos commits del brief que escribió el
  coordinador y que también están en `main`. La diferencia es esa y solo esa.
- Commit final: `4403666`
- Fecha: 2026-09-06

### Resultado

El bloque de explicación se lee sin repeticiones y sin asperezas. El aviso posterior a generar dice
qué quedó guardado en vez de repetir la leyenda que está dos líneas más arriba; la plantilla no
escribe un decimal que vale cero; y las reglas se disparan en vez de coincidir. No cambió el motor,
ni el conjunto de hechos, ni la validación de grounding, ni el ciclo de vida, ni el contrato, ni el
esquema.

La versión de plantilla subió a `e7-v2`, que es lo que hace que el arreglo alcance a una evaluación
ya explicada: la versión es parte de la identidad de la fila, así que la plantilla nueva escribe al
lado de la vieja en vez de no escribir nada. La ampliación la autorizó el coordinador después de la
primera entrega, junto con el único cambio que arrastra fuera del proveedor: `e7-v2` entró a
`NumberTokenizer.VersionStrings`. Es precaución, no requisito —la versión no aparece en el resumen,
el proveedor solo la guarda como columna—, y el test nuevo lo afirma en vez de dejarlo al comentario.

El texto del mismo pedido, antes y después:

- `ORD_000011`, razón `23.2`: «… **Coincidieron** 3 reglas. El monto, 2.011,11 BRL, es **23,2** veces
  la mediana del comercio …» → «… **Se dispararon** 3 reglas. El monto, 2.011,11 BRL, es **23,2**
  veces la mediana del comercio …». El decimal se conserva, que es el punto.
- `ORD_000171`, razón `15.0`: «… **Coincidieron** 2 reglas. El monto, 1.297,71 USD, es **15,0** veces
  la mediana del comercio …» → «… **Se dispararon** 2 reglas. El monto, 1.297,71 USD, es **15** veces
  la mediana del comercio …».
- Aviso posterior a generar: «Cada cifra y cada regla del texto se verificaron contra la evaluación
  antes de guardarlo.» → «El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una
  explicación escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la
  suya.»

### Archivos modificados

- `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`
- `backend/src/Salvo.Domain/Explanations/NumberTokenizer.cs`
- `backend/tests/Salvo.Api.IntegrationTests/ExplanationGoldenTests.cs`
- `backend/tests/Salvo.Api.IntegrationTests/ExplanationTemplateVersionTests.cs` (nuevo)
- `frontend/src/app/alerts/[id]/explanation-action.ts`
- `frontend/src/app/alerts/[id]/explanation-action.test.tsx` (nuevo)

`scripts/smoke-ui.sh` estaba autorizado bajo condición y no hizo falta tocarlo: ninguna de sus
comprobaciones fija una de las tres cadenas que cambiaron.

### Verificación

| Comando | Resultado |
| --- | --- |
| `dotnet test --filter ExplanationGoldenTests` | 2 correctas, 0 fallas |
| Falsación 1: `Ratio` vuelve a formatear siempre con un decimal | Falla el caso `ORD_000171` («es 15,0 veces» contra «es 15 veces»); el de `ORD_000011` queda verde, que es lo que prueba que las dos ramas son independientes |
| Falsación 2: vuelve «Coincidieron» | Fallan los dos casos dorados |
| `dotnet test --filter ExplanationTemplateVersionTests` | 1 correcta |
| Falsación 4: la versión se deja en `e7-v1` | Falla en `Assert.True(written.Applied)`: la petición se vuelve un no-op y el párrafo viejo sobrevive, que es el defecto dicho como falla |
| La versión no llega al texto | `Assert.DoesNotContain("e7-v", summary)` sobre el resumen escrito por el proveedor, verde. Los dos textos dorados, que fijan el párrafo entero, tampoco la contienen |
| `npx vitest run explanation-action.test.tsx` | 4 correctas |
| Falsación 3: se copia la oración de la leyenda al aviso | El test la rechaza y la nombra: «cada cifra y cada regla del texto se verificaron contra esta evaluación antes de guardarlo» |
| `./scripts/check.sh` | Verde, salida `0`: restore bloqueado, build Release con 0 advertencias y 0 errores, modelo EF sin cambios, 94 tests de dominio + 147 de integración, typecheck, ESLint y 201 tests de frontend, build Next.js 16.3.3. Ejecutada dos veces: antes y después de subir la versión |
| `./scripts/smoke-ui.sh` | Verde las dos veces: **37 comprobaciones, 0 fallas** |
| Cifras en letras | Ninguna. Se buscaron palabras-número (`uno`…`mil`, `cien`, `media`) sobre las dos cadenas doradas: los dos aciertos son falsos positivos, «media» de «la severidad resultante es media» y «once» dentro de un comentario en inglés. Los literales de la plantilla no contienen ninguna. La rama singular escribe «Se disparó **1** regla», con dígito |
| `git status --porcelain` | Solo los cuatro paths autorizados |

### Decisiones y supuestos

- **La condición sobre la parte fraccionaria vive en la plantilla, no en el formateador.**
  `SpanishNumberFormat` sigue siendo el primitivo «formateá con N decimales», que es lo que la hace
  determinista entre máquinas. `Minutes` ya resolvía a mano el mismo problema dentro del proveedor,
  así que ahí es donde corresponde: los dos ahora comparten `Trimmed(value, decimals)`, que redondea
  primero y decide después. Redondear primero importa: preguntarle a un valor sin redondear si tiene
  parte fraccionaria haría que `22,98` se escribiera «23,0», el defecto que esta tarea corrige.
- **El aviso sigue el molde de `review-action.ts`**: qué quedó guardado y qué se sigue de eso. Lo que
  se sigue —una explicación escrita no se reescribe— es además lo que explica el botón que
  desaparece, que hasta ahora la pantalla no decía en ninguna parte.
- **El dorado fija dos textos completos, en dos tests hermanos** con un método común. Un solo test
  con dos aserciones habría dado un único diagnóstico para dos afirmaciones distintas.
- **El test de no repetición compara el aviso contra el bloque renderizado**, no contra una copia del
  literal. Se parte en oraciones por `.` y `:`, se normaliza espacio y capitalización, y se exige
  intersección vacía. Una copia del literal habría probado que dos constantes son distintas, que no
  es lo que dice el criterio.
- **`explanation-action.ts` no tenía tests** y ahora los tiene: los dos avisos de éxito, el
  `applied=false` y la fila sin texto utilizable, además del de no repetición.
- **La versión de plantilla se sube en vez de reescribir la fila.** Escribir encima habría borrado
  el párrafo que una analista pudo haber leído al formar su veredicto, que es exactamente lo que la
  API se niega a hacer cuando rechaza regenerar una explicación `READY`. Subir la versión conserva
  esa fila y agrega otra; `EfAlertStore.GetExplanationsAsync` se queda con la petición más reciente,
  así que la consola lee la nueva sin que haya que tocar el camino de lectura.
- **El test de convivencia inserta la fila vieja por SQL**, porque la plantilla que la escribió ya
  no existe. Es el mismo recurso que usa `AlertSchemaTests` y la única forma de reproducir el estado
  que este cambio arregla.

### Riesgos o pendientes

- **La API ya acepta el pedido, pero la consola no lo ofrece, y eso deja la única fila `e7-v1` de
  `backend/src/Salvo.Api/salvo.db` con su párrafo viejo en pantalla.** El cambio de versión no
  reescribe nada por su cuenta: hace que pedir la explicación otra vez sea aceptado, que antes no lo
  era. Pero el botón aparece solo cuando no hay explicación o cuando la que hay falló con intentos
  disponibles —`explanation-block.tsx:178`—, y sobre una `READY` no hay botón. Sobre esa base el
  texto corregido se obtiene con un `POST` directo a `/api/alerts/{id}/explanation`, que es lo que
  hace el smoke. En una base recién sembrada no hay nada que hacer.
- **Ese desajuste contradice una regla que `E7B` dejó escrita**: «el botón sigue lo que la API
  acepta, no lo que la pantalla podría ofrecer». Con `e7-v2` la API acepta un caso que la pantalla
  no ofrece. Alinearlas es una decisión de producto —el botón pasaría a aparecer sobre una
  explicación escrita cuya versión de plantilla ya no es la vigente— y está fuera del alcance de
  este brief. **Queda para el coordinador.**
- **Nada obliga a subir la versión cuando el texto cambia.** El test nuevo afirma que subirla
  funciona, no que se haya subido: un cambio futuro de la plantilla que se olvide de la constante
  vuelve a dejar atrás lo ya escrito, con la compuerta en verde. El comentario de la constante lo
  dice; una comprobación automática exigiría fijar el texto contra la versión, y no la escribí.
- **`DesignAgent/Salvo-Portability.md:72` sigue con `ANTHROPIC_MODEL="claude-sonnet-5"`**, pendiente
  heredado de `E7A` y `E7B` y fuera de los paths de esta tarea.
- **Los nombres de país siguen siendo códigos** (`US`, `AR`, `BR`): declarado fuera de alcance y
  registrado como candidata de internacionalización de la Etapa 8.
- **El aviso no tiene comprobación en el smoke.** Solo se ve tras enviar el formulario, y el smoke
  lee páginas, no envía acciones. Lo cubre el test de Vitest contra el bloque renderizado.

### Integración

- Orden sugerido: rama única, sin dependencias. Etapa 7 ya integrada; esto es pulido sobre ella.
- Migraciones o pasos manuales: **ninguno**. No hay cambios de esquema. El modelo EF quedó sin
  cambios pendientes según la compuerta.
- Posibles conflictos: `DeterministicExplanationProvider.cs`, `NumberTokenizer.cs`,
  `ExplanationGoldenTests.cs` y `explanation-action.ts` si algo más los tocó en `main` desde
  `0d05d0d`.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre `main`, y
  abrir a mano una alerta del corpus para leer el párrafo y el aviso. Conviene elegir `ORD_000171`,
  que es el que ejercita la razón sin decimal. Sobre `salvo.db`, la alerta que ya tenía explicación
  sigue mostrando el párrafo viejo y no ofrece botón; el texto corregido se trae con un `POST` a
  `/api/alerts/{id}/explanation`.

## `E7D-PLANTILLA-VIGENTE` — La consola puede pedir la plantilla vigente

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 7
- Rama/worktree: `claude/e7d-plantilla-vigente`
- Commit base: `2a7cff3` (`docs: register E7D on the board and refresh the master status`), que es el
  `HEAD` de `main`. El brief declara `459be2c`; los dos commits intermedios son del coordinador —el
  brief y el alta en el tablero— y no tocan código, así que la base de código es la misma.
- Commit final: `bbba7bf`
- Fecha: 2026-09-06

### Resultado

Cuando la explicación que la consola muestra la escribió una plantilla anterior a la vigente, la
analista ve un botón discreto —«Redactar con la plantilla vigente»— y el texto nuevo se escribe al
lado del anterior. La fila vieja no se toca. El aviso posterior dice qué pasó; no hay insignia ni
cartel de «desactualizada» en ninguna parte.

La costura que se corrige: `EfExplanationStore.FindAsync` busca por la identidad completa,
`templateVersion` incluida, mientras que `EfAlertStore.GetExplanationsAsync` agrupa por evaluación
sin mirar la versión. Con la plantilla en `e7-v2` la escritura no encontraba fila y la lectura
devolvía la de `e7-v1`; el bloque veía una explicación escrita, no ofrecía nada, y el arreglo de
redacción de `E7C` no llegaba a ninguna evaluación ya explicada. La última línea del handoff de
`E7C` describe exactamente ese estado.

`AlertExplanationView` gana `writtenByAnotherTemplate`, calculado al leer contra la versión que el
proveedor registrado declara y guardado en ninguna parte: la forma de `isOutdated` y de la
divergencia de banda.

### Archivos modificados

- `backend/src/Salvo.Application/Explanations/ExplanationViews.cs`
- `backend/src/Salvo.Application/Explanations/RequestExplanationHandler.cs`
- `backend/src/Salvo.Application/Alerts/AlertProjection.cs`
- `backend/src/Salvo.Application/Alerts/GetAlertHandler.cs`
- `backend/src/Salvo.Application/Alerts/ReviewAlertHandler.cs`
- `backend/tests/Salvo.Api.IntegrationTests/AlertSchemaTests.cs`
- `backend/tests/Salvo.Api.IntegrationTests/ExplanationTemplateVersionTests.cs`
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts` (recapturados)
- `frontend/src/lib/api/guards.ts` y `guards.test.ts`
- `frontend/src/test/fixtures.ts`
- `frontend/src/app/alerts/[id]/explanation-block.tsx` y `explanation-block.test.tsx`
- `frontend/src/app/alerts/[id]/explanation-actions.tsx`
- `frontend/src/app/alerts/[id]/explanation-action.ts` y `explanation-action.test.tsx`
- `frontend/src/app/alerts/[id]/explanation-state.ts`
- `scripts/smoke-ui.sh`

Sin migraciones: no hay columna nueva. `salvo.db` no se tocó.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | Verde. 94 tests de dominio + 149 de integración, 209 de frontend, ESLint sin advertencias, build Release y build Next.js |
| `./scripts/smoke-ui.sh` | Verde: 43 comprobaciones, 0 fallas (eran 37) |
| `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration» |
| `npm run api:types:check --prefix frontend` | Al día tras recapturar el contrato |
| `git status --porcelain` | Limpio; todo lo tocado está en los paths autorizados |

Falsaciones, cada una sobre el test que debía atraparla:

| Mutación | Test que cae |
| --- | --- |
| La proyección compara contra `"e7-v2"` copiado en vez del parámetro | `TheComparisonFollowsTheRegisteredProviderAndNotAConstant` |
| `GetExplanationsAsync` ordena ascendente y se queda con la fila vieja | `ANewTemplateVersionWritesBesideTheOldRowAndTheConsoleReadsTheNewOne` |
| Se agrega `template_version` a los nombres que el test de esquema prohíbe | `WhetherTheCurrentTemplateWroteTheTextIsComputedAndNeverStored` |
| `askOf` deja de mirar la versión | Los dos tests nuevos del bloque |
| `askOf` invierte la condición | Cuatro tests más del bloque: el botón aparece sobre la plantilla vigente |
| La pregunta de la plantilla vigente viaja como `regenerate: true` | «solo el reintento viaja como regeneración» |
| El aviso posterior usa una oración de la leyenda | «no repite ninguna oración de la leyenda del bloque: currentTemplate» |

El smoke se falsó solo: la primera corrida del escenario nuevo falló en «plantilla determinista
(e7-v1)» sobre una página cuyo DOM decía lo correcto. Expresiones JSX adyacentes son nodos de texto
separados en el HTML del servidor, y el smoke lee ese HTML, no un DOM. La letra chica pasó a ser una
sola interpolación.

### Evidencia sobre la base

Sobre una **copia** de `backend/src/Salvo.Api/salvo.db` —la base del usuario quedó intacta y se
verificó después— con la API apuntando a la copia:

| Momento | Filas de `alert_explanations` |
| --- | --- |
| Antes | `35167dc3` · `e7-v1` · `READY` · 1 intento · `2026-09-06T15:26:57.592Z` · «Coincidieron 4 reglas» |
| Lectura antes | `templateVersion: e7-v1`, `writtenByAnotherTemplate: true` |
| `POST` con `regenerate: false` | `applied: true`, fila `41509015` · `e7-v2` · `READY`, `writtenByAnotherTemplate: false` |
| `POST` con `regenerate: true` sobre la vigente | `409`, la negativa de siempre |
| Después | Las dos filas. La vieja idéntica: mismo `id`, `status`, `attempt_count`, `row_version` 2 e instantes; su texto sigue diciendo «Coincidieron». La nueva dice «Se dispararon» |

### Decisiones y supuestos

- **El campo se llama `writtenByAnotherTemplate`, no `writtenByAnOlderTemplate`.** Es una
  desigualdad, no un orden: las versiones son cadenas opacas y tras un rollback la fila guardada es
  la más nueva de las dos. La oferta de la consola es la misma en ambos casos.
- **La versión vigente entra por el puerto `IExplanationProvider`.** `GetAlertHandler` y
  `ReviewAlertHandler` lo reciben por esa única cadena. Application ya conocía el puerto, así que no
  hizo falta una abstracción nueva y la comparación sigue al proveedor registrado por construcción.
- **El botón manda `regenerate: false`,** como pide el brief, y la traducción vive en la acción de
  servidor: el formulario manda un `ask` que nombra la situación, no la petición. Un `ask` que la
  acción no conoce se lee como la pregunta más inocente, nunca como la que reemplaza un párrafo.
- **La tabla de estado × `regenerate` no cambió de sentido.** Cruzando un cambio de versión no hay
  fila que reemplazar, así que el `POST` cae en la rama de «no existe» y reserva una nueva; ninguna
  de sus filas describe ese caso mal.
- **El smoke escribe la fila anterior con `node:sqlite`**, que viene con el Node que el repositorio
  fija, en vez de depender del binario `sqlite3`. Va sobre la base temporal del escenario y sobre
  una segunda alerta, para no destruir el estado que verifica el escenario `1c`.
- Un fallo de otra plantilla recibe la oferta de la vigente y no un reintento, incluso con los
  intentos agotados: el pedido no toca esa fila, así que su presupuesto describe otra cosa.

### Riesgos o pendientes

- Nadie regenera en masa al subir la versión, por diseño: cada explicación vieja se corrige cuando
  alguien la mira. En `salvo.db` hay **una sola** fila `e7-v1`; el texto corregido se trae abriendo
  esa alerta y pulsando el botón.
- La consola no nombra la versión vigente, solo la que escribió el texto. El rótulo del botón dice
  lo demás. Si alguna vez hace falta contrastar las dos, el contrato tendría que exponerla.
- La pantalla no distingue un `PENDING` de otra plantilla: ofrece la vigente igual, lo que es
  correcto pero convive con la leyenda «Redactando la explicación…». No se puede producir con esta
  instalación y no se cubrió con un test.

### Integración

- Orden sugerido: rama única, sin dependencias. Etapa 7 ya integrada; esto cierra su costura.
- Migraciones o pasos manuales: **ninguno**. El contrato ya viene recapturado; `E7D` reservó
  `salvo-openapi.json` y `schema.d.ts`.
- Posibles conflictos: `explanation-block.tsx`, `explanation-actions.tsx`, `explanation-action.ts`,
  `guards.ts` y `scripts/smoke-ui.sh` si algo más los tocó en `main` desde `2a7cff3`.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre `main`, y
  abrir en la consola la alerta de `salvo.db` que ya tiene explicación: debe mostrar el párrafo
  viejo con «(e7-v1)» en la letra chica y ofrecer el botón. Pulsarlo deja dos filas y el texto con
  «Se dispararon».

## `E8A-README-DIAGRAMAS` — El README como argumento, y su verificación ejecutable

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 8
- Rama/worktree: `claude/e8a-readme-diagramas`
- Commit base: `95d5db3` (`docs: add the Etapa 8 task briefs`), el que declara el brief. Dos commits
  del coordinador quedan debajo del trabajo tras el rebase: `5ded6e1`, que corrigió el brief y
  terminó la sincronización canónica que el brief daba por hecha, y `cb02dba`, que cerró tres de los
  pendientes de esta entrega.
- Commit final: el de esta entrada de handoff. El trabajo va en `c1c7c62`, `47df6d3` y el commit de
  los diagramas endurecidos.
- Fecha: 2026-09-06

### Resultado

El README dejó de ser falso y pasó a ser el argumento del proyecto: nueve secciones, cuatro
diagramas Mermaid, cinco decisiones citadas por su número, y cada afirmación de hecho escrita con el
archivo que la sostiene abierto. `scripts/check-docs.sh` hace exigible la mitad mecánica de eso y
entró en la compuerta. Los cuatro documentos derivados dejaron de contradecir al código, y tres
comentarios que nombraban a la «Etapa 8» para lo que hoy es la 9 quedaron corregidos.

Ninguna línea de código de producción cambió: los tres archivos de código que se tocaron cambiaron
solo el texto de un comentario.

### Archivos modificados

- `README.md` — reescrito completo.
- `scripts/check-docs.sh` — nuevo.
- `scripts/check.sh` — una invocación, en primer lugar.
- `DesignAgent/Salvo-Overview.md`, `Salvo-MOC.md`, `Salvo-Getting-Started.md`, `Salvo-Portability.md`.
- `frontend/src/app/dashboard/quality-section.tsx`, `frontend/src/components/console-header.tsx`,
  `backend/src/Salvo.Domain/Explanations/SignalFacts.cs` — solo texto de comentarios.
- `Coordination/Handoffs/Claude.md` — esta entrada.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check-docs.sh` | 54 comprobaciones, 0 fallas, en 0,49 s |
| `./scripts/check.sh` | `EXIT=0`, repetida tras el rebase y tras rehacer los dos diagramas: `check-docs.sh` 54/0, compilación correcta con 0 advertencias, 94 + 149 tests .NET, 209 de frontend y build de producción |
| Falsación 1: ruta inexistente en el README | Falla, `exit 1`, nombrando la ruta y su línea |
| Falsación 2: test inexistente en el README | Falla, `exit 1`, nombrando el identificador buscado |
| Falsación 3: enlace Markdown roto | Falla, `exit 1`, nombrando el destino |
| Cifras del corpus regeneradas sobre base nueva | Migración, seed, corrida, evaluación externa y callbacks sobre una base temporal; `salvo.db` intacta |
| `grep` de cifras fuera del bloque marcado | Ninguna dependiente del corpus. La comprobación encontró una: `F1 = 1,00` estaba en «Límites declarados», fuera del bloque. Corregida |
| `git status --porcelain` | Limpio; solo los paths autorizados en el diff contra la base |

### Las tres falsaciones de `check-docs.sh`

Un script de verificación que nunca se vio fallar no verifica nada. Las tres se hicieron sobre el
README real y se revirtieron después.

**1 — Ruta inexistente.** Se agregó ``La regla vive en `backend/src/Salvo.Domain/Risk/VelocityRule.cs`.``

```
  FALLA  ruta     backend/src/Salvo.Domain/Risk/VelocityRule.cs
         README.md:495 — no existe en el árbol de trabajo.

54 comprobaciones, 1 fallas.
```

**2 — Test inexistente.** Se agregó ``Lo afirma `TemporalRiskEngineTests.TheBaselineNeverLooksForward`.``,
que es un nombre plausible en una clase que sí existe. El script busca el método, no la clase, que es
justamente la deriva que hay que cazar:

```
  FALLA  test     TemporalRiskEngineTests.TheBaselineNeverLooksForward
         README.md:495 — «TheBaselineNeverLooksForward» no aparece en backend/tests ni en frontend/src.

54 comprobaciones, 1 fallas.
```

**3 — Enlace roto.** Se agregó `Ver el [guion de demo](DesignAgent/Salvo-Demo-Script.md).`, que es
exactamente el enlace que `E8B` va a querer poner:

```
  FALLA  enlace   DesignAgent/Salvo-Demo-Script.md
         README.md:495 — el destino del enlace no existe.

54 comprobaciones, 1 fallas.
```

Las tres devolvieron `1`. Con el README restaurado, 54 comprobaciones y 0 fallas.

### Tabla de verificación del README

Cada afirmación de hecho del README, con el archivo que la sostiene, el comando que la produce o el
test que la afirma. Las que no tenían fuente no entraron. Las rutas y los nombres de test de esta
tabla los vuelve a comprobar `scripts/check-docs.sh` en cada corrida de la compuerta, así que la
tabla no es la única defensa.

#### Qué es y qué no

| Afirmación | Fuente |
| --- | --- |
| Seis reglas, con esos nombres | `backend/src/Salvo.Domain/Risk/RiskRuleNames.cs:5-10` |
| Pesos 40, 40, 30, 30, 20 y 10 | `RuleConfig.cs:16,23,20,34,39,29` |
| Ventanas: 90 días monto, 10 min velocity, 2 h cross-border, 30 días y franja de 6 h en `unusual_hour`, 90 días país | `RuleConfig.cs:12,18,22,25-28,36` |
| Tope 100 y umbral 60 | `RuleConfig.cs:8-9` |
| Bandas 60–69, 70–89, 90–100 | `AlertPolicy.cs:35-38` |
| La severidad no se persiste; se persiste la versión de la política | `AlertPolicy.cs:9-13`; la tabla `alerts` tiene `alert_policy_version` y no tiene columna de severidad |
| La IA no escribe en ninguna superficie de decisión | `ExplanationIsolationTests.ExplainingEveryAlertChangesNoDecisionSurface` |
| `isFraudLabel` solo mide, en superficie aparte tras `DemoData:Enabled` | `SystemEndpoints.cs:19-23`; `DashboardEndpointTests.TheDashboardIsIndependentOfGroundTruth` |

#### Simulación, sandbox y producción

| Afirmación | Fuente |
| --- | --- |
| Ocho requisitos antes de un sandbox, con esa lista | Blueprint §5.3, ocho viñetas |
| `KOIN_MODE=sandbox` y `AI_PROVIDER=anthropic` hacen fallar el arranque | `DependencyInjection.cs:95-106` y `:128-138` |
| Un valor desconocido en cualquiera de las dos también falla | mismas líneas: la rama `else` del mensaje |
| Cabecera `X-Salvo-Callback-Secret` | `ExternalCallbackEndpoints.cs:12` |
| Sin secreto configurado, `401` a todo | `ExternalCallbackEndpoints.cs:31-37`; `ExternalCallbackEndpointTests.WithNoSecretConfiguredEveryCallbackIsRefused` |
| No es el mecanismo de una integración real | `ExternalCallbackEndpoints.cs:38-43`, que lo dice en su propio comentario |

#### El recorrido de un pedido

| Afirmación | Fuente |
| --- | --- |
| Importar no procesa; la corrida es una acción explícita | `frontend/src/app/import/page.tsx:45-48`; Blueprint §3 pasos 1 y 2; decisión 39 |
| Validación estricta por registro, escritura atómica por archivo | `ImportOrdersHandler.cs`; `import/page.tsx:78-80` |
| Baseline solo con historia estrictamente anterior | `TemporalRiskEngineTests.AddingFutureOrdersCannotChangeEarlierAssessments` |
| Como máximo una alerta abierta por pedido | índice único parcial `ux_alerts_open_order` sobre `order_id WHERE status = 'OPEN'` |
| El veredicto es terminal y su auditoría va en la misma transacción | `AlertReviewTests.TwoConcurrentReviewsLeaveOneVerdictAndExactlyOneAudit`; `CHECK ck_alerts_review_consistency` |

#### Las tres fuentes de verdad

| Afirmación | Fuente |
| --- | --- |
| `risk_evaluations` restringida a `source = 'LOCAL'` por `CHECK` | `RiskEvaluationConfiguration.cs:19-20` |
| Su estado solo puede ser `APPROVED` o `DENIED` | `RiskEvaluationConfiguration.cs:22-23` |
| Lo vigente lo define `run_evaluations` de la corrida, no la fila más reciente | `ScoringRunPersistenceTests.AScoreThatReturnsToAnEarlierValueKeepsTheCurrentEvaluationCorrect` |
| `alert_reviews.explanation_id` es una clave foránea real | `FK_alert_reviews_alert_explanations_explanation_id` en el esquema |
| Las diez tablas del diagrama, con esos nombres y esas columnas | `ToTable(...)` en `Salvo.Infrastructure/Persistence/Configurations/`; esquema leído con `sqlite3 .schema` sobre la base regenerada |
| `external_evaluations.error_code` es catálogo cerrado y nunca el mensaje del proveedor | `ExternalEvaluationErrorCode.cs:1-12` |

#### Las cinco decisiones

| Afirmación | Fuente |
| --- | --- |
| Decisiones 29 y 31, con ese contenido | bitácora del Blueprint, líneas 746 y 748 |
| Decisión 33 | línea 750 |
| Decisiones 37 y 38 | líneas 754 y 755 |
| Decisiones 45 y 47 | líneas 762 y 764 |
| Decisiones 52 y 54 | líneas 769 y 771 |
| El rebote 0 → 40 → 0 está escrito como test | `ScoringRunPersistenceTests.AScoreThatReturnsToAnEarlierValueKeepsTheCurrentEvaluationCorrect` |
| Reusar en vez de duplicar cuando el corpus no cambió | `ScoringRunPersistenceTests.RepeatedRunOverTheSameCorpusAppendsNothingAndReferencesTheSameEvaluations` |
| La identidad es el contenido, no la fila | `RiskEvaluationIdentityTests.FingerprintIdentifiesContentAndNotTheRow` |
| La alerta conserva su snapshot y expone la divergencia | `AlertCreationTests.AnOpenAlertKeepsItsSnapshotAndExposesTheDivergenceAfterABackfill` |
| Una escalada abre una alerta nueva enlazada | `AlertCreationTests.AnEscalationAfterABackfillOpensANewAlertLinkedToTheReviewedOne` |
| El dashboard no cambia al invertir las etiquetas | `DashboardEndpointTests.TheDashboardIsIndependentOfGroundTruth` |
| Reserva antes de llamar, una sola evaluación bajo concurrencia | `ExternalEvaluationConcurrencyTests.TwoConcurrentRequestsCallTheProviderOnceAndLeaveOneEvaluation` |
| `TIMEOUT`, `PROVIDER_ERROR` e `INVALID_RESPONSE` dejan `PENDING`; `UNREACHABLE` y `PROVIDER_REJECTED` cierran | `ExternalEvaluationErrorCode.cs:7-12`; `ExternalProviderExchange.cs`, ramas `Apply` |
| Un timeout lo resuelve la reconciliación en una sola fila | `ExternalEvaluationReconciliationTests.ATimeoutIsResolvedByReconciliationIntoASingleRow` |
| Dos rutas de correlación de un callback | `IAntifraudProvider.GetStatusAsync` toma el lookup entero, no el identificador; `ExternalCallbackRaceTests.ACallbackThatArrivesBeforeTheIdentifierIsWrittenDownStillSettlesTheEvaluation` |
| Un duplicado es la ausencia de una segunda fila | `ExternalCallbackEndpointTests.DeliveringTheSameCallbackTwiceIsARecordedReplay` |
| Tres capas de grounding, en ese orden, y la forma primero | `ExplanationGrounding.cs:61-69` y el comentario de `:72-76` |
| Tope de 1200 caracteres, igual al de la base | `ExplanationGrounding.cs:36-40` |
| La verificación vive en el caso de uso, entre puerto y almacenamiento | `RequestExplanationHandler.cs:207-224`; el comentario `:16-17` describe el test de mutación |
| El texto rechazado no se persiste; queda el código y el token, nunca la frase | `RequestExplanationHandler.cs:207-224`; `GroundingVerdict` en `ExplanationGrounding.cs:6-11` |
| Un tokenizador único que corre sobre los dos lados | `NumberTokenizer.cs:24-33` |
| Al modelo no entra texto que no escriba el motor | `ExplanationIsolationTests.NoTextTheEngineDidNotWriteReachesTheProvider` |
| Invertir las etiquetas no cambia una palabra del texto | `ExplanationIsolationTests.InvertingEveryLabelChangesNoSummary` |
| Una cifra inventada se rechaza y no queda nada escrito | `ExplanationGroundingTests.AnInventedFigureIsRefusedAndNoTextIsStored` |

#### Cómo se verifica

| Afirmación | Fuente |
| --- | --- |
| El orden exacto de los pasos de la compuerta | `scripts/check.sh`, tal como quedó en esta tarea |
| El smoke cubre cinco rutas | `scripts/smoke-ui.sh`: `/`, `/import`, `/alerts`, `/alerts/${alert_id}` y `/dashboard` |
| En seis escenarios | `smoke-ui.sh:279, 327, 376, 409, 466, 479` |
| Puertos propios, bases temporales, no borra nada | `smoke-ui.sh:44-57, 74-86` |
| Los tests no hacen red | `AGENTS.md`, regla de calidad; los dos proveedores son mock en proceso |
| Ningún componente cliente recibe objetos de la API | `frontend/src/test/boundary.test.ts` |
| El contrato capturado no derivó | `OpenApiDriftTests` |
| El seed es idempotente y sin datos personales | `DemoSeedTests.SeedIsFullyIdempotentAndContainsOnlySyntheticPseudonymousData` |

#### Límites declarados

| Afirmación | Fuente |
| --- | --- |
| La advertencia de la fixture está escrita en la consola | `frontend/src/app/dashboard/quality-section.tsx:26-34`, constante `FIXTURE_CAVEAT` |
| Solo tres de las seis reglas disparan sobre el corpus | corrida regenerada: `foreign_country` 34, `amount_anomaly` 18, `new_buyer_high_value` 5; las otras tres, cero |
| `unusual_hour` exige una franja de 6 h con no más del 10 % en 30 días | `RuleConfig.cs:26-29` |
| La banda 70–89 no aparece | corrida regenerada: `bySeverity` da `HIGH: 0` |
| Los detalles de las señales están en inglés y el fingerprint los hashea | `RiskSignalSerializerTests.cs:68-71`; `SignalFacts.cs:18-27` |
| `SignalFacts` ya convierte esos detalles en campos tipados | `backend/src/Salvo.Domain/Explanations/SignalFacts.cs` |
| Sin autenticación, la aplicación es local | `AGENTS.md`, decisiones invariantes |

#### Cómo correrlo

| Afirmación | Fuente |
| --- | --- |
| .NET 10.0.400 | `global.json` |
| Node 24.20.0 y npm 11.19.0 | `.nvmrc` y `frontend/package.json:6-9` |
| `DemoData:Enabled` enciende el botón demo, los dos del proveedor y la calidad del criterio | `SystemEndpoints.cs:19-23`; `import/page.tsx:70` y `:113` |
| 5 MiB por archivo | `OrderEndpoints.cs:10` |
| 10.000 registros y hasta 1.000 errores con detalle | `ImportOrdersHandler.cs:13-14` |
| Ninguna clave con prefijo `NEXT_PUBLIC_`; Next nunca lee el secreto | `.env.example`; `ExternalCallbackEndpoints.cs:15-17` |
| Las rutas de datos son dinámicas y el build pasa con la API apagada | `force-dynamic` en las cuatro rutas de datos; el build de la compuerta corre sin API |
| El dominio no referencia framework | `ArchitectureSmokeTests.DomainAssemblyCanBeLoadedWithoutFrameworkDependencies` |

#### Los cuatro diagramas

| Diagrama | Estado |
| --- | --- |
| El recorrido de un pedido | Renderiza. Rótulos de arista en la forma `-->|texto|` |
| Las tres fuentes de verdad | Renderiza. Rótulos de relación y comentarios de atributo entrecomillados, **con acentos** |
| La máquina de estados externa | Renderiza sin superposiciones, en la versión que el coordinador verificó. Rehecho dos veces. Ver abajo |
| Cómo se verifica una explicación | Renderiza. `flowchart LR` con dos subgrafos con título entrecomillado |

Ninguno lleva cifras del corpus, como pide D2.

**El coordinador los renderizó con `mermaid-cli` y los cuatro salen sin error de sintaxis**, así que
el criterio de renderizado está cumplido. Mirarlos, sin embargo, encontró dos cosas que la sintaxis
correcta no impide, y las dos se corrigieron:

- **Los comentarios de atributo del `erDiagram` habían perdido los acentos** —«catalogo cerrado»,
  «lo unico que define vigente», «que explicacion tenia delante»— mientras que los rótulos de
  relación del mismo diagrama sí los llevaban. Los había quitado por precaución al endurecer la
  sintaxis, sin comprobar que hicieran falta; el coordinador verificó que Mermaid los admite ahí
  renderizando un caso con «catálogo», «qué explicación tenía» y «lo único». Restaurados. La lección
  es la que ya conoce este proyecto: degradar el texto «por si acaso» es una decisión, y una
  decisión sin comprobar es lo mismo que una afirmación sin comprobar.
- **La máquina de estados era el más difícil de leer de los cuatro.** Tenía `PENDING --> APPROVED` y
  `PENDING --> DENIED` dos veces cada una —una por respuesta en el acto y otra por callback o
  reconciliación—, y las cuatro aristas se cruzaban en el medio; la nota quedaba a la izquierda con
  una línea de puntos que atravesaba el dibujo. Cada par se unificó en una sola transición con la
  etiqueta combinada, «en el acto, por callback o por reconciliación», y las dos aristas que
  importan absorbieron su porqué en el rótulo: `TIMEOUT, PROVIDER_ERROR o INVALID_RESPONSE — salió y
  no se sabe` frente a `UNREACHABLE o PROVIDER_REJECTED — no salió, o rechazo definitivo`. El
  diagrama pasó de nueve aristas a siete.

  **La nota se reubicó fuera del diagrama**, al párrafo que ahora lo sigue, en vez de a otro lado
  del dibujo. El motivo: lo que la nota explicaba —que los tres códigos describen una petición que
  sí salió, que se anota `lastErrorCode` y que se vuelve a preguntar— es prosa, y la única razón por
  la que estaba dentro del diagrama era la costumbre. Fuera no puede cruzar nada, y el diagrama
  sigue diciendo lo único que la tabla del §4.5 no dice, que es qué códigos dejan la fila `PENDING`
  y cuáles la cierran: eso está en los rótulos de las dos aristas, no en la nota. Si preferís la
  nota dentro, vuelve en una línea.

### Inventario de cifras del corpus que viven fuera del README

La regla del bloque marcado es ciega fuera del README. Esta es la lista con la que la Etapa 9
empieza, para que no empiece con un `grep`. Está verificada archivo por archivo hoy, no copiada del
borrador del hallazgo 7 de la revisión adversarial: **tres de sus referencias estaban corridas** y
una de sus entradas resultó ser dos cosas distintas.

| Dónde | Qué dice | Qué pasa en la Etapa 9 |
| --- | --- | --- |
| `frontend/src/app/import/page.tsx:66` | «Trescientos pedidos sintéticos con sus etiquetas de fraude» | Texto de pantalla: cambia con el corpus, y sale en la captura de `/import` de `E8B` |
| `backend/src/Salvo.Infrastructure/External/MockAntifraudProvider.cs:21-23` | Sobre `demo-orders.v1.json`, las bandas producen 225 aprobadas, 45 denegadas, 21 pendientes y 9 en error | Comentario que declara el reparto sobre este corpus; las bandas en sí, 75/90/97, no dependen del corpus |
| `backend/tests/Salvo.Api.IntegrationTests/ExternalEvaluationIsolationTests.cs:167-171` | Fija esos cuatro números como aserción | **Rompe** con un corpus nuevo. Es el primer test que va a fallar en la Etapa 9 |
| `backend/tests/Salvo.Api.IntegrationTests/ExplanationGoldenTests.cs:30-58` | Los dos textos dorados: `ORD_000011` con score 90, 2.011,11 BRL y 23,2 veces la mediana; `ORD_000171` con score 60 y ratio 15,0 | **Rompen** si esos dos pedidos cambian. Son las dos ramas del formato decimal, así que hay que reelegir dos pedidos con la misma propiedad |
| `backend/tests/Salvo.Domain.Tests/ExplanationFactsTests.cs:18-24` | Las tres señales de `ORD_000011`, score 90, en orden canónico | Datos de corpus embebidos en un test de dominio. Nota: el `56.0` de `:44` **no** es corpus, es un caso del tokenizador |
| `frontend/src/app/dashboard/page.test.tsx:65, 186, 195, 213, 280` | «300 pedidos en la corrida» y variantes | Los montos por moneda de `:98` son inventados para el test; el que sí es del corpus es su suma |
| `frontend/src/app/dashboard/page.test.tsx:98-101` y `scripts/smoke-ui.sh:318` | `3.942.246`, la suma de las tres monedas, que no puede aparecer en pantalla | **Es del corpus.** La corrida regenerada da 1.898.778 + 1.279.386 + 764.082 = 3.942.246 exacto, lo que además confirma las cifras del bloque marcado |
| `DesignAgent/Salvo-Blueprint.md:129-130` | 300 pedidos, ventana de 120 días, 18 fraudes y 282 legítimos | Del coordinador |
| `DesignAgent/Salvo-Blueprint.md:736`, decisión 19 | «fixture fija de 300 pedidos y 300 etiquetas» | Del coordinador. Una decisión de la bitácora no se reescribe: se agrega otra |
| `Coordination/Workboard.md:82-90` | 18 alertas con `amount_anomaly` y `foreign_country`; la franja más rara en 13,8 % | Del coordinador. Es la nota que justifica la Etapa 9 |
| `Coordination/Workboard.md:110` y `DesignAgent/Salvo-Progress.md:46, 146, 318` | 18 alertas, 13 `MEDIUM`, 5 `CRITICAL`, 300 evaluaciones | Del coordinador. Son evidencias fechadas de una etapa cerrada: describen lo que se verificó ese día y probablemente deban quedarse como están |

Dos cosas que este inventario deja claras y que conviene decidir antes de empezar la Etapa 9. La
primera es que **el corpus nuevo rompe tests, no solo textos**: los cuatro números del mock y los dos
textos dorados son aserciones, y hay que decidir si el corpus se diseña para conservarlos o si se
reescriben. La segunda es que **las evidencias históricas del Progress no son cifras a actualizar**:
dicen qué se observó en una fecha, y corregirlas sería falsificar el registro.

### Decisiones y supuestos

- **El `grep` de cierre encontró una cifra fuera del bloque, y valió la pena hacerlo.** «F1 vale
  1,00» estaba en el cuerpo de «Límites declarados» y es exactamente lo que la Etapa 9 va a
  invalidar: su propia línea dice que F1 deja de valer 1,00. El cuerpo ahora dice «la puntuación es
  perfecta» y remite al bloque, donde está el valor.
- **El bloque marcado va dentro de «Límites declarados».** Es donde el lector ya está leyendo qué
  tan lejos llegan estas cifras, y deja el resto del documento libre de números volátiles. Las
  estructurales —seis reglas, sus pesos, umbral 60, tres bandas, 5 MiB, 10.000 registros— se quedan
  en el cuerpo, como pide D2.
- **Las cifras se regeneraron, no se copiaron.** Ninguna salió del Progress ni de `salvo.db`, que
  tiene 328 pedidos y siete corridas. Se migró una base nueva en un directorio temporal, se cargó el
  corpus, se corrió el scoring, se pidió la evaluación externa del corpus entero y se entregaron sus
  callbacks, y se leyeron `/api/dashboard` y `/api/evaluation-metrics`. `salvo.db` no se tocó.
- **`check-docs.sh` comprueba tres clases de token, no dos.** El brief pide rutas y nombres de test;
  agregué los destinos de los enlaces Markdown relativos porque el mapa de documentación es
  exactamente la parte de un README que se pudre sin que nadie la lea, y porque la tercera falsación
  muestra que atrapa el enlace que `E8B` va a querer poner antes de tiempo.
- **Una ruta es un token entre acentos graves con al menos una barra**, que no empiece con `/` —eso
  es una ruta HTTP o de la consola—, que no sea una URL, que no lleve `=` y que no tenga espacios.
  Con ese criterio `POST /api/demo-data/seed`, `/dashboard`, `http://localhost:3000/import` y
  `Data Source=salvo.db` quedan fuera solos. El único falso positivo previsible sería un
  identificador con barra que no es ruta, como un huso horario: el README no escribe ninguno, y si
  alguno hiciera falta, va sin acentos graves.
- **De `Clase.Metodo` se busca el método, no la clase.** Una clase que sigue existiendo con el
  método renombrado es justamente la deriva que hay que cazar, y buscar la clase la dejaría pasar.
- **`check-docs.sh` entró en `check.sh`, y primero.** No necesita dependencias instaladas ni ningún
  proceso escuchando, y tarda 0,49 s. Para llegar ahí hubo que excluir `bin/` y `obj/` de la
  búsqueda: con los artefactos de compilación adentro, veinte `grep` recursivos tardaban más de un
  minuto, que es lo que separa un paso de compuerta de un paso que nadie quiere correr.
- **El README no cita cantidades de tests.** La compuerta de esta entrega dio 94 + 149 tests .NET y
  209 de frontend; ninguno de esos números está en el README, a propósito.
- **El pitch dice «no es un proveedor», no «no es un motor».** Salvo sí tiene motor de reglas; lo que
  no tiene es la posición de un proveedor. La v1 del diseño decía lo contrario y contradecía el §1
  del Blueprint.
- **Los diagramas se endurecieron antes de darlos por buenos.** Dos construcciones se cambiaron por
  formas más conservadoras: los rótulos de arista entrecomillados pasaron a `-->|texto|`, y el
  diagrama de estados tenía rótulos entrecomillados y con `<br/>`, que en `stateDiagram-v2` se
  dibujan literalmente. No es lo mismo que verlos renderizados, y está en los pendientes.

### Riesgos o pendientes

- **Los tres pendientes que esta entrega dejó abiertos los cerró el coordinador** en `cb02dba`, que
  entró por rebase: el §9 del Blueprint nombra `SALVO_CALLBACK_SHARED_SECRET` y explica que es la
  variable que el endpoint lee de verdad; la cabecera de `scripts/smoke-ui.sh` dice cinco rutas y
  seis escenarios, con la lista de rutas explícita; y la fila del Workboard lleva la reserva completa
  de `E8A`. Comprobado que el README quedó coherente con la cabecera corregida: las dos dicen las
  mismas cinco rutas —`/`, `/import`, `/alerts`, el detalle de una alerta y `/dashboard`— y los
  mismos seis escenarios, en el mismo orden. El README no había copiado el error; lo había corregido
  por su cuenta contra el código, que es por lo que se notó.
- **El renderizado de los cuatro diagramas está confirmado** con `mermaid-cli`, y mirarlos produjo
  las dos correcciones de arriba. Ya no es un pendiente de integración.
- **El segundo renderizado se hizo, y encontró una superposición**: el rótulo del bucle
  `PENDING --> PENDING` caía encima del de `PENDING --> APPROVED` y el texto quedaba ilegible.
  `stateDiagram-v2` pone el rótulo de un bucle en la misma columna que el de la arista contigua, así
  que el choque no depende del texto: el coordinador probó tres variantes —moverlo al final, que
  choca con `ERROR`, y acortarlo a «sigue pendiente», que choca igual— y todas fallaron. El diagrama
  quedó reemplazado por la versión que él verificó renderizada: el bucle sin rótulo, la nota de
  vuelta —su línea de puntos ya no cruza nada, porque el dibujo tiene dos aristas menos que antes— y
  los cinco códigos adentro. El párrafo siguiente se recortó a una línea para no repetir lo que la
  nota ya dice.

  Vale la pena registrar por qué hicieron falta dos vueltas. Haber sacado la nota del diagrama era
  correcto como razonamiento y el renderizador lo derrotó igual, porque el problema no estaba en la
  nota sino en el rótulo del bucle, que el argumento no tocaba. Es la tercera vez en esta entrega
  que un diagrama sintácticamente válido resulta ilegible: **parsear y verse bien son dos
  comprobaciones distintas**, y solo la primera la puede hacer un script.
- **La Etapa 9 rompe tests, no solo textos.** Está en el inventario: los cuatro números del mock y
  los dos textos dorados son aserciones sobre este corpus.

### Integración

- Orden sugerido: esta rama sola. No depende de nada y `E8B` depende de ella integrada.
- Migraciones o pasos manuales: ninguno. No hay migración, ni cambio de contrato, ni recaptura de
  OpenAPI, ni dependencia nueva.
- Posibles conflictos: `scripts/check.sh` gana una línea al principio; cualquier otra tarea que lo
  toque va a conflictuar ahí. `Coordination/Handoffs/Claude.md` crece al final, como siempre.
- Verificación posterior al merge: `./scripts/check.sh` —que ahora incluye `check-docs.sh`— y
  `./scripts/smoke-ui.sh` sobre el estado integrado. Los cuatro diagramas ya están renderizados y
  mirados; no queda nada de esta entrega sin verificar.

## `E8B-DEMO-CAPTURAS` — Capturas, guion de demo y las dos formas de reproducirlos

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 8
- Rama/worktree: `claude/e8b-demo-capturas`
- Commit base: `d0d8c03`, el `merge-base` real con `main`
- Commit final: `bf70c08`
- Fecha: 2026-09-06

### Resultado

Cualquiera que clone el repositorio puede **regenerar las seis capturas con un comando** y **ensayar
la demo tantas veces como quiera** sobre una base nueva, sin tocar la suya. El guion de diez minutos
existe, con la frase, el clic y una línea «si falla» por bloque, y el README enlaza las dos cosas.

Las cuatro muestras de importación, que vivían en `_local/` y por lo tanto no existían para nadie
más, quedaron versionadas con los códigos de error que devuelve la API de verdad.

### Archivos modificados

- `docs/muestras/`: los cuatro archivos, copiados sin modificar desde `_local/muestras/`, y un
  README propio.
- `docs/capturas/`: las seis PNG y la nota de regenerables con el texto alternativo de cada una.
- `docs/guion-demo.md`: el guion de diez minutos, en siete bloques.
- `scripts/demo.sh`: levanta la demo sobre una base nueva con fecha.
- `scripts/capturas.sh`: regenera las capturas sobre una base temporal.
- `tools/capturas/`: `package.json`, `package-lock.json` y `capturar.mjs`.
- `README.md`: la sección de capturas y los dos comandos nuevos en «Cómo correrlo».

`frontend/package.json` y `frontend/package-lock.json` **no cambian**, y
`git diff` sobre `frontend/` entre la base y el final está vacío. `scripts/smoke-ui.sh` está byte a
byte igual al merge-base.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | **Verde**. `check-docs.sh` 68/0; build Release 0 advertencias y 0 errores; 94 tests de dominio y 149 de integración; `npm run check` con 209 tests de frontend; build de producción de Next.js |
| `./scripts/smoke-ui.sh` | **Verde**: 43 comprobaciones, 0 fallas |
| `./scripts/demo.sh` | Base nueva arriba, corpus de 300 pedidos y corrida con 18 alertas; API y consola sirviendo `200` |
| `shasum -a 256` de `salvo.db`, antes y después | **Idéntico**: `92f4f3fb…8a5ff`, 1437696 bytes, mtime `2026-09-06T14:46:58` sin cambiar |
| `./scripts/capturas.sh` ×2 seguidas | Las dos verdes, seis PNG cada vez. `git status` muestra seis `M` y ningún `D`: sobrescribe, no borra |
| `git diff --stat base..HEAD -- frontend/` | Sin salida |
| `git status --porcelain` | Limpio; todos los cambios en paths autorizados |
| Inspección visual de las seis capturas | Hecha, una por una. Las seis se leen y muestran lo que prometen |

Falsaciones, porque el valor de una comprobación es que pueda fallar:

- **La aserción previa a cada disparo falla de verdad.** La primera corrida de `capturas.sh` se
  detuvo en la toma 5 con «la página no muestra REFERENCE_CONFLICT» y **no escribió el PNG**. El
  equivocado era yo: la consola muestra la traducción del código, nunca el identificador. La
  aserción pasó a ser «La referencia ya existe con otros datos», que es texto que la pantalla sí
  escribe.
- **La resolución por pedido se niega a adivinar.** Exige que `ORD_000011` identifique exactamente
  una alerta abierta; con dos comercios usando esa numeración se detendría en vez de elegir una.
- **`check-docs.sh` sobre `docs/guion-demo.md` encontró un nombre de test inventado.** El guion
  decía `GroundedExplanationTests`; la clase es `ExplanationGroundingTests`. Un guion que te pide
  decir un nombre de test en una entrevista tiene que nombrar uno que exista.

### Decisiones y supuestos

- **Playwright, en `tools/capturas/`, versión exacta `1.63.0`.** El brief pedía verificar si el
  paquete descarga navegadores al instalarse. **No lo hace**: ni `playwright` ni `playwright-core`
  declaran script de instalación en esa versión (`hasInstallScript: false` en el lockfile), así que
  `npm ci` no baja nada y `playwright install chromium` es un paso propio del script. La descarga
  es de unos 240 MB entre el navegador y su shell headless, una vez por máquina, y el README lo dice
  junto al comando.
- **La base de `demo.sh` vive junto a `salvo.db`**, con la fecha y la hora en el nombre, para que se
  vean todas juntas y se note cuántos ensayos hubo. `*.db` está en `.gitignore`.
- **Las capturas se sacaron con todo el estado puesto.** La toma 2 muestra los cuatro bloques del
  detalle porque «el detalle» de esta consola son los cuatro, y las tomas 3 y 6 son capturas de
  elemento sobre esa misma página.
- **La toma 6 usa `ORD_000011`**, el mismo pedido de las tomas 2 y 3: su referencia cae en la banda
  11 del proveedor simulado, que aprueba por debajo de 75 y responde de forma síncrona, mientras el
  motor local lo denegó con 90. La divergencia de criterio es determinista y no necesita callback.
- **Las capturas versionadas son las que revisé a ojo.** La tercera corrida —la que cierra el
  criterio de las dos seguidas— produjo un juego equivalente que restauré con `git restore`, para
  que en el repositorio queden exactamente las imágenes que miré.

### Riesgos o pendientes

- **Dos observaciones de producto, fuera de alcance y sin tocar.** La pantalla de importación dice
  «Se importaron 1 pedidos», sin concordancia de número; y el mensaje que acompaña a cada registro
  rechazado llega en inglés desde la API, que ya es un ítem registrado para la Etapa 9. Las dos se
  ven en `docs/capturas/05-import.png`.
- **El mapa de documentación del README no menciona el guion.** El brief acota el README a las
  secciones de capturas, guion y comandos, así que no lo toqué. Es una fila para el coordinador.
- **Quedan dos bases de ensayo** de las pruebas de `demo.sh`, en `backend/src/Salvo.Api/`. Están
  ignoradas y no se borran desde acá: `rm` está denegado y es regla del proyecto.
- **El smoke informa 43 comprobaciones y el brief esperaba 37.** No las cambió esta tarea: el
  archivo está intacto respecto del merge-base y el número ya era 43 en `main` desde `E7D`.
- **Las capturas envejecen con la interfaz.** Nada las verifica automáticamente; la nota que las
  acompaña dice cuándo conviene regenerarlas.

### Integración

- Orden sugerido: esta rama sola. Cierra la Etapa 8 junto con `E8A`, ya integrada.
- Migraciones o pasos manuales: ninguno. No hay migración, ni cambio de contrato, ni de esquema, ni
  de interfaz. Ninguna línea de código de producción cambia.
- Posibles conflictos: `README.md` gana una sección y una subsección; `Coordination/Handoffs/Claude.md`
  crece al final. `main` está dos commits adelante de esta rama, los dos de coordinación, y no tocan
  ninguno de estos paths.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre el estado
  integrado. `./scripts/capturas.sh` no hace falta repetirlo: las imágenes ya están versionadas.

## `E9A-FIXTURE` — El corpus que no recupera sus propias etiquetas

### Identificación

- Work ID: `E9A-FIXTURE`
- Etapa: 9
- Rama: `claude/e9a-fixture`
- Commit base: `82f9804`
- Modelo y esfuerzo: Opus 5 · `high`
- Estado: **en curso**

### La tabla de arquetipos esperada, escrita antes de construir la fixture

Esto se escribe **primero y en su propio commit**, y ese orden es la falsación de la tarea: predecir
la celda de la matriz de cada arquetipo y después medirla es una comprobación; medir primero y
llamarlo predicción no comprueba nada. Cuando el motor real corra sobre el corpus, cada fila de esta
tabla tiene que coincidir, y toda diferencia se explica en la entrega.

De dónde sale cada score. Los seis pesos son `amount_anomaly` 40, `velocity` 30,
`cross_border_velocity` 40, `unusual_hour` 10, `new_buyer_high_value` 30 y `foreign_country` 20; el
umbral de alerta es 60 y el tope 100. Las claves importan tanto como los umbrales: `amount_anomaly`
compara contra la mediana **del comprador** cuando tiene tres pedidos previos en noventa días y
contra la del comercio cuando no; `velocity` y `cross_border_velocity` cuentan por
**(comercio, comprador)**; `unusual_hour` y `foreign_country` describen el hábito **del comercio**;
y `new_buyer_high_value` exige que el comprador no tenga **ningún** pedido previo, así que nunca
convive con las dos reglas de ráfaga.

Los tres falsos negativos son invisibles cada uno por una razón distinta, y esa es la propiedad que
los vuelve didácticos:

- **`FN1`, fraude amigo.** Un comprador con historia compra lo de siempre, lo recibe, y desconoce el
  cargo. No hay nada que ver en la transacción porque es una compra legítima hasta el contracargo.
  Lo vería el historial de disputas de ese comprador en la red de un proveedor.
- **`FN2`, cuenta tomada, vista en el dispositivo.** Comprador con historia, `deviceSessionId`
  nuevo, el doble de su mediana, tres pedidos separados por veinte minutos. Las dos reglas por
  comprador piden 3× y cuatro pedidos en diez minutos; país y hora miran al comercio. **El
  dispositivo está en el archivo desde la Etapa 2 y el motor no lo lee**, y eso es exactamente lo
  que enseña.
- **`FN3`, prueba de tarjetas.** Cinco compradores nuevos, un mismo dispositivo, montos mínimos,
  minutos entre uno y otro. `velocity` cuenta por comprador y cada comprador tiene cero previos;
  `new_buyer_high_value` pide monto alto. La regla existe y está atada a la entidad equivocada.

Los cuatro falsos positivos son el costo del criterio, y dos de ellos existen para que `velocity`,
`unusual_hour` y `amount_anomaly` en clave comprador **decidan algo por primera vez**:

- **`FP1`, el salto de país en dos horas.** No es «un cliente que viaja»: `foreign_country` vale 20
  y sola no llega al umbral. Es un comprador que hace dos compras en noventa minutos desde dos redes
  distintas —roaming, VPN o proxy—, y la segunda dispara `cross_border_velocity` más
  `foreign_country`.
- **`FP2`, la primera compra grande.** Un cliente nuevo comprando algo caro: el costo declarado de
  toda regla «nuevo y caro».
- **`FP3`, el revendedor de madrugada.** Cuatro compras alrededor de la medianoche; la cuarta cruza
  a la franja que el comercio casi no tiene y dispara las tres reglas a la vez.
- **`FP4`, el regalo desde afuera.** Un cliente habitual de viaje compra un regalo caro: dispara
  exactamente las mismas dos reglas que un `TP` de 60, y el motor no puede distinguirlos. Ese es el
  punto.

Las filas marcadas `acompañante` no son arquetipos sino los pedidos que un arquetipo necesita para
existir: la compra chica de prueba que precede a la grande, el pedido de casa noventa minutos antes
del salto de país, y las tres compras previas del revendedor.

| Referencia | Arquetipo | Comercio | Comprador | Monto | Instante local | País | Dispositivo | Etiqueta | Reglas previstas | Score | Celda | Cohorte |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ORD_000011` | TP | `MER_BR_STORE` | `BUY_000011` | 50786 BRL | 2026-05-05 02:15 | AR | `DEV_000011` | fraude | monto, nuevo, país | 90 | TP | calibración |
| `ORD_000027` | TP | `MER_UY_STORE` | `BUY_000001` | 635251 UYU | 2026-05-11 12:33 | AR | `DEV_000001` | fraude | monto, país | 60 | TP | calibración |
| `ORD_000043` | TP | `MER_UY_STORE` | `BUY_000039` | 660785 UYU | 2026-05-17 18:26 | UY | `DEV_000039` | fraude | monto, nuevo | 70 | TP | calibración |
| `ORD_000059` | TP | `MER_US_MARKET` | `BUY_000044` | 33337 USD | 2026-05-24 05:50 | BR | `DEV_000044` | fraude | monto, nuevo, país | 90 | TP | calibración |
| `ORD_000064` | FP2 | `MER_US_MARKET` | `BUY_000048` | 28832 USD | 2026-05-26 11:05 | AR | `DEV_000048` | legítimo | monto, nuevo | 70 | FP | calibración |
| `ORD_000075` | FN3 | `MER_US_MARKET` | `BUY_000052` | 270 USD | 2026-05-30 16:14 | AR | `DEV_900001` | fraude | — | 0 | FN | calibración |
| `ORD_000076` | FN3 | `MER_US_MARKET` | `BUY_000053` | 271 USD | 2026-05-30 16:17 | AR | `DEV_900001` | fraude | — | 0 | FN | calibración |
| `ORD_000077` | FN3 | `MER_US_MARKET` | `BUY_000054` | 272 USD | 2026-05-30 16:20 | AR | `DEV_900001` | fraude | — | 0 | FN | calibración |
| `ORD_000078` | FN3 | `MER_US_MARKET` | `BUY_000055` | 273 USD | 2026-05-30 16:23 | AR | `DEV_900001` | fraude | — | 0 | FN | calibración |
| `ORD_000079` | FN3 | `MER_US_MARKET` | `BUY_000056` | 274 USD | 2026-05-30 16:26 | AR | `DEV_900001` | fraude | — | 0 | FN | calibración |
| `ORD_000091` | TP | `MER_BR_STORE` | `BUY_000002` | 59857 BRL | 2026-06-06 09:12 | AR | `DEV_000002` | fraude | monto, país | 60 | TP | calibración |
| `ORD_000107` | TP | `MER_BR_STORE` | `BUY_000066` | 72719 BRL | 2026-06-12 15:40 | AR | `DEV_000066` | fraude | monto, nuevo, país | 90 | TP | calibración |
| `ORD_000111` | acompañante | `MER_BR_STORE` | `BUY_000005` | 21681 BRL | 2026-06-14 11:52 | BR | `DEV_000005` | legítimo | — | 0 | TN | calibración |
| `ORD_000112` | FP1 | `MER_BR_STORE` | `BUY_000005` | 21897 BRL | 2026-06-14 13:22 | UY | `DEV_000005` | legítimo | cruce, país | 60 | FP | calibración |
| `ORD_000123` | TP | `MER_BR_STORE` | `BUY_000008` | 91426 BRL | 2026-06-18 22:18 | AR | `DEV_000008` | fraude | monto, país | 60 | TP | calibración |
| `ORD_000138` | acompañante | `MER_BR_STORE` | `BUY_000012` | 11627 BRL | 2026-06-25 10:20 | AR | `DEV_000012` | fraude | país | 20 | FN | calibración |
| `ORD_000139` | TP | `MER_BR_STORE` | `BUY_000012` | 43661 BRL | 2026-06-25 11:05 | BR | `DEV_000012` | fraude | monto, cruce | 80 | TP | calibración |
| `ORD_000155` | TP | `MER_BR_STORE` | `BUY_000082` | 55006 BRL | 2026-07-01 19:47 | AR | `DEV_000082` | fraude | monto, nuevo, país | 90 | TP | calibración |
| `ORD_000157` | acompañante | `MER_UY_STORE` | `BUY_000004` | 75902 UYU | 2026-07-02 23:52 | AR | `DEV_000004` | legítimo | país | 20 | TN | calibración |
| `ORD_000158` | acompañante | `MER_UY_STORE` | `BUY_000004` | 63736 UYU | 2026-07-02 23:55 | AR | `DEV_000004` | legítimo | país | 20 | TN | calibración |
| `ORD_000159` | acompañante | `MER_UY_STORE` | `BUY_000004` | 77967 UYU | 2026-07-02 23:58 | AR | `DEV_000004` | legítimo | país | 20 | TN | calibración |
| `ORD_000160` | FP3 | `MER_UY_STORE` | `BUY_000004` | 94881 UYU | 2026-07-03 00:01 | AR | `DEV_000004` | legítimo | ráfaga, hora, país | 60 | FP | calibración |
| `ORD_000171` | TP | `MER_BR_STORE` | `BUY_000018` | 69256 BRL | 2026-07-08 08:30 | AR | `DEV_000018` | fraude | monto, país | 60 | TP | calibración |
| `ORD_000186` | acompañante | `MER_US_MARKET` | `BUY_000003` | 12435 USD | 2026-07-14 12:37 | BR | `DEV_000003` | fraude | país | 20 | FN | calibración |
| `ORD_000187` | TP | `MER_US_MARKET` | `BUY_000003` | 42279 USD | 2026-07-14 13:22 | AR | `DEV_000003` | fraude | monto, cruce | 80 | TP | calibración |
| `ORD_000203` | TP | `MER_BR_STORE` | `BUY_000021` | 78207 BRL | 2026-07-20 20:55 | AR | `DEV_000021` | fraude | monto, país | 60 | TP | holdout |
| `ORD_000219` | TP | `MER_BR_STORE` | `BUY_000101` | 67328 BRL | 2026-07-27 10:41 | BR | `DEV_000101` | fraude | monto, nuevo | 70 | TP | holdout |
| `ORD_000231` | acompañante | `MER_US_MARKET` | `BUY_000009` | 8832 USD | 2026-08-01 11:52 | AR | `DEV_000009` | legítimo | — | 0 | TN | holdout |
| `ORD_000232` | FP1 | `MER_US_MARKET` | `BUY_000009` | 8157 USD | 2026-08-01 13:22 | UY | `DEV_000009` | legítimo | cruce, país | 60 | FP | holdout |
| `ORD_000235` | TP | `MER_BR_STORE` | `BUY_000108` | 63896 BRL | 2026-08-02 17:09 | AR | `DEV_000108` | fraude | monto, nuevo, país | 90 | TP | holdout |
| `ORD_000241` | acompañante | `MER_UY_STORE` | `BUY_000016` | 54170 UYU | 2026-08-04 23:52 | AR | `DEV_000016` | legítimo | país | 20 | TN | holdout |
| `ORD_000242` | acompañante | `MER_UY_STORE` | `BUY_000016` | 84240 UYU | 2026-08-04 23:55 | AR | `DEV_000016` | legítimo | país | 20 | TN | holdout |
| `ORD_000243` | acompañante | `MER_UY_STORE` | `BUY_000016` | 90759 UYU | 2026-08-04 23:58 | AR | `DEV_000016` | legítimo | país | 20 | TN | holdout |
| `ORD_000244` | FP3 | `MER_UY_STORE` | `BUY_000016` | 80438 UYU | 2026-08-05 00:01 | AR | `DEV_000016` | legítimo | ráfaga, hora, país | 60 | FP | holdout |
| `ORD_000251` | TP | `MER_BR_STORE` | `BUY_000026` | 82573 BRL | 2026-08-09 02:15 | AR | `DEV_000026` | fraude | monto, país | 60 | TP | holdout |
| `ORD_000258` | FP4 | `MER_US_MARKET` | `BUY_000034` | 17129 USD | 2026-08-11 22:18 | BR | `DEV_000034` | legítimo | monto, país | 60 | FP | holdout |
| `ORD_000267` | TP | `MER_UY_STORE` | `BUY_000118` | 605514 UYU | 2026-08-15 12:33 | UY | `DEV_000118` | fraude | monto, nuevo | 70 | TP | holdout |
| `ORD_000275` | FN1 | `MER_UY_STORE` | `BUY_000007` | 283204 UYU | 2026-08-18 19:47 | UY | `DEV_000007` | fraude | — | 0 | FN | holdout |
| `ORD_000277` | FN2 | `MER_BR_STORE` | `BUY_000024` | 30804 BRL | 2026-08-19 13:22 | BR | `DEV_900002` | fraude | — | 0 | FN | holdout |
| `ORD_000278` | FN2 | `MER_BR_STORE` | `BUY_000024` | 31506 BRL | 2026-08-19 13:42 | BR | `DEV_900002` | fraude | — | 0 | FN | holdout |
| `ORD_000279` | FN2 | `MER_BR_STORE` | `BUY_000024` | 32698 BRL | 2026-08-19 14:02 | BR | `DEV_900002` | fraude | — | 0 | FN | holdout |
| `ORD_000283` | TP | `MER_UY_STORE` | `BUY_000124` | 767889 UYU | 2026-08-21 18:26 | AR | `DEV_000124` | fraude | monto, nuevo, país | 90 | TP | holdout |

Y las cifras agregadas que se predicen junto con ella, sobre las mismas 300 referencias
`ORD_000001`–`ORD_000300` y los mismos tres comercios:

| Magnitud | Predicción |
| --- | --- |
| Pedidos y etiquetas | 300 y 300, con 28 fraudes y 272 legítimos |
| Cohortes | 300 instantes distintos: 200 de calibración y 100 de holdout |
| Matriz total | TP 17, FP 6, FN 11, TN 266 |
| Matriz de calibración | TP 11, FP 3, FN 7, TN 179 |
| Matriz de holdout | TP 6, FP 3, FN 4, TN 87 |
| Holdout | precisión 6/9, recall 6/10, F1 0,632 |
| Umbral que elige el barrido | 60 |
| Reglas que disparan | `foreign_country` 50, `amount_anomaly` 19, `new_buyer_high_value` 10, `cross_border_velocity` 4, `velocity` 2, `unusual_hour` 2 |
| Bandas de alerta | 11 media, 6 alta, 6 crítica |
| Denegados por el proveedor sin alerta local | 43, de los cuales 10 son fraude |

Dos predicciones que conviene declarar porque son incómodas y deliberadas. La primera: `velocity` y
`unusual_hour` disparan **dos veces cada una y siempre sobre pedidos legítimos**. No es que no se
haya podido construir un fraude que las active; es que el fraude que las activaría —la ráfaga de
prueba de tarjetas— usa un comprador distinto por pedido, y `velocity` cuenta por comprador. El
corpus dice que esas dos reglas, con estos datos, solo producen falsos positivos, y esa afirmación
vale más que un fraude fabricado para que la regla acierte. La segunda: la única banda `ALTA` de la
cohorte de calibración la abre un pedido **legítimo**, `ORD_000064`.

### La tabla obtenida, y en qué difiere de la predicha

El motor .NET corrió sobre el corpus construido y **las cuarenta y dos filas cayeron en la celda
que la tabla de arriba predijo: cero desvíos**. La comparación no se hizo solo sobre el score: se
compararon ocho campos por pedido —comercio, comprador, monto, país, score, reglas, pesos y el
texto de cada señal— sobre los 300, y de esas 2.400 comprobaciones **una sola** difirió, que es la
única diferencia real de esta entrega:

| Pedido | Predicho | Obtenido | Por qué |
| --- | --- | --- | --- |
| `ORD_000233` | «…observado en 73 de 80 pedidos previos (91,2 %)» | «…(91,3 %)» | 73/80 es 91,25 y cae justo en la mitad. Mi transcripción del motor en Python redondea al par y .NET redondea alejándose del cero. El motor real tiene razón; el que estaba mal era mi modelo |

Las cifras agregadas coincidieron con la predicción salvo una, y conviene decir cuál y por qué:

| Magnitud | Predicho | Obtenido |
| --- | --- | --- |
| Pedidos, etiquetas, fraudes | 300 · 300 · 28 | igual |
| Cohortes | 200 y 100 | igual |
| Matriz total | TP 17 · FP 6 · FN 11 · TN 266 | igual |
| Matriz de calibración | TP 11 · FP 3 · FN 7 · TN 179 | igual |
| Matriz de holdout | TP 6 · FP 3 · FN 4 · TN 87 | igual |
| Umbral que elige el barrido | 60 | igual |
| Reglas que disparan | 50 · 19 · 10 · 4 · 2 · 2 | igual |
| Bandas de alerta | 11 media · 6 alta · 6 crítica | igual |
| Denegados por el proveedor sin alerta local | 43, con 10 de fraude | **43 tras pedir la evaluación externa, 51 después de entregar los callbacks**; los de fraude son 10 en los dos casos |

La última fila es una predicción incompleta, no un error del corpus: conté solo las denegaciones
síncronas, que son las de la banda 75–89. Los callbacks resuelven después la banda pendiente y nueve
de esos veintiún pedidos vuelven denegados, ninguno de ellos con alerta. Los diez fraudes no se
mueven porque ningún fraude sin alerta cae en la banda pendiente. El test del panel fija 43, que es
el estado en el que la consola lo muestra durante el recorrido.

### La matriz, con sus conteos y su `n`

Publicada así y no como una razón con dos decimales: con cien pedidos de holdout, **cada falso
negativo mueve el recall diez puntos**, y dos decimales sugieren una precisión que ese tamaño no
tiene.

| Cohorte | `n` | TP | FP | FN | TN | Precisión | Recall |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Calibración | 200 | 11 | 3 | 7 | 179 | 11/14 | 11/18 |
| Holdout | 100 | 6 | 3 | 4 | 87 | 6/9 | 6/10 |

**El umbral que eligió el barrido es 60**, el mismo que el producto usa. No hubo discrepancia que
explicar, y la decisión 63 sigue en pie por si la hubiera en el futuro. El F1 del holdout es 0,632 y
el de la calibración 0,688: el corpus dejó de recuperar sus propias etiquetas.

### Cómo se construyó el corpus, para que se pueda rehacer

La fixture es el artefacto; no se versiona ningún generador, igual que en la v1. Lo que hay que
saber para reconstruirla está acá.

- **Tres comercios y trescientas referencias, las mismas.** `MER_UY_STORE` en pesos uruguayos,
  `MER_BR_STORE` en reales y `MER_US_MARKET` en dólares, cien pedidos cada uno. El identificador
  `MER_US_MARKET` **no cambió y no podía cambiar**: es la mitad de la clave de conflicto, y
  renombrarlo dejaría convivir dos corpus contradictorios. Lo que sí cambió es dónde opera: su país
  habitual pasó a ser Argentina, porque el corredor UTC−3 es lo que hace que «hora del comercio»
  signifique lo mismo para los tres. Un comercio que factura en dólares en Argentina es lo
  ordinario, pero el nombre quedó heredado y conviene decirlo antes que un revisor lo pregunte.
- **Ciento veinte días, del 2026-05-01 al 2026-08-28**, con trescientos instantes distintos. La v1
  tenía los pedidos separados por exactamente 9,6 horas —mínimo, mediana y máximo iguales—, así que
  en hora local solo existían cinco horarios. Acá hay quince horarios locales por cada ciclo de seis
  días.
- **La trampa que costó una corrida entera.** El patrón que reparte los comercios tiene trece
  posiciones y cada ciclo de seis días aporta exactamente trece ranuras fuera de la franja nocturna.
  Con las dos periodicidades alineadas, **cada comercio quedaba clavado en las mismas cinco horas
  del día**, alguna de sus franjas quedaba vacía y `unusual_hour` disparaba en diecisiete
  evaluaciones que nadie había pedido. La rotación del patrón por ciclo lo arregla, y la regla
  vuelve a estar disponible solo para los pedidos que la buscan.
- **Los montos legítimos se mueven en una banda estrecha alrededor del nivel de gasto de cada
  comprador**, para que ninguna anomalía sea accidental: todas las diecinueve de `amount_anomaly`
  son deliberadas, y el informe lo comprueba listando los pedidos no arquetipo con señales
  inesperadas, que son cero.
- **Cada arquetipo se lleva una cuenta distinta.** La primera versión elegía siempre al comprador
  con más historia y terminaba con un solo comprador que era el revendedor legítimo, la víctima del
  fraude amigo y el titular de la cuenta tomada — y sus propios montos altos le subían la mediana
  hasta volver absurdo el arquetipo siguiente: la «compra del doble de lo habitual» de la cuenta
  tomada salía a 1.285 reales contra una mediana del comercio de 170.
- **`velocity` y `unusual_hour` disparan dos veces cada una, y siempre sobre pedidos legítimos.**
  Es deliberado y es la mitad de lo que enseña el corpus: el fraude que activaría a `velocity` —la
  ráfaga de prueba de tarjetas— usa un comprador distinto por pedido, y la regla cuenta por
  comprador. La regla existe, funciona, y está atada a la entidad equivocada.
- **La única alerta de banda alta de la cohorte de calibración la abre un pedido legítimo**,
  `ORD_000064`: un cliente nuevo comprando algo caro.

### Archivos modificados

| Archivo | Qué cambió |
| --- | --- |
| `backend/src/Salvo.Infrastructure/Seed/Fixtures/demo-orders.v2.json` | El corpus nuevo |
| `backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj` | El recurso embebido apunta a la v2 |
| `backend/src/Salvo.Infrastructure/Seed/EmbeddedDemoOrderSource.cs` | El nombre del recurso sale de la versión declarada |
| `backend/src/Salvo.Application/Orders/Seed/DemoDatasetShape.cs` | Nuevo: la forma que este build espera |
| `backend/src/Salvo.Application/Orders/Seed/DemoSeedConflictException.cs` | Gana la causa del conflicto |
| `backend/src/Salvo.Application/Orders/Seed/DemoSeedWireNames.cs` | Nuevo: la ortografía de esa causa en el cable |
| `backend/src/Salvo.Application/Orders/Seed/SeedDemoOrdersHandler.cs` | Un solo plan que el seed aplica y el ensayo informa |
| `backend/src/Salvo.Application/Orders/Seed/SeedDemoOrdersResult.cs` | Gana el resultado del ensayo |
| `backend/src/Salvo.Api/OrderEndpoints.cs` | `GET /api/demo-data/seed-preview` y los dos códigos de conflicto |
| `backend/src/Salvo.Application/Dashboard/*` y `Persistence/EfDashboardReader.cs` | El panel de denegados sin alerta |
| `backend/src/Salvo.Infrastructure/External/MockAntifraudProvider.cs` | **Fuera de la reserva**: una palabra de un comentario que nombraba el archivo v1 |
| `frontend/openapi/salvo-openapi.json`, `src/lib/api/schema.d.ts` | Recaptura del contrato |
| `frontend/src/lib/api/{contract,guards,console,messages}.ts` y sus tests | El panel, el ensayo y el código nuevo |
| `frontend/src/app/dashboard/{page,panels,page.test}.tsx` | El panel en pantalla |
| `frontend/src/app/import/{page.tsx,page.test.tsx}` | El aviso antes del clic |
| `frontend/src/app/alerts/[id]/divergence.ts` y su test | El aviso compara la evaluación, no su identificador |
| `frontend/src/test/fixtures.ts` | El panel, el ensayo, y una evaluación vigente que ahora es coherente |
| `backend/tests/**` | Quince tests movidos al corpus nuevo y siete nuevos |
| `scripts/smoke-ui.sh` | Cuatro comprobaciones y la suma de monedas |

### Verificación

| Comprobación | Resultado |
| --- | --- |
| Los siete arquetipos contra el motor real | 42 filas, 0 desvíos |
| Los 300 pedidos, ocho campos cada uno | 1 diferencia, de redondeo, en mi modelo y no en el motor |
| `./scripts/check.sh` | Verde. 68 comprobaciones de documentación, 94 + 154 tests .NET, 223 de frontend, los dos builds de producción |
| `./scripts/smoke-ui.sh` | Verde: 47 comprobaciones, 0 fallas, sobre bases nuevas |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios: el panel es una lectura |
| Seed sobre base vacía | 300 pedidos, 300 etiquetas, 28 fraudes; repetirlo inserta cero |
| Seed sobre una base con la versión anterior | `409 DEMO_DATA_PREVIOUS_CORPUS`, nada escrito, y la consola lo dice al abrir `/import` |
| Seed sobre pedidos importados que chocan | `409 DEMO_DATA_CONFLICT`, que es el código que ya existía |
| Panel del dashboard | 43 pedidos, entre ellos `ORD_000275`, `ORD_000277` y `ORD_000075`: los tres arquetipos que el motor no ve |

Falsaciones, porque una comprobación que no puede fallar no comprueba nada:

- **El panel deja de excluir los pedidos con alerta** → cae
  `TheExternalDenialsPanelShowsTheFraudTheRulesNeverFlagged`.
- **El panel lee la etiqueta de fraude** → cae `TheDashboardIsIndependentOfGroundTruth`, que ahora
  pide las evaluaciones externas primero para que el panel tenga filas de las que ser independiente.
  Sin ese cambio, la aserción más fuerte de la suite pasaba sobre un campo vacío.
- **La causa del conflicto deja de mirar la etiqueta** → cae
  `AnEarlierVersionOfTheCorpusIsNamedAsSuchAndNothingIsWritten`.
- **El aviso de divergencia vuelve a comparar identificadores** → cae «no avisa cuando la evaluación
  vigente es otra fila con el mismo score y las mismas reglas», que es exactamente el escenario que
  `E9B` va a producir.

### Decisiones y supuestos

- **El fingerprint dorado se mudó de `ORD_000001` a `ORD_000011`.** El anterior puntúa cero sin
  señales, y un fingerprint sobre una evaluación vacía depende del identificador del pedido y de la
  versión de las reglas y de nada que diga el corpus: **sobrevivió intacto al reemplazo completo de
  la fixture**. El digest del manifiesto de las 300 filas es lo que de verdad sostiene el corpus.
- **La aserción de zona horaria se volvió su propio test.** Vivía pegada al dorado decimal y decía
  «4 de mayo, no 5 de mayo»; con el corpus nuevo ese pedido dejó de cruzar la medianoche y la
  aserción habría pasado por la razón equivocada. Ahora usa `ORD_000123`, que está guardado a las
  01:18 UTC del 19 de junio y ocurrió a las 22:18 del 18 en hora del comercio.
- **El segundo dorado ganó cobertura sin pedirlo**: `ORD_000171` compara ahora contra la mediana
  **del comprador**, así que la pareja de textos dorados fija por fin las dos frases que la
  plantilla puede escribir sobre el alcance de la mediana. Los dos anteriores usaban la del
  comercio.
- **El panel lista hasta cien pedidos y hoy lista los 43.** Un tope apretado escondería justamente
  lo que el panel existe para mostrar; el tope está para que el contrato siga siendo un arreglo
  acotado. El total se informa siempre completo.
- **El aviso de divergencia compara score, reglas y pesos, y no el texto de cada señal.** El texto
  es cómo está escrita la evaluación, no qué decidió, y `E9B` lo va a reescribir entero: comparar la
  prosa devolvería el aviso falso en cada alerta el día del cambio de versión.
- **El ensayo del seed es un `GET`.** La pantalla lo lee al abrirse y no escribe nada; un `POST` con
  bandera para una lectura de página habría sido peor.
- **`e7-v2` no subió de versión.** La plantilla no cambió ni una palabra: lo que cambió es el corpus
  sobre el que escribe. La decisión 64 lo dice al revés y vale igual.

### Riesgos o pendientes

- **`backend/src/Salvo.Infrastructure/Seed/Fixtures/demo-orders.v1.json` quedó en el repositorio y
  ya no lo carga nadie.** No lo borré: `rm` está denegado y es regla del proyecto. Conviene que lo
  borres vos, o que el coordinador decida conservarlo.
- **Documentos con cifras del corpus, todos fuera de la reserva de esta tarea y para `E9D`:** el
  bloque marcado del README, el bloque del guion de demo, `docs/muestras/README.md` —que además
  nombra `demo-orders.v1.json` y repite el `13,8 %` que ya se corrigió en otros cuatro documentos—,
  el §4.1 del Blueprint y las notas del Workboard.
- **Las seis capturas envejecieron.** Muestran 18 alertas y ninguna banda alta. `capturas.sh` sigue
  funcionando sin tocarlo: `ORD_000011` sigue siendo el pedido de la demo, sigue siendo crítico,
  sigue identificando exactamente una alerta y el proveedor lo sigue aprobando. Regenerarlas es de
  `E9D`.
- **`docs/muestras/import-con-errores.csv` sigue valiendo entero**: su fila 6 choca contra
  `MER_BR_STORE / ORD_000011`, que existe en el corpus nuevo con otros datos.
- **Cuatro bases `.db` en `backend/src/Salvo.Api/`** —`salvo.db`, `salvo.design.db` y dos
  `salvo-demo-*`—, todas ignoradas por Git. La primera tiene el corpus v1 y a partir de ahora no
  puede tomar el nuevo: la consola te lo va a decir al abrir `/import`. Se borran desde tu terminal.
- **`velocity` y `unusual_hour` disparan dos veces cada una.** Es deliberado y está argumentado más
  arriba, pero es la clase de cifra que un revisor va a preguntar.

### Integración

- Orden sugerido: esta rama sola. `E9B` depende de ella integrada.
- Migraciones o pasos manuales: **ninguna migración**. Sí hay recaptura de OpenAPI, ya versionada.
- **La base de demostración de quien integre deja de servir.** Es el efecto declarado de la etapa:
  hay que crear una nueva, y `./scripts/demo.sh` la crea.
- Posibles conflictos: `main` está un commit adelante de esta rama, de coordinación
  —`AGENTS.md`, la decisión 65 y el §4.4 del Blueprint—, y no toca ninguno de estos paths. La rama
  no se rebasó a propósito: rebasar movería el `merge-base` que el brief declara.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre el estado
  integrado.

Estado: **Lista para integrar**.

## `E9B-SENALES-TIPADAS` — El motor escribe los campos de una señal, no una frase

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 9
- Rama/worktree: `claude/e9b-senales-tipadas`
- Commit base: `5776232` (el `merge-base` real con `main`, como declara el brief). Los tres commits
  anteriores a los míos son del coordinador: el brief, la corrección de la regla de dominio y el
  cierre de las observaciones del segundo `brief-check`. Mi primer commit es `150286d`.
- Commit final: `95da151`
- Fecha: 2026-09-07

### Resultado

El motor dejó de escribir una frase inglesa por señal y escribe **los campos** de esa señal. La
prosa dejó de ser el formato de intercambio entre el motor y todo lo que lo lee, y las seis
expresiones regulares que tapaban ese hueco **ya no existen**, borradas en el último commit después
de haber certificado a su reemplazo sobre las evaluaciones reales del corpus v2.

**El diferencial no tuvo un solo desvío.** Cincuenta y seis evaluaciones con señales, ochenta y
siete señales, las seis reglas: lo que `e3-v2` emite es campo por campo lo que el extractor leyó de
la prosa `e3-v1`, con el score y el conjunto de reglas de cada evaluación intactos. La captura está
commiteada en `3121535` y el motor cambia en `66affde`, así que el orden de los commits muestra que
la predicción precede a la medición.

La consola compone la frase en castellano desde los campos, y ahora muestra **la mediana**, que la
prosa nombraba y la pantalla nunca había mostrado: «El monto, BRL 507,86, es 3,4 veces la mediana
del comercio, BRL 149,37, calculada sobre 3 pedidos previos de los últimos 90 días.»

### Archivos modificados

Treinta y ocho archivos entre `7b5ee90` y `95da151`. **Ningún documento canónico**: ni `AGENTS.md`,
ni el Blueprint, ni el Progress, ni el Workboard, ni el README.

- **Dominio**: `Risk/RiskSignal.cs` (la forma plana y nullable, seis constructores por regla y la
  precisión canónica garantizada por el tipo), `Risk/RiskSignalSerializer.cs` (el orden de campos
  por regla y la escala fija como texto), `Risk/RuleConfig.cs` (`E3V2`, `Current`, `Known`,
  `ForVersion`), `Risk/TemporalRiskEngine.cs`, `Explanations/SignalFacts.cs` (los campos y la
  enumeración de hechos; el extractor borrado),
  `Explanations/SignalDetailNotRecognizedException.cs`.
- **Aplicación y API**: `Alerts/AlertViews.cs` y `Alerts/AlertProjection.cs` (el contrato),
  `Explanations/RequestExplanationHandler.cs`, `Risk/EvaluateLocalRiskHandler.cs`,
  `Risk/RunScoringHandler.cs`, `Dashboard/GetDashboardHandler.cs`, `Salvo.Api/Program.cs`.
- **Infraestructura**: `Explanations/DeterministicExplanationProvider.cs`.
- **Tests backend**: `Goldens/signal-facts.v2.json` y `Goldens/grounding-facts.v2.json` (nuevos),
  `SignalFactsGoldenTests.cs`, `LegacyEvaluationTests.cs`, `RiskSignalTests.cs`,
  `RuleConfigTests.cs` (nuevos), más `ExplanationFactsTests.cs`, `RiskSignalSerializerTests.cs`,
  `RiskEvaluationTests.cs`, `ScoringRunPersistenceTests.cs`, `AlertEndpointTests.cs`,
  `AlertCreationTests.cs`, `RiskEvaluationIdentityTests.cs`, `ExplanationTestCorpus.cs`.
- **Frontend**: `lib/format.ts` y `lib/format.test.ts` (nuevo), `lib/api/guards.ts` y su test,
  `app/alerts/[id]/evaluation-blocks.tsx`, `app/alerts/[id]/divergence.test.ts`,
  `test/fixtures.ts`, y la recaptura de `openapi/salvo-openapi.json` y `lib/api/schema.d.ts`.
- **Scripts**: `scripts/smoke-ui.sh`, cinco comprobaciones nuevas.

### Verificación

| Comando | Resultado |
| --- | --- |
| `./scripts/check.sh` | Verde. 0 advertencias, 0 errores; `check-docs.sh` 68 comprobaciones, 0 fallas |
| `dotnet ef migrations has-pending-model-changes` (dentro de la compuerta) | «No changes have been made to the model since the last migration» |
| `dotnet test` backend | 117 de dominio + 158 de integración, 0 fallas |
| `npm run check` | 238 tests de frontend, 0 fallas; typecheck y ESLint limpios |
| `./scripts/smoke-ui.sh` | Verde: **52 comprobaciones, 0 fallas** (eran 47) |
| Diferencial del dorado | 56 evaluaciones, 87 señales, 6 reglas, **0 desvíos** |
| `ExplanationGoldenTests` | Pasa **sin tocar un solo texto** |
| `grep -rn "GeneratedRegex" backend/src/Salvo.Domain/Explanations/` | Solo la de `NumberTokenizer`, que lee el resumen del proveedor y tiene que quedarse. Las seis del extractor no existen |
| Recorrido manual sobre base nueva | Las **seis** reglas componen su frase en pantalla; ninguna de las 23 alertas muestra el aviso de divergencia; la explicación se pide y queda `READY` |

**Programa de formatos, corrido antes de tocar el motor.** Las cuatro afirmaciones del punto 2 del
brief se comprobaron contra el runtime y las cuatro son ciertas:

```
  4                    '0.0'  => 4.0        2.25   '0.0'  => 2.3
  90                   '0.##' => 90         22.125 '0.##' => 22.13
  decimal.Round(4, 1)  => 4                 decimal.Round(2.25, 1) => 2.2
  Math.Round + F1/F2   => 4.0, 2.3, 90.00, 22.13
  TimeSpan 100 s: TotalMinutes 1.6666666666666667, prosa 1.67, canónico 1.67  IGUAL
  TimeSpan 7,5 s: TotalMinutes 0.125,            prosa 0.13, canónico 0.13  IGUAL
```

**Fingerprints dorados, cambiados a propósito.** La versión y la cadena canónica son dos de los
cinco componentes del hash:

| Valor | Antes | Ahora |
| --- | --- | --- |
| `ORD_000011` | `39a65ad9…905a6` | `d745b194…a9521` |
| Digest del manifiesto | `f7c82225…d8514` | `c7d84558…81f72` |

**Las cinco falsaciones exigidas, más dos.** Cada una se rompió, se corrió, se anotó y se deshizo:

1. `ratio` con la escala cruda → el diferencial falla:
   `Ratio = 3,4000133895695253397603267055` contra `Ratio = 3,4`.
2. `medianCents` fuera del conjunto de hechos → el test por campo falla en dos entradas,
   `amount_anomaly.medianCents = 8685 is not a fact` y la de `new_buyer_high_value`; y el
   diferencial del conjunto congelado también.
3. `RuleConfig.E3V1` fijo en `RequestExplanationHandler` →
   `Assert.Throws() Failure: No exception was thrown`.
4. La guarda acepta una señal vacía → `guards.test.ts`:
   `expected { rule: 'amount_anomaly', …(23) } to be null`.
5. El dorado borrado → el test falla y **no lo regenera**: «The golden capture is missing… It is
   never written by this test». Lo mismo con el conjunto congelado.

Y dos que no estaban pedidas:

6. **Con `medianCents` fuera, una frase verdadera se rechaza.** «El monto es 3,4 veces la mediana
   del comercio, que fue 149,37 BRL» pasa de aceptada a `NotGroundedNumber`. Es exactamente el
   defecto que el hallazgo 4.2 de la revisión predijo, reproducido y revertido.
7. **La composición rota deja la comprobación del smoke en rojo**: dos de 52 fallan,
   `no contiene «veces la mediana del comercio»`.

### Decisiones y supuestos

- **`scope` no viaja en `new_buyer_high_value`.** La tabla del punto 2 declara cinco campos para esa
  regla y `scope` no es uno. El extractor lo ponía en `merchant` siempre: es verdad y es inútil,
  porque la regla dispara precisamente cuando el comprador no tiene historia, así que en el cable
  sería una constante y en el fingerprint de cada señal de esa regla, también. Lo encontré leyendo
  la primera captura antes de congelarla, que es para lo que sirve congelarla (`25c859e`).
- **`foreign_country` estrena `country` en vez de tomar prestado `toCountry`.** Es el nombre que la
  tabla declara. Mantener el prestado habría obligado al diferencial a mapear un campo sobre otro, y
  un mapeo dentro de un oráculo es un lugar donde se esconde un error.
- **El redondeo vive en los constructores de `RiskSignal`.** Así el valor en memoria es el valor en
  disco. Si redondeara el serializador, la plantilla compondría su frase con el número sin redondear
  y escribiría «23,156131 veces». El modo es `AwayFromZero`, que es lo que hacían los formatos y lo
  que `decimal.Round` **no** hace.
- **La escala escrita no es el redondeo.** `Math.Round(4m, 1)` es `4`; la cadena canónica necesita
  `4.0`. Los decimales se escriben con ancho fijo por `ToString("F<n>")` y `WriteRawValue`.
- **Una lectura por campo medido, no varias.** Mi primera enumeración agregaba cada número también
  a cada precisión menor, con lo que un `ratio` de 3,4 metía un 3 pelado en el conjunto como hecho
  propio. Es innecesario —el verificador ya respalda una lectura de `d` decimales con cualquier
  hecho que redondee a ella— y ensanchaba en silencio lo que cuenta como medido. Lo encontró el
  diferencial de conjuntos.
- **El diferencial de conjuntos no es una igualdad, y no puede serlo.** El brief pide las dos cosas
  a la vez: que el conjunto desde campos sea *igual* al conjunto desde prosa, y que todo campo en
  centavos aporte además sus lecturas en unidades —que la prosa no tenía—. Lo resolví por
  contención con contabilidad exacta: ningún hecho que la prosa fundamentaba se perdió, y lo único
  que se agrega son las unidades de los montos, calculadas aparte en el test. Es estrictamente más
  fuerte que la igualdad en el eje que importa.
- **El conjunto de hechos derivado de la prosa quedó congelado en un segundo dorado.** Si no, la
  comparación moría con el extractor y el argumento pasaba a ser «una vez estuvo verde», que nadie
  puede volver a correr. La prosa de la que se deriva ya estaba congelada y es anterior al cambio de
  motor, así que el archivo es función pura de un archivo que nadie puede mover. Mientras el
  extractor existió, un test certificó el congelado en cada corrida.
- **La comprobación del smoke se ancla a `ORD_000011`, no a la primera alerta de la cola.** Ese
  pedido lo pone la fixture a propósito y dispara `amount_anomaly`, así que la frase que tiene que
  aparecer se sabe de antemano. Comprobar solo la ausencia de la prosa inglesa pasaría igual si la
  frase saliera vacía, que es justamente cómo esto se rompe.
- **El `grep` de la tabla de verificación del brief es demasiado ancho.** Pide «sin resultados» para
  `GeneratedRegex` en `Salvo.Domain/Explanations/`, y ahí queda la de `NumberTokenizer`, que
  tokeniza el **resumen que devuelve el proveedor** —el otro lado del grounding— y no tiene nada que
  ver con cómo se guarda una señal. Las seis del extractor sí desaparecieron.

### Riesgos o pendientes

- **El código de fallo de una evaluación `e3-v1` es una decisión del coordinador, y el brief se
  contradice.** Las decisiones delegadas incluyen «el nombre del código de fallo de una evaluación
  sin campos», pero los nueve `ExplanationFailureCode` están enumerados en un `CHECK` de la base
  (`ck_alert_explanations_failure_code`), así que un décimo valor **exige una migración** — y el
  brief prohíbe migraciones y manda parar ante una. Elegí no bloquear: el caso conserva
  `PROVIDER_UNAVAILABLE`, que es el más cercano de los nueve y **no es la verdad**, porque no se
  llama a ningún proveedor. El motivo está escrito donde se elige el código, no solo acá. Sobre una
  base recién sembrada el caso no existe; sobre una base vieja, un analista que pida la explicación
  de una alerta anterior lee «el proveedor falló antes de responder», que es falso y manda a alguien
  a depurar el proveedor. **Recomiendo el décimo código con su migración**, en `E9D` o como decisión
  aparte.
- **Una base que cruza el cambio de versión conserva alertas con snapshot en prosa.** Es consecuencia
  de la decisión 33 y está dicho, con test de extremo a extremo: la consola muestra la frase tal
  como el motor la escribió y pedir su explicación devuelve un fallo con código, no una excepción, y
  la fila queda cerrada en vez de reservada.
- **La verificación manual la hice sobre una base nueva, no sobre `salvo.db`.** En el primer intento
  usé la clave de configuración equivocada —es `ConnectionStrings__SalvoDb`, no `__Salvo`— y la API
  levantó contra `backend/src/Salvo.Api/salvo.db`, la tuya. **No la modificó**: el seed fue
  rechazado por el guardián de corpus previo y el archivo quedó con su fecha original y el mismo
  `md5`, `31b03168cad549716c2c99756f399033`, comprobado antes y después. La verificación buena corrió
  sobre una base propia en el directorio temporal de la sesión.
- **`format.test.ts` lee la captura del backend.** Es una dependencia cruzada inusual en un test de
  frontend, y es deliberada: hace que las seis frases se compongan desde señales que el motor
  produjo de verdad en vez de desde ejemplos escritos a mano. Si `E9C` mueve esos literales a los
  diccionarios, ese test es el que hay que traducir con ellos.
- **Nada de `e7-v3`.** La plantilla alimentada con los mismos campos produce los mismos bytes, y los
  textos dorados salieron idénticos. Escribir la mediana en el texto sigue siendo una razón legítima
  para subir la versión y sigue siendo decisión del coordinador; el campo `medianCents` ya está.

### Integración

- Orden sugerido: esta rama sola. `E9C` y `E9D` dependen de ella integrada.
- Migraciones o pasos manuales: **ninguna migración**; la compuerta lo comprueba. Sí hay recaptura de
  OpenAPI y de `schema.d.ts`, ya versionadas.
- **Toda base anterior al merge conserva sus alertas con snapshot en prosa.** No se rompe nada y
  está cubierto por test, pero un recorrido de demostración conviene hacerlo sobre una base nueva:
  `./scripts/demo.sh` la crea.
- Posibles conflictos: `main` avanzó a `16d80aa` después de crear la rama, con la sincronización del
  estado canónico tras `E9A`. No toca ninguno de estos paths. **Integrar por merge y no por rebase**:
  rebasar sobre esa punta movería el `merge-base` y volvería falso el commit base declarado arriba.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre el estado
  integrado.

Estado: **Lista para integrar**.

## `E9C1-IDIOMA` — El idioma deja de estar escrito en el código

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 9
- Rama/worktree: `claude/e9c1-idioma`
- Commit base: `163d7ea` (`docs: partir E9C en idioma y accesibilidad`), el `merge-base` real con
  `main`. Los tres commits que lo siguen antes del código son del coordinador y de las dos vueltas
  del `brief-check`.
- Commit final: `bd25f5b`
- Fecha: 2026-09-07

### Resultado

El idioma es una propiedad del despliegue. `SALVO_LANGUAGE=es|pt` **la lee solo la API**, que se
niega a arrancar ante un valor desconocido con el molde de `AI_PROVIDER` y publica el que parseó en
`GET /api/system/capabilities`. La consola lo toma de ahí y nunca lee la variable, así que un
despliegue mal configurado no puede servir una consola en un idioma alrededor de un párrafo en el
otro: esa discrepancia deja de ser representable. `Accept-Language` queda descartado y dicho.

**El idioma entra en la identidad de la explicación**, que es la parte que no es cosmética. Es la
quinta columna de `ux_alert_explanations_identity` y la tercera del índice parcial de `PENDING`, y
las dos hacen falta por motivos distintos. Las tres costuras de consulta filtran por idioma, la
lectura del detalle incluida — que es la mitad fácil de olvidar, porque escribir bien la fila
portuguesa y seguir leyendo la castellana la deja existiendo sin que nadie la vea.

La consola entera se compone desde dos diccionarios tipados, con la propiedad que no es negociable:
una clave que falta en un idioma es un error de compilación. La plantilla del backend escribe en el
idioma de su input, con las cifras compartidas y las palabras por idioma.

`LEGACY_SIGNAL_FORMAT` es el décimo `ExplanationFailureCode` y entra en la misma migración. Una
evaluación `e3-v1` fallaba con `PROVIDER_UNAVAILABLE`, que es falso: no se llama a ningún proveedor.

### Archivos modificados

**Backend — el idioma**

- `backend/src/Salvo.Domain/Explanations/ExplanationLanguage.cs` (nuevo)
- `backend/src/Salvo.Domain/Explanations/ExplanationWireNames.cs`
- `backend/src/Salvo.Domain/Explanations/ExplanationFailureCode.cs`
- `backend/src/Salvo.Domain/Explanations/AlertExplanation.cs`
- `backend/src/Salvo.Domain/Explanations/ExplanationInput.cs`
- `backend/src/Salvo.Domain/Explanations/SpanishNumberFormat.cs` →
  `ExplanationNumberFormat.cs` (`git mv`, sin una línea de cambio en el cuerpo)
- `backend/src/Salvo.Application/Explanations/DeploymentLanguage.cs` (nuevo)
- `backend/src/Salvo.Application/Explanations/{IExplanationStore,ExplanationInputFactory,RequestExplanationHandler}.cs`
- `backend/src/Salvo.Infrastructure/DependencyInjection.cs`
- `backend/src/Salvo.Infrastructure/Persistence/ExplanationLanguageConverter.cs` (nuevo)
- `backend/src/Salvo.Infrastructure/Persistence/Configurations/AlertExplanationConfiguration.cs`
- `backend/src/Salvo.Infrastructure/Persistence/{EfExplanationStore,EfAlertStore}.cs`
- `backend/src/Salvo.Infrastructure/Persistence/Migrations/20260907150822_ExplanationLanguage.*`
- `backend/src/Salvo.Api/SystemEndpoints.cs`

**Backend — la plantilla**

- `backend/src/Salvo.Infrastructure/Explanations/ExplanationVocabulary.cs` (nuevo, los dos idiomas)
- `backend/src/Salvo.Infrastructure/Explanations/ExplanationFigures.cs` (nuevo, las cifras)
- `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`

**Backend — tests**

- `ExplanationLanguageTests.cs` y `ExplanationGoldenPortugueseTests.cs` (nuevos)
- `LegacyEvaluationTests.cs`, `SalvoApiFactory.cs`, `AlertSchemaTests.cs`,
  `ExplanationTemplateVersionTests.cs`, `ExplanationEndpointTests.cs`, `AlertReviewTests.cs`,
  `SignalFactsGoldenTests.cs`, `ExplanationFactsTests.cs`

**Frontend**

- `src/lib/i18n/{dictionary.ts,es.ts,pt.ts,glosario.mjs,glosario-pt.md}` (nuevos)
- `src/lib/format.ts` y `src/lib/api/messages.ts`, reescritos por idioma
- `src/lib/api/{contract.ts,guards.ts,console.ts}`
- Las cinco rutas y sus componentes: `app/layout.tsx`, `app/page.tsx`, `app/alerts/**`,
  `app/dashboard/**`, `app/import/**`, `components/**`
- `openapi/salvo-openapi.json` y `src/lib/api/schema.d.ts`, recapturados
- Los tests de todo lo anterior, más `src/test/fixtures.ts`

**Otros**

- `.env.example`, `scripts/smoke-ui.sh`, `DesignAgent/Salvo-Blueprint.md`

### Verificación

| Comando / comprobación | Resultado |
| --- | --- |
| `./scripts/check.sh` | Verde. 0 warnings, 117 tests de dominio, 172 de integración, 245 de frontend, `check-docs.sh` con 68 comprobaciones, los dos builds |
| `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration» |
| `./scripts/smoke-ui.sh` | **Verde: 71 comprobaciones, 0 fallas** (eran 52) |
| `SALVO_LANGUAGE=fr` | La API no arranca: `SALVO_LANGUAGE='fr' is not a language this build can write. Use one of: es, pt.` |
| Sin la variable, `./scripts/demo.sh` | Capacidades `"language":"es"`, `<html lang="es">`, las cuatro pantallas en castellano y la explicación abriendo con «El pedido obtuvo». Base nueva `salvo-demo-20260907-125920.db`; `salvo.db` con su `md5` original |
| **Comprobación central**, en el smoke y en `ExplanationLanguageTests` | Con la explicación castellana escrita, el despliegue en `pt` la ve como no explicada, escribe una fila nueva, y las dos conviven: `es` con «El pedido obtuvo» y `pt` con «O pedido obteve», ids distintos, resto de la identidad igual |
| Dos reservas `PENDING` en idiomas distintos | Conviven. Y dos en el **mismo** idioma siguen chocando: el índice se estrechó, no se apagó |
| `ExplanationGoldenTests` | Pasa **sin tocar un carácter** |
| Migración sobre una copia de `salvo.db` | Las 2 filas existentes quedan en `es` con su texto intacto; el índice lleva `language`; el `CHECK` incluye `LEGACY_SIGNAL_FORMAT` |
| `grep -rn "BUSINESS_TIMEZONE"` | Una sola aparición, en el Blueprint, declarando que se retiró |

**Las siete falsaciones.** Cada una se rompió a propósito, se corrió el test, se anotó el error y se
deshizo.

1. **El idioma fuera de la identidad.** Sacarlo del índice sola no llega a correr: EF lo frena antes
   con `PendingModelChangesWarning`, que es su propia guarda haciendo su trabajo. Sacarlo de
   `FindAsync` —la búsqueda que representa esa identidad— sí reproduce el defecto exacto de `E7D`:
   `ChangingTheLanguageWritesANewRow…` falla con `Assert.True() Failure — Expected: True, Actual:
   False` sobre `result.Applied`. El despliegue portugués encontró la fila castellana, la dio por
   buena y no escribió nada.
2. **El idioma fuera del índice parcial de `PENDING`.** Falsado contra el esquema anterior real:
   una base migrada hasta `Explanations` y dos inserts que solo difieren en idioma dan
   `UNIQUE constraint failed: alert_explanations.risk_evaluation_id, alert_explanations.provider
   (19)`. Con el índice de esta tarea, las dos filas entran: `filas pendientes: 2, idiomas: es,pt`.
3. **Una clave faltante que cae al castellano.** Con el tipo como está, quitar `reviewSubmit` de
   `pt.ts` da `error TS2741: Property 'reviewSubmit' is missing…`. Cambiando `Dictionary` por una
   mezcla con caída silenciosa y quitando la misma clave: **0 errores de tipos y 239 de 239 tests
   verdes**. Ése es el punto: el modo de falla es invisible, y lo que se vería es un botón en
   castellano en una consola portuguesa.
4. **`SALVO_LANGUAGE=fr`.** La API muere en el arranque con el mensaje de arriba, que nombra el
   valor recibido y los válidos.
5. **`PROVIDER_UNAVAILABLE` en la evaluación `e3-v1`.**
   `Assert.Equal() Failure: Strings differ — Expected: "LEGACY_SIGNAL_FORMAT", Actual:
   "PROVIDER_UNAVAILABLE"`.
6. **La lectura de la consola sin filtrar por idioma.** `TheConsoleReadsTheExplanationOfItsOwnLanguage`
   falla con `Assert.Null() Failure: Value is not null` y el valor es la explicación castellana
   entera —«El pedido obtuvo 70 puntos…»— servida a un lector portugués. Es el modo de falla más
   caro de la tarea: la fila correcta existe, nadie la ve, y todo lo demás está verde.
7. **`projectCapabilities` sin tocar tras la recaptura.**
   `src/lib/api/guards.ts(795,3): error TS2741: Property 'language' is missing…`. Salió sola al
   recapturar, antes de que hiciera falta provocarla.

### Decisiones y supuestos

- **El décimo código se llama `LEGACY_SIGNAL_FORMAT`.** Nombra la causa —la evaluación guarda sus
  señales en el formato anterior del motor— y no un síntoma, y entra en los 21 caracteres que la
  columna admite. Era decisión delegada.
- **`SpanishNumberFormat` pasó a `ExplanationNumberFormat`** con `git mv` y sin una línea de cambio
  en el cuerpo: el portugués de Brasil usa los mismos separadores. `git rm` está denegado y esto no
  es un borrado.
- **El diccionario es un objeto anidado y `Dictionary` es `typeof es`.** Los valores son frases
  enteras, y las que llevan cifras son funciones de esas cifras. Era decisión delegada, con la
  restricción que no lo era.
- **El idioma baja como primitiva y los componentes cliente buscan sus propias palabras.** Es la
  lección de `E7B`: un rótulo pasado como prop viaja en el payload RSC de toda página, haya control
  o no. Los dos diccionarios entran al bundle del cliente, que para una consola de cinco rutas es el
  precio correcto.
- **La raíz deja de ser estática, y se elige.** El `<html lang>` vive en el layout, el layout envuelve
  todas las rutas, y leer las capacidades ahí las vuelve dinámicas. La alternativa era un segundo
  lector de `SALVO_LANGUAGE` dentro del proceso de Next, que es exactamente la discrepancia que el
  origen único existe para impedir. El comentario de `page.tsx` lo dice en vez de seguir afirmando lo
  contrario.
- **`fetchCapabilities` quedó memoizada y las tres pantallas que ya la piden derivan el idioma de esa
  misma respuesta.** No estaba en el brief: apareció porque el test que afirma «no pregunta por las
  capacidades más de una vez» se puso rojo, y tenía razón. `cache()` de React no se puede observar
  bajo Vitest, así que la propiedad se sostiene en el código y no en el framework.
- **El glosario vive en `frontend/src/lib/i18n/glosario-pt.md`, no en `docs/`**, que está reservado
  para `E9D`. Queda al lado de lo que describe, que además es mejor para quien lo corrige. **528
  entradas**, regenerables con `glosario.mjs`, y su primera línea dice que el portugués no lo revisó
  un hablante nativo.
- **`error.message` de un registro rechazado sigue en inglés**, y hay un test que lo afirma. Es la
  única parte de esa frase que la consola no escribe: nombra el valor rechazado, y es detalle técnico
  y no explicación. El código que lo acompaña sí se traduce, que es el ítem del checklist.
- **`REVIEW_CHOICES` perdió sus palabras** y quedó como los dos valores de dominio. El rótulo se
  busca donde se pinta el radio.
- **Los tests de acciones de servidor cambiaron de forma**, porque el comportamiento cambió: cada
  acción pregunta el idioma antes de hacer nada. `mockConsoleFetch` en las fixtures contesta esa ruta
  y entrega una respuesta nueva por llamada — un `Response` se lee una sola vez, y `mockResolvedValue`
  devolvía siempre el mismo objeto. Las aserciones que indexaban `mock.calls[0]` ahora buscan la
  llamada que les importa.
- **La versión de plantilla no subió** (decisión 64). El castellano no cambió un byte.

### Riesgos o pendientes

- **El portugués no lo revisó un hablante nativo.** Está dicho en `pt.ts`, en el glosario, en los
  dorados portugueses y en el vocabulario del backend. La palabra que más conviene revisar es
  «estabelecimento» para *comercio*: es la del mercado adquirente brasileño, pero es una elección.
- **La raíz es dinámica desde ahora.** Ningún documento público afirmaba que fuera estática —lo
  comprobé—, así que no hay nada que corregir fuera del comentario de `page.tsx`, pero es un cambio
  de forma del build que conviene tener presente.
- **Las cuatro anclas de `tools/capturas/capturar.mjs` siguen en castellano y no se tocaron**, como
  el brief exige. Las capturas de `E9D` salen en `es` y siguen valiendo.
- **Nadie traduce lo ya guardado, por diseño.** Una explicación escrita en castellano se queda en
  castellano; el despliegue portugués escribe la suya al lado. Traducir una fila guardada sería
  inventar un registro que nadie escribió.
- **Un `e3-v1` en portugués sigue fallando con `LEGACY_SIGNAL_FORMAT`**, y el rótulo está en los dos
  idiomas. Sobre una base recién sembrada el caso no existe.
- **La pasada `pt` del smoke cubre cinco pantallas y la comprobación central, no las 41 anclas.** Es
  deliberado y está escrito en el script: duplicarlas a mano era exactamente lo que el brief prohibía.
- **`E9C2` hereda un diccionario puesto**, que es la razón por la que va después: el texto que cree
  la pasada de accesibilidad nace en los dos idiomas en vez de nacer en castellano y traducirse.

### Integración

- Orden sugerido: esta rama sola. `E9C2` y `E9D` dependen de ella integrada.
- **Migraciones: una**, `20260907150822_ExplanationLanguage`. Rellena `es` en las filas existentes,
  agrega el idioma a los dos índices y el décimo código al `CHECK`. Probada sobre una copia de
  `salvo.db`, que quedó correcta. Toda base anterior al merge la necesita.
- Hay recaptura de OpenAPI y de `schema.d.ts`, ya versionadas.
- **Integrar por merge y no por rebase**: rebasar movería el `merge-base` y volvería falso el commit
  base declarado arriba.
- Posibles conflictos: `main` no se movió desde `163d7ea`. Si se moviera, los puntos de roce son el
  Blueprint, `scripts/smoke-ui.sh` y el borde del contrato.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre el estado
  integrado. Y, si se quiere verlo a mano, `./scripts/demo.sh` para el castellano y la misma base con
  `SALVO_LANGUAGE=pt` para el portugués: es el mismo `next start`.

Estado: **Lista para integrar**.

## `E9C2-ACCESIBILIDAD` (fase 1) — Lo que una máquina puede decidir, y la lista de lo que no

### Identificación

- Estado de la rama: `Parcial — en espera del recorrido con lector de pantalla`
- Etapa: 9
- Rama/worktree: `claude/e9c2-accesibilidad`
- Commit base: `a994855`
- Commit final: `e9645af`, el último de código. El commit de esta entrada lo sigue.
- Fecha: 2026-09-07

### Resultado

La consola pasa de **seis** reglas de accesibilidad a **treinta y una**, y gana una comprobación de
`axe-core` sobre el árbol renderizado que corre dentro de `npm run check` y por lo tanto en la
compuerta. Entre las dos encontraron **un defecto real**, que está corregido. Todo lo demás que
apareció está en la lista de hallazgos de más abajo, **sin corregir**, porque es lo que el
coordinador decide después del recorrido.

La base del recorrido queda preparada y sus pasos escritos, incluida una alerta con **divergencia
dentro de la misma banda**, que era lo que el brief prefería y no estaba garantizado que se pudiera
producir.

### Las seis reglas que ya estaban activas

Medidas con `npx eslint --print-config`, que es la configuración efectiva y no lo que un paquete
declara. `eslint-config-next` registra `eslint-plugin-jsx-a11y` y activa estas seis, todas en `warn`;
como `lint` corre con `--max-warnings=0`, **ya rompían la compuerta**:

| Regla | Nivel |
| --- | --- |
| `jsx-a11y/alt-text` | `warn`, con opciones para `next/image` |
| `jsx-a11y/aria-props` | `warn` |
| `jsx-a11y/aria-proptypes` | `warn` |
| `jsx-a11y/aria-unsupported-elements` | `warn` |
| `jsx-a11y/role-has-required-aria-props` | `warn` |
| `jsx-a11y/role-supports-aria-props` | `warn` |

Las seis comprueban que un atributo `aria-*` esté **bien escrito**. Ninguna mira si un control se
puede operar, si un `label` está asociado a su campo, o si un `<a>` tiene contenido. El conjunto
recomendado del plugin son treinta y una: faltaban veinticinco, entre ellas
`label-has-associated-control`, `anchor-is-valid`, `interactive-supports-focus`,
`no-noninteractive-element-interactions` y `scope`.

### Qué aporta cada dependencia que la otra no

Las dos ya estaban en el árbol como transitivas de `eslint-config-next`, y se declaran en **la
versión exacta que ya estaba instalada**: no entra código de terceros que no estuviera corriendo.
`npm ls` lo confirma: una sola copia de cada una y `eslint-config-next` deduplicando contra la del
proyecto.

| Paquete | Versión | Qué aporta | Por qué había que declararlo |
| --- | --- | --- | --- |
| `eslint-plugin-jsx-a11y` | `6.10.2` | Mira el **código fuente**: la forma del JSX antes de renderizarse | Quedaba anidado bajo `eslint-config-next/node_modules/`, donde `eslint.config.mjs` no lo alcanza con un `import` |
| `axe-core` | `4.13.0` | Mira el **árbol renderizado**: los elementos ya compuestos, con el texto del diccionario adentro | Estaba en la raíz, pero depender de un transitivo no declarado se rompe en una instalación limpia el día que `eslint-config-next` cambie de rango |

**No entró una tercera.** El enlace entre `axe-core` y Vitest se escribe a mano en
`src/test/axe.ts`, y son cincuenta líneas contando los comentarios. La interacción de las pruebas va
por `fireEvent`, como ya hacía `review-form.test.tsx`, y no por `user-event`.

**La división del trabajo entre las dos no es simétrica en este proyecto, y conviene saberlo.** Todo
el texto vive en `src/lib/i18n/es.ts` y `pt.ts` y llega al JSX como expresión, así que cualquier
regla del linter que comprueba **contenido** ve que hay una expresión y la da por buena sin poder
leerla. Por eso `axe-core` pesa más acá que en un proyecto donde los literales están en el JSX, y
por eso `anchor-ambiguous-text` queda apagada: su lista de palabras es inglesa y no hay un literal
contra el que pudiera dispararse. Una regla que no puede fallar nunca promete una verificación que
no existe.

### El defecto que encontraron, y que sí está corregido

**`definition-list`, impacto `serious`, en la sección de calidad del dashboard.** La aclaración de
«Sin etiqueta» era un `<p>` **hermano** del `<dd>`, dentro de una `<dl>`. Una lista de definiciones
solo admite grupos `<dt>`/`<dd>`, así que esa frase no pertenecía a ningún término: quien recorría
la lista escuchaba «Sin etiqueta, 44» y después una frase suelta, sin nada que dijera de cuál de las
cuatro cifras hablaba. Pasa adentro del `<dd>`, que es lo que describe, con el número en un `<span>`
para que el `<p>` no herede su tamaño ni su peso. Se ve exactamente igual que antes.

El otro aviso del linter **no era un defecto del marcado** y por eso no se cambió el marcado.
`label-has-associated-control` recorre dos niveles buscando el texto del `label`, y en el formulario
de veredicto está a tres: el `label` envuelve el radio y un `<span>` que agrupa el nombre de la
opción y su aclaración en dos renglones. Ese marcado es el que se quiere, porque hace que el nombre
accesible del radio sea «Confirmar segura El pedido no es fraude» —que es justo lo que hay que oír
antes de emitir un veredicto, y lo que el paso 13 del recorrido va a comprobar—. Lo que se corrigió
es la profundidad que la regla mira, a `3` y no más, y está falsado: sigue fallando sobre un `label`
sin texto y sobre uno cuyo texto está a profundidad 4.

### La lista de hallazgos

Ninguno está corregido. Severidad: `alta` impide completar una tarea, `media` la vuelve confusa,
`baja` es incomodidad.

| # | Pantalla | Qué pasa | Severidad | Arreglo propuesto |
| --- | --- | --- | --- | --- |
| 1 | Detalle de alerta | El aviso de divergencia dice «La evaluación del pedido cambió (70 → 70)» cuando lo que cambió fueron las reglas y no el score. De oído es una contradicción: anuncia un cambio y repite el mismo número | `media` | Cuando los dos scores son iguales, redactar la frase sobre las señales en vez de sobre la flecha. Dos claves nuevas en los dos diccionarios |
| 2 | Detalle de alerta | Al emitir un veredicto con éxito, el árbol pasa de `ReviewForm` a `RecordedVerdict`. **`RecordedVerdict` no tiene ninguna región viva**: ni `role="status"` ni `aria-live`. Y el botón que tenía el foco desaparece con el formulario | `alta`, **si el recorrido lo confirma** | Reservado a la fase 2. Es el paso 15 del guión y el propio guión lo llama «el más probable y el más importante» |
| 3 | Dashboard | La tabla del barrido de umbrales, dentro del `<details>`, es la **única** tabla de la consola sin `<caption>`. Al entrar en ella se anuncia «tabla, 5 columnas» y nada más | `baja` | Una `<caption class="sr-only">` como la de la cola y la del panel de denegados, en los dos diccionarios |
| 4 | Importación y detalle | `ActionOutcome` y `ReviewOutcome` usan `role="alert"` también cuando la acción **salió bien**. `alert` es asertivo e interrumpe lo que el lector esté diciendo; para un éxito lo convencional es `role="status"` | `baja` | Elegir el rol según `state.outcome`. Un cambio de una línea en cada uno |
| 5 | Dashboard e importación | `Panel` y `ActionSection` derivan el `id` del encabezado **del título traducido**, con `[^a-záéíóúñ]+`. En portugués `ção` queda `-o`. Hoy no hay colisión —verificado sobre los 7 paneles y las 4 secciones en los dos idiomas—, pero dos títulos que difieran solo en esos caracteres producirían el mismo `id`, y entonces un `aria-labelledby` nombraría a una sección con el título de otra | `baja` | Derivar el `id` de una clave estable y no del texto traducido |
| 6 | Todas | **El contraste de color no lo comprueba nada.** jsdom no calcula estilos, así que `axe-core` devuelve `color-contrast` como «incompleto»; está apagada por su nombre en `src/test/axe.ts` para que el incompleto no se lea como aprobado | `baja` | Queda para el ojo en el recorrido, o para un navegador de verdad si el coordinador lo quiere en la compuerta |
| 7 | Cola de alertas | Es la única pantalla con un solo encabezado: un `h1` y ningún `h2`. La lista de encabezados del rotor tiene una sola entrada | `baja`, probablemente sin acción | Es una pantalla de una sola tabla, y la tabla ya tiene su `<caption>`. Se anota para que el paso 5 del recorrido no lo reporte como sorpresa |

Sobre el número 2 conviene ser exacto en lo que esta tarea midió y lo que no. **Medido**:
`RecordedVerdict` no tiene región viva, y el botón de envío vive dentro del formulario que se
desmonta. **No medido**: si el `role="alert"` del resultado del propio formulario llega a montarse
antes de que React aplique el árbol revalidado, porque en React 19 el resultado de la acción y la
carga revalidada pueden llegar en la misma respuesta y confirmarse en el mismo commit. Eso se
contesta con el oído, no leyendo el código, y es exactamente para lo que existe el recorrido.

### Los pasos para dejar la base en el estado que el recorrido necesita

`./scripts/demo.sh` estrena una base cada vez, así que este estado **hay que rehacerlo**. Son cuatro
llamadas y tardan segundos. Con la demo ya en pie y su puerto de API en `5100`:

```bash
# 1. La alerta con divergencia dentro de la misma banda.
#    Tres pedidos del mismo comprador en los diez minutos previos a ORD_000219.
cat > /tmp/divergencia.csv <<'CSV'
merchantId,merchantReferenceId,buyerReferenceId,occurredAt,amountCents,currencyCode,countryCode,city,channel,deviceSessionId
MER_BR_STORE,ORD_900201,BUY_000101,2026-07-27T13:32:00.000Z,8000,BRL,BR,Belo Horizonte,WEB,DEV_000101
MER_BR_STORE,ORD_900202,BUY_000101,2026-07-27T13:35:00.000Z,10000,BRL,BR,Belo Horizonte,WEB,DEV_000101
MER_BR_STORE,ORD_900203,BUY_000101,2026-07-27T13:38:00.000Z,12000,BRL,BR,Belo Horizonte,WEB,DEV_000101
CSV
curl -sS -X POST -F "file=@/tmp/divergencia.csv" -F "format=CSV" http://127.0.0.1:5100/api/order-imports
curl -sS -X POST http://127.0.0.1:5100/api/risk-evaluations:run

# 2. La explicación ya escrita, sobre la alerta de ORD_000011.
ID=$(curl -sS "http://127.0.0.1:5100/api/alerts?pageSize=50" \
  | node -e 'let r="";process.stdin.on("data",c=>r+=c).on("end",()=>console.log(JSON.parse(r).items.find(i=>i.merchantReferenceId==="ORD_000011").id))')
curl -sS -X POST "http://127.0.0.1:5100/api/alerts/$ID/explanation"

# 3. El panel de denegados por el proveedor, que sin esto está vacío
#    y deja el paso 17 sin ninguna tabla que recorrer.
curl -sS -X POST http://127.0.0.1:5100/api/demo-data/external-evaluations:request
curl -sS -X POST http://127.0.0.1:5100/api/demo-data/external-callbacks:deliver
```

Lo que queda montado, verificado contra la API:

| Qué | Dónde | Estado |
| --- | --- | --- |
| Alertas abiertas | `/alerts` | 23: 11 `MEDIA`, 6 `ALTA`, 6 `CRÍTICA`. Las tres bandas |
| Divergencia **sin cambio de banda** | alerta de `ORD_000219` | snapshot `70 amount_anomaly+new_buyer_high_value`, vigente `70 amount_anomaly+velocity`. `hasBandDivergence: false` |
| Explicación escrita | alerta de `ORD_000011` | `READY`, plantilla `e7-v2` |
| Denegados sin alerta local | `/dashboard` | 51 pedidos |

**Por qué esa divergencia y no otra.** El aviso solo aparece si cambia el score o cambia el conjunto
de reglas; cambiar solo una cifra dentro de una señal no alcanza. Y como las bandas son estrechas
frente a los pesos —`MEDIA` 60-69, `ALTA` 70-89, `CRÍTICA` 90-100, con pesos de 10, 20, 30 y 40—,
casi cualquier cambio de una sola regla cruza una banda. El único peso de 10 es `unusual_hour`, y
ninguna alerta de banda `ALTA` lo lleva, así que el ±10 no estaba disponible.

El camino que sí funciona es **intercambiar dos reglas del mismo peso**. Tres pedidos previos del
mismo comprador apagan `new_buyer_high_value` —que exige que el comprador no tenga historia— y
encienden `velocity` —que exige tres pedidos previos en diez minutos—: −30 y +30, el score queda en
70 y la banda no se mueve. `amount_anomaly` sigue disparando porque pasa a alcance de comprador y
67.328 sobre una mediana de 10.000 son 6,7 veces, muy por encima del multiplicador de 3.

**La perturbación es mínima y está medida**: la corrida creó 7 evaluaciones, reusó 296, y abrió
**cero** alertas nuevas. Ninguna otra alerta cambió su score vigente. Los tres pedidos son montos
bajos en el país habitual del comercio, así que ninguno dispara nada por su cuenta.

Los identificadores empiezan en `ORD_900201` para no chocar con el corpus, que llega a `ORD_000300`,
ni con las muestras de `docs/muestras/`, que usan `ORD_9001xx` y `ORD_95xxxx`. El archivo **no se
versiona**: `docs/**` está reservado a `E9D`, y de todas formas es un payload de un solo uso.

**Los dos avisos que no se pueden producir sobre una base nueva** siguen sin poderse: «explicación
desactualizada» y «escrita por otra plantilla» exigen dos versiones de plantilla vivas. El guión ya
los declara no verificados en su paso 12.

### La falsación de la compuerta, con su error exacto

Le saqué el nombre accesible al botón de cargar el corpus —`{pending ? seedButtonPending : ""}` en
`corpus-actions.tsx`— y corrí `./scripts/check.sh` entera. Se puso roja:

```
FAIL  src/test/accessibility.test.tsx > las cinco pantallas, con su marco > la importación
AssertionError: expected '[critical] button-name\n   Buttons mu…' to be '' // Object.is equality
+ [critical] button-name
+    https://dequeuniversity.com/rules/axe/4.13/button-name?application=axeAPI

Tests  3 failed | 251 passed (254)
```

Las otras dos que fallaron son de `import/page.test.tsx` y ya existían. La que importa es la
primera: **la comprobación nueva es la que caza el botón sin nombre**, y lo hace con impacto
`critical`. Restaurado, la compuerta vuelve a verde.

Hay dos falsaciones más, de las dos decisiones que podrían haber quedado como adorno:

- **La profundidad de `label-has-associated-control`.** Con `depth: 3`, un archivo con un `label`
  vacío y otro con el texto a profundidad 4 producen los dos `A form label must have accessible
  text`. La opción sube lo justo y no desactiva la regla.
- **Componer cada pantalla con su cabecera y su `<main>` no es decoración.** Metí un segundo
  `<main>` en el helper y fallaron **siete de las nueve** pruebas, con `landmark-no-duplicate-main`
  y `landmark-unique`. Sobre un fragmento suelto esas reglas están *inaplicables*, junto con
  `region`, `landmark-one-main`, `page-has-heading-one` y `bypass`; son justo las que contestan si
  alguien puede saltar por regiones y encabezados en vez de recorrer la página con la flecha.

### Archivos modificados

- `frontend/package.json` y `frontend/package-lock.json`: las dos dependencias, declaradas en
  versión exacta. El lockfile las mueve de anidadas a la raíz y deduplica; no cambia ninguna versión.
- `frontend/eslint.config.mjs`: el conjunto recomendado del plugin, y la profundidad de una regla.
- `frontend/src/test/axe.ts`: **nuevo**. El helper, escrito a mano.
- `frontend/src/test/accessibility.test.tsx`: **nuevo**. Nueve pruebas: las cinco pantallas con su
  marco, la cola vacía, el detalle en portugués, y los dos estados que solo existen después de
  apretar un botón.
- `frontend/src/app/dashboard/quality-section.tsx`: el único defecto corregido.
- `Coordination/Handoffs/Claude.md`: esta entrada.

`Coordination/Tasks/E9C2-ACCESIBILIDAD.md` aparece en el diff contra `52cbdea` porque el coordinador
lo editó en `a09c1e7`; esta tarea no lo tocó.

### Verificación

| Comando | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9C2-ACCESIBILIDAD.md` | Válido en la tercera vuelta, tras aprobar las dependencias en D7 |
| `npx eslint --print-config` antes de tocar nada | 6 reglas activas, todas `warn` |
| `npx eslint --print-config` después | 31 activas, en `error` |
| `npm run check` | Verde. 18 archivos de prueba, 254 pruebas |
| `./scripts/check.sh` | **Verde** |
| `./scripts/smoke-ui.sh` | **Verde: 71 comprobaciones, 0 fallas** |
| Falsación del botón sin nombre | La compuerta se pone roja con `[critical] button-name` |
| Falsación de `depth: 3` | La regla sigue fallando con un `label` vacío y con texto a profundidad 4 |
| Falsación del marco | Un segundo `<main>` rompe 7 de 9 pruebas |
| `git status --porcelain` | Limpio; solo paths autorizados en el diff |
| `npm ls eslint-plugin-jsx-a11y axe-core` | Una copia de cada una, deduplicadas |
| Estado del recorrido | Verificado contra la API: 23 alertas, divergencia sin cambio de banda, explicación `READY`, 51 denegados |

### Decisiones y supuestos

- **Se toma la lista de reglas del propio plugin** (`flatConfigs.recommended.rules`) en vez de copiar
  treinta y un nombres con sus opciones: una lista a mano deja de cubrir, en silencio, la regla que
  agregue la próxima versión. Se extienden solo las `rules` y no el objeto entero, porque
  `eslint-config-next` ya registró el plugin y la configuración plana prohíbe registrarlo dos veces.
- **Las seis heredadas suben de `warn` a `error`.** Con `--max-warnings=0` el resultado de la
  compuerta es el mismo; el nivel dice lo que se quiere decir en vez de depender de una bandera del
  script.
- **`control-has-associated-label` se consideró y se dejó afuera.** Viene apagada en el conjunto
  recomendado por ruidosa, y lo que comprueba lo cubre `axe-core` sobre el árbol renderizado, que
  además puede leer el texto del diccionario. Agregarla sería superficie sin beneficio.
- **El helper devuelve texto y no una cuenta**, para que el diff de una prueba fallada traiga la
  regla, su impacto, el elemento y el enlace. Un `expect(3).toBe(0)` obligaría a correr axe otra vez
  a mano.
- **Se comprobó que `sr-only` compila como corresponde** —`clip-path`, `position:absolute`, 1×1, sin
  `display:none`—, porque es lo que sostiene las `<caption>` ocultas y `axe-core` en jsdom no lo ve.
- La tarea **no** tocó backend, contrato, migraciones, ni `frontend/openapi/**`.

### Riesgos o pendientes

- **La parada es la forma de la tarea, no una interrupción.** Los siete hallazgos están sin
  corregir a propósito. El número 2 en particular necesita el oído antes que el teclado.
- **Cero violaciones no es «la consola es accesible».** Es «ninguna regla que una máquina puede
  decidir está rota». No hay nada acá sobre orden de foco, sobre si un anuncio llega a tiempo, ni
  sobre contraste.
- **Las pruebas corren sobre fixtures.** Se contrastó a mano el HTML servido de las cinco pantallas
  contra lo que las pruebas afirman —un `<main>`, un `<header>`, un `<nav aria-label>`, un `h1`, las
  `<caption>` y el `<html lang>`— y coincide. Aun así, una fixture que se aleje del contrato haría
  que estas pruebas midan otra cosa; el guardián de eso sigue siendo `OpenApiDriftTests`.
- **`E9D` regenera las capturas.** El único cambio visible de esta fase es dentro del `<dd>` de la
  sección de calidad y es imperceptible, pero la fase 2 puede no serlo.

### Integración

- Orden sugerido: **no integrar todavía.** La rama queda en espera del recorrido; la fase 2 se
  despacha con la lista aprobada y se integra entera.
- Migraciones o pasos manuales: ninguna. No hay migración ni recaptura de contrato.
- Posibles conflictos: `frontend/package-lock.json`, si otra tarea toca dependencias. Ninguna otra
  está asignada.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh`.

## `E9C2-ACCESIBILIDAD` (fase 2) — Los cinco que el coordinador aprobó

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 9
- Rama/worktree: `claude/e9c2-accesibilidad`
- Commit base: `a994855`
- Commit final: `71b62cf`, el último de código. El commit de esta entrada lo sigue.
- Fecha: 2026-09-07

### Lo primero, porque condiciona todo lo que la etapa puede decir

**El recorrido con lector de pantalla fue parcial.** Se recorrieron con VoiceOver la portada (`/`)
entera y el encabezado de la cola de alertas (`/alerts`), sin hallazgos; la tabla, el detalle de
alerta, el dashboard y la importación quedaron sin recorrer. Ningún documento de la etapa puede
decir «alguien recorrió la consola sin ver la pantalla», porque no es cierto. Lo que se puede decir,
y es lo que `E9D` tiene que escribir, es esto: **la pasada automática se hizo entera; el recorrido
con lector de pantalla se hizo sobre dos pantallas y se interrumpió.**

Los cinco hallazgos que entraron **no dependían del recorrido para existir**: los midieron las
herramientas y la lectura del código en la fase 1. El único punto que el oído habría aportado —si el
`role="alert"` del formulario alcanza a montarse antes del árbol revalidado— no cambia el arreglo,
porque hacerlo es correcto en los dos casos.

### Qué entró

Los cinco de la tabla del coordinador, en su orden de severidad. Cada uno con su prueba y con su
falsación.

| # | Sev. | Qué se hizo |
| --- | --- | --- |
| 2 | `alta` | El veredicto registrado se anuncia y recibe el foco. Dos mecanismos, abajo |
| 1 | `media` | Con el score quieto, el aviso de divergencia habla de las señales y no de la flecha |
| 3 | `baja` | `<caption class="sr-only">` en la tabla del barrido, la única de la consola que no tenía |
| 4 | `baja` | `role="status"` cuando la acción salió bien; `alert` solo para el fallo |
| 5 | `baja` | El `id` del encabezado sale de una clave del código y no del título traducido |

### Qué quedó afuera, y por qué

- **6, contraste de color, `baja`. Afuera y dicho.** No lo comprueba nada: jsdom no calcula estilos,
  así que `axe-core` devuelve `color-contrast` como *incompleta* y la regla está apagada por su
  nombre en `src/test/axe.ts` para que un incompleto no se lea como aprobado. El recorrido tampoco lo
  habría cubierto —quien no ve la pantalla no lo nota— y las dos pantallas que sí se recorrieron no
  lo miran. **Queda no verificado, y dicho.**
- **7, la cola de alertas con un solo encabezado, `baja`. Sin acción.** Es una pantalla de una sola
  tabla y la tabla ya tiene su `<caption>`; un `h2` puesto para llenar la lista del rotor sería un
  encabezado que no encabeza nada.

### El hallazgo 2, con el detalle que merece

El problema tenía dos mitades y necesitó dos respuestas, porque son dos preguntas distintas: **qué
pasó** y **dónde quedé**.

**La región viva.** `ReviewPanel` renderiza siempre un `<p role="status">`, vacío y oculto mientras
no haya nada que decir. Que exista **antes** es la condición entera: una región viva que se inserta
en el mismo commit que su propio texto no dispara nada en ningún lector, así que ponerle
`role="status"` a `RecordedVerdict` habría sido exactamente la clase de comprobación que promete algo
que no ocurre. De paso es el mismo lugar donde ahora vive el aviso de divergencia, que antes estaba
dentro de la sección del formulario: una sola región viva por panel, que dice lo que corresponda en
cada momento. El efecto visible es que el aviso de divergencia pasó a estar **arriba** del encabezado
«Emitir veredicto» en vez de debajo.

**El foco.** `VerdictFocus` es un vecino que no dibuja nada y que ve la transición de `false` a
`true`. Ver la transición es todo el punto: un componente que solo existiera dentro del veredicto no
podría distinguir «acabo de revisar» de «entré a una alerta ya revisada», y en el segundo caso mover
el foco sería arrebatárselo a alguien que recién llega. Por eso el panel lo renderiza en los dos
estados, el estado anterior se recuerda en un `ref`, y la primera carga nunca dispara.

**Es un vecino y no un envoltorio porque la guarda de la frontera tenía razón.** La primera versión
envolvía el panel y recibía el bloque como `children`; `boundary.test.ts` la rechazó en el acto —
«ReviewRegion recibe la prop no primitiva «children»»—. La frontera de esta consola admite
primitivas y nada más, y debilitar esa guarda durante una pasada de accesibilidad habría sido cambiar
una comprobación real por una comodidad. El componente recibe ahora dos primitivas, `recorded` y
`targetId`, y busca el elemento en el documento; `RecordedVerdict` puso el `id` y un `tabIndex={-1}`,
sin el cual `focus()` sobre una sección no hace nada.

**Lo que sigue sin estar medido**, y conviene que `E9D` no lo confunda con medido: si el anuncio
llega **a tiempo** y si el orden en que se oyen las dos cosas es el cómodo. Eso se contesta con el
oído. Lo que estas pruebas fijan es que la región existe en los dos estados y que el foco aterriza en
el bloque correcto, que es condición necesaria y no suficiente.

### Las falsaciones, una por corrección

Cada arreglo se deshizo, se corrió su prueba, y la prueba falló. Restaurados los cinco, todo vuelve a
verde.

| # | Qué se deshizo | Qué dijo la prueba |
| --- | --- | --- |
| 1 | Una sola redacción para el aviso | 4 fallas: `expected 'La evaluación del pedido cambió (100 …' not to match /100 → 100/` |
| 2, foco | Quitar el `focus()` del efecto | `expected <body><div>…</div></body> to be <section …>…</section>` |
| 2, región | La región viva solo cuando hay aviso | 2 fallas: `Unable to find an accessible element with the role "status"` |
| 3 | Quitar la `<caption>` del barrido | `Unable to find an accessible element with the role "table" and name /Precisión, recall, F1…/` |
| 4 | Volver a `role="alert"` siempre | 2 fallas, una por componente: `Unable to find an accessible element with the role "status"` |
| 5 | Derivar el `id` del título otra vez | 3 fallas: `panel-open-alerts: expected null not to be null` y `section-seed: …` |

La del hallazgo 5 merece una nota: **falla ya en castellano**, porque el `id` derivado del título
castellano es `panel-alertas-abiertas`. La prueba recorre igual los dos idiomas, porque una sola
pasada no distingue un `id` estable de uno que coincide por casualidad.

### Los literales nuevos

Tres claves, las tres en `es.ts` **y** en `pt.ts`, en un commit propio y anterior al código que las
usa. Una clave que falte en uno es error de compilación, como estableció `E9C1`:

- `alertDetail.divergenceAdvisorySignals(score)` — el aviso cuando el score no se movió.
- `alertDetail.verdictAnnounced` — lo que dice la región viva al registrarse el veredicto.
- `dashboard.qualitySweepCaption` — el nombre de la tabla del barrido.

La prueba de divergencia comprueba la redacción en los **dos** idiomas; las de identificadores
renderizan el dashboard y la importación en `es` y en `pt`.

### Archivos modificados

Ninguno fuera de los paths autorizados. No se tocó backend, contrato, migraciones, `frontend/openapi/**`,
`docs/**` ni `README.md`.

- `frontend/src/lib/i18n/es.ts` y `pt.ts`: las tres claves.
- `frontend/src/app/alerts/[id]/divergence.ts` y `divergence.test.ts`: hallazgo 1.
- `frontend/src/app/alerts/[id]/review-panel.tsx`: la región viva estable y el destino del foco.
- `frontend/src/app/alerts/[id]/verdict-focus.tsx` y `verdict-focus.test.tsx`: **nuevos**.
- `frontend/src/app/alerts/[id]/page.test.tsx`: tres pruebas del panel.
- `frontend/src/app/alerts/[id]/review-form.tsx` y `review-form.test.tsx`: hallazgo 4.
- `frontend/src/app/import/action-outcome.tsx` y `action-outcome.test.tsx`: **la prueba es nueva**;
  el componente no tenía ninguna propia.
- `frontend/src/app/dashboard/quality-section.tsx`: hallazgo 3.
- `frontend/src/app/dashboard/panels.tsx`, `dashboard/page.tsx`, `import/action-section.tsx` e
  `import/page.tsx`: hallazgo 5, con sus dos archivos de prueba.
- `Coordination/Handoffs/Claude.md`: esta entrada.

Cinco commits, uno por parte: las claves primero, después un hallazgo por commit salvo el 3 y el 5,
que van juntos porque comparten los dos archivos de prueba y no se pueden partir sin partir un
archivo.

### Verificación

| Comando | Resultado |
| --- | --- |
| `npx tsc --noEmit` | Limpio |
| `npx eslint .` | Limpio, con las treinta y una reglas de accesibilidad en `error` |
| `npx vitest run` | 20 archivos, **273 pruebas**; eran 254 |
| `./scripts/check.sh` | **Verde.** 117 tests de dominio, 172 de integración, 273 de frontend, build Release y build Next.js |
| `./scripts/smoke-ui.sh` | **Verde: 71 comprobaciones, 0 fallas**, con la pasada `pt` y el `SALVO_LANGUAGE=fr` que no arranca |
| Falsación de las cinco correcciones | Las cinco ponen su prueba en rojo; ver la tabla de arriba |
| `git status --porcelain` | Limpio |

### Decisiones y supuestos

- **La guarda de la frontera no se tocó.** Cuando `boundary.test.ts` rechazó la primera versión del
  arreglo del foco, se cambió el arreglo y no la guarda. Un componente cliente que recibe `children`
  recibe un objeto, y la regla dice primitivas.
- **El hallazgo 6 no se arregló ni se disimuló.** Habría sido fácil agregar una comprobación de
  contraste que no comprueba contraste; queda apagada por su nombre y declarada.
- **Los `id` son claves del código, no del diccionario.** `panel-open-alerts` y `section-seed` no se
  traducen ni se muestran; el diccionario sigue siendo solo para lo que alguien lee.
- **El aviso de divergencia cambió de lugar en la pantalla.** Es consecuencia directa de que la
  región viva tiene que ser una sola y tiene que preexistir. Se declara porque es lo único visible
  que esta fase mueve, y `E9D` regenera las capturas.

### Riesgos o pendientes

- **Lo que el recorrido no cubrió sigue sin cubrir**, y son cuatro pantallas: la tabla de alertas, el
  detalle, el dashboard y la importación. La afirmación de la etapa tiene que decir eso y nada más.
- **Cero violaciones no es «la consola es accesible».** Es «ninguna regla que una máquina puede
  decidir está rota». Sigue sin haber nada sobre orden de foco fuera del caso del veredicto, sobre si
  un anuncio llega a tiempo, ni sobre contraste.
- **`E9D` regenera las capturas.** El aviso de divergencia arriba del encabezado es un cambio visible.

### Integración

- Orden sugerido: esta rama sola. `E9D` va después de ella integrada.
- Migraciones o pasos manuales: **ninguna**. No hay migración, ni contrato, ni recaptura de OpenAPI,
  ni dependencias nuevas: las dos de la fase 1 ya están declaradas y no entró una tercera.
- **Integrar por merge y no por rebase**: rebasar movería el `merge-base` y volvería falso el commit
  base declarado arriba.
- Posibles conflictos: `frontend/package-lock.json` si otra tarea tocara dependencias; ninguna otra
  está asignada.
- Verificación posterior al merge: `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre el estado
  integrado. Para verlo a mano, `./scripts/demo.sh` y los pasos de preparación que dejó la fase 1:
  emitir un veredicto sobre una alerta y comprobar que el bloque verde recibe el foco.

Estado: **Lista para integrar**.

## `E9D-CIERRE` — El repaso final, y las cifras que salen de una corrida

### Identificación

- Estado de la rama: `Lista para integrar`
- Etapa: 9, la última del MVP
- Rama/worktree: `claude/e9d-cierre`
- Commit base: `ad70c58`
- Commit final: `<FINAL>`
- Fecha: 2026-09-07

### Resultado

El repositorio dejó de mentir en cifras. El bloque marcado del README describía el corpus v1
entero, el guion repetía lo mismo, y cuatro párrafos de prosa describían un proyecto que ya no
existe. Todo eso salió de una corrida real y fechada, o se reescribió contra el árbol.

Las nueve deudas que la etapa decidió no pagar están escritas con su motivo, y con ellas el límite
más incómodo que este README puede declarar: **ninguna de sus cifras está verificada por script**, y
se comprobó cambiando una por otra falsa.

### La corrida de la que salen las cifras

**2026-09-07**, sobre una base recién migrada en un directorio temporal. Nunca sobre `salvo.db`.

```bash
dotnet build Salvo.slnx --configuration Release
dotnet ef database update \
  --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
  --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
  --configuration Release --no-build --connection "Data Source=${base}"
ASPNETCORE_URLS=http://127.0.0.1:5399 \
ConnectionStrings__SalvoDb="Data Source=${base}" DemoData__Enabled=true \
  dotnet backend/src/Salvo.Api/bin/Release/net10.0/Salvo.Api.dll

curl -X POST .../api/demo-data/seed
curl -X POST .../api/risk-evaluations:run
curl      .../api/dashboard          .../api/evaluation-metrics      .../api/alerts
curl -X POST .../api/demo-data/external-evaluations:request
curl -X POST .../api/demo-data/external-callbacks:deliver
```

Cada cifra publicada se leyó de esas respuestas y se contrastó además contra la base con SQL. **Nada
se transcribió de un handoff.** Lo que los handoffs sirvieron fue para contrastar, y ahí apareció lo
que sigue.

| Magnitud | Corrida | Handoff que la produjo | ¿Coincide? |
| --- | --- | --- | --- |
| Fraudes | 28 de 300 | `E9A`: 28 | sí |
| Alertas | 23: 11 media, 6 alta, 6 crítica | `E9A`: 11/6/6 | sí |
| Matriz de holdout | 6 / 3 / 4 / 87, F1 0,632 | `E9A`: 6/3/4/87, F1 0,632 | sí |
| Matriz de calibración | 11 / 3 / 7 / 179, F1 0,688 | `E9A`: 11/3/7/179 | sí |
| Reglas que disparan | 50 / 19 / 10 / 4 / 2 / 2 | `E9A`: idénticas | sí |
| Umbral del barrido | 60 | `E9A`: 60 | sí |
| Denegados sin alerta local | **43** tras pedir las externas; **51** tras entregar los callbacks | `E9A` y el test fijan 43 | sí, **en su momento** |

**La única diferencia aparente no lo es, y conviene dejarla escrita.** El panel de denegados vale 43
inmediatamente después de `external-evaluations:request`, que es donde
`DashboardEndpointTests.TheExternalDenialsPanelShowsTheFraudTheRulesNeverFlagged` mide, y 51 después
de entregar los veintiún callbacks pendientes, porque ocho de ellos vuelven denegados. Son dos
instantes distintos del mismo flujo, no dos mediciones en desacuerdo. El README publica las dos con
su momento en vez de elegir una.

### Tres cifras del brief que el árbol desmiente

Las tres son menores y las tres se escribieron como manda el árbol, no como decía el brief.

1. **«Cada falso negativo mueve el recall doce puntos».** El holdout tiene **diez** pedidos
   fraudulentos —seis verdaderos positivos y cuatro falsos negativos—, así que uno más mueve el
   recall exactamente **diez** puntos. El README dice diez.
2. **«Los dos códigos de error de fila que no tienen rótulo».** Es **uno**: `UNKNOWN_FIELD`, y solo
   lo emite el parser de JSON. Los códigos por registro son seis en total —`REQUIRED`,
   `INVALID_FORMAT`, `OUT_OF_RANGE`, `UNSUPPORTED_VALUE` y `REFERENCE_CONFLICT` del dominio y del
   handler, más `UNKNOWN_FIELD`— y el diccionario traduce los cinco primeros.
3. **«El `16,7 %` de `unusual_hour` recalculado».** Recalculado sobre el v2 da **0 %**, y con eso la
   conclusión que ese número sostenía se invierte: la regla **sí** dispara, dos veces, en
   `ORD_000160` y `ORD_000244`. La franja más rara que **no** llega a disparar da 12 %. La
   verificación se hizo replicando la aritmética del motor en Python sobre la fixture y comprobando
   que da exactamente las dos señales que el motor escribió, con sus mismos `totalCount` de 25 y 26.

### La prosa invalidada, que ningún `grep` de cifras encuentra

Las cuatro entradas que el brief listaba, más tres que no listaba y aparecieron leyendo enteras las
secciones de límites:

| Dónde | Qué decía | Por qué era falso |
| --- | --- | --- |
| README, «Límites declarados» | «El corpus alcanza tres de las seis reglas», «un solo arquetipo», «la banda alta tampoco aparece» | Las seis disparan, hay siete arquetipos y la banda alta tiene seis alertas |
| README, «Límites declarados» | «Los detalles de las señales están en inglés… cuando el motor los emita, el extractor se borra» | El motor ya los emite y el extractor se borró en `E9B` |
| README, «Cinco decisiones» | «El tokenizador corre sobre los dos lados: sobre el `detail` en inglés… para construir los hechos» | **No listada en el brief.** Los hechos se construyen en el dominio desde campos tipados; el tokenizador corre sobre un solo lado |
| README, diagrama del recorrido | «Seis reglas puras emiten señales con su detalle» | **No listada.** Emiten campos medidos |
| README, «Cómo se verifica» | «en seis escenarios» | **No listada.** El smoke corre ocho desde `E9C1` |
| Guion, minuto 9–10 | «Las métricas dan perfectas» y «tres de las seis reglas nunca abren una alerta» | Lo contrario de lo que la etapa consiguió |
| Guion, minuto 2–5 | «monto atípico —veintitrés veces la mediana del comercio—» | En el v2 son 3,4 veces, que es lo que dice el texto dorado |
| `docs/muestras/`, dos secciones | «`unusual_hour` no puede dispararse» y «la banda `ALTA`, la única que el corpus nunca produce» | Las dos se invirtieron |
| `docs/capturas/` | «18 alertas abiertas» y «con scores de solo 60 y 90» | 23 alertas y cuatro scores distintos |

### La falsación exigida

Cambié `| Alertas abiertas | 23: 11 media, 6 alta y 6 crítica |` por `| Alertas abiertas | 902: 400
media, 250 alta y 252 crítica |` y corrí la comprobación:

```
$ ./scripts/check-docs.sh
72 comprobaciones, 0 fallas.
```

**Nada la detectó, y no podía detectarla.** `check-docs.sh` extrae de un documento exactamente tres
clases de token —rutas entre acentos graves, destinos de enlace relativo y nombres de test— y no
mira ninguna otra cosa. Ningún otro paso de la compuerta lee el README: el único que lo abre es ese
script, invocado desde `check.sh`. Una cifra de nueve veces la real pasa en verde.

De ahí salen dos cosas que quedaron escritas. La primera, en «Límites declarados» del README: el
documento dice qué parte de sí mismo está verificada mecánicamente y qué parte depende de que
alguien la regenere. La segunda, la **decisión 69** de la bitácora: un bloque de cifras declara de
qué corrida salió y de qué fecha, porque lo que no se puede detectar se fecha.

Después restauré el archivo y volví a correr el script, que dio verde con la cifra verdadera.

### Las seis capturas, y el panel que salía vacío

Las seis se regeneraron sobre una base nueva y **las aserciones previas a cada disparo pasaron sin
cambios**: la consola cambió de texto —la frase de cada señal se compone desde campos y muestra la
mediana— pero no de forma, así que ninguna ancla se rompió. Eso no era lo esperado y conviene
decirlo, porque el brief anticipaba lo contrario.

**El hallazgo salió de mirar los PNG, no de que el script pasara.** En la primera tanda, el panel
«Denegados por el proveedor sin alerta local» aparecía vacío en la toma del dashboard, con su texto
de estado diciendo que nadie había pedido todavía la evaluación externa. Era cierto: `capturas.sh`
solo pedía la evaluación externa de `ORD_000011`, que es la que la toma 6 necesita. El resultado era
que la captura que el README publica mostraba en blanco justamente el panel que `E9A` construyó para
que «hay fraude que un proveedor ve y el motor local no» dejara de ser prosa.

Dos cambios, los dos dentro de la reserva:

- `scripts/capturas.sh` pide ahora la evaluación externa del **corpus entero**, después del pedido
  individual. El endpoint solo pregunta por los pedidos que nunca se le consultaron a ese proveedor,
  así que `ORD_000011` conserva su `APPROVED` y la toma 6 sigue teniendo su divergencia. **No** se
  entregan los callbacks: con las síncronas el panel ya muestra 43 pedidos, y dejar veintiuno
  pendientes es el estado más honesto para fotografiar.
- `tools/capturas/capturar.mjs` exige antes de disparar la toma 4 que en pantalla estén la columna
  «Score local» —que solo existe cuando el panel tiene filas; vacío, el panel es un párrafo— y
  `ORD_000275`, uno de los tres arquetipos de fraude que las reglas no pueden ver. Sin esa aserción
  el panel podía volver a vaciarse en silencio.

La tabla del panel tiene scroll propio, así que las 43 filas no estiran la captura.

Las seis se miraron una por una. Dos observaciones más:

- La toma 5 deja ver **dos de las deudas declaradas**, y se dejaron a la vista a propósito: el aviso
  dice «Se importaron 1 pedidos» y el mensaje técnico de cada registro rechazado sigue en inglés.
  Una captura que las escondiera sería una captura peor.
- La toma 3 reproduce exactamente el texto que `ExplanationGoldenTests` fija, con sus 3,4 veces la
  mediana y el pie con la versión de plantilla y el idioma.

### Las siete bases `.db`, con su fecha y su contenido

**Ninguna se borró.** `rm` está denegado en este repositorio y es regla del usuario; esta lista
existe para que decida él. Se consultaron sobre **copias** en un directorio temporal, porque cinco
de las siete se niegan a abrirse en solo lectura y no valía la pena arriesgar una escritura.

El repositorio tiene **siete** migraciones. La columna dice cuántas aplicó cada base.

| Archivo | Modificada | Tamaño | Migr. | Qué contiene |
| --- | --- | --- | --- | --- |
| `salvo-demo-20260906-194054.db` | 2026-09-06 19:41 | 644 KiB | 6 de 7 | Ensayo de `demo.sh` con el **corpus v1**: 300 pedidos, 18 fraudes, una corrida `e3-v1`, 18 alertas (13 media, 5 crítica, ninguna alta). Sin externas ni explicaciones |
| `salvo-demo-20260906-194152.db` | 2026-09-06 19:42 | 640 KiB | 6 de 7 | El mismo ensayo repetido un minuto después. Contenido idéntico al anterior |
| `salvo-demo-20260907-125920.db` | 2026-09-07 12:59 | 648 KiB | 7 de 7 | Primer ensayo con el **corpus v2**: 300 pedidos, 28 fraudes, una corrida `e3-v2`, 23 alertas (11/6/6), una explicación. Sin externas |
| `salvo-demo-20260907-155213.db` | 2026-09-07 16:04 | 904 KiB | 7 de 7 | Corpus v2 más tres pedidos importados a mano (303), dos corridas, 23 alertas, 303 evaluaciones externas, una explicación |
| `salvo-demo-20260907-160731.db` | 2026-09-07 16:14 | 912 KiB | 7 de 7 | El ensayo siguiente, mismo contenido. **Dejó sueltos sus dos sidecars**, `.db-shm` y `.db-wal`, en el mismo directorio |
| `salvo.db` | 2026-09-06 14:46 | 1,4 MiB | 6 de 7 | La base de desarrollo. **No es el corpus demo**: 328 pedidos del v1 más importaciones manuales, con un cuarto comercio `MER_UY_PHARMA`; 18 fraudes, siete corridas `e3-v1`, 21 alertas (13/1/7), una revisada, dos explicaciones, 328 externas y 21 recibos |
| `salvo.design.db` | 2026-09-02 17:15 | 96 KiB | 2 de 7 | **Vacía**: cero pedidos, cero evaluaciones. Su esquema tiene cinco tablas y ni siquiera existe `alerts`. Quedó de la preparación de la Etapa 4 |

Lo que yo haría, y es solo una recomendación:

- **Las cinco `salvo-demo-*` son desechables.** Son ensayos de `demo.sh` y ninguna guarda nada que
  no se regenere en un minuto. Las dos del 6 de septiembre son además del corpus anterior.
- **`salvo.design.db` no sirve para nada**: está vacía y su esquema está cinco migraciones atrás.
- **`salvo.db` es la decisión que hay que pensar.** Es la base de desarrollo y tiene una alerta
  revisada y dos explicaciones que nada más tiene, pero está una migración atrás y su corrida vigente
  es `e3-v1`, así que hoy la consola contra esa base no muestra el corpus de la etapa. Si se
  conserva, conviene migrarla antes de volver a usarla.
- Y los dos sidecars `salvo-demo-20260907-160731.db-shm` y `.db-wal`, que se van con su base.

### `demo-orders.v1.json`: recomiendo conservarlo

`EmbeddedDemoOrderSource` compone el nombre del recurso desde `DemoDatasetShape.Current.Version`, y
el `.csproj` declara como recurso embebido **solo** el v2. El v1 no está embebido, así que **no se
puede sembrar ni por error**: es un archivo inerte. Conservarlo documenta de dónde salió el v2 y
deja comparable el cambio de reparto de etiquetas, que es el corazón de la etapa. Retirarlo solo
evitaría que alguien lo confunda con el corpus vigente, y para eso alcanza con que `docs/muestras/`
nombre el v2, que ya lo hace desde esta tarea.

### `glosario.mjs` se queda donde está

Moverlo a `tools/` obligaría a tocar la ruta que su propio encabezado documenta y la que
`glosario-pt.md` cita en su línea 9, para ganar prolijidad de árbol y perder cercanía con los dos
diccionarios que lee y el archivo que produce. Se queda, y el motivo está escrito en «Límites
declarados» junto con la objeción: `frontend/src/` es código de la consola y un generador no lo es.

### El artículo para revisores, listo para publicar

Se escribe de cero: el artículo vive fuera del repositorio y esta tarea no puede leer el anterior.
Va entero acá para que se copie de una sola vez. La geografía está corregida y hay además una
aclaración que no estaba pedida y que conviene: los tres comercios del corpus están en el corredor
UTC−3 por una razón técnica, no porque dibujen el mercado de nadie.

---

# Construí una consola antifraude donde la IA no decide nada

Salvo es la consola de riesgo de un comercio electrónico ficticio. Puntúa pedidos con reglas
deterministas, abre alertas auditables, le pide una segunda opinión a un proveedor antifraude
externo y pone cada evaluación en palabras. Es un proyecto de portfolio: los datos son sintéticos,
el proveedor externo es una simulación en proceso y no hay una credencial de nadie en ningún lado.

Lo interesante no es qué hace. Es qué decidí que **no** hiciera.

## Es la consola de un comercio, no un proveedor antifraude

La distinción parece de tamaño y no lo es. Un proveedor evalúa para muchos comercios y ve el fraude
a través de toda su base: la reputación de una tarjeta, el dispositivo que aparece en cinco
comercios la misma tarde, el efecto de red. Un comercio ve una sola cosa, su propio historial de
pedidos, y de ahí salen las seis reglas de Salvo: monto atípico contra la mediana, ráfaga de
pedidos, dos países en dos horas, comprador nuevo con monto alto, país fuera del habitual y franja
horaria rara.

Elegir el lado del comercio cambia qué señales existen, y es la primera decisión de dominio del
proyecto. Todo lo demás se apoya en ella.

## Tres fuentes de verdad que no se mezclan nunca

El score local, la opinión del proveedor externo y el veredicto de la analista son tres cosas
distintas, y cada una tiene su tabla, su ciclo de vida y su regla de escritura. Mezclarlas es el
error de diseño que este modelo existe para no cometer.

Dos consecuencias que se ven en el código y no en un párrafo. La primera: la tabla de evaluaciones
locales tiene un `CHECK` que la restringe a la fuente local, así que ningún estado externo puede
colarse ahí ni por error de un caso de uso. La segunda es más sutil. Una evaluación no se actualiza
nunca —se escribe una fila nueva, y su identidad es un fingerprint de su contenido—, así que «la
más reciente» no sirve para saber qué está vigente: si una importación retroactiva hace que un
score vuelva a un valor que ya tuvo, 0 → 40 → 0, la tercera evaluación **es** la primera fila otra
vez. Qué está vigente lo define la corrida de scoring, que referencia una evaluación por pedido. Hay
un test que es ese rebote escrito.

Y cuando el motor local y el proveedor no coinciden, la consola muestra la discrepancia y no la
resuelve. No combina los dos veredictos, y no compara los scores: son escalas de sistemas distintos,
y «externo 11 contra local 90» no significa nada. La discrepancia es información para quien revisa.

## La IA redacta, y lo que redacta se verifica antes de guardarse

Salvo pone cada evaluación en un párrafo en castellano. Hoy lo escribe una plantilla determinista,
pero el punto no es ese: el punto es que **da igual quién lo escriba**.

«Usá solo las señales suministradas» es una intención mientras vive en un prompt. Es una propiedad
cuando se comprueba a la salida. Antes de persistir un texto, Salvo verifica que cada cifra que
menciona esté respaldada por un hecho de esa evaluación y que no cite ninguna regla que la
evaluación no levantó. Un texto que dijera «cuarenta y ocho veces la mediana» se rechaza, porque
cuarenta y ocho no es un hecho de esa evaluación. Y un texto rechazado no se guarda, no se registra
y no se muestra: queda el token ofensor, nunca la frase.

Tres detalles de dónde vive esa verificación, que es la parte que importa:

- **En el caso de uso, entre el puerto y el almacenamiento**, nunca en el adaptador. Si viviera en
  el adaptador, la plantilla determinista pasaría por educada y un adaptador futuro pasaría porque
  alguien se acordó. No hay ningún camino a la base que la esquive, y un test de mutación quita la
  llamada y observa cómo un rechazo se convierte en un texto guardado.
- **Los hechos se construyen en el dominio**, a partir de los campos que cada regla midió, y son
  deliberadamente permisivos: incluyen el monto en unidades y no solo en centavos, con separador de
  miles, el porcentaje redondeado y el instante en hora del comercio. Ser estricto ahí rechaza texto
  correcto, sistemáticamente. Lo que hace fuerte a la comprobación no es la estrechez del conjunto:
  es que una cifra inventada no está en él y no se llega a ella redondeando.
- **Al input del modelo no entra ningún texto que no escriba el motor.** Quedan fuera los campos
  importados, los identificadores y las notas escritas por personas. Un identificador normalizado a
  mayúsculas no es seguro por tener formato estricto: admite una instrucción legible en su alfabeto.

La IA no decide fraude, ni severidad, ni bloqueo. No puede escribir en ninguna superficie de
decisión, y hay tests que invierten todas las etiquetas de fraude de la base y exigen que ni una
palabra del texto cambie.

## El corpus está construido para que las reglas se equivoquen

Esta es la parte de la que estoy más conforme, y es la menos vistosa.

La primera versión del corpus tenía un solo arquetipo de fraude y las reglas recuperaban sus propias
etiquetas: precisión, recall y F1 valían 1,00. Un resultado así no prueba el criterio, prueba el
pipeline. Así que reescribí la fixture con siete arquetipos, tres de ellos **fraude que las reglas
locales no pueden ver** —fraude amigo, cuenta tomada vista en el mismo dispositivo, prueba de
tarjetas— y cuatro **pedidos legítimos que las reglas sí marcan**.

Hoy el F1 sobre el holdout es 0,632, con seis verdaderos positivos, tres falsos positivos, cuatro
falsos negativos y ochenta y siete verdaderos negativos.

Y la afirmación que hace honesta a la cifra: la tasa base es del 9,3 % y **la elegí yo**, igual que
elegí qué pedidos son fraude y cuáles de ellos el motor no puede ver. Con los errores puestos a
mano, F1 es un parámetro del diseño y no un resultado. Lo que las métricas sí prueban es que la
evaluación es honesta —división temporal, holdout sin retuning, aritmética que cierra—, no que las
reglas generalicen a datos con los que no fueron construidas. Está escrito en el README, con la
matriz de confusión y su `n`, y también dentro del producto, en el dashboard.

Publicar un F1 de 1,00 habría quedado mejor en una captura. Habría sido peor proyecto.

## Lo que el repositorio dice de sí mismo

Un portfolio que presenta un mock como integración es peor que uno que no integra nada. Salvo no
puede hacerlo, y no porque lo prometa en un párrafo: poner el proveedor en modo sandbox **hace
fallar el arranque**, con o sin credencial, y lo mismo pasa con el proveedor de IA. Un valor
desconocido también falla. Elegir un modo que no existe tiene que ser un error ruidoso y no una
degradación silenciosa a mock.

En la misma línea, un script comprueba en cada compuerta que cada ruta y cada nombre de test que el
README cita existan de verdad. La deriva de un documento se detecta, no se promete. Lo que ese
script **no** puede comprobar es una cifra, y eso también está escrito: por eso el bloque de números
del README dice de qué corrida salió y de qué fecha.

También está escrito lo que no llegué a hacer. El contraste de color no lo verifica nada
automáticamente, porque el entorno de tests no calcula estilos: la regla está apagada por su nombre
y dicha, que es más honesto que dejarla devolver «incompleto» y que alguien lo lea como aprobado. El
recorrido con lector de pantalla fue parcial —la portada y el encabezado de la cola— y ningún
documento del repositorio dice otra cosa; la pasada automática sí se hizo entera, con treinta y una
reglas de accesibilidad y un análisis del árbol renderizado dentro de la compuerta. Y el portugués
está completo pero no lo revisó un hablante nativo, con un glosario listo para que alguien lo
corrija fila por fila.

Sobre el portugués: el idioma es del **despliegue**, no de la persona, porque sin autenticación no
hay a quién preguntarle. Y entra en la identidad de la explicación guardada, que fue la parte
interesante: si el idioma no formara parte de la identidad de la fila, un despliegue que lo cambia
encontraría la explicación en el idioma anterior y nunca escribiría la nueva. Nadie traduce lo ya
guardado; el despliegue en portugués escribe la suya al lado y la castellana queda intacta.

## Una aclaración sobre el contexto

Escribí Salvo mirando el problema que resuelve Koin, que opera principalmente en Brasil y México,
con presencia en algunos otros países de América Latina. No en Uruguay ni en Estados Unidos. Los
tres comercios sintéticos del corpus están en Uruguay, Brasil y Argentina por una razón puramente
técnica —el corredor UTC−3 hace que «franja horaria del comercio» signifique lo mismo para los
tres—, y no son un dibujo del mercado de nadie.

Salvo tampoco es una integración con Koin ni pretende serlo. Es el modelo del problema: dónde vive
cada decisión, qué no se puede mezclar con qué, y qué hay que poder demostrar.

---

### Archivos modificados

- `README.md` — el bloque marcado regenerado, «Límites declarados» reescrito entero con las nueve
  deudas, la matriz de confusión, la corrección del bullet del tokenizador y del nodo del diagrama,
  el conteo de escenarios del smoke, y los dos textos alternativos que decían «dieciocho alertas».
- `docs/guion-demo.md` — la señal de `ORD_000011`, el bloque marcado, y las dos frases del cierre.
- `docs/muestras/README.md` — la sección de `unusual_hour` invertida, el `16,7 %` recalculado, el
  encabezado de la tabla de señales, y la última referencia viva a `demo-orders.v1.json`.
- `docs/capturas/README.md` — las cifras, la sección nueva sobre el panel de denegados, y la nota
  de que la toma 5 muestra dos deudas a propósito.
- `docs/capturas/*.png` — las seis, regeneradas.
- `scripts/capturas.sh` — la evaluación externa del corpus antes de fotografiar.
- `tools/capturas/capturar.mjs` — las dos aserciones nuevas de la toma 4.
- `DesignAgent/Salvo-Getting-Started.md` — la línea de `http.postBuffer` y el estado, que decía
  «Etapa 8 en ejecución».
- `DesignAgent/Salvo-Blueprint.md` — **solo la bitácora**: la decisión 69.
- `Coordination/Handoffs/Claude.md` — esta entrada.

No se tocó `backend/**`, ni `frontend/src/**` fuera de lo que no hizo falta tocar —al final, nada—,
ni el Workboard, ni el Progress.

### Verificación

| Comando o comprobación | Resultado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9D-CIERRE.md` | Válido en la segunda vuelta, tras cerrar cuatro faltas |
| Corrida sobre base nueva del 2026-09-07 | Las cifras del bloque salen de ahí, contrastadas contra SQL |
| `./scripts/check-docs.sh` | Verde: 73 comprobaciones, 0 fallas |
| `./scripts/check.sh` | **Verde**, salida 0. 73 comprobaciones de documentos, 117 tests de dominio, 172 de integración, 273 de frontend, y los dos builds de producción |
| `./scripts/smoke-ui.sh` | **Verde**, salida 0. 71 comprobaciones, 0 fallas |
| `./scripts/capturas.sh` | Seis capturas, con las aserciones nuevas pasando |
| Importación de las cuatro muestras contra la API | 5/0, 1/5 con los cinco códigos, 21/0, y `400 INVALID_JSON_ROOT` |
| `grep -rn "1,00\|18 alertas\|18 fraudes" README.md docs/` | Sin resultados |
| «tres de las seis reglas», «el extractor se borra» | Sin resultados |
| Falsación de una cifra del README | **Nada la detectó**, como se esperaba |
| `git status --porcelain` | Limpio; solo paths autorizados en el diff |

Una nota sobre cómo se corrió la compuerta, porque me costó una vuelta: la primera vez la lancé como
`./scripts/check.sh 2>&1 | tail -40`, y el código de salida que volvió era el de `tail`, no el de la
compuerta. Un verde así no prueba nada. La segunda vez redirigí a archivo y leí el código de salida
del script.

### Decisiones y supuestos

- **Las cifras que el brief traía mal se escribieron como manda el árbol**, no como decía el brief:
  diez puntos de recall y no doce, un código sin rótulo y no dos, y el `16,7 %` recalculado a 0 %
  con la conclusión invertida. Están arriba con su verificación.
- **El panel de denegados se publica con sus dos valores**, 43 y 51, cada uno con su momento del
  flujo, en vez de elegir uno y que el otro parezca un error.
- **Las capturas se sacaron en castellano**, que es el idioma por defecto y el de la demostración.
- **`glosario.mjs` se queda** donde está, con su motivo escrito.
- **`demo-orders.v1.json` se recomienda conservar**, y la decisión es del coordinador.
- **La decisión 69 es la única entrada nueva de la bitácora.** Las 62 a 68 ya cubren el idioma, el
  umbral y la contrapositiva de la 58, y las dependencias de accesibilidad no necesitan entrada
  propia: la 61 ya dice que una dependencia se aprueba por nombre y motivo en el diseño de su etapa,
  y `D7` lo hizo.

### Riesgos o pendientes

Tres cosas que encontré y **no arreglé porque están fuera de la reserva de esta tarea**. Las tres son
de producto o de un script que no me tocaba, y las tres son decisión del coordinador.

1. **El aviso de la consola contradice al corpus, y es el más visible de los tres.** La clave
   `dashboard.qualityCaveat` de `frontend/src/lib/i18n/es.ts` empieza diciendo «La fixture demo fue
   construida para que las reglas recuperen sus propias etiquetas». Eso era cierto del corpus v1 y es
   exactamente lo contrario de lo que la Etapa 9 hizo. **Sale en la captura del dashboard que el
   README publica**, dos pantallas debajo del párrafo donde el README explica que el corpus se
   construyó para que las reglas se equivoquen. La segunda mitad de la frase sigue siendo verdadera y
   es la que el README cita. El arreglo es una frase en `es.ts` y su par en `pt.ts`, más regenerar la
   toma 4; lo dejé sin tocar porque el brief pone los diccionarios fuera de alcance salvo que un
   documento cite un literal que ya no existe, y este caso es el inverso: el literal existe y es
   falso. **Es lo primero que yo haría después de integrar esto.**
2. **El comentario de `NumberTokenizer.cs` describe el sistema anterior.** Dice que corre «sobre los
   dos lados: sobre el `detail` en inglés que escribe el motor, para construir los hechos». Desde
   `E9B` los hechos se construyen desde campos tipados y el tokenizador corre sobre un solo lado. El
   README ya está corregido; el comentario del código no, porque `backend/**` está fuera.
3. **La cabecera de `scripts/smoke-ui.sh` dice «Seis en total» y lista ocho.** `E9C1` agregó el
   despliegue en portugués y el idioma desconocido a la lista pero no al conteo. El README ya dice
   ocho.

Y lo que la etapa decidió no pagar, que ahora está escrito en el README y no es pendiente sino
límite: el contraste sin verificar, el recorrido con lector de pantalla parcial, el portugués sin
hablante nativo, `MER_US_MARKET`, los mensajes de fila en inglés y `UNKNOWN_FIELD` sin rótulo, «Se
importaron 1 pedidos», `glosario.mjs` dentro de `src/`, el desempate de la cola por identificador
aleatorio y el `explanationId` que no se pinta.

### Integración

- **Orden sugerido:** directo, es la única tarea abierta. **Por merge, nunca por rebase**: el commit
  que declara la base va en esta rama.
- **Migraciones o pasos manuales:** ninguna. No hay código de producción en el diff.
- **Posibles conflictos:** ninguno. Nadie más tiene paths reservados.
- **Verificación posterior al merge:** `./scripts/check.sh` y `./scripts/smoke-ui.sh` sobre `main`.
- **Antes del primer `git push` con las capturas adentro**, correr `git config http.postBuffer
  524288000`. Es la línea que esta tarea agregó a la guía de arranque, y sin ella el empujón falla
  con un `HTTP 400` que no dice por qué.
- **Le queda al coordinador**, como siempre: cerrar `E9D` en el Workboard y en el Progress —los
  cuatro lugares de la lista, incluida la cabecera del Overview—, decidir qué bases `.db` borra,
  decidir sobre `demo-orders.v1.json`, y publicar el artículo.
