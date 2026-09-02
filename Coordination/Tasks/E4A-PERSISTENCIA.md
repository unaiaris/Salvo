# Salvo — Task brief `E4A-PERSISTENCIA`

## Identificación

- Work ID: `E4A-PERSISTENCIA`
- Etapa: 4
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-02
- Rama/worktree: `claude/e4a-persistencia`
- Commit base: el commit de `main` que incorpora las decisiones 28–36 del Blueprint y este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`
- Dependencias: ninguna. `E4B-ALERTAS` depende de esta tarea integrada.

## Resultado esperado

El sistema persiste evaluaciones locales de riesgo y las corridas que las produjeron, de forma
idempotente y auditable, y expone la corrida y la lectura de pedidos por HTTP. Repetir una corrida
sobre el mismo corpus no crea filas nuevas y deja la evaluación vigente correcta incluso cuando el
score vuelve a un valor anterior.

## Contexto obligatorio

- `Coordination/Tasks/E4-DISENO.md`, decisiones D2, D3 y D8, y el alcance de `E4A-PERSISTENCIA`.
- `Coordination/Tasks/E4-revision-adversarial.md`, hallazgos 2, 5, 6, 7 y 8. Son el origen de las
  exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
- `DesignAgent/Salvo-Blueprint.md`: §4.2, §7 y decisiones 28–36 de la bitácora.
- `AGENTS.md`: reglas de dominio, calidad y seguridad.
- Código existente: `backend/src/Salvo.Domain/Risk/**` —en especial `TemporalRiskEngine.ScoreOne` y
  el orden en que emite señales—, `backend/src/Salvo.Domain/Orders/Order.cs`,
  `backend/src/Salvo.Application/Risk/**`, `backend/src/Salvo.Infrastructure/Persistence/**`.
- Tests existentes: `TemporalRiskEngineTests.ScoreIsCappedAndSignalsRemainInCanonicalOrder`, que
  fija el orden canónico de señales, y `backend/tests/Salvo.Api.IntegrationTests/SalvoApiFactory.cs`.

## Alcance

### Dentro

**Dominio**

- `RiskEvaluation`: `id`, `orderId`, `source`, `ruleConfigVersion` nullable, `score` nullable,
  `status`, `signalsJson` nullable, `evaluationFingerprint` nullable, `externalEvaluationId`
  nullable, `errorCode` nullable, `createdAt`. Append-only: sin mutadores públicos.
- `RiskEvaluationSource` con `LOCAL`, `EXTERNAL_MOCK`, `KOIN_SANDBOX`, y `RiskEvaluationStatus` con
  `PENDING`, `APPROVED`, `DENIED`, `ERROR`. Solo `LOCAL` se usa en esta tarea; el resto se define
  para no rehacer el schema en E6.
- Serialización canónica de señales, en Domain. Usa **el orden de reglas que emite
  `TemporalRiskEngine.ScoreOne`**, no orden alfabético. Produce una cadena estable e independiente
  de cultura.
- Cálculo del fingerprint: `SHA-256(orderId | source | ruleConfigVersion | score | signalsCanonical)`.
  La cadena exacta que se hashea es la que se persiste en `signalsJson`; nunca se re-serializa.
- `ScoringRun`: `id`, `ruleConfigVersion`, `startedAt`, `completedAt`, `orderCount`,
  `evaluationsCreated`, `evaluationsReused`.
- `RunEvaluation`: `runId`, `orderId`, `evaluationId`, único por `(runId, orderId)`.

**Aplicación**

- Caso de uso de corrida de scoring. Invoca `TemporalRiskEngine.Score` **directamente**, nunca
  `EvaluateLocalRiskHandler`, que exige etiquetas que una importación no produce.
- La corrida completa se persiste en un único `SaveChangesAsync`: si falla, no queda ni la corrida,
  ni las evaluaciones, ni las referencias.
- Antes de insertar, preconsultar los fingerprints existentes. Un `AddRange` contra el índice único
  falla entero si una sola fila ya existe.
- Cada pedido del corpus produce exactamente una fila en `RunEvaluation`, apunte a una evaluación
  nueva o a una reusada.
- Lectura paginada de pedidos.

**Infraestructura**

- Configuraciones EF Core y una migración con: índice único **parcial** sobre
  `evaluationFingerprint WHERE source = 'LOCAL'`, único de `RunEvaluation(runId, orderId)`, claves
  foráneas, y timestamps en el mismo formato UTC ISO canónico que usa el schema de E2.
- Repositorios y puertos necesarios. Ningún tipo de EF Core cruza hacia Domain o Application.

**API**

- `POST /api/risk-evaluations:run` — ejecuta una corrida y devuelve su resumen. Una violación de
  unicidad por corridas concurrentes se mapea a `409`, nunca a `500`.
- `GET /api/orders` — lectura paginada de hechos ya persistidos. **No expone `isFraudLabel` bajo
  ninguna circunstancia**, ni directa ni indirectamente.

**Tests**

- Dos corridas seguidas sobre el mismo corpus: la segunda registra `evaluationsCreated = 0` y
  `RunEvaluation` referencia las mismas evaluaciones.
- Escenario de rebote: score 0 → 40 → 0 mediante importaciones retroactivas. La tercera corrida
  reusa la fila del primer estado y la evaluación vigente del pedido es la correcta, no la
  intermedia.
- Test dorado que fija los fingerprints del corpus demo.
- Estabilidad del fingerprint bajo al menos `en-US`, `es-UY` y `de-DE`.
- Un fallo antes de completar la corrida no deja corrida, evaluaciones ni referencias parciales.
- Importar un pedido sin etiqueta y correr el scoring responde `200`, no `500`.
- `GET /api/orders` no expone `isFraudLabel` en ningún campo ni con ningún parámetro.

### Fuera

- `Alert`, `AlertReview`, `AlertPolicy` y todo lo relativo a alertas y revisión. Es `E4B-ALERTAS`.
- Cualquier UI o cambio en `frontend/`.
- `IAntifraudProvider`, evaluaciones externas y `CallbackReceipt`. Etapa 6.
- `explanation`, `recommendedAction` y `explanationStatus`. Etapa 7; las columnas no se crean.
- Modificar `Order`, `OrderEvaluationLabel`, el importador o el seed de E2.
- Modificar `TemporalRiskEngine`, `RuleConfig` o las métricas de E3. Se consumen tal como están.
- Autenticación.
- `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` y el Blueprint: estado canónico, del
  coordinador.

### Paths autorizados

- `backend/src/Salvo.Domain/Risk/**`
- `backend/src/Salvo.Domain/Evaluation/**` solo si es imprescindible; justificarlo en el handoff
- `backend/src/Salvo.Application/Risk/**`
- `backend/src/Salvo.Application/Orders/**` solo para la lectura paginada
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `Directory.Packages.props` y lockfiles, solo si una dependencia nueva resulta imprescindible y se
  consulta antes

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths autorizados.
- Instalación o actualización de dependencias: **no** sin consulta previa. El fingerprint usa
  `System.Security.Cryptography`, que es parte del framework.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna. No borrar la base local del desarrollador; si una migración exige
  recrearla, detenerse y reportarlo.
- Commits locales en la rama: autorizados.
- Crear la migración con `dotnet ef migrations add`: autorizado.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E4A-PERSISTENCIA.md` sin faltantes antes de empezar.
- [ ] `RiskEvaluation` no expone mutadores públicos.
- [ ] La serialización canónica usa el orden de reglas del motor y hay un test que lo verifica
      contra `TemporalRiskEngine`.
- [ ] La cadena hasheada y la persistida en `signalsJson` son idénticas.
- [ ] El índice único de fingerprint es parcial sobre `source = 'LOCAL'`.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] Dos corridas seguidas: la segunda con `evaluationsCreated = 0`.
- [ ] El escenario de rebote deja la evaluación vigente correcta.
- [ ] Un fallo parcial no deja estado a medias.
- [ ] Importar sin etiquetas y correr el scoring no da `500`.
- [ ] `GET /api/orders` no expone `isFraudLabel`.
- [ ] `Salvo.Domain` sigue sin referencias a ASP.NET Core, EF Core ni SDKs externos.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E4A-PERSISTENCIA`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E4A-PERSISTENCIA.md` | Brief válido |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes |
| `dotnet test` | Todos los tests previos siguen pasando, más los nuevos |
| Corrida doble en test de integración | `evaluationsCreated = 0` en la segunda |
| Test de rebote 0 → 40 → 0 | Evaluación vigente correcta |
| Test dorado de fingerprints | Valores fijos del corpus demo |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `git diff --check` | Pasa |
| `/handoff E4A-PERSISTENCIA` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Forma concreta de la serialización canónica, siempre que sea estable, independiente de cultura y
  respete el orden de reglas del motor.
- Estructura interna de puertos y repositorios, respetando las fronteras de `AGENTS.md`.
- Nombres de tablas y columnas, siguiendo la convención `snake_case` de la migración de E2.
- Forma del DTO de resumen de la corrida y de la paginación de `GET /api/orders`.
- Cómo expresar el índice único parcial en EF Core.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- hace falta una dependencia nueva;
- el índice único parcial no puede expresarse en EF Core sobre SQLite sin SQL crudo;
- una migración exigiría borrar o recrear la base local del desarrollador;
- aparece una contradicción entre el diseño y el código real que no se resuelva con una corrección
  menor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
