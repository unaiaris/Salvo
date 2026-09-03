# Salvo — Task brief `E5A-API-LECTURA`

## Identificación

- Work ID: `E5A-API-LECTURA`
- Etapa: 5
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-03
- Rama/worktree: `claude/e5a-api-lectura`
- Commit base: el commit de `main` que incorpora las decisiones 37–43 del Blueprint y este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. El razonamiento de diseño ya está congelado en el
  diseño v2 y en este brief; lo que queda es ingeniería con criterio. Si el test diferencial no
  puede volverse determinista tras dos intentos, detenerse y consultar antes de insistir.
- Dependencias: ninguna. `E5B-ALERTAS-UI` depende de esta tarea integrada.

## Resultado esperado

La API expone todo lo que la consola necesita leer: un dashboard operativo que **no puede** alcanzar
la etiqueta de fraude, las métricas de calidad como superficie separada tras bandera, las
capacidades del backend, y un feed de alertas ordenable por score vigente con la procedencia de la
corrida en la respuesta. Ninguna de las tres rutas nuevas responde `500` en los flujos que la propia
UI va a invitar.

## Contexto obligatorio

- `Coordination/Tasks/E5-DISENO.md` (v2), decisiones **D1, D2, D4, D5, D6, D9, D10, D12 y D17**.
- `Coordination/Tasks/E5-revision-adversarial.md`, hallazgos **1, 4, 5, 8, 10, 11, 18 y 19**. Son el
  origen de las exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
- `DesignAgent/Salvo-Blueprint.md`: §3, §4.3, §4.4 y decisiones 37–43 de la bitácora.
- `AGENTS.md`: reglas de dominio, calidad y seguridad.
- Código existente: `backend/src/Salvo.Application/Risk/EvaluateLocalRiskHandler.cs` y
  `backend/src/Salvo.Domain/Evaluation/**` —para entender qué **no** reutilizar—,
  `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs` (en especial
  `GetPageAsync` y `GetCurrentEvaluationsAsync`),
  `backend/src/Salvo.Infrastructure/Persistence/EfScoringRunStore.cs`,
  `backend/src/Salvo.Application/Alerts/**`, `backend/src/Salvo.Api/**`.
- Tests existentes: `AlertEndpointTests.TheAlertContractNeverExposesGroundTruth`,
  `backend/tests/Salvo.Api.IntegrationTests/SalvoApiFactory.cs`.

## Alcance

### Dentro

**`GET /api/dashboard`**

Devuelve, todo sobre la **corrida vigente** y siempre vía `run_evaluations`:

- `scoringRun { sequence, completedAt, orderCount }`, o `null` si no hubo ninguna corrida.
- `ordersPendingScoring`: pedidos sin fila en `run_evaluations` de la última corrida.
- `openAlerts`: conteo total y por severidad derivada.
- `amountAtRisk`: **lista** de `{ currencyCode, amountCents, alertCount }`, una entrada por moneda
  presente entre las alertas abiertas. **Nunca un total.**
- `reportedFraud`: misma forma, agregado por **`DISTINCT order_id`**, no por alerta.
- `flagRate`: evaluaciones `DENIED` sobre total de la corrida vigente.
- `riskOverTime`: cubetas **semanales** por `occurredAt`, en `America/Montevideo`
  (`RuleConfig.BusinessTimeZone`), con conteo y conteo marcado por cubeta.
- `topSignals`: conteo de reglas sobre los **snapshots de las alertas abiertas**, no sobre todas las
  evaluaciones vigentes.

**No lee `OrderEvaluationLabel` por ningún camino**: ni directo, ni por otro manejador, ni por un
`JOIN` dentro de la implementación EF.

**`GET /api/evaluation-metrics`**

- Registrado **solo si `DemoData:Enabled`**, con el mismo patrón que `POST /api/demo-data/seed`.
- Manejador **propio**, que parte de las evaluaciones vigentes de la corrida y lee etiquetas solo de
  los pedidos que esa corrida cubre. **No** se engancha `EvaluateLocalRiskHandler`, que exige
  etiquetas completas, re-puntúa el corpus vivo y lanza con menos de dos cohortes.
- Los pedidos sin etiqueta se **cuentan y se declaran** en `unlabeledOrders`; no se lanza.
- Sin corrida vigente, o con menos de dos cohortes temporales, responde `409` con código
  `METRICS_UNAVAILABLE` y un `detail` legible.
- Devuelve el barrido **colapsado a los umbrales donde cambia la matriz**, no 101 filas casi
  idénticas.

**`GET /api/system/capabilities`**

- Devuelve `{ demoDataEnabled: boolean }`. Sin bandera, sin autenticación, siempre disponible.

**`GET /api/alerts` — orden y procedencia**

- Parámetro `sort` con `CREATED_DESC` (actual y predeterminado) y `SCORE_DESC`. Valor desconocido →
  `400` con `INVALID_SORT`.
- `SCORE_DESC` ordena por el **score local de la evaluación vigente**, descendente, desempatando por
  `createdAt` descendente y luego por `id`. El nombre y la semántica quedan documentados en el
  endpoint: en la Etapa 6 habrá evaluaciones externas y «score vigente» a secas sería ambiguo.
- El `JOIN` con `run_evaluations` de la corrida vigente va **dentro de la consulta, antes de
  `Skip`/`Take`**. Hoy `EfAlertStore.GetPageAsync` compone después de paginar.
- `ListAlertsResult` gana `scoringRunSequence`.
- `ListAlertsResult` y `AlertDetail` ganan `currentRun { sequence, completedAt }`.
  `GetCurrentEvaluationsAsync` ya lee esa corrida; falta devolverla.
- Se documenta que el filtro `severity` opera sobre el **snapshot** mientras el orden opera sobre la
  **vigente**, y que un score nulo queda al final en `DESC`.

**Tests**

- **Test diferencial de etiquetas, el criterio central de la tarea**: seed, corrida, snapshot de
  `GET /api/dashboard`; invertir todas las `is_fraud_label` en la base; afirmar que la respuesta es
  **idéntica**. Debe fallar si alguien conecta el dashboard a la etiqueta por cualquier camino.
- Test de nombres de propiedad sobre `/api/dashboard`, en la línea de
  `TheAlertContractNeverExposesGroundTruth`.
- `amountAtRisk` sobre el corpus demo: tres entradas, una por moneda, **sin** campo de total.
- Fraude reportado por pedido: corpus con escalada, ambas alertas reportadas, el importe se cuenta
  **una vez**.
- `/api/evaluation-metrics` con pedidos importados sin etiqueta: `200` con `unlabeledOrders > 0`, no
  `500`.
- `/api/evaluation-metrics` sin corrida: `409` con `METRICS_UNAVAILABLE`.
- Variante de `SalvoApiFactory` con `DemoData:Enabled = false`: seed y métricas dan `404`, y
  `/api/system/capabilities` devuelve `demoDataEnabled: false`.
- `SCORE_DESC` devuelve las alertas ordenadas por score vigente y es determinista ante empates.
- `scoringRunSequence` y `currentRun` reflejan la última corrida tras una segunda corrida.

### Fuera

- **Todo `frontend/`.** Esta tarea no toca una sola línea de TypeScript ni de React.
- La generación de tipos desde OpenAPI y `openapi-typescript`: es `E5B-ALERTAS-UI`.
- `scripts/smoke-ui.sh`: es `E5C-IMPORT-DASHBOARD`.
- Modificar `EvaluateLocalRiskHandler`, `RiskMetricsEvaluator`, `TemporalRiskEngine`, `RuleConfig` o
  el fingerprint. Se consumen o se dejan en paz.
- Modificar la semántica de alertas de E4B: predicado de creación, escalada, revisión, token de
  concurrencia o índices.
- Modificar `Order`, `OrderEvaluationLabel`, el importador o el seed de E2.
- Enriquecer la fixture con casos duros. Agendado para la Etapa 8.
- Conversión de divisas.
- Autenticación.
- `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` y el Blueprint: estado canónico, del
  coordinador.

### Paths autorizados

- `backend/src/Salvo.Application/Dashboard/**`
- `backend/src/Salvo.Application/Metrics/**`
- `backend/src/Salvo.Application/Alerts/**` — solo `IAlertStore`, `ListAlertsHandler` y las vistas
  que ganan `currentRun` y `scoringRunSequence`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`

### Paths reservados por otros trabajos

- Ninguno. Esta tarea **reserva** `backend/src/Salvo.Application/Alerts/IAlertStore.cs` y
  `ListAlertsHandler.cs`, que son código de `E4B-ALERTAS`; el cambio es deliberado y está en el
  diseño v2, D6.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths autorizados.
- Instalación o actualización de dependencias: **no**. Esta tarea no necesita ninguna;
  `openapi-typescript` pertenece a `E5B` y se instala allí.
- Migraciones: **no se esperan**. Esta tarea es de lectura. Si algo pareciera exigir un cambio de
  schema, detenerse y consultar.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna. No borrar `salvo.db`. El test diferencial invierte etiquetas en su
  propia base de test, nunca en la del desarrollador.
- Commits locales en la rama: autorizados.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E5A-API-LECTURA.md` sin faltantes antes de empezar.
- [ ] El test diferencial de etiquetas existe, pasa, y **se verificó que falla** si el dashboard
      consulta la etiqueta. Documentar en el handoff cómo se comprobó ese fallo.
- [ ] `amountAtRisk` es una lista por moneda y no existe ningún campo de total.
- [ ] El fraude reportado se agrega por `DISTINCT order_id`.
- [ ] `riskOverTime` usa `occurredAt`, cubeta semanal y `RuleConfig.BusinessTimeZone`.
- [ ] `topSignals` se cuenta sobre snapshots de alertas abiertas.
- [ ] Ninguna consulta de estado vigente lee `risk_evaluations.status` sin pasar por
      `run_evaluations`.
- [ ] `/api/evaluation-metrics` no responde `500` con pedidos sin etiqueta ni sin corrida.
- [ ] `/api/evaluation-metrics` y el seed dan `404` con `DemoData:Enabled = false`.
- [ ] El `JOIN` de `SCORE_DESC` está antes de `Skip`/`Take`.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] `Salvo.Domain` sigue sin referencias a ASP.NET Core, EF Core ni SDKs externos.
- [ ] Los 109 tests previos siguen verdes.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E5A-API-LECTURA`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E5A-API-LECTURA.md` | Brief válido |
| Test diferencial de etiquetas | Respuesta idéntica tras invertir; y falla si se conecta la etiqueta |
| Test de `amountAtRisk` | Tres monedas, sin total |
| Test de fraude reportado con escalada | Importe contado una vez |
| Test de métricas sin etiquetas | `200` con `unlabeledOrders > 0` |
| Test de métricas sin corrida | `409` `METRICS_UNAVAILABLE` |
| Test con `DemoData:Enabled = false` | `404` en seed y métricas |
| Test de `SCORE_DESC` | Orden por score vigente, determinista |
| `dotnet test` | Los 109 previos más los nuevos |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados; nada de `frontend/` |
| `/handoff E5A-API-LECTURA` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Forma concreta de los DTO de dashboard, métricas y capacidades, y sus nombres de campo.
- Estructura de puertos y repositorios, respetando las fronteras de `AGENTS.md`.
- Cómo expresar el `JOIN` de `SCORE_DESC` en EF Core sobre SQLite manteniéndolo traducible.
- Cómo se colapsa el barrido a los umbrales donde cambia la matriz.
- Mecanismo concreto para que un test disponga de una fábrica con `DemoData:Enabled = false`.
- Cómo se invierten las etiquetas en el test diferencial.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- algo pareciera exigir una migración o un cambio de schema;
- hace falta una dependencia nueva;
- el `JOIN` antes de paginar no puede expresarse en EF Core sobre SQLite sin SQL crudo;
- el test diferencial no puede construirse de forma determinista;
- aparece una contradicción entre el diseño v2 y el código real que no se resuelva con una
  corrección menor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos, incluida la comprobación de que el test diferencial
  falla cuando debe fallar.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
