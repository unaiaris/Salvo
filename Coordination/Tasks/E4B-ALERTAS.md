# Salvo — Task brief `E4B-ALERTAS`

## Identificación

- Work ID: `E4B-ALERTAS`
- Etapa: 4
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-02
- Rama/worktree: `claude/e4b-alertas`
- Commit base: el commit de `main` que incorpora este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. Excepción declarada: si el test de carrera no se
  vuelve determinista tras dos intentos, detenerse y consultar antes de insistir; esa pieza puntual
  se reevalúa a `xhigh` con el coordinador.
- Dependencias: `E4A-PERSISTENCIA`, integrada en `main` mediante `1d9ee83` y cerrada en `fdfbde8`.

## Resultado esperado

Una evaluación local marcada produce una alerta operable: la analista puede listarla, abrirla, ver
las señales que la originaron junto con la evaluación vigente y su divergencia, y emitir un veredicto
terminal que queda auditado. Dos revisiones simultáneas de la misma alerta terminan en conflicto
explícito, nunca en sobrescritura silenciosa.

## Contexto obligatorio

- `Coordination/Tasks/E4-DISENO.md`, decisiones **D4, D5, D6 y D7**, y el alcance de `E4B-ALERTAS`.
- `Coordination/Tasks/E4-revision-adversarial.md`, hallazgos **1, 3, 4, 9 y 10**. Son el origen de
  las exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
- `DesignAgent/Salvo-Blueprint.md`: §4.3 y decisiones 28–36 de la bitácora.
- `AGENTS.md`: reglas de dominio, calidad y seguridad.
- Código existente: `backend/src/Salvo.Domain/Risk/**` —en particular `RiskEvaluation`,
  `ScoringRun`, `RunEvaluation` y `RuleConfig.E3V1.FlagThreshold`—,
  `backend/src/Salvo.Application/Risk/RunScoringHandler.cs`,
  `backend/src/Salvo.Infrastructure/Persistence/EfScoringRunStore.cs`,
  `backend/src/Salvo.Api/RiskEvaluationEndpoints.cs`.
- Tests existentes: `backend/tests/Salvo.Api.IntegrationTests/SalvoApiFactory.cs`, que hoy comparte
  una única conexión `:memory:` entre todos los scopes.

## Alcance

### Dentro

**Corrección de arrastre**

- `SalvoDbContextFactory` toma hoy `"Data Source=salvo.design.db"` fija, mientras el arranque real
  usa `ConnectionStrings:SalvoDb` con `salvo.db` por defecto. Eso hizo que `dotnet ef database
  update` migrara una base distinta a la que sirve la API, con el síntoma `no such table: orders`.
  La fábrica de diseño debe leer la cadena de configuración —`ConnectionStrings__SalvoDb` o
  `appsettings`— y dejar `salvo.design.db` solo como último recurso cuando no haya ninguna definida.
  Documentarlo en el handoff.

**Dominio**

- `AlertStatus`: `OPEN`, `CONFIRMED_SAFE`, `REPORTED_FRAUD`. Los dos últimos son terminales.
- `AlertSeverity`: `MEDIUM`, `HIGH`, `CRITICAL`.
- `AlertPolicy`, inmutable, versión `e4-v1`, con las bandas 60–69 `MEDIUM`, 70–89 `HIGH`,
  90–100 `CRITICAL`. Mismo patrón que `RuleConfig`.
  - `Validate()` exige que el piso de la banda más baja sea igual a `RuleConfig.FlagThreshold`, y
    que las bandas cubran sin huecos ni solapes hasta 100. Se invoca al arrancar: si mañana el
    umbral baja a 50, la aplicación falla en el arranque en vez de dejar un score 55 sin banda.
  - La severidad **no se persiste**: es función pura de `riskScoreSnapshot`.
- `Alert`: `id`, `orderId`, `riskEvaluationId`, `riskScoreSnapshot`, `signalsSnapshotJson`,
  `alertPolicyVersion`, `status`, `supersedesAlertId` nullable, `createdAt`, `reviewedAt` nullable.
  - El snapshot documenta por qué se abrió la alerta y **nunca se sobrescribe**.
  - `Severity` es propiedad calculada, no columna.
  - Único mutador: la transición de revisión, que valida que el estado actual sea `OPEN`.
- `AlertReview`: `id`, `alertId`, `previousStatus`, `newStatus`, `note`, `reviewedAt`.
  - Sin autenticación no puede atribuirse a una persona. Queda declarado en el handoff, no se
    inventa un campo de identidad.
- `ScoringRun` gana `alertsCreated`, `alertsSkippedOpen` y `alertsSkippedReviewed`. Es un cambio de
  schema sobre una tabla ya migrada: requiere una migración nueva, no editar la de E4A.

**Aplicación**

- Creación de alertas **dentro de la corrida de scoring**, extendiendo `RunScoringHandler` y
  persistida en el **mismo y único `SaveChangesAsync`** que ya usa la corrida. Si algo falla, no
  queda ni corrida, ni evaluaciones, ni alertas.
- Predicado de creación, evaluado sobre **estado persistido**, nunca sobre el delta de la corrida.
  Se crea alerta cuando:
  1. la evaluación vigente del pedido está marcada; **y**
  2. el pedido no tiene ninguna alerta `OPEN`; **y**
  3. el pedido no tiene alertas revisadas, **o** la banda de severidad de la evaluación vigente
     **supera** la banda de la última alerta del pedido. En ese caso la alerta nueva enlaza a la
     anterior mediante `supersedesAlertId`.
  - Subir de puntos sin cambiar de banda **no** crea alerta.
- Caso de uso de revisión. Un único `SaveChangesAsync` que actualiza la alerta y escribe su
  `AlertReview`. Reglas de idempotencia y conflicto según D7:

  | Situación | Respuesta |
  | --- | --- |
  | Alerta `OPEN`, transición válida | `200`, alerta revisada |
  | Mismo estado terminal, misma nota | `200`, sin cambios |
  | Mismo estado terminal, nota distinta | `409` |
  | Estado terminal distinto al pedido | `409` |
  | Bandas divergentes sin `acknowledgedDivergence` | `409` |

- Lectura de alertas: listado paginado y detalle. El detalle devuelve el snapshot **y** la
  evaluación vigente del pedido —la referenciada por la última `ScoringRun`— con un indicador de
  divergencia de banda.

**Infraestructura**

- Configuraciones EF Core y una migración con:
  - índice único **parcial** sobre `alerts(order_id) WHERE status = 'OPEN'`;
  - único total sobre `alerts(risk_evaluation_id)`;
  - único sobre `alert_reviews(alert_id)`;
  - FK de `supersedes_alert_id` a `alerts(id)`, nullable;
  - `IsConcurrencyToken()` sobre `alerts.status`;
  - las tres columnas nuevas de `scoring_runs`;
  - timestamps en el mismo formato UTC ISO canónico que E2 y E4A.
- `EfScoringRunStore` incorpora las alertas al `SaveChangesAsync` existente y mapea la violación del
  único parcial a `ScoringRunConflictException`, igual que ya hace con los códigos SQLite 2067/1555.
- `DbUpdateConcurrencyException` en la revisión se propaga como excepción de dominio/aplicación
  propia; ningún tipo de EF Core cruza hacia Domain o Application.

**API**

- `GET /api/alerts` — listado paginado. Filtro por `status` y por `severity`. Sin `isFraudLabel`.
- `GET /api/alerts/{id}` — detalle con snapshot, evaluación vigente y divergencia.
- `POST /api/alerts/{id}/review` — cuerpo con `newStatus`, `note` y `acknowledgedDivergence`.
  `409` en todos los casos de conflicto de la tabla anterior; nunca `500`.
- `404` para un `id` inexistente.

**Tests**

- **La fábrica de tests debe poder dar una base en archivo.** `SalvoApiFactory` comparte hoy una
  `SqliteConnection(":memory:")`; un test de carrera sobre esa fábrica pasaría en secuencial y el
  bug llegaría igual. Agregar la opción sin romper los tests existentes, que siguen usando memoria.
- Carrera de revisión sobre base en archivo: dos requests concurrentes sobre la misma alerta dan un
  `200` y un `409`, y queda exactamente **un** `AlertReview`.
- Escalada de banda tras backfill: crea alerta nueva con `supersedesAlertId` apuntando a la anterior.
- Aumento de score dentro de la misma banda: **no** crea alerta.
- Caída de score tras backfill: la alerta abierta conserva su snapshot y el detalle expone la
  divergencia.
- Revisión con bandas divergentes sin `acknowledgedDivergence`: `409`.
- Revisión repetida con mismo estado y misma nota: `200` idempotente; con nota distinta: `409`.
- `AlertPolicy.Validate()` falla si el piso de la banda más baja no coincide con `FlagThreshold`.
- Sobre el corpus demo con `e3-v1`: 18 alertas, **13 `MEDIUM` y 5 `CRITICAL`**, 0 `HIGH`.
- Segunda corrida sobre corpus sin cambios: `alertsCreated = 0`.
- `GET /api/alerts` no expone `isFraudLabel` en ningún campo ni con ningún parámetro.

### Fuera

- Cualquier UI o cambio en `frontend/`. Es la Etapa 5, incluido el orden del feed y `amountAtRisk`.
- `IAntifraudProvider`, evaluaciones externas y `CallbackReceipt`. Etapa 6.
- `explanation`, `recommendedAction` y `explanationStatus` de `Alert`. Etapa 7; las columnas **no**
  se crean todavía.
- Autenticación e identidad de revisor. Post-MVP.
- Reabrir una alerta revisada. El veredicto es terminal por diseño.
- Modificar `Order`, `OrderEvaluationLabel`, el importador o el seed de E2.
- Modificar `TemporalRiskEngine`, `RuleConfig` o las métricas de E3. Se consumen tal como están.
- Modificar la migración de E4A o el cálculo del fingerprint.
- `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` y el Blueprint: estado canónico, del
  coordinador.

### Paths autorizados

- `backend/src/Salvo.Domain/Alerts/**`
- `backend/src/Salvo.Domain/Risk/ScoringRun.cs` solo para las tres contadoras nuevas
- `backend/src/Salvo.Application/Alerts/**`
- `backend/src/Salvo.Application/Risk/**`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `Directory.Packages.props` y lockfiles, solo si una dependencia nueva resulta imprescindible y se
  consulta antes

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths autorizados.
- Instalación o actualización de dependencias: **no** sin consulta previa.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna. No borrar `salvo.db` ni `salvo.design.db`; si una migración exige
  recrear la base local, detenerse y reportarlo.
- Commits locales en la rama: autorizados.
- Crear la migración con `dotnet ef migrations add`: autorizado.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E4B-ALERTAS.md` sin faltantes antes de empezar.
- [ ] `SalvoDbContextFactory` lee la cadena de conexión de configuración.
- [ ] `AlertPolicy.Validate()` ata el piso de la banda más baja a `RuleConfig.FlagThreshold` y se
      invoca al arrancar.
- [ ] `severity` no existe como columna; `alertPolicyVersion` sí.
- [ ] El índice único de `orderId` es parcial sobre `status = 'OPEN'`.
- [ ] `alerts.status` es token de concurrencia y el conflicto se mapea a `409`.
- [ ] La carrera de revisión sobre base en archivo deja exactamente un `AlertReview`.
- [ ] Las alertas se crean dentro del mismo `SaveChangesAsync` de la corrida.
- [ ] El predicado de creación se evalúa sobre estado persistido.
- [ ] La escalada crea alerta nueva enlazada; el aumento dentro de banda no crea nada.
- [ ] La divergencia se expone en el detalle y bloquea la revisión sin reconocimiento.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] Sobre el corpus demo: 18 alertas, 13 `MEDIUM`, 5 `CRITICAL`.
- [ ] `GET /api/alerts` no expone `isFraudLabel`.
- [ ] `Salvo.Domain` sigue sin referencias a ASP.NET Core, EF Core ni SDKs externos.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E4B-ALERTAS`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E4B-ALERTAS.md` | Brief válido |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes |
| `dotnet test` | Todos los tests previos siguen pasando, más los nuevos |
| Test de carrera sobre base en archivo | Un `200`, un `409`, un solo `AlertReview` |
| Test de escalada de banda | Alerta nueva con `supersedesAlertId` |
| Test de distribución del corpus demo | 18 alertas: 13 `MEDIUM`, 5 `CRITICAL` |
| Segunda corrida sin cambios | `alertsCreated = 0` |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `git diff --check` | Pasa |
| `/handoff E4B-ALERTAS` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Forma concreta de los DTO de listado, detalle y revisión, y de la paginación.
- Cómo expresar el índice único parcial y el token de concurrencia en EF Core sobre SQLite.
- Estructura interna de puertos y repositorios, respetando las fronteras de `AGENTS.md`.
- Nombres de tablas y columnas, siguiendo la convención `snake_case` de E2 y E4A.
- Mecanismo concreto para que `SalvoApiFactory` ofrezca base en archivo sin romper los tests
  existentes.
- Cómo se representa la divergencia en el DTO de detalle.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- hace falta una dependencia nueva;
- el índice único parcial o el token de concurrencia no pueden expresarse en EF Core sobre SQLite
  sin SQL crudo;
- la carrera de revisión no puede reproducirse de forma determinista;
- una migración exigiría borrar o recrear la base local del desarrollador;
- aparece una contradicción entre el diseño y el código real que no se resuelva con una corrección
  menor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos.
- Supuestos, decisiones, riesgos y pendientes, incluida la falta de identidad de revisor.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
