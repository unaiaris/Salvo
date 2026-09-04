# Salvo — Task brief `E6A-PROVEEDOR`

## Identificación

- Work ID: `E6A-PROVEEDOR`
- Etapa: 6
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-04
- Rama/worktree: `claude/e6a-proveedor`
- Commit base: el commit de `main` que incorpora las decisiones 44–50 del Blueprint y este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. El razonamiento de diseño está congelado en el
  diseño v2 y en este brief; queda criterio de ingeniería. Si el test de concurrencia no puede
  volverse determinista, o la migración no conserva los fingerprints, detenerse y consultar tras dos
  intentos en vez de insistir.
- Dependencias: ninguna. `E6B-CALLBACK-UI` depende de esta tarea integrada.

## Resultado esperado

Salvo puede pedirle a un proveedor antifraude externo que evalúe un pedido, guarda esa evaluación
como una entidad separada de la local, sobrevive a que el proveedor falle o tarde sin perder nada, y
puede cerrar las evaluaciones pendientes mediante una reconciliación explícita. La evaluación local,
las alertas, el dashboard y las métricas **no cambian en absoluto**.

## Contexto obligatorio

- `Coordination/Tasks/E6-DISENO.md` (v2), decisiones **D1 a D8, D11, D13 y D14**.
- `Coordination/Tasks/E6-revision-adversarial.md`, hallazgos **1, 2, 6, 7, 9, 10, 11, 12, 14 y 15**.
  Son el origen de las exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
- `DesignAgent/Salvo-Blueprint.md`: §4.5, §5, §7 y decisiones 44–50 de la bitácora.
- `DesignAgent/Salvo-Progress.md`, entrada «Etapa 6 — Proveedor antifraude mock».
- `AGENTS.md`, en particular las reglas nuevas sobre evaluación externa y sobre `run_evaluations`.
- Código existente: `backend/src/Salvo.Domain/Risk/**` —para entender qué **no** tocar—,
  `Salvo.Domain/Orders/Order.cs` (la referencia compuesta),
  `Salvo.Infrastructure/Persistence/**` con sus tres migraciones,
  `Salvo.Application/Risk/RunScoringHandler.cs`, `Salvo.Api/**`.
- Patrones a reutilizar: el token de concurrencia y el mapeo de violación de unicidad de
  `EfAlertStore`; el registro condicional por bandera de `OrderEndpoints` y
  `EvaluationMetricsEndpoints`; el proveedor que inyecta fallos de
  `ScoringRunPersistenceTests.ConflictingScoringRunStore`; `SalvoApiFactory.WithFileDatabase`.

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**

## Alcance

### Dentro

**Dominio — `backend/src/Salvo.Domain/External/`**

- `ExternalProvider` (`EXTERNAL_MOCK`, `KOIN_SANDBOX`), `ExternalEvaluationStatus`
  (`PENDING`, `APPROVED`, `DENIED`, `ERROR`) y sus nombres de cable. **Tipos propios: no se
  reutilizan los de `Salvo.Domain/Risk/`.** Compartir la enumeración reintroduce a nivel de tipo la
  mezcla que la separación combate a nivel de tabla.
- `ExternalEvaluation` con los campos de §7 del Blueprint: `referenceId`, `externalEvaluationId`
  nullable, `status`, `score` nullable, `errorCode`, `lastErrorCode`, `attemptCount`, `settledBy`,
  `requestedAt`, `updatedAt`, `settledAt`.
  - Mutable a propósito, con transiciones validadas en el dominio según la tabla de §4.5.
  - `errorCode` de catálogo cerrado: `UNREACHABLE`, `PROVIDER_REJECTED`, `TIMEOUT`,
    `PROVIDER_ERROR`, `INVALID_RESPONSE`.

**Migración — acotada, y en este orden**

1. Eliminar `external_evaluation_id` y `error_code` de `risk_evaluations`.
2. En la **misma** reconstrucción, endurecer `ck_risk_evaluations_source` a `source = 'LOCAL'` y
   `ck_risk_evaluations_status` a `('APPROVED', 'DENIED')`.
3. Quitar los miembros externos de `RiskEvaluationSource` y `RiskEvaluationStatus`. El fingerprint
   solo hashea `"LOCAL"`, así que el test dorado no se entera.
4. Crear `external_evaluations` con: único parcial `(order_id, provider) WHERE status = 'PENDING'`;
   único parcial `(provider, external_evaluation_id) WHERE external_evaluation_id IS NOT NULL`;
   índice `(provider, reference_id)`; y restricciones de consistencia — terminal ⇔ `settled_at`
   no nulo, `error_code` no nulo ⇒ `status = 'ERROR'`, `settled_by` no nulo ⇔ `settled_at` no nulo.

**El índice parcial de fingerprint y la nulabilidad de `RiskEvaluation` quedan como están.** Quitar
el filtro rompe un test que lo afirma sobre `sqlite_master`, y hacer la completitud incondicional
propaga cambios a seis archivos de aplicación.

**Aplicación**

- `IAntifraudProvider` con `EvaluateAsync` y
  `GetStatusAsync(ExternalEvaluationLookup, CancellationToken)`, donde el lookup lleva
  `ExternalEvaluationId` nullable y `ReferenceId`.
- **Solicitud en dos fases**, que es el corazón de la tarea:
  1. Insertar la fila `PENDING` con `referenceId` y `SaveChangesAsync` **antes** de llamar al
     proveedor. El único parcial serializa las solicitudes concurrentes cuando todavía no hay nada
     del lado del proveedor.
  2. `EvaluateAsync` con timeout explícito, y actualizar la fila con `status` como token de
     concurrencia.
  - La vinculación tardía de recibos `UNMATCHED` es de `E6B`; acá se deja el punto de extensión.
- **Taxonomía de fallo**, según D4:

  | Fallo | Momento | Resultado |
  | --- | --- | --- |
  | Conexión rechazada, DNS | Antes de enviar | `ERROR` terminal, `UNREACHABLE` |
  | Rechazo definitivo (`4xx` de validación) | Respuesta recibida | `ERROR` terminal, `PROVIDER_REJECTED` |
  | `TIMEOUT`, `5xx`, respuesta ilegible | Después de enviar | **Sigue `PENDING`**, `lastErrorCode`, sin `settledAt` |

- **Reconciliación**: recorre las `PENDING` con más del umbral de antigüedad, llama al proveedor y
  aplica transiciones. **Umbral configurable con 0 por defecto**, y **unidad de trabajo por fila**:
  un `SaveChangesAsync` por evaluación, para que un conflicto no revierta el barrido.
- La evaluación local, las alertas y la corrida de scoring **no se tocan**.

**Infraestructura**

- `MockAntifraudProvider`: determinista, sin red. Deriva el resultado del **sufijo numérico de
  `merchantReferenceId` módulo 100**, con bandas: `APPROVED` 0–74, `DENIED` 75–89, `PENDING` 90–96,
  `ERROR` 97–99. `GetStatusAsync` de una `PENDING` devuelve el estado terminal de la **misma**
  función. Latencia simulada configurable, cero por defecto.
- `KOIN_MODE` gobierna el registro, `mock` por defecto. **`KOIN_MODE=sandbox` falla al arrancar** con
  un mensaje explícito que cita §5.3. `KoinSandboxProvider` no se implementa.
- Ningún tipo del cliente HTTP cruza fuera de Infrastructure.

**API**

| Ruta | Semántica |
| --- | --- |
| `POST /api/orders/{orderId}/external-evaluations` | Idempotente. Con `PENDING` vigente: `200` con esa fila y `applied: false`. Con terminal vigente: `200` con esa fila, salvo pedido explícito de una nueva, permitido **solo** si la última es `ERROR` |
| `GET /api/orders/{orderId}/external-evaluations` | Historial del pedido |
| `GET /api/external-evaluations/{id}` | Una evaluación |
| `POST /api/external-evaluations:reconcile` | Barrido, con resumen |

Códigos: `EXTERNAL_EVALUATION_PENDING` 409, `EXTERNAL_EVALUATION_NOT_FOUND` 404,
`PROVIDER_NOT_REGISTERED` 404, `RECONCILIATION_CONFLICT` 409.

**Un fallo del proveedor no es un `5xx` para el cliente**: la fila queda en `ERROR` o `PENDING` y la
respuesta es `200` con la fila.

**Contrato**

- Recapturar `frontend/openapi/salvo-openapi.json` con `DemoData__Enabled=true` y regenerar
  `frontend/src/lib/api/schema.d.ts`. Sin esto, `OpenApiDriftTests` y `api:types:check` fallan.
- **Ningún otro archivo de `frontend/`.** Si algo más pareciera necesario, detenerse y consultar.

**Tests**

- **Solicitud concurrente**: dos solicitudes simultáneas sobre el mismo pedido, sobre **base en
  archivo**. Una gana, la otra recibe `409`, y **el proveedor se invoca una sola vez** — verificable
  con un proveedor que cuente invocaciones. Es lo que demuestra que la reserva precede a la llamada.
- **Timeout y luego reconciliación**: un proveedor que lanza `TimeoutException` tras «aceptar» y
  después responde `DENIED` a `GetStatusAsync`. Resultado: **una sola fila**, en `DENIED`, con
  `settledBy = RECONCILIATION`.
- Conexión rechazada antes de enviar: `ERROR` terminal con `UNREACHABLE`.
- `ERROR` y solicitud nueva: segunda fila permitida. Con `PENDING` vigente: `409`.
- Reconciliación con conflicto en una fila: las demás se procesan igual.
- Conteos del mock sobre el corpus demo: **225 aprobados, 45 denegados, 21 pendientes, 9 con error**.
  Clavados como el test dorado de fingerprints.
- **Diferencial**: `GET /api/dashboard`, `GET /api/alerts?sort=SCORE_DESC` y
  `GET /api/evaluation-metrics` **byte a byte idénticos** antes y después de crear evaluaciones
  externas en los cuatro estados. Molde: `DashboardEndpointTests`.
- Proveedor que lanza: `POST /api/risk-evaluations:run` responde igual y el test dorado no cambia.
- `ExternalEvaluation` **fuera** de `AppendOnlyEntitiesExposeNoPublicMutator` y **dentro** de
  `PersistedRiskEntitiesCarryNoGroundTruthLabel`.
- `KOIN_MODE=sandbox` impide el arranque.
- Migración: los 328 fingerprints existentes siguen idénticos.

### Fuera

- `CallbackReceipt`, el endpoint de callback, la vinculación tardía de `UNMATCHED` y el disparador de
  demo. Son `E6B-CALLBACK-UI`.
- **Todo `frontend/`** salvo el documento OpenAPI recapturado y `schema.d.ts` regenerado.
- `KoinSandboxProvider` y cualquier llamada real.
- Alertas originadas por evaluación externa.
- Combinar el criterio local y el externo.
- Reintentos automáticos, backoff o trabajos en segundo plano.
- Modificar el motor, `RuleConfig`, el fingerprint, la corrida de scoring, la semántica de alertas o
  el dashboard.
- Quitar el filtro del índice de fingerprint o volver incondicional la completitud.

### Paths autorizados

- `backend/src/Salvo.Domain/External/**`
- `backend/src/Salvo.Domain/Risk/RiskEvaluationSource.cs` y `RiskEvaluationStatus.cs`, solo para
  quitar los miembros externos
- `backend/src/Salvo.Application/External/**`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, solo recaptura y
  regeneración

### Paths reservados por otros trabajos

- Ninguno. Esta tarea reserva los anteriores.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Crear la migración con `dotnet ef migrations add`: autorizado.
- **Antes de aplicar la migración a `backend/src/Salvo.Api/salvo.db`, hacer una copia del archivo.**
  EF ejecuta `PRAGMA foreign_keys = 0` fuera de transacción y lo advierte.
- Levantar la API para recapturar el OpenAPI: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna. No borrar bases.
- Commits locales en la rama: autorizados, y **se pide commitear por partes**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E6A-PROVEEDOR.md` sin faltantes antes de empezar.
- [ ] `ExternalEvaluation` no comparte enumeración ni tipo con `Salvo.Domain/Risk/`.
- [ ] La fila se persiste **antes** de llamar al proveedor, y el test de concurrencia demuestra que
      **el proveedor se invoca una sola vez**. Documentar en el handoff que ese test **falla** si se
      invierte el orden.
- [ ] Un timeout deja la fila en `PENDING`, y la reconciliación la cierra en una sola fila.
- [ ] Los conteos del mock sobre el corpus demo son 225 / 45 / 21 / 9.
- [ ] El test diferencial demuestra que crear evaluaciones externas **no cambia** dashboard, feed ni
      métricas. Documentar que falla si se conectan.
- [ ] Los 328 fingerprints existentes siguen idénticos tras la migración.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] `KOIN_MODE=sandbox` impide el arranque.
- [ ] Sin dependencias nuevas y sin cambios en `frontend/` fuera de los dos archivos autorizados.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E6A-PROVEEDOR`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E6A-PROVEEDOR.md` | Brief válido |
| Test de solicitud concurrente sobre base en archivo | Un `200`, un `409`, **una** invocación al proveedor; falla al invertir el orden |
| Test de timeout + reconciliación | Una sola fila, en `DENIED`, `settledBy = RECONCILIATION` |
| Test diferencial | Dashboard, feed y métricas idénticos; falla si se conectan |
| Test de conteos del mock | 225 / 45 / 21 / 9 |
| Test de fingerprints tras la migración | Los 328 idénticos |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `npm run api:types:check --prefix frontend` | Al día |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E6A-PROVEEDOR` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Estructura de puertos, repositorios y DTO, respetando las fronteras de `AGENTS.md`.
- Nombres de tablas y columnas, en `snake_case`.
- Cómo se expresa el doble índice único parcial en EF Core sobre SQLite.
- Forma concreta del resumen de reconciliación.
- Cómo se simula el timeout y la conexión rechazada en los tests.
- Cómo se detecta el sufijo numérico de la referencia en el mock, y qué hace con una referencia sin
  sufijo numérico.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- la migración no puede quitar las columnas conservando los fingerprints;
- el test de concurrencia no puede volverse determinista;
- hace falta tocar cualquier archivo de `frontend/` fuera de los dos autorizados;
- hace falta una dependencia nueva;
- aparece una contradicción entre el diseño v2 y el código real que no se resuelva con una
  corrección menor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos y resultados exactos, incluidas las dos comprobaciones de que los tests fallan cuando
  deben: el de concurrencia con el orden invertido, y el diferencial con las fuentes conectadas.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
