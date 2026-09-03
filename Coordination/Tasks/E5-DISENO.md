# Salvo — Diseño de la Etapa 5: UI y dashboard

> Estado: propuesta v1, pendiente de revisión adversarial y de aprobación del usuario
> Fecha: 2026-09-03
> Base: `main` en `df26f00`
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §4.4 y «Etapa 5 — UI y dashboard»

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | Dos superficies de métricas: dashboard operativo sin etiqueta, y métricas de calidad derivadas de la etiqueta, separadas y con bandera | Hallazgo 1 |
| D2 | Cualquier métrica por umbral es un artefacto derivado de la etiqueta; se declara en vez de fingir que se sanea | Hallazgo 1 |
| D3 | El dashboard se calcula sobre la corrida vigente, no sobre «todas las evaluaciones» | Coherencia con D3 de la Etapa 4 |
| D4 | `amountAtRisk` se desglosa por moneda y nunca se totaliza | Hallazgo 2 |
| D5 | El feed ordena por score vigente, lo que exige `sort` en la API antes que UI | Hallazgo 3 |
| D6 | Cliente de API tipado y validado en tiempo de ejecución, siguiendo el patrón de `health.ts` | Extensión de lo existente |
| D7 | La revisión se renderiza y se envía desde el servidor; sin estado optimista | v1 |
| D8 | La divergencia bloquea el envío y se explica en palabras | Continuidad de E4B |
| D9 | Cada código de conflicto tiene su mensaje y su acción de recuperación | v1 |
| D10 | Partición en `E5A-API-LECTURA`, `E5B-ALERTAS-UI` y `E5C-IMPORT-DASHBOARD` | v1 |

---

## Los tres hallazgos

### Hallazgo 1 — El dashboard puede filtrar la etiqueta por agregación

`EvaluateLocalRiskHandler` está registrado en el contenedor de dependencias y cubierto por tests,
pero **no está expuesto en ninguna ruta HTTP**. Todo el trabajo de métricas de la Etapa 3 —precisión,
recall, F1, tasa de falsos positivos, barrido de umbral, división temporal con holdout— es hoy
invisible desde afuera. La tentación obvia al construir `/dashboard` es engancharlo y listo.

El problema: esas métricas se calculan **a partir de `isFraudLabel`**. Todo el proyecto sostiene que
la etiqueta vive en una entidad separada, fuera del contrato público, y hay un test por reflexión
—`ScoringBoundaryCannotReceiveGroundTruthLabels`— que verifica que el motor de scoring no pueda
recibirla. Ese test protege la **frontera de escritura**. Nada protege hoy la **frontera de lectura**.

Y no es teórico. El score de cada pedido ya es público: `GET /api/alerts` devuelve
`riskScoreSnapshot` por alerta. Si además se expone la matriz de confusión en cada umbral del
barrido, la diferencia entre dos umbrales consecutivos revela **cuántos pedidos de esa banda de
score son fraude**. Con una banda que contenga un solo pedido, su etiqueta queda revelada exacta.

Esto no se arregla exponiendo precisión en vez de la matriz. Conociendo los scores se conoce
`TP + FP` en cada umbral; con la precisión se despeja `TP`. Con recall se despeja igual. **Cualquier
métrica por umbral reconstruye la distribución de etiquetas por banda de score.**

### Hallazgo 2 — `amountAtRisk` como número único es falso

El corpus demo tiene tres monedas repartidas en partes iguales: 100 pedidos en UYU, 100 en USD y
100 en BRL. Las 18 alertas abiertas se reparten 6, 6 y 6.

Sumar `amount_cents` a través de las tres da 3.942.246 **de nada**. No es una imprecisión: es una
cifra sin unidad, presentada como si fuera dinero, en el panel principal de una consola antifraude.
Cualquiera que mire el dashboard con un ojo de fintech lo ve en dos segundos.

### Hallazgo 3 — El feed que pide el Blueprint no es el que devuelve la API

El Blueprint §4.4 pide «feed de alertas abiertas **ordenadas por score**». `GET /api/alerts` ordena
por `createdAt` descendente y luego por `id`, decisión deliberada de E4B para que la paginación sea
estable. Ordenar en el cliente rompe con la paginación: la página 1 traería las 20 alertas más
recientes, no las 20 de mayor score.

Es un cambio de API, no de UI. Si se descubre a mitad de `E5B` obliga a volver al backend con el
frontend a medio hacer.

---

## D1 — Dos superficies de métricas, separadas por construcción

**El dashboard operativo nunca lee `OrderEvaluationLabel`.** Todo lo que pide el Blueprint para
`/dashboard` sale de alertas y evaluaciones, sin ninguna etiqueta:

| Métrica | De dónde sale |
| --- | --- |
| Alertas abiertas | `alerts WHERE status = 'OPEN'`, por severidad derivada |
| Monto en riesgo | Suma de `orders.amount_cents` de esas alertas, por moneda |
| Fraude reportado | `alerts WHERE status = 'REPORTED_FRAUD'` — **el veredicto de la analista**, no la verdad de campo |
| Tasa de marcado | Evaluaciones `DENIED` sobre total, en la corrida vigente |
| Riesgo temporal | Score de la evaluación vigente agrupado por período |
| Señales principales | Conteo de reglas sobre `signalsJson` de las evaluaciones vigentes |

Esa distinción es real, no cosmética: **en producción no existe la verdad de campo**. Un comercio no
sabe qué pedidos eran fraude; sabe qué decidió su analista y, con suerte, qué terminó en contracargo
meses después. Un dashboard operativo que dependiera de la etiqueta sería un dashboard que no puede
existir fuera de una demo.

**Las métricas de calidad viven aparte**, en `GET /api/evaluation-metrics`, condicionado a la misma
bandera `DemoData:Enabled` que ya gobierna `POST /api/demo-data/seed`. En la UI se presentan en una
sección aparte, rotulada como lo que son: calidad del criterio medida contra etiquetas sintéticas,
disponible solo porque el dataset es de demostración.

La invariante se verifica, no se promete: un test de arquitectura por reflexión afirma que el
manejador del dashboard **no puede depender de `IEvaluationLabelReader`**, en el mismo espíritu que
`ScoringBoundaryCannotReceiveGroundTruthLabels`. Si alguien enchufa las métricas al dashboard por
comodidad, la compuerta se pone roja.

## D2 — El barrido se declara, no se disfraza

Dado el hallazgo 1, sanear el barrido es imposible sin volverlo inútil. La decisión es honesta:

- El barrido completo **se expone** en `/api/evaluation-metrics`. Es valioso para la demo y para
  explicar la calibración en una entrevista.
- Se expone **solo bajo `DemoData:Enabled`**, junto con el resto de las métricas derivadas de la
  etiqueta.
- El diseño y la respuesta documentan que es un artefacto derivado de la etiqueta y que **no
  existiría en una consola de producción**.

La defensa no es ofuscar un número: es que la superficie operativa y la superficie de evaluación
sean dos endpoints distintos, con dos manejadores distintos, uno de los cuales no puede alcanzar la
etiqueta ni queriendo.

## D3 — Todo se calcula sobre la corrida vigente

La Etapa 4 definió que **la evaluación vigente de un pedido es la que referenció la última
`ScoringRun`**. El dashboard usa esa misma definición para tasa de marcado, riesgo temporal y
señales principales.

Sin esta regla el dashboard contradiría al feed: promediar «todas las evaluaciones» de
`risk_evaluations` cuenta las históricas de un pedido que rebotó de score, y da una tasa de marcado
que no coincide con las alertas que la analista tiene en pantalla.

## D4 — El monto en riesgo se desglosa por moneda

`amountAtRisk` es **una lista**, no un escalar: un par `{ currencyCode, amountCents, alertCount }`
por moneda presente entre las alertas abiertas. La UI las muestra como filas, no como total.

No se introduce conversión de divisas. Convertir exigiría una fuente de tasas, una fecha de
referencia y una decisión sobre qué tasa usar para un pedido de hace tres meses; nada de eso aporta
al MVP y todo puede estar mal. Declarar «no convertimos» es más defendible que una cifra única con
una tasa inventada.

Lo mismo aplica al fraude reportado, que se presenta desglosado y **separado** del monto en riesgo,
según §4.3 del Blueprint.

## D5 — El orden del feed es una decisión de API

`GET /api/alerts` gana un parámetro `sort` con dos valores: `CREATED_DESC`, el actual y el
predeterminado, y `SCORE_DESC`, que ordena por score de la evaluación vigente descendente y
desempata por `createdAt` descendente y luego por `id`, para que la paginación siga siendo estable y
determinista.

El feed usa `SCORE_DESC`. Un valor desconocido devuelve `400` con código `INVALID_SORT`, igual que
los otros parámetros.

Ordenar por score **vigente** y no por el snapshot es deliberado: la analista quiere atender primero
lo que hoy es más riesgoso, no lo que era más riesgoso cuando se abrió la alerta.

## D6 — El cliente de API valida en el borde

`frontend/src/lib/api/health.ts` ya fijó el patrón: interfaz explícita más una guarda de tipo en
tiempo de ejecución que lanza si el contrato no coincide. La Etapa 5 lo extiende a todos los
recursos en lugar de abandonarlo apenas crece el número de campos.

TypeScript estricto no valida nada en tiempo de ejecución. Sin guardas, un cambio de contrato en el
backend se manifiesta como `undefined` renderizado en pantalla en vez de un error claro. Con guardas
se manifiesta donde ocurre.

Se agrega un lector de `application/problem+json` que extrae `status`, `title`, `detail` y la
extensión `code`, porque toda la API la devuelve y la UI necesita el `code` para D9.

Sin dependencias nuevas: `fetch` y guardas escritas a mano. Se consulta antes de agregar cualquier
biblioteca de datos o de validación.

## D7 — La revisión se envía desde el servidor y no se anticipa

El detalle de alerta se renderiza en el servidor. La revisión es una acción de servidor que, al
volver, revalida la ruta y vuelve a leer el estado persistido.

**Sin estado optimista.** Una revisión puede terminar en `409` por cuatro razones distintas, y una
UI optimista pinta el veredicto como aplicado antes de saberlo. En una consola antifraude, mostrar
«marcado como fraude» sobre una alerta que no se marcó es el peor error posible de esta pantalla:
la analista sigue adelante creyendo que actuó.

Consecuencia deliberada: la interacción es un poco más lenta y bastante más honesta.

## D8 — La divergencia bloquea y se explica

Cuando `divergence.hasBandDivergence` es verdadero, el detalle muestra un aviso destacado —no una
nota al pie— que dice en palabras qué cambió: *«Esta alerta se abrió con score 60 (MEDIA). La
evaluación vigente del pedido es 0, por debajo del umbral. El texto de las señales de abajo describe
una situación que ya no es la actual.»*

El botón de veredicto queda deshabilitado hasta que se marque una casilla de reconocimiento. Es el
espejo exacto del `409` con código `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED` que devuelve la API, para que
el cliente no envíe una petición que ya sabe que va a fallar. El `409` se sigue manejando igual: otra
pestaña, u otra persona, pueden haber cambiado el estado entre el renderizado y el envío.

El snapshot y la evaluación vigente se presentan como **dos bloques visualmente distintos**, cada
uno con su instante. Nunca se mezclan señales de uno con score del otro.

## D9 — Cada conflicto tiene su mensaje

| `code` | Qué le pasó a la analista | Qué ofrece la UI |
| --- | --- | --- |
| `ALERT_ALREADY_REVIEWED` | Otra persona ya emitió un veredicto distinto | Recargar y ver el veredicto vigente |
| `ALERT_REVIEW_NOTE_CONFLICT` | El mismo veredicto ya existe con otra nota | Recargar y ver la nota registrada |
| `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED` | El corpus cambió desde que se abrió la alerta | Volver al aviso de divergencia |
| `ALERT_REVIEW_CONFLICT` | Dos revisiones simultáneas; ésta perdió | Recargar |
| `ALERT_NOT_FOUND` | La alerta no existe | Volver al feed |

Nunca se muestra «Error 409» ni el `detail` crudo del backend como único texto. El `detail` puede
mostrarse como información secundaria; el mensaje principal es de la UI, en español y accionable.

## D10 — Partición en tres ítems

| Ítem | Alcance | Depende de |
| --- | --- | --- |
| `E5A-API-LECTURA` | `GET /api/dashboard`, `GET /api/evaluation-metrics`, `sort` en `GET /api/alerts`, test de arquitectura del dashboard | — |
| `E5B-ALERTAS-UI` | Cliente de API tipado, `/alerts`, `/alerts/[id]`, revisión, divergencia, conflictos | `E5A` integrada |
| `E5C-IMPORT-DASHBOARD` | `/import` con errores por fila, `/dashboard`, sección de calidad | `E5A` y `E5B` integradas |

`E5A` es backend puro. Mezclarlo con React haría el diff irrevisable y esconde que la decisión de
fondo de esta etapa —D1— es de backend.

`E5B` es el corazón: es donde se juega si el trabajo de divergencia y concurrencia de E4B se
presenta con honestidad o se tira por la borda.

`E5C` reutiliza el cliente de `E5B` y consume los dos endpoints de `E5A`.

---

## Requisitos transversales de UI

Aplican a las cuatro rutas y son criterios de aceptación, no aspiraciones:

- **Severidad con texto además de color.** `CRÍTICA`, `ALTA`, `MEDIA` escritas. El color acompaña,
  nunca informa solo. Requisito explícito del Blueprint §4.4.
- **Cuatro estados por ruta**: con datos, cargando, vacío y con error. El estado vacío distingue
  «todavía no importaste nada» de «no hay alertas abiertas», que significan cosas opuestas.
- **Accesibilidad**: contraste suficiente, foco visible, tablas con encabezados asociados, el aviso
  de divergencia anunciado a lectores de pantalla, y todos los controles alcanzables por teclado.
- **Ningún campo de etiqueta**, en ninguna ruta, ni en el marcado renderizado ni en payloads
  embebidos. Test que verifica que el HTML servido no contiene `isFraudLabel`.
- **Formato dependiente de cultura fijado explícitamente.** Montos e instantes se formatean con
  configuración regional determinista para que los tests no dependan de la máquina.

## Qué queda fuera de la Etapa 5

- Autenticación e identidad de revisor. La limitación de `AlertReview` sin revisor se declara en la
  UI y se documenta en la Etapa 8.
- Conversión de divisas.
- Reabrir alertas revisadas.
- `IAntifraudProvider` y evaluación externa. Etapa 6.
- `explanation`, `recommendedAction` y `explanationStatus`. Etapa 7.
- Modificar el motor, `RuleConfig`, el fingerprint, las migraciones existentes o la semántica de
  alertas de E4B.
- Actualizaciones en vivo, WebSockets o sondeo automático del feed.

## Preguntas abiertas para la revisión adversarial

1. ¿El test de arquitectura del dashboard puede evadirse con una dependencia indirecta —por ejemplo
   un manejador que consuma otro que sí lee etiquetas— y cómo se cierra esa vía?
2. ¿`SCORE_DESC` sobre score vigente es estable bajo paginación si una corrida se ejecuta entre la
   página 1 y la página 2? ¿Hace falta fijar la corrida en el parámetro?
3. ¿Hay alguna forma de que la sección de calidad, aun bajo bandera, aparezca en un build donde
   `DemoData:Enabled` sea falso?
4. ¿El renderizado del servidor introduce alguna vía por la que la respuesta de la API completa
   —incluidos campos no proyectados— llegue al HTML como estado serializado?
5. ¿Qué pasa en `/alerts/[id]` cuando la alerta existe pero el pedido fue importado sin evaluación
   vigente, y `currentEvaluation` es nulo?
