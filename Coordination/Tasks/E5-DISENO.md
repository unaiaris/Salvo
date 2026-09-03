# Salvo — Diseño de la Etapa 5: UI y dashboard

> Estado: **v2**, con los diecinueve hallazgos de la revisión adversarial incorporados
> Fecha: 2026-09-03
> Base: `main` tras el informe de revisión
> Sustituye a la v1. El informe queda en `Coordination/Tasks/E5-revision-adversarial.md`
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §3, §4.3, §4.4 y «Etapa 5»

## Qué cambió respecto de la v1

| Cambio | Origen |
| --- | --- |
| La corrida de scoring entra al producto; sin ella la consola no funciona | Hallazgo 1 |
| Las rutas de datos se declaran dinámicas; el build debe pasar sin API | Hallazgo 2 |
| El cliente es `server-only`, con URL absoluta y timeout | Hallazgo 3 |
| La invariante del dashboard se verifica con un test diferencial, no por reflexión | Hallazgo 4 |
| Las métricas tienen manejador propio sobre la corrida vigente y nunca responden `500` | Hallazgo 5 |
| **La motivación de D1 se reescribe**: el argumento de inferencia era incorrecto | Hallazgo 6 |
| Endpoint de capacidades: la UI no puede adivinar `DemoData:Enabled` | Hallazgo 7 |
| `currentRun` en el contrato de alertas; `evaluatedAt` no es la fecha de la corrida vigente | Hallazgo 8 |
| Aviso secundario cuando cambia la evaluación sin cambiar de banda | Hallazgo 9 |
| Fraude reportado agregado por pedido, no por alerta | Hallazgo 10 |
| Paginación: `scoringRunSequence` en la respuesta y feed de una sola página | Hallazgo 11 |
| Tipos generados desde OpenAPI; las guardas proyectan en vez de comprobar | Hallazgo 12 + decisión del usuario |
| Props primitivas a componentes cliente; el test del HTML deja de ser vacuo | Hallazgo 13 |
| `scripts/smoke-ui.sh` como criterio de aceptación verificable | Hallazgo 14 |
| Gráfico en SVG de servidor; se corrige `AGENTS.md` | Hallazgo 15 + decisión del usuario |
| Tabla de códigos completa, con `404`, `400` y el `200` sin efecto | Hallazgo 16 |
| La nota sobrevive a un conflicto | Hallazgo 17 |
| Población, cubeta y zona horaria definidas | Hallazgo 18 |
| `SCORE_DESC` se define como score **local** vigente | Hallazgo 19 |

---

## Resumen de decisiones

| # | Decisión |
| --- | --- |
| D1 | Dos superficies de métricas: operativa sin etiqueta, y de calidad tras bandera |
| D2 | La invariante se verifica invirtiendo las etiquetas y exigiendo una respuesta idéntica |
| D3 | La corrida de scoring es parte de la consola, no un detalle de operación |
| D4 | Todo se calcula sobre la corrida vigente, siempre vía `run_evaluations` |
| D5 | `amountAtRisk` por moneda; fraude reportado por pedido distinto |
| D6 | El orden del feed es de la API, con el `JOIN` antes de paginar |
| D7 | Cliente `server-only`, tipos desde OpenAPI, guardas que proyectan |
| D8 | Las rutas de datos son dinámicas por declaración |
| D9 | Manejador de métricas propio; sin corrida o sin cohortes responde `409`, nunca `500` |
| D10 | La UI descubre las capacidades del backend en tiempo de ejecución |
| D11 | Sin estado optimista en la revisión |
| D12 | Divergencia de banda bloquea; cambio dentro de banda avisa; el bloque vigente se rotula con su corrida |
| D13 | Cada código de la API tiene mensaje y acción de recuperación |
| D14 | Los componentes cliente reciben primitivas |
| D15 | La verificación de recorrido es un smoke HTTP ejecutable |
| D16 | El gráfico es SVG renderizado en el servidor |
| D17 | Población, cubeta temporal, zona horaria y configuración regional fijadas |
| D18 | Partición en `E5A-API-LECTURA`, `E5B-ALERTAS-UI` y `E5C-IMPORT-DASHBOARD` |

---

## D1 — Dos superficies de métricas

**El dashboard operativo nunca lee `OrderEvaluationLabel`:**

| Métrica | De dónde sale |
| --- | --- |
| Alertas abiertas | `alerts WHERE status = 'OPEN'`, por severidad derivada |
| Monto en riesgo | Importe de los pedidos de esas alertas, por moneda |
| Fraude reportado | Pedidos distintos con alerta `REPORTED_FRAUD` — **el veredicto de la analista** |
| Tasa de marcado | Evaluaciones `DENIED` sobre total, vía `run_evaluations` de la corrida vigente |
| Riesgo temporal | Score de la evaluación vigente, por semana de `occurredAt` |
| Señales principales | Conteo de reglas sobre los snapshots de las alertas abiertas |

**Las métricas de calidad viven en `GET /api/evaluation-metrics`**, tras la bandera
`DemoData:Enabled`, y la UI las presenta en una sección aparte y rotulada.

### La motivación, corregida

La v1 justificaba esta separación con un ataque de inferencia: matriz por umbral más scores públicos
reconstruye la etiqueta por banda. **Ese argumento es incorrecto para la fixture que existe.** En el
corpus demo `score ≥ 60 ⇔ isFraudLabel`; las 18 alertas abiertas son exactamente los 18 pedidos
etiquetados, y `SeedDemoOrdersResult.FraudLabelCount` ya publica un agregado de etiqueta desde la
Etapa 2. No hay nada que inferir: la etiqueta ya es pública de facto.

La separación se sostiene por **la forma que tendría el producto en producción**: la verdad de campo
no existe fuera de la demo. Un comercio no sabe qué pedidos eran fraude; sabe qué decidió su
analista y, con suerte, qué terminó en contracargo meses después. Un dashboard operativo que
dependiera de la etiqueta sería un dashboard que no puede existir. Por eso «fraude reportado» es el
veredicto de la analista y no la verdad de campo, y por eso la calidad del criterio es otra
superficie, condicionada a que el dataset sea de demostración.

### El rótulo obligatorio

La sección de calidad muestra, siempre y sin poder ocultarse:

> La fixture demo fue construida para que las reglas recuperen sus propias etiquetas. Estas métricas
> prueban el pipeline de evaluación —división temporal, holdout sin retuning, cálculo correcto—, no
> la calidad del criterio de detección.

Un F1 de 1,00 sin ese rótulo se lee como fixture sobreajustada. Declarado, se lee como criterio.

## D2 — La invariante se verifica invirtiendo las etiquetas

El test por reflexión que proponía la v1 es evadible por tres vías verificadas: existe un segundo
puerto de etiquetas, `IOrderDataStore.GetLabelsByOrderIdsAsync`; un manejador puede consumir a otro
que sí las lee; y sobre todo la costura real está en Infrastructure, donde cualquier `Ef*Store`
recibe `SalvoDbContext` completo y puede hacer `JOIN order_evaluation_labels` sin que ningún tipo de
Application lo delate. La reflexión sobre constructores no ve una consulta LINQ.

**El test que sí cierra la vía** es diferencial y de caja negra:

1. Seed y corrida.
2. Snapshot de `GET /api/dashboard`.
3. Invertir todas las `is_fraud_label` de la base.
4. Afirmar que la respuesta es **idéntica**.

No depende de saber por dónde entraría la fuga. Se complementa con un test de nombres de propiedad
sobre la respuesta, en la línea de `TheAlertContractNeverExposesGroundTruth`, y opcionalmente con un
`DbCommandInterceptor` que afirme que ningún comando emitido durante la petición menciona
`order_evaluation_labels`.

## D3 — La corrida de scoring es parte de la consola

El flujo del Blueprint §3 es importar → procesar → revisar → dashboard. El paso que produce
evaluaciones y alertas es `POST /api/risk-evaluations:run`, y **ninguna de las cuatro rutas de §4.4
lo invoca**. La v1 heredó esa omisión: la analista importa y el feed queda vacío para siempre.

Peor: con una corrida previa y una importación posterior, el dashboard presenta la corrida vieja
como si fuera el estado del corpus.

- `/import` gana la acción **«Ejecutar scoring»**, que invoca la corrida y presenta su resumen:
  secuencia, pedidos, evaluaciones creadas y reusadas, alertas abiertas y omitidas. Un conflicto por
  corridas concurrentes se mapea a `SCORING_RUN_CONFLICT` en la tabla de D13.
- `GET /api/dashboard` devuelve `scoringRun { sequence, completedAt, orderCount }` y
  `ordersPendingScoring`: pedidos sin fila en `run_evaluations` de la última corrida.
- La cabecera del dashboard y del feed muestra siempre de qué corrida es lo que se está viendo. Si
  `ordersPendingScoring > 0`, un aviso con enlace a `/import`.

**Hay tres estados vacíos, no dos**: sin pedidos, con pedidos pero sin corrida, y con corrida pero
sin alertas abiertas. Significan cosas distintas y se redactan distinto.

Esto exige **corregir §4.4 del Blueprint**, que no nombra dónde se dispara el scoring.

## D4 — Todo sobre la corrida vigente, siempre vía `run_evaluations`

La evaluación vigente de un pedido es la que referenció la última `ScoringRun` (decisión 31). El
dashboard usa esa definición para tasa de marcado, riesgo temporal y señales.

La regla de implementación es explícita: **nunca leer `risk_evaluations.status` a secas**. En la
Etapa 6 aparecerán filas `EXTERNAL_MOCK` con su propio estado, y cualquier consulta que no pase por
`run_evaluations` empezará a contarlas.

## D5 — Monto en riesgo por moneda, fraude reportado por pedido

`amountAtRisk` es una **lista** de `{ currencyCode, amountCents, alertCount }`, una entrada por
moneda presente entre las alertas abiertas. La UI muestra filas, nunca un total. Verificado en la
base: 6 alertas BRL, 6 USD y 6 UYU; la suma cruzada, 3.942.246, es una cifra sin unidad.

No se convierten divisas: exigiría fuente de tasas, fecha de referencia y una decisión sobre qué
tasa aplicar a un pedido de hace tres meses.

**El fraude reportado se agrega por `DISTINCT order_id`.** El predicado de escalada compara bandas y
no mira el veredicto anterior, así que un pedido reportado en `MEDIUM` puede recibir después una
alerta `CRITICAL` enlazada y volver a reportarse: dos alertas `REPORTED_FRAUD`, un solo pedido, un
solo importe. Contar alertas duplicaría el monto. Las alertas abiertas no tienen el problema, porque
el índice único parcial lo impide.

## D6 — El orden del feed es de la API

`GET /api/alerts` gana `sort` con `CREATED_DESC`, el actual y predeterminado, y `SCORE_DESC`. Valor
desconocido → `400` con `INVALID_SORT`.

Precisiones que el contrato debe declarar:

- `SCORE_DESC` ordena por el **score local de la evaluación vigente**, descendente, desempatando por
  `createdAt` descendente y luego por `id`. Llamarlo «score vigente» a secas quedará ambiguo en la
  Etapa 6, cuando existan evaluaciones externas.
- Un score nulo queda **al final** en `DESC` bajo la semántica de SQLite. Es lo correcto, pero se
  declara.
- El filtro `severity` opera sobre el **snapshot**, mientras el orden opera sobre la **vigente**. Son
  dos cosas distintas y la documentación del endpoint lo dice.

**Ningún orden es estable si una corrida ocurre entre dos páginas**, y eso ya pasa hoy con
`CREATED_DESC`: las alertas nuevas se insertan al principio y desplazan la página 2. Fijar la corrida
en el parámetro no arregla las inserciones. La solución:

- `ListAlertsResult` devuelve `scoringRunSequence`. Si cambia entre páginas, la UI avisa que el
  corpus cambió y recarga desde la primera.
- Para el volumen del MVP el feed pide **una sola página de hasta 200** (`MaximumPageSize`), con lo
  que la paginación no interviene. El `sort` queda en la API para cuando sí intervenga.

Consecuencia de implementación: el `JOIN` con `run_evaluations` debe estar **dentro de la consulta,
antes de `Skip/Take`**. Hoy `EfAlertStore.GetPageAsync` compone después de paginar. E5A modifica
`IAlertStore.GetPageAsync` y `ListAlertsHandler`, que son código de E4B; el brief lo reserva
explícitamente.

## D7 — Cliente `server-only`, tipado desde OpenAPI, guardas que proyectan

El patrón de `health.ts` **no sirve tal cual**: usa URL relativa, que resuelve por el rewrite de
`next.config.ts`, y ese rewrite solo existe para peticiones que llegan desde el navegador. En Node,
`fetch("/api/alerts")` lanza `TypeError`. Además `AGENTS.md` exige timeout explícito y `health.ts` no
fija ninguno.

El cliente de la Etapa 5:

- Es un módulo con `import "server-only"`. El navegador no llama a la API en esta etapa.
- Construye **URLs absolutas** desde `SALVO_API_BASE_URL`.
- Fija timeout con `AbortSignal.timeout(...)`.
- Lee `application/problem+json` y extrae `status`, `title`, `detail` y la extensión `code`.

**Los tipos se generan desde OpenAPI.** La decisión 14 de la bitácora dice que OpenAPI gobierna el
contrato; escribir interfaces a mano lo duplica y deriva en silencio. Se agrega `openapi-typescript`
como devDependency fijada y un paso de generación; `check.sh` verifica que el archivo generado esté
al día. Es la única dependencia nueva de la etapa y se aprueba aquí.

**Las guardas de tiempo de ejecución siguen existiendo y deben proyectar**: construir un objeto nuevo
con las claves conocidas, exigir las obligatorias e **ignorar las desconocidas**. La guarda de
`health.ts` es un predicado sobre el mismo objeto, que deja pasar todo lo demás.

La diferencia decide el futuro: la Etapa 7 agregará `explanation`, `recommendedAction` y
`explanationStatus` a `AlertDetail`. Con guardas que rechacen claves desconocidas, desplegar el
backend antes que la UI rompe la consola. Con guardas que las dejen pasar, el campo nuevo viaja al
navegador antes de que nadie decida mostrarlo. Solo la proyección explícita sirve para los dos casos.

## D8 — Las rutas de datos son dinámicas por declaración

Next.js 16.3.3, sin `cacheComponents`, prerenderiza en `next build` toda ruta que no use APIs de
tiempo de petición, y ejecuta sus `fetch` **una vez durante el build**. `/alerts`, `/dashboard` e
`/import` no leen `params`, `searchParams`, `cookies` ni `headers`: caen en ese caso. Y `check.sh`
corre `npm run build` sin API levantada.

Sin esto, o el build falla con `ECONNREFUSED`, o —si el código captura el error para pintar el estado
«con error»— Next congela ese error como HTML estático y lo sirve para siempre. `/alerts/[id]` usa
`params` y sí sería dinámica, lo que hace el fallo más difícil de notar: el detalle anda y el feed no.

Cada ruta de datos declara `export const dynamic = 'force-dynamic'`. **Criterio de aceptación
verificable**: el build pasa con `SALVO_API_BASE_URL` apuntando a un puerto cerrado, y la salida de
`next build` lista las cuatro rutas como dinámicas (`ƒ`), no como estáticas (`○`).

## D9 — Manejador de métricas propio, que nunca responde `500`

`EvaluateLocalRiskHandler` **no se engancha a HTTP**. Tres razones verificadas:

- Lanza si **algún** pedido no tiene etiqueta, y las importaciones nunca las escriben (decisión 36).
  `/import` invita a importar; en cuanto alguien lo hace, la sección de calidad daría `500`.
- `RiskMetricsEvaluator.SplitByTime` lanza con menos de dos cohortes temporales: base vacía o con un
  solo instante, `500`.
- **Re-puntúa el corpus vivo** en vez de leer la corrida vigente. Contradice D4: tras una importación
  sin corrida, el dashboard describiría la corrida N y la calidad el corpus N+1. Las dos superficies
  de la misma pantalla se contradirían.

E5A crea un manejador de métricas que parte de `GetCurrentEvaluationsAsync`, lee etiquetas solo de
los pedidos cubiertos, y **cuenta y declara** los pedidos sin etiqueta en `unlabeledOrders` en vez de
lanzar. Sin corrida, o con menos de dos cohortes, responde `409` con código `METRICS_UNAVAILABLE` y
un `detail` legible; la UI tiene ese estado. `EvaluateLocalRiskHandler` queda intacto, para tests.

## D10 — La UI descubre las capacidades en tiempo de ejecución

`DemoData:Enabled` es configuración del **backend en tiempo de ejecución**; el build de Next es
idéntico en ambos casos. Hoy la única señal para la UI sería un `404`, indistinguible de una ruta mal
escrita o de una API caída.

`GET /api/system/capabilities` devuelve `{ demoDataEnabled: boolean }`. Con `false`, `/import` oculta
«Cargar demo» y `/dashboard` oculta la sección de calidad **sin llamar** a `/api/evaluation-metrics`.
Tests con las dos configuraciones: hoy `SalvoApiFactory` fija `true`; falta la variante con `false`
que afirme `404` en seed y métricas.

## D11 — Sin estado optimista

El detalle se renderiza en el servidor; la revisión es una acción de servidor que revalida la ruta y
vuelve a leer el estado persistido.

Una revisión puede terminar en `409` por cuatro razones. Una UI optimista pinta el veredicto como
aplicado antes de saberlo, y en una consola antifraude eso significa que la analista sigue adelante
creyendo que actuó sobre un pedido que quedó sin decidir.

**La nota sobrevive al conflicto.** React 19 restablece los campos no controlados de un `<form
action>` al terminar la acción, incluso con error; sin mitigación, tras un `409` la analista pierde
lo que escribió. Se usan campos controlados, o se devuelven los valores enviados en el estado de la
acción y se aplican como `defaultValue`. Criterio de aceptación explícito.

## D12 — Divergencia, cambio de evaluación y la corrida que rotula

**Divergencia de banda**: aviso destacado que dice en palabras qué cambió, y casilla de
reconocimiento que habilita el envío. Espeja el `409` con `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED`; el
`409` se sigue manejando porque otra pestaña puede cambiar el estado entre el render y el envío.

**Cambio de evaluación sin cambio de banda**: `snapshot.evaluationId` y
`currentEvaluation.evaluationId` difieren y las señales cambiaron, pero la API no exige
reconocimiento. La UI muestra un aviso **secundario** —«la evaluación del pedido cambió (90 → 100)
sin cambiar de banda; las señales vigentes están abajo»— y **no** exige casilla. Ser más estricto que
la API crearía un `200` inalcanzable para cualquier otro cliente.

**El instante del bloque vigente**: `evaluatedAt` es la fecha en que la evaluación se insertó por
primera vez, no la de la corrida que la hizo vigente. En la base hay 3 corridas, 300 evaluaciones y
un solo `created_at_utc`. Mostrarlo como el instante de lo «actual» sería mostrar una fecha vieja
como si fuera de ahora.

E5A añade `currentRun { sequence, completedAt }` a `AlertDetail` y a `ListAlertsResult`;
`GetCurrentEvaluationsAsync` ya lee esa corrida y solo falta devolverla. La UI rotula «Vigente desde
la corrida #3, 2026-09-02 18:14» y, si conserva `evaluatedAt`, lo llama «calculada por primera vez
el…».

**`currentEvaluation` nulo**: es inalcanzable para una alerta existente, porque toda corrida puntúa
el corpus completo. Pero el contrato lo permite, así que la UI lo trata: el texto dice «no hay
evaluación vigente para este pedido», no «la evaluación vigente es 0», y la revisión procede sin
reconocimiento, como hace la API.

Snapshot y evaluación vigente se presentan como **dos bloques visualmente distintos**, cada uno con
su procedencia. Nunca se mezclan señales de uno con score del otro.

## D13 — Cada código tiene mensaje y recuperación

| `code` | HTTP | Qué le pasó | Qué ofrece la UI |
| --- | --- | --- | --- |
| `ALERT_NOT_FOUND` | 404 | La alerta no existe | Volver al feed |
| `INVALID_STATUS` | 400 | Veredicto no válido | Error de formulario |
| `NOTE_TOO_LONG` | 400 | Nota > 2000 caracteres | `maxLength` en el campo, y el mensaje igual |
| `ALERT_ALREADY_REVIEWED` | 409 | Otra persona emitió otro veredicto | Recargar y ver el vigente |
| `ALERT_REVIEW_NOTE_CONFLICT` | 409 | Mismo veredicto, otra nota | Recargar y ver la nota registrada |
| `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED` | 409 | El corpus cambió | Volver al aviso de divergencia |
| `ALERT_REVIEW_CONFLICT` | 409 | Dos revisiones simultáneas | Recargar |
| `SCORING_RUN_CONFLICT` | 409 | Corridas concurrentes | Reintentar la corrida |
| `METRICS_UNAVAILABLE` | 409 | Sin corrida o sin cohortes | Enlace a «Ejecutar scoring» |

Además, `AlertReviewResult.Applied = false` significa «ya estaba exactamente así»: la UI lo dice, no
«revisión registrada». Nunca se muestra «Error 409» ni el `detail` crudo como único texto.

## D14 — Los componentes cliente reciben primitivas

En React Server Components, **todas las props de un componente cliente se serializan enteras** dentro
del payload RSC del HTML. El formulario de revisión tiene que ser cliente. Si recibe `detail`
completo, el objeto entero viaja al navegador, incluidos campos que no se renderizan.

Hoy el test «el HTML no contiene `isFraudLabel`» pasaría **vacuamente**: no puede aparecer un texto
que ninguna fuente produce.

- Los componentes cliente reciben primitivas: `alertId`, `status`, `hasBandDivergence`, textos ya
  formateados. Nunca objetos de API.
- Se habilita `experimental.taint` y se marca la respuesta cruda en el módulo `server-only` con
  `experimental_taintObjectReference`.
- El test se reemplaza por uno que, con `fetch` simulado devolviendo un campo extra
  (`isFraudLabel: true`), afirma que no llega ni al render ni a las props serializadas.

Junto con la proyección de D7, esto es lo que hace que la frontera de lectura esté verificada y no
prometida.

## D15 — La verificación de recorrido es ejecutable

El Blueprint fija para la Etapa 5 «recorrido completo con datos, sin datos y con errores».
`check.sh` corre Vitest en jsdom y el build; componentes de servidor asíncronos, acciones de
servidor y `revalidatePath` no se ejercitan ahí sin simular medio framework.

`scripts/smoke-ui.sh`: levanta la API con `DemoData:Enabled=true` y `next start`, hace seed y
corrida, y comprueba con `curl` las cuatro rutas en tres escenarios —con datos, base vacía, API
apagada— buscando textos fijos. Es criterio de aceptación de E5C, no una aspiración. Playwright
quedaría como dependencia nueva y no se aprueba en esta etapa.

## D16 — El gráfico es SVG renderizado en el servidor

Diecisiete barras semanales. Recharts obligaría a que el componente sea de cliente, y entonces los
datos del dashboard viajarían completos en el payload RSC: exactamente la superficie que D14 cierra.
Con SVG de servidor esa superficie no existe para esa pantalla. El costo es perder los tooltips, que
con etiquetas visibles no se echan de menos.

El gráfico lleva `<title>` y `<desc>` y una tabla equivalente para lectores de pantalla.

Consecuencia: **se corrige la línea de `AGENTS.md` que lista Recharts como stack de UI** y se
registra en la bitácora.

## D17 — Población, cubeta, zona horaria y configuración regional

- **Señales principales**: se cuentan sobre los **snapshots de las alertas abiertas**, no sobre todas
  las evaluaciones vigentes. Contar todas incluiría las 16 apariciones de `foreign_country` a score
  20, que no alertan a nadie.
- **Riesgo temporal**: se agrupa por `occurredAt` —tiempo de negocio—, no por la fecha de la
  evaluación. Cubeta **semanal**; el corpus va del 2026-05-01 al 2026-08-28, unas 17 semanas.
- **Zona horaria**: `America/Montevideo`, la misma `RuleConfig.BusinessTimeZone` con la que las
  reglas leen el día. Usar otra haría que el dashboard y el motor discreparan sobre qué día es cada
  pedido.
- **Configuración regional fija** para montos e instantes, para que los tests no dependan de la
  máquina. Node 24.20.0 trae ICU completa.

## D18 — Partición

| Ítem | Alcance | Depende de |
| --- | --- | --- |
| `E5A-API-LECTURA` | `GET /api/dashboard`, `GET /api/evaluation-metrics`, `GET /api/system/capabilities`, `sort` en `/api/alerts` con `JOIN` antes de paginar, `currentRun` y `scoringRunSequence` en el contrato de alertas, manejador de métricas propio, test diferencial de etiquetas | — |
| `E5B-ALERTAS-UI` | Generación de tipos desde OpenAPI, cliente `server-only`, `/alerts`, `/alerts/[id]`, revisión, divergencia, códigos, rutas dinámicas | `E5A` integrada |
| `E5C-IMPORT-DASHBOARD` | `/import` con errores por fila y ejecución de scoring, `/dashboard`, gráfico SVG, sección de calidad, `smoke-ui.sh` | `E5A` y `E5B` integradas |

`E5A` reserva `IAlertStore.GetPageAsync` y `ListAlertsHandler`, que son código de E4B.

---

## Requisitos transversales de UI

- **Severidad con texto además de color**: `CRÍTICA`, `ALTA`, `MEDIA` escritas.
- **Cuatro estados por ruta**, y los **tres estados vacíos** de D3 redactados distinto.
- **Accesibilidad**: contraste, foco visible, encabezados de tabla asociados, avisos anunciados a
  lectores de pantalla, todo alcanzable por teclado.
- **Ningún campo de etiqueta** en ninguna ruta, verificado como dice D14.
- **Cabecera de procedencia**: toda pantalla de datos dice de qué corrida es lo que muestra.

## Qué queda fuera

- Autenticación e identidad de revisor; la limitación se declara en la UI y en la Etapa 8.
- Conversión de divisas.
- Enriquecer la fixture con casos duros. Se agenda para la Etapa 8; hasta entonces vale el rótulo
  de D1.
- Reabrir alertas revisadas.
- `IAntifraudProvider` y evaluación externa (Etapa 6); el detalle **reserva el lugar** de un bloque
  «evaluación externa» para no rediseñar la página.
- `explanation`, `recommendedAction` y `explanationStatus` (Etapa 7).
- Playwright y cualquier dependencia distinta de `openapi-typescript`.
- Actualizaciones en vivo, WebSockets o sondeo automático.

## Cambios de estado canónico que exige este diseño

1. **Blueprint §4.4**: agregar que `/import` dispara la corrida de scoring y que las pantallas de
   datos declaran su procedencia. La omisión originó el hallazgo 1.
2. **`AGENTS.md`**: corregir la línea que lista Recharts como stack de UI.
3. **Bitácora del Blueprint**, entradas 37 a 43:
   - 37 — El dashboard operativo no lee la etiqueta; la calidad del criterio es otra superficie tras
     bandera, porque la verdad de campo no existe en producción.
   - 38 — La invariante anterior se verifica invirtiendo las etiquetas y exigiendo idéntica
     respuesta, no por reflexión sobre constructores.
   - 39 — La corrida de scoring se dispara desde la consola y toda pantalla de datos declara su
     procedencia.
   - 40 — Los tipos del cliente se generan desde OpenAPI y las guardas proyectan en vez de comprobar.
   - 41 — Las rutas de datos son dinámicas por declaración y el build debe pasar sin API.
   - 42 — Los componentes cliente reciben primitivas; ningún objeto de API cruza la frontera.
   - 43 — El gráfico del dashboard es SVG de servidor; Recharts sale del stack.
