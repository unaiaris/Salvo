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
