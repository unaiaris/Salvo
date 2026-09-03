# Salvo — Revisión adversarial del diseño propuesto de Etapa 5

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-03
> Revisor: Claude
> Objeto: `Coordination/Tasks/E5-DISENO.md` v1 (base `main` en `df26f00`; revisado sobre `afdd8dd`)
> Método: lectura de Blueprint §3, §4.3, §4.4, §4.5, §6, §7, §10 y bitácora, `AGENTS.md`,
> `E4-DISENO.md`, y del código real de `Salvo.Domain/Evaluation`, `Salvo.Domain/Alerts`,
> `Salvo.Domain/Risk`, `Salvo.Application/Alerts`, `Salvo.Application/Risk`,
> `Salvo.Application/Orders`, `Salvo.Api`, `Salvo.Infrastructure/Persistence`, `backend/tests` y
> `frontend/` completo. Las afirmaciones cuantitativas se verificaron con consultas de solo lectura
> sobre la base demo local `backend/src/Salvo.Api/salvo.db` (ignorada por Git, 3 corridas) y con la
> documentación de Next.js 16.3.3 incluida en `frontend/node_modules/next/dist/docs/`. No se editó
> ningún archivo versionado salvo este informe.

## Hallazgos, por severidad

### 1. Alcance, alta. El diseño no le da a la analista ninguna forma de ejecutar el scoring

El flujo principal del Blueprint §3 es: importar (paso 1), procesar (pasos 2 a 5), inspeccionar y
revisar (6 y 7), ver el dashboard (8). El paso que produce evaluaciones y alertas es
`POST /api/risk-evaluations:run` (`backend/src/Salvo.Api/RiskEvaluationEndpoints.cs:9`). Las cuatro
rutas del diseño, `/import`, `/alerts`, `/alerts/[id]` y `/dashboard`, no lo invocan en ningún
punto; el diseño no menciona el endpoint ni `ScoringRunSummary`.

Por qué rompe: después de importar, el feed queda vacío y el dashboard en cero, y el «estado vacío
que distingue "todavía no importaste nada" de "no hay alertas abiertas"» es una dicotomía falsa. Hay
un tercer estado, «importado pero sin corrida», y es el que va a encontrar cualquier persona que
siga el guion de la demo. Peor: si hubo una corrida anterior y después se importa un archivo, el
dashboard muestra la corrida vieja como si fuera el estado del corpus. Es exactamente «la UI
muestra como actual algo que no lo es».

Corrección concreta:

- `/import` gana la acción «Ejecutar scoring», que invoca la corrida y presenta el
  `ScoringRunSummary` (secuencia, pedidos, evaluaciones creadas y reusadas, alertas abiertas y
  omitidas). `SCORING_RUN_CONFLICT` entra en la tabla de D9.
- `GET /api/dashboard` devuelve `scoringRun { sequence, completedAt, orderCount }` y
  `ordersPendingScoring` (pedidos sin fila en `run_evaluations` de la última corrida; con corridas
  sobre todo el corpus equivale a `COUNT(orders) - run.orderCount`). La UI muestra «Corrida #3 ·
  2026-09-02 18:14 · 300 pedidos» y, si `ordersPendingScoring > 0`, un aviso con enlace a
  `/import`.
- El estado vacío de `/alerts` y `/dashboard` se resuelve con esa misma información: sin pedidos,
  sin corrida, o sin alertas abiertas, son tres mensajes distintos.

### 2. D7, alta. Las páginas de datos se prerenderizan en `next build` y la compuerta corre sin API

Next.js 16.3.3, sin `cacheComponents`, prerenderiza en build toda ruta que no use APIs de tiempo de
petición, y ejecuta sus `fetch` **una vez durante `next build`**:

> `auto no cache` (default): Next.js fetches the resource from the remote server on every request in
> development, but will fetch once during `next build` because the route will be statically
> prerendered. — `frontend/node_modules/next/dist/docs/01-app/03-api-reference/04-functions/fetch.md:52`

`/alerts`, `/dashboard` y `/import` no reciben `params` ni leen `searchParams`, `cookies` o
`headers`, así que caen en ese caso. `scripts/check.sh:20-21` ejecuta `npm run check` y
`npm run build` sin ninguna API levantada. Resultado: o el build falla con `ECONNREFUSED` contra
`127.0.0.1:5100`, o, si el código captura el error para pintar el estado «con error», Next congela
ese estado de error como HTML estático y lo sirve para siempre. `/alerts/[id]` usa `params` y sí
sería dinámica, lo que hace el fallo más difícil de notar: el detalle funciona y el feed no.

Corrección concreta: cada ruta de datos declara `export const dynamic = 'force-dynamic'` (o llama
`await connection()` antes del primer `fetch`). Como criterio de aceptación de E5B y E5C: el build
debe pasar con `SALVO_API_BASE_URL` apuntando a un puerto cerrado, y la salida de `next build` debe
listar las cuatro rutas como dinámicas (`ƒ`), no como estáticas (`○`). El diseño no puede prometer
«cuatro estados por ruta» si uno de ellos queda horneado en build.

### 3. D6 contra D7, alta. El «patrón de `health.ts`» no funciona desde el servidor

`frontend/src/lib/api/health.ts:11` llama `fetcher("/api/health")` con URL relativa, que resuelve
gracias al rewrite `/api/:path*` de `frontend/next.config.ts:9-11`. Ese rewrite solo existe para
peticiones que llegan al servidor Next desde el navegador. D7 dice que el detalle se renderiza en el
servidor y que la revisión es una acción de servidor; en Node, `fetch("/api/alerts")` lanza
`TypeError: Failed to parse URL`. El diseño extiende un patrón que en el lugar donde lo va a usar no
puede ejecutarse.

Además, `AGENTS.md` exige «toda red externa usa timeout explícito» y `health.ts` no fija ninguno.
La Etapa 5 pasa de un smoke a la ruta crítica de la consola: sin `AbortSignal.timeout`, una API
colgada cuelga el render del servidor.

Corrección concreta: un módulo `server-only` (`import "server-only"`) que construye URLs absolutas
desde `SALVO_API_BASE_URL`, fija timeout con `AbortSignal.timeout(...)`, lee `problem+json` y aplica
las guardas. El navegador no llama a la API directamente en E5; si en el futuro lo hace, usa el
rewrite y una variante explícita del cliente. D6 debe decir esto en vez de «siguiendo el patrón de
`health.ts`».

### 4. D1, alta. El test de arquitectura por reflexión es evadible y no vigila la costura real

El diseño promete un test que afirma que «el manejador del dashboard no puede depender de
`IEvaluationLabelReader`». Contra el código:

- Hay **otro** puerto que lee etiquetas: `IOrderDataStore.GetLabelsByOrderIdsAsync`
  (`backend/src/Salvo.Application/Orders/IOrderDataStore.cs:12`). Un manejador que dependa de
  `IOrderDataStore` pasa el test y lee etiquetas.
- La costura real es Infrastructure: cualquier `Ef*Store` recibe `SalvoDbContext`, que expone
  `OrderEvaluationLabels` (`backend/src/Salvo.Infrastructure/Persistence/SalvoDbContext.cs:13`). El
  manejador del dashboard va a depender de un puerto nuevo, `IDashboardReader` o similar, cuya
  implementación EF puede hacer `JOIN order_evaluation_labels` sin que ningún tipo de Application lo
  delate. La reflexión sobre parámetros de constructor no ve una consulta LINQ.
- La dependencia indirecta de la pregunta abierta 1 también pasa: un manejador que consuma otro
  manejador que sí lee etiquetas no tiene `IEvaluationLabelReader` en su constructor.

Corrección concreta, en orden de fuerza:

1. **Test diferencial de caja negra**, el que realmente cierra la vía: seed, corrida, snapshot de
   `GET /api/dashboard`; después invertir todas las `is_fraud_label` en la base (o borrar la tabla
   de etiquetas) y afirmar que la respuesta es **byte a byte idéntica**. Si el dashboard depende de
   la etiqueta por cualquier camino, cambia.
2. Test de nombres de propiedad como `TheAlertContractNeverExposesGroundTruth`
   (`backend/tests/Salvo.Api.IntegrationTests/AlertEndpointTests.cs:83-108`) sobre `/api/dashboard`.
3. Opcional: un `DbCommandInterceptor` registrado en `SalvoApiFactory` que capture los comandos
   emitidos durante la petición y afirme que ninguno menciona `order_evaluation_labels`.
4. Si se conserva el test por reflexión, que recorra el cierre transitivo de tipos de constructor
   dentro de `Salvo.Application` y prohíba tanto `IEvaluationLabelReader` como `IOrderDataStore` y
   cualquier tipo de `Salvo.Domain.Evaluation`.

### 5. D1 y D2, alta. `EvaluateLocalRiskHandler` devuelve `500` en el flujo que la propia UI invita

El diseño engancha `/api/evaluation-metrics` a `EvaluateLocalRiskHandler`. Tres problemas
verificados en `backend/src/Salvo.Application/Risk/EvaluateLocalRiskHandler.cs`:

- Líneas 18-22: lanza `InvalidOperationException` si **algún** pedido no tiene etiqueta. Las
  importaciones nunca escriben etiquetas (decisión 36 de la bitácora). `/import` invita a importar un
  archivo; en cuanto alguien lo hace en modo demo, la sección de calidad responde `500`.
- `RiskMetricsEvaluator.SplitByTime` lanza con menos de dos cohortes temporales
  (`backend/src/Salvo.Domain/Evaluation/RiskMetricsEvaluator.cs:81-85`): base vacía o con un solo
  instante → `500`.
- Líneas 12-14: **re-puntúa el corpus vivo** con `TemporalRiskEngine.Score`, no lee la corrida
  vigente. D3 dice que «todo se calcula sobre la corrida vigente». Tras una importación sin corrida,
  el dashboard describe la corrida N y la sección de calidad describe el corpus N+1; las dos
  superficies del mismo `/dashboard` se contradicen.

Corrección concreta: E5A crea un manejador propio de métricas que parte de
`IScoringRunStore.GetCurrentEvaluationsAsync` y lee etiquetas solo de los pedidos cubiertos; los
pedidos sin etiqueta se cuentan y se declaran en la respuesta (`unlabeledOrders`), no se lanza. Si
no hay corrida o hay menos de dos cohortes, el endpoint responde `409` con `problem+json` y código
`METRICS_UNAVAILABLE` con `detail` legible; la UI tiene ese estado. `EvaluateLocalRiskHandler` queda
como está, para tests.

### 6. Hallazgo 1 del diseño, media. La «fuga por barrido» no revela nada que el feed no revele ya

El diseño justifica D1 y D2 con un ataque de inferencia: la matriz por umbral menos los scores
públicos reconstruye la etiqueta por banda. Verificado contra `salvo.db` (corrida 3) y contra el
test dorado `backend/tests/Salvo.Api.IntegrationTests/RiskEvaluationTests.cs:36-51`:

| Score vigente | Pedidos | Con `isFraudLabel = 1` |
| --- | --- | --- |
| 0 | 266 | 0 |
| 20 | 16 | 0 |
| 60 | 13 | 13 |
| 90 | 5 | 5 |

En el corpus demo `score ≥ 60 ⇔ isFraudLabel`. Las 18 alertas abiertas **son** los 18 pedidos
etiquetados; precisión, recall y F1 valen 1 en holdout y la tasa de falsos positivos es 0. La
etiqueta ya es pública de facto vía `GET /api/alerts`, y
`SeedDemoOrdersResult.FraudLabelCount` (`backend/src/Salvo.Application/Orders/Seed/SeedDemoOrdersResult.cs:10`)
publica un agregado de etiqueta bajo la misma bandera desde E2.

Por qué importa aunque no rompa código: el argumento del diseño es incorrecto para la única fixture
que existe, y una entrevista lo detecta. Y el número que se va a mostrar, F1 = 1,00, sin contexto,
se lee como fixture sobreajustada y hunde la credibilidad de E8.

Corrección concreta: D1 y D2 se sostienen por el argumento de forma de producción («la verdad de
campo no existe fuera de la demo»), no por el de inferencia; reescribir la motivación. La sección
de calidad lleva un rótulo fijo: «La fixture demo fue construida para que las reglas recuperen sus
etiquetas; estas métricas prueban el pipeline de evaluación, no la calidad del criterio». El
barrido se presenta colapsado a los umbrales donde cambia la matriz (cuatro filas), no 101 filas
casi idénticas.

### 7. Pregunta abierta 3, media. `DemoData:Enabled` es del backend en tiempo de ejecución; el frontend no tiene forma de saberlo

`backend/src/Salvo.Api/OrderEndpoints.cs:32` registra `/api/demo-data/seed` solo si la
configuración lo habilita; `appsettings.json` lo pone en `false` y `appsettings.Development.json` en
`true`. El build de Next.js es idéntico en ambos casos: hablar de «un build donde `DemoData:Enabled`
sea falso» es una categoría equivocada. La UI solo puede descubrirlo en tiempo de ejecución, y hoy la
única señal es un `404` en el seed o en las métricas, indistinguible de una ruta mal escrita o de una
API caída.

Corrección concreta: `GET /api/system/capabilities` (o extender `/health`) devuelve
`{ demoDataEnabled: boolean }`. `/import` oculta «Cargar demo» y `/dashboard` oculta la sección de
calidad cuando es `false`, y en ese caso ni siquiera llama a `/api/evaluation-metrics`. Tests con las
dos configuraciones: `SalvoApiFactory.cs:72` fija `true`; falta una variante con `false` que
afirme `404` en seed y métricas y `demoDataEnabled = false`. E8 documenta que `dotnet run` usa
`Development` y por tanto la demo siempre está habilitada localmente.

### 8. D8, media. `evaluatedAt` de la evaluación vigente es la fecha de su primera inserción, no la de la corrida vigente

`RiskEvaluation.ForLocal` recibe `startedAt` de la corrida que la inserta
(`backend/src/Salvo.Application/Risk/RunScoringHandler.cs:32-36`); las corridas siguientes reúsan la
fila por fingerprint (líneas 54-57) y no la tocan. `AlertProjection.ToEvaluationView` expone
`evaluation.CreatedAt` como `EvaluatedAt` (`backend/src/Salvo.Application/Alerts/AlertProjection.cs:108`).
En `salvo.db`: 3 corridas, 300 evaluaciones, **un solo** `created_at_utc` distinto.

D8 promete «dos bloques visualmente distintos, cada uno con su instante». El bloque «evaluación
vigente» mostraría la hora de la corrida 1 para una evaluación que la corrida 3 volvió a hacer
vigente; en el rebote 0 → 40 → 0 de la decisión 31, la fila vigente tras la corrida 3 es la de la
corrida 1 con su fecha de entonces. La UI mostraría como «actual» un instante viejo.

Corrección concreta: E5A añade a `AlertDetail` y `ListAlertsResult` un `currentRun { sequence,
completedAt }`; `EfAlertStore.GetCurrentEvaluationsAsync` ya lee esa corrida
(`backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs:160-163`), solo falta devolverla. La
UI rotula el bloque vigente con la corrida («Vigente desde la corrida #3, 2026-09-02 18:14») y, si
conserva `evaluatedAt`, lo llama «calculada por primera vez el…».

### 9. D8, media. Un cambio de evaluación dentro de la misma banda es invisible para la analista

`HasBandDivergence` compara bandas (`backend/src/Salvo.Application/Alerts/AlertProjection.cs:86`).
`AHigherScoreInsideTheSameBandOpensNothing` (`backend/tests/Salvo.Api.IntegrationTests/AlertCreationTests.cs:88`)
verifica 90 → 100 sin cambio de banda: no hay alerta nueva, no hay divergencia, no hay
reconocimiento. Pero las señales cambiaron. D8 solo destaca la divergencia de banda; el snapshot se
mostraría junto a un bloque vigente con otras señales sin ninguna llamada de atención.

Corrección concreta: el contrato ya trae `snapshot.evaluationId` y `currentEvaluation.evaluationId`
(`backend/src/Salvo.Application/Alerts/AlertViews.cs:10,22`). Cuando difieren y no hay divergencia de
banda, la UI muestra un aviso secundario: «La evaluación del pedido cambió (90 → 100) sin cambiar de
banda; las señales vigentes están abajo». No exige casilla porque la API no la exige; ser más
estricto que la API en la UI crea un `200` imposible de alcanzar por otro cliente sin sentido.

### 10. D4, media. «Fraude reportado» debe agregarse por pedido, no por alerta

El predicado de escalada (`backend/src/Salvo.Application/Risk/RunScoringHandler.cs:150-155`) compara
solo bandas; **no mira el veredicto** de la alerta anterior. Un pedido marcado `REPORTED_FRAUD` en
`MEDIUM` puede recibir, tras un backfill, una alerta `CRITICAL` enlazada por `supersedesAlertId` y
volver a reportarse. Resultado: dos alertas `REPORTED_FRAUD` para un solo pedido y un solo importe.
Sumar `alerts WHERE status = 'REPORTED_FRAUD'` duplica el monto. Las alertas abiertas no tienen este
problema por el índice único parcial (`AlertConfiguration.cs:98-101`).

Corrección concreta: el dashboard agrega fraude reportado por `DISTINCT order_id`, y su test usa
`AlertTestCorpus.EscalationBase` + `EscalationBackfill` reportando fraude en ambas alertas y
afirmando que el monto se cuenta una vez.

### 11. D5 y pregunta abierta 2, media. Ningún orden es estable si hay una corrida entre página 1 y página 2, y `CREATED_DESC` no ordena nada útil

- `CREATED_DESC`: una corrida que abre alertas nuevas las inserta **al principio**; la página 2
  repite elementos de la página 1. No es un problema de `SCORE_DESC`: ya existe hoy.
- `SCORE_DESC`: además, un cambio de score vigente reordena.
- Todas las alertas de una corrida comparten `createdAt` (`salvo.db`: 18 alertas, 1 `created_at_utc`
  distinto), así que `CREATED_DESC` ordena de hecho por GUID aleatorio: determinista, pero sin
  significado para la analista.
- Fijar la corrida en el parámetro no arregla las inserciones y agrega una consulta para corridas
  arbitrarias que nadie más necesita.

Corrección concreta: `ListAlertsResult` devuelve `scoringRunSequence`; la UI, si cambia entre
páginas, muestra «el corpus cambió» y recarga desde la página 1. Para el tamaño del MVP, el feed pide
una sola página de hasta `MaximumPageSize = 200` (`ListAlertsHandler.cs:8`), con lo que la
paginación no interviene, sin renunciar al `sort` en la API para cuando sí intervenga. Dos
precisiones que el diseño debe declarar: el orden de los `null` en `SCORE_DESC` (en SQLite un `NULL`
es menor que cualquier valor y queda **al final** en `DESC`: correcto, pero hay que decirlo), y que
el filtro `severity` es por snapshot mientras el orden es por vigente. Y una consecuencia de
implementación: el `JOIN` con `run_evaluations` de la última corrida debe estar **dentro** de la
consulta antes de `Skip/Take`; hoy `EfAlertStore.GetPageAsync` compone después de paginar
(`EfAlertStore.cs:37-44`), así que E5A modifica `IAlertStore.GetPageAsync` y `ListAlertsHandler`, código
de E4B. Aceptable, pero el brief debe reservarlo.

### 12. D6, media. Contradice la decisión 14 de la bitácora y las guardas no proyectan

`AGENTS.md` y la decisión 14 dicen «OpenAPI gobierna el contrato backend–frontend» y «cliente
TypeScript tipado». D6 propone interfaces escritas a mano por recurso: es el contrato duplicado que
la decisión 14 quiso evitar, y deriva en silencio. Además, la guarda de `health.ts:27-34` es un
predicado `value is HealthResponse` sobre **el mismo objeto**: comprueba que existan los campos
conocidos y deja pasar los desconocidos. No es una proyección; ver hallazgo 13.

Corrección concreta: cada guarda **construye un objeto nuevo** con las claves conocidas (parse, no
predicado), ignora claves desconocidas y exige las conocidas; así una Etapa 7 que agregue
`explanation*` a `AlertDetail` (columnas hoy vedadas, `AlertSchemaTests.cs:50-53`) no rompe la UI
desplegada ni filtra el campo nuevo. Para la deriva, o se generan los tipos desde `/openapi/v1.json`
con una devDependency fijada (`openapi-typescript`), o se añade un test de contrato que compare las
claves de cada guarda contra las propiedades del schema OpenAPI servido por la API. La segunda opción
respeta «sin dependencias nuevas»; la primera respeta la decisión 14. El diseño debe elegir y decirlo.

### 13. Pregunta abierta 4, media. El payload RSC serializa completas las props de los componentes cliente

Documentado en `frontend/node_modules/next/dist/docs/01-app/02-guides/server-and-client-boundary.md:28,121`:
el servidor «serializes the props passed to them» y «Props are serialized and sent to the browser».
El formulario de revisión (casilla, nota, `pending`) tiene que ser componente cliente; el gráfico de
riesgo temporal con Recharts también. Si reciben `detail` o `dashboard` enteros, el objeto completo
va al HTML dentro del payload RSC, incluidos campos no renderizados.

Hoy la API no emite `isFraudLabel`, así que el test «el HTML no contiene `isFraudLabel`» pasa
**vacuamente** y no protege nada: no puede aparecer un texto que ninguna fuente produce.

Corrección concreta: los componentes cliente reciben primitivas, no objetos de API; habilitar
`experimental.taint` y marcar con `experimental_taintObjectReference` la respuesta cruda en el
módulo `server-only` (`.../02-guides/data-security.md:219-236`); y reemplazar el test vacuo por uno
que, con `fetch` simulado, devuelva un campo extra (`isFraudLabel: true`) y afirme que no llega al
render ni a las props serializadas. Junto con el hallazgo 12, es lo que hace que la frontera de
lectura sea verificada y no prometida.

### 14. Verificación, media. La compuerta no puede verificar «recorrido completo con datos, sin datos y con errores»

El Blueprint fija esa verificación para E5. `scripts/check.sh` ejecuta Vitest en jsdom y el build.
Componentes de servidor asíncronos, acciones de servidor, `revalidatePath` y `redirect` no se
ejercitan en jsdom sin simular medio framework, y no hay smoke HTTP ni e2e. El diseño no dice cómo
se verifica.

Corrección concreta: un `scripts/smoke-ui.sh` que levanta la API con `DemoData:Enabled=true` y
`next start`, hace seed y corrida, y comprueba con `curl` las cuatro rutas en los tres escenarios
(con datos, base vacía, API apagada → página de error) buscando textos fijos. Si se quiere Playwright,
es una dependencia nueva y se aprueba como tal. Cualquiera de las dos, pero una tiene que estar en el
brief como criterio de aceptación.

### 15. Alcance, baja. Recharts no está instalado y el diseño dice «sin dependencias nuevas»

`frontend/package.json` no incluye `recharts`; `AGENTS.md` lo lista como parte del stack. El
«riesgo temporal» de `/dashboard` necesita un gráfico. El diseño debe decidir: Recharts fijado y
`package-lock.json` serializado en E5C, o SVG a mano. En ambos casos el componente es cliente y aplica
el hallazgo 13.

### 16. D9, baja. La tabla mezcla códigos, omite otros y no cubre el `200` sin efecto

- `ALERT_NOT_FOUND` es `404`, no `409` (`AlertEndpoints.cs:167-173`).
- Faltan `INVALID_STATUS` y `NOTE_TOO_LONG` (`AlertEndpoints.cs:115-129`); la UI debe fijar
  `maxLength={2000}` y aun así mapear el código.
- `AlertReviewResult.Applied = false` (`AlertViews.cs:110`) significa «ya estaba exactamente así»;
  la UI debe decirlo, no «revisión registrada».
- Falta `SCORING_RUN_CONFLICT` si se acepta el hallazgo 1.

### 17. D7, baja, sospecha no verificada contra código. React 19 reinicia el formulario al completar la acción, también con error

React 19 restablece los campos no controlados de un `<form action>` cuando la acción termina, incluso
si devolvió un estado de error. Con D9, tras un `409` la nota escrita se pierde y la analista tiene
que volver a redactarla. No hay código en el repo contra el que verificarlo; es comportamiento
documentado de React, no de Salvo. Mitigación: inputs controlados, o devolver los valores enviados en
el estado de `useActionState` y usarlos como `defaultValue`.

### 18. D1 y D3, baja. «Señales principales» y «riesgo temporal» no definen población, cubeta ni zona horaria

- Señales: contar reglas sobre **todas** las evaluaciones vigentes incluye las 16 apariciones de
  `foreign_country` a score 20 en el corpus demo, que no alertan a nadie. Definir la población:
  evaluaciones marcadas, o snapshots de alertas abiertas.
- Riesgo temporal: agrupar por `occurredAt` (tiempo de negocio), no por `createdAt` de la
  evaluación; cubeta semanal (el corpus va de 2026-05-01 a 2026-08-28, unas 17 semanas); zona
  horaria `RuleConfig.BusinessTimeZone`, América/Montevideo (`RuleConfig.cs:10`), la misma con la
  que las reglas leen el día. La UI formatea con esa zona y una configuración regional fija; Node
  24.20.0 trae ICU completa (78.3), verificado con `es-UY` para montos e instantes.

### 19. D5 y E6, baja. `SCORE_DESC` quedará ambiguo cuando exista la evaluación externa

En E6 `RiskEvaluation` tendrá filas `EXTERNAL_MOCK` con su propio score. «Score vigente» debe quedar
definido en el contrato como el **local** de la corrida vigente, o el valor llamarse
`LOCAL_SCORE_DESC`. Lo mismo para «tasa de marcado»: `DENIED` de la corrida (D3 ya lo dice; que el
código lo haga vía `run_evaluations`, nunca por `risk_evaluations.status` a secas).

## Decisiones que resistieron

**D3 es correcta y coincide con el código.** `EfAlertStore.GetCurrentEvaluationsAsync` y
`EfScoringRunStore.GetCurrentEvaluationsAsync` definen vigente por la última corrida
(`EfAlertStore.cs:156-180`, `EfScoringRunStore.cs:119-142`). El dashboard debe usar la misma consulta.

**D4 es correcta y las cifras del diseño son exactas.** Verificado en `salvo.db`: alertas abiertas
6 BRL (1.279.386), 6 USD (764.082), 6 UYU (1.898.778); la suma cruzada 3.942.246 es la que el diseño
denuncia. La fixture tiene 100 pedidos por moneda y 6 etiquetas de fraude por moneda.

**El hallazgo 3 del diseño es real.** `EfAlertStore.GetPageAsync` ordena por `CreatedAt` descendente
y `Id` (`EfAlertStore.cs:37-42`); ordenar en cliente rompe la paginación. Mover `sort` a la API es lo
correcto; ver hallazgo 11 para lo que falta.

**D7 sin estado optimista es correcta.** Cuatro razones de `409` en `ReviewAlertHandler.cs:56-100` y
`EfAlertStore.SaveReviewAsync`; pintar el veredicto antes de saberlo sería mentir.

**D8 espeja exactamente el backend.** `ReviewAlertHandler.cs:56-65` exige `acknowledgedDivergence`
solo con divergencia de banda; `AnOpenAlertKeepsItsSnapshotAndExposesTheDivergenceAfterABackfill`
(`AlertCreationTests.cs:119-160`) confirma el caso 70 → 0 con señales vacías que el texto de D8
describe.

**Los cinco códigos de D9 existen** (`AlertEndpoints.cs:156-173`); solo falta lo del hallazgo 16.

**D10 es una buena partición.** E5A backend puro, E5B el corazón, E5C consumidor. Con los hallazgos 1,
5, 8 y 11, E5A crece: capacidades, dashboard con corrida y pendientes, métricas propias, `currentRun`
en alertas y `sort` con `JOIN` dentro de la consulta.

## Contradicciones con el Blueprint, la bitácora y `AGENTS.md`

- Decisión 14 («OpenAPI gobierna el contrato; cliente TypeScript tipado») contra D6: hallazgo 12.
- `AGENTS.md` «UI: Tailwind y Recharts» contra «sin dependencias nuevas»: hallazgo 15.
- `AGENTS.md` «toda red externa usa timeout explícito» contra «patrón de `health.ts`»: hallazgo 3.
- Blueprint §3 pasos 2 a 5 contra las rutas de §4.4 y del diseño: hallazgo 1. Es una omisión del
  propio §4.4, que tampoco nombra dónde se dispara el scoring; el diseño la heredó.
- Blueprint §4.5 y decisión 25 («fronteras separadas para scoring y etiquetas») son coherentes con
  D1/D2; lo que falla es el mecanismo de verificación, no la decisión: hallazgo 4.
- §4.3 «`amountAtRisk` suma alertas abiertas; el fraude ya reportado se presenta por separado»: D4
  lo refina por moneda sin contradecirlo; hallazgo 10 lo corrige por pedido.

## Respuestas a las cinco preguntas abiertas

**1. ¿El test de arquitectura puede evadirse con una dependencia indirecta?** Sí, y por tres caminos:
un manejador que consuma otro que lea etiquetas; `IOrderDataStore.GetLabelsByOrderIdsAsync`, que es
un segundo puerto de etiquetas; y, sobre todo, la implementación EF del puerto del dashboard, que
tiene `SalvoDbContext` completo y puede hacer el `JOIN` sin que ningún tipo de Application lo revele.
Se cierra con el test diferencial de caja negra del hallazgo 4: invertir las etiquetas en la base y
exigir una respuesta idéntica. Es el único test que no depende de saber por dónde entraría la fuga.

**2. ¿`SCORE_DESC` es estable si una corrida ocurre entre páginas?** No, y `CREATED_DESC` tampoco: las
alertas nuevas se insertan al principio y desplazan la página 2. Fijar la corrida en el parámetro no
arregla las inserciones. Lo razonable es devolver `scoringRunSequence` en la respuesta, que la UI
recargue si cambió, y que para el volumen del MVP el feed pida una sola página de hasta 200.
Hallazgo 11.

**3. ¿La sección de calidad puede aparecer con `DemoData:Enabled` falso?** La pregunta está mal
planteada: la bandera es del backend en tiempo de ejecución y el build de Next es el mismo siempre.
La sección aparece o no según lo que la UI descubra en tiempo de ejecución, y hoy solo puede
descubrirlo por un `404` ambiguo. Hace falta un endpoint de capacidades. Hallazgo 7.

**4. ¿El render de servidor puede llevar campos no proyectados al HTML?** Sí: toda prop de un componente
cliente se serializa entera en el payload RSC. El formulario de revisión y el gráfico son cliente.
Además la guarda de `health.ts` no proyecta, deja pasar campos desconocidos, y el test «el HTML no
contiene `isFraudLabel`» es vacuo porque la API no lo emite. Se cierra con guardas que construyen
objetos nuevos, props primitivas, `experimental.taint` y un test con un campo extra inyectado.
Hallazgos 12 y 13.

**5. ¿Qué pasa en `/alerts/[id]` con `currentEvaluation` nulo?** Contra el código, para una alerta
existente es **inalcanzable**: toda corrida puntúa el corpus completo
(`EfRiskOrderReader.GetAllChronologicallyAsync`, `RunScoringHandler.cs:26`), así que cualquier
pedido alertado en la corrida k está referenciado por todas las corridas ≥ k, y la última siempre lo
cubre. El comentario de `AlertContext.cs:11-14` lo dice y es correcto. Un pedido importado después
de la última corrida no tiene evaluación vigente, pero tampoco tiene alerta, así que no tiene
detalle. Dos consecuencias prácticas: primero, la UI debe tratar el `null` de todos modos porque el
contrato lo permite, y en ese caso `HasBandDivergence` es `false` (`AlertProjection.cs:86`) y la
revisión procede sin reconocimiento; el texto de D8 no puede decir «la evaluación vigente es 0», debe
decir «no hay evaluación vigente para este pedido». Segundo, el caso real que la pregunta roza es el
del hallazgo 1: pedidos importados sin corrida, invisibles en el feed y ausentes del dashboard, que
solo se hacen visibles si el dashboard cuenta `ordersPendingScoring`.

## Riesgos de rehacer trabajo en E6, E7 y E8

- **E6**: si «tasa de marcado» o «score vigente» se leen de `risk_evaluations` sin pasar por la
  corrida, la primera fila `EXTERNAL_MOCK` los rompe. D3 lo previene si el código lo respeta;
  hallazgo 19 pide fijarlo en el contrato. El detalle de alerta debería reservar un bloque
  «evaluación externa» vacío para no rediseñar la página.
- **E7**: `AlertDetail` ganará `explanation`, `recommendedAction` y `explanationStatus`. Con guardas
  estrictas que rechacen claves desconocidas, desplegar el backend antes que la UI rompe la consola;
  con guardas que dejen pasar claves desconocidas, el campo nuevo va al payload RSC antes de que
  nadie decida mostrarlo. Solo la proyección explícita del hallazgo 12 sirve para ambos.
- **E8**: F1 = 1,00 sin el rótulo del hallazgo 6 es un pasivo en el README y en la demo. El
  endpoint de capacidades del hallazgo 7 es lo que permite explicar «mock, sandbox y producción» en
  la propia UI, como exige el README.

## Resumen para el brief

| # | Decisión | Severidad | Acción mínima |
| --- | --- | --- | --- |
| 1 | Alcance | Alta | Acción «Ejecutar scoring» en `/import`; `scoringRun` y `ordersPendingScoring` en `/api/dashboard`; tercer estado vacío |
| 2 | D7 | Alta | `dynamic = 'force-dynamic'` en las rutas de datos; build debe pasar sin API y listarlas como dinámicas |
| 3 | D6/D7 | Alta | Cliente `server-only` con URL absoluta desde `SALVO_API_BASE_URL` y `AbortSignal.timeout` |
| 4 | D1 | Alta | Test diferencial: invertir etiquetas en la base y exigir `/api/dashboard` idéntico; test de nombres de propiedad |
| 5 | D1/D2 | Alta | Manejador de métricas sobre la corrida vigente; pedidos sin etiqueta contados, no `500`; `METRICS_UNAVAILABLE` |
| 6 | Hallazgo 1 | Media | Reescribir la motivación; rótulo «la fixture recupera sus etiquetas»; barrido colapsado a umbrales con cambio |
| 7 | Pregunta 3 | Media | `GET /api/system/capabilities` con `demoDataEnabled`; tests con la bandera en `false` |
| 8 | D8 | Media | `currentRun { sequence, completedAt }` en alertas; rotular `evaluatedAt` como primera inserción |
| 9 | D8 | Media | Aviso secundario cuando cambia `evaluationId` sin cambiar de banda |
| 10 | D4 | Media | Fraude reportado por `DISTINCT order_id`; test con escalada reportada dos veces |
| 11 | D5 | Media | `scoringRunSequence` en el listado; una página de hasta 200; declarar `NULL` al final y `JOIN` antes de `Skip/Take` |
| 12 | D6 | Media | Guardas que proyectan y toleran claves desconocidas; tipos desde OpenAPI o test de contrato contra el schema |
| 13 | Pregunta 4 | Media | Props primitivas a componentes cliente; `experimental.taint`; test con campo extra inyectado |
| 14 | Verificación | Media | `scripts/smoke-ui.sh` con los tres escenarios, o Playwright aprobado como dependencia |
| 15 | Alcance | Baja | Decidir Recharts fijado o SVG propio |
| 16 | D9 | Baja | `ALERT_NOT_FOUND` como `404`; añadir `NOTE_TOO_LONG`, `INVALID_STATUS`, `SCORING_RUN_CONFLICT`; mensaje para `applied = false` |
| 17 | D7 | Baja | Preservar la nota tras un `409` (inputs controlados o `defaultValue` desde el estado) |
| 18 | D1/D3 | Baja | Definir población de señales, cubeta semanal por `occurredAt` y zona América/Montevideo |
| 19 | D5/E6 | Baja | Definir `SCORE_DESC` como score local vigente en el contrato |

## Cómo se verificó

- Lectura completa de los archivos listados en el encabezado; toda cita `archivo:línea` del informe
  se abrió y se corresponde con `main` en `afdd8dd`.
- Consultas `sqlite3 -readonly` sobre `backend/src/Salvo.Api/salvo.db`: corridas, alertas por
  estado y moneda con suma de `amount_cents`, cruce alertas–etiquetas, estado de evaluaciones de la
  última corrida, score vigente contra etiqueta, y cardinalidad de `created_at_utc` en alertas y
  evaluaciones. La base no se modificó.
- Conteo de la fixture `backend/src/Salvo.Infrastructure/Seed/Fixtures/demo-orders.v1.json`:
  monedas, países, comercios, rango temporal y etiquetas por moneda.
- Documentación de Next.js 16.3.3 en `frontend/node_modules/next/dist/docs/`: `fetch.md` (prerender
  en build con `auto no cache`), `server-and-client-boundary.md` (serialización de props),
  `data-security.md` (taint), `caching-without-cache-components.md` (`dynamic`),
  `07-mutating-data.md` (`useActionState`, `revalidatePath`, `redirect`).
- `node -e` para confirmar ICU completa y formato `es-UY` de montos e instantes con zona explícita.
- El hallazgo 17 se marca como sospecha: describe comportamiento documentado de React 19 y no hay
  código de Salvo contra el que contrastarlo.
- No se ejecutó ninguna compuerta ni se generó ningún artefacto dentro del repo; el único archivo
  escrito es este informe.
