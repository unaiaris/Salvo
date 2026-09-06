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
- Commit base: `95d5db3` (`docs: add the Etapa 8 task briefs`), el que declara el brief. El commit
  `5ded6e1`, que corrigió el brief y terminó la sincronización canónica que el brief daba por hecha,
  es del coordinador y ya estaba en la rama al empezar.
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
| `./scripts/check.sh` | Verde, con `check-docs.sh` ya adentro como primer paso |
| Falsación 1: ruta inexistente en el README | Falla, `exit 1`, nombrando la ruta y su línea |
| Falsación 2: test inexistente en el README | Falla, `exit 1`, nombrando el identificador buscado |
| Falsación 3: enlace Markdown roto | Falla, `exit 1`, nombrando el destino |
| Cifras del corpus regeneradas sobre base nueva | Migración, seed, corrida, evaluación externa y callbacks sobre una base temporal; `salvo.db` intacta |
| `grep` de cifras fuera del bloque marcado | Ninguna dependiente del corpus fuera de `corpus:inicio`/`corpus:fin` |
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
| El recorrido de un pedido | Sintaxis endurecida: rótulos de arista en la forma `-->|texto|`. **Falta confirmar el renderizado en la vista previa de GitHub** |
| Las tres fuentes de verdad | `erDiagram` con rótulos de relación entrecomillados y atributos en la forma `tipo nombre "comentario"`. **Falta confirmar el renderizado** |
| La máquina de estados externa | Reescrito: los rótulos de transición iban entrecomillados y con `<br/>`, y las comillas se dibujan. Ahora son texto plano con una nota. **Falta confirmar el renderizado** |
| Cómo se verifica una explicación | `flowchart LR` con dos subgrafos con título entrecomillado. **Falta confirmar el renderizado** |

Ninguno lleva cifras del corpus, como pide D2.

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

- **Los cuatro diagramas no se vieron renderizados.** Es el único criterio de aceptación que no pude
  ejecutar: no hay Mermaid en la máquina, instalarlo sería una dependencia nueva que el brief
  prohíbe, y la vista previa de GitHub necesita ojos. Un error de sintaxis se renderiza como bloque
  de código sin aviso, así que **hay que mirarlos antes de integrar**. La mitigación es la de arriba:
  solo se usaron construcciones conservadoras, y las dos que más riesgo tenían se reescribieron.
- **El §9 del Blueprint sigue sin nombrar `SALVO_CALLBACK_SHARED_SECRET`** y sigue listando
  `KOIN_CALLBACK_SHARED_SECRET`, que no lee nada. La revisión adversarial lo marcó como «falso por
  omisión» y el Blueprint está fuera del alcance de esta tarea. `Salvo-Portability.md` ahora
  distingue las dos en una tabla, así que la contradicción entre los dos documentos es visible;
  resolverla es del coordinador.
- **La cabecera de `scripts/smoke-ui.sh` dice «las cuatro rutas»** cuando pide cinco, y dice «los
  tres escenarios» cuando corre seis. Es el mismo error que el README tenía y del que probablemente
  lo copió. El archivo no está en los paths autorizados; queda anotado.
- **`Coordination/Workboard.md` reserva para `E8A` una lista de paths más corta que la del brief**:
  no menciona `scripts/check.sh` ni `Coordination/Handoffs/Claude.md`. No hubo conflicto con nadie
  porque `E8B` no toca ninguno de los dos, pero conviene alinearlos antes de despachar `E8B`.
- **La Etapa 9 rompe tests, no solo textos.** Está en el inventario: los cuatro números del mock y
  los dos textos dorados son aserciones sobre este corpus.

### Integración

- Orden sugerido: esta rama sola. No depende de nada y `E8B` depende de ella integrada.
- Migraciones o pasos manuales: ninguno. No hay migración, ni cambio de contrato, ni recaptura de
  OpenAPI, ni dependencia nueva.
- Posibles conflictos: `scripts/check.sh` gana una línea al principio; cualquier otra tarea que lo
  toque va a conflictuar ahí. `Coordination/Handoffs/Claude.md` crece al final, como siempre.
- Verificación posterior al merge: `./scripts/check.sh` —que ahora incluye `check-docs.sh`— y
  `./scripts/smoke-ui.sh` sobre el estado integrado. Y **mirar los cuatro diagramas en GitHub**,
  que es lo único de esta entrega que ningún script cubre.
