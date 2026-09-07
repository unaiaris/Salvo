# Salvo — Task brief `E9B-SENALES-TIPADAS`

## Identificación

- Work ID: `E9B-SENALES-TIPADAS`
- Etapa: 9
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-07
- Rama/worktree: `claude/e9b-senales-tipadas`
- Commit base: `5776232`, el `merge-base` real de `claude/e9b-senales-tipadas` con `main`.
  **Esta línea se commitea en la rama, no en `main`**: un commit que declara la base y va a `main`
  pasa a ser la base y vuelve falso el campo que acaba de escribir. Es la lección de `E8B`.
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E9A-FIXTURE` integrada (merge `41343c1`). `E9C` y `E9D` dependen de esta.
- Integración: **por merge, nunca por rebase**. `main` avanzó a `16d80aa` después de crear la rama;
  rebasar sobre esa punta movería el `merge-base` y volvería falso el commit base declarado arriba.
  Es la lección de `E8B`, y esta rama es el caso donde se aplica.

## Resultado esperado

El motor deja de escribir una frase inglesa por señal y pasa a escribir **los campos** de esa señal.
La prosa deja de ser el formato de intercambio entre el motor y todo lo que lo lee, y el extractor
de expresiones regulares que hoy tapa ese hueco **se borra**, después de haber certificado a su
reemplazo sobre las evaluaciones reales del corpus v2.

Es el cierre de una costura que se plantó a propósito en la Etapa 7 y que hasta hoy nunca se pudo
verificar de verdad: sobre el corpus v1 solo tres de las seis reglas disparaban.

## Contexto obligatorio

- `Coordination/Tasks/E9-DISENO.md` (**v2**), decisiones **D1 y D5**, y el párrafo de D11 que dice
  por qué esta tarea va segunda y no primera.
- `Coordination/Tasks/E9-revision-adversarial.md`, hallazgos **4 y 5**. El 4 enumera los cuatro
  lugares donde «el extractor se borra y todo lo demás queda intacto» es falso; el 5 es el que
  invirtió el orden de la etapa.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 9 — Corpus, idiomas y cierre», el ítem
  «Señales estructuradas (`e3-v2`), con `detail` conservado como campo heredado» (línea 276): es el
  único que esta tarea cierra.
- `Coordination/Handoffs/Claude.md`, entradas de `E7A`, `E7D` y `E9A`. La de `E7A` es la lección de
  la recaptura del contrato; la de `E7D` es «un arreglo que no alcanza a lo ya guardado no está
  terminado», que en esta tarea vuelve a aparecer con otra cara.
- `DesignAgent/Salvo-Blueprint.md`: §4.2, §6 y las decisiones **33** (un snapshot no se reescribe
  nunca), **57** (los códigos de la consola se escriben a mano y tienen su test de exactitud) y
  **58** con su contrapositiva (una versión de plantilla no sube si el texto no cambia).
- Código, abierto antes de escribir nada:
  - `backend/src/Salvo.Domain/Risk/RiskSignal.cs`,
    `backend/src/Salvo.Domain/Risk/RiskSignalSerializer.cs`,
    `backend/src/Salvo.Domain/Risk/RiskEvaluationFingerprint.cs`,
    `backend/src/Salvo.Domain/Risk/RuleConfig.cs`,
    `backend/src/Salvo.Domain/Risk/TemporalRiskEngine.cs`.
  - `backend/src/Salvo.Domain/Explanations/SignalFacts.cs`,
    `backend/src/Salvo.Domain/Explanations/ExplanationFacts.cs`,
    `backend/src/Salvo.Domain/Explanations/NumberTokenizer.cs`,
    `backend/src/Salvo.Domain/Explanations/SignalDetailNotRecognizedException.cs`.
  - `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`.
  - `backend/src/Salvo.Application/Explanations/RequestExplanationHandler.cs`,
    `backend/src/Salvo.Application/Alerts/AlertViews.cs`,
    `backend/src/Salvo.Application/Alerts/AlertProjection.cs`.
  - `frontend/src/lib/api/guards.ts`, `frontend/src/lib/api/contract.ts`,
    `frontend/src/app/alerts/[id]/evaluation-blocks.tsx`,
    `frontend/src/app/alerts/[id]/divergence.ts`, `frontend/src/lib/format.ts`.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Una regla de dominio que el coordinador corrigió antes de despachar

`AGENTS.md` decía «Cada señal incluye un `detail` legible», y el Blueprint lo repetía en §4.2 y lo
daba por hecho en §4.7. Esta tarea la contradice de frente, así que la regla se corrigió **antes**
de despachar y no durante la ejecución: **toda señal se lee sin intérprete —antes por su `detail`,
ahora por sus campos con nombre—**, y `ExplanationFacts` se construye desde los campos de cada
señal, no desde los números de su prosa.

La corrección viene con dientes, y son parte de esta tarea: **un test compone la frase de cada una
de las seis reglas desde la fila almacenada**, sin nada más que la fila. Una regla que dice
«legible» y no tiene test es una intención; ésta se puede romper.

## Alcance

### Dentro

#### 1. El oráculo, y va primero

`SignalFacts` existe exactamente para leer la prosa que el motor escribe. Con el corpus v2 sembrado
y el motor **quieto** en `e3-v1`, la base tiene evaluaciones reales de las seis reglas, y eso lo
vuelve el único oráculo capaz de certificar el cambio de motor sin frases inventadas en un test.

**El extractor se extiende antes de capturar, y esto va primero de todo.** Tres de los seis patrones ya
capturan grupos que `SignalFacts` descarta: `amount`, `currency` y `median` en `amount_anomaly` y en
`new_buyer_high_value`, y `zone` en `unusual_hour`. Tal como está, el dorado **no podría certificar**
`amountCents`, `currencyCode`, `medianCents` ni `timeZoneId` —cuatro columnas de la tabla del punto
2, y `medianCents` es justamente la que el punto 5 necesita—: el oráculo cubriría un subconjunto y
las cuatro columnas nuevas entrarían al fingerprint sin que nada las haya verificado nunca. Así que
**en un commit anterior al de la captura**, `SignalFacts` gana esos cuatro campos leyéndolos de los
grupos que ya existen, con su test. Es trabajo que muere con el extractor, y es el precio de que el
oráculo cubra la tabla entera.

La captura dorada:

- Se genera corriendo el motor `e3-v1` sobre el corpus v2 —el mismo camino que usa
  `RiskEvaluationTests`— y pasando cada señal por `SignalFacts.Parse`.
- Se escribe en `backend/tests/Salvo.Api.IntegrationTests/Goldens/signal-facts.v2.json`, con una
  entrada por evaluación con señales: la referencia del pedido, el score, y por cada señal la regla,
  el peso, **la prosa `e3-v1` tal cual** y los campos tipados que el extractor leyó de ella.
- **Se commitea antes de tocar el motor.** Es la falsación de esta tarea, con la misma forma que la
  tabla de arquetipos de `E9A`: la predicción precede a la medición, y se puede demostrar que
  precede mirando el orden de los commits.
- El test que la usa **falla si el archivo no está o difiere**. Nunca lo regenera: un dorado que se
  reescribe solo no afirma nada. Regenerarlo es una edición deliberada, con su commit y su motivo.

Después del cambio de motor, el test permanente afirma que **para las 300 evaluaciones, los campos
que `e3-v2` emite son iguales campo a campo a los que el extractor leyó de la prosa `e3-v1`**, y que
el score y el conjunto de reglas no se movieron. El extractor ya no hace falta para eso: el dorado
es un archivo.

#### 2. `e3-v2`: los campos, con su precisión canónica declarada acá

`RiskSignal` pasa de `(Rule, Weight, Detail)` a la forma **plana y nullable por regla** que
`SignalFacts` ya tiene, con `Detail` **opcional y heredado**. Nada de polimorfismo: la forma en el
cable de una señal es la misma para las seis reglas, con los campos que no le corresponden en nulo.

**La precisión de cada campo es parte de la identidad de toda evaluación para siempre**, porque el
fingerprint hashea la cadena canónica tal cual —`RiskEvaluationFingerprint` es
`SHA-256(orderId | source | ruleConfigVersion | score | signalsCanonical)`— y un `decimal` arrastra
la escala de la división que lo produjo: `201111 / 8685` no es `23.2` hasta que alguien lo redondea.
Por eso se declara acá y no se descubre en el dorado:

| Regla | Campo | Tipo | Escala canónica |
| --- | --- | --- | --- |
| `amount_anomaly` | `amountCents` | entero | — |
| | `currencyCode` | texto | — |
| | `ratio` | decimal | **1**, fija |
| | `scope` | `buyer` \| `merchant` | — |
| | `medianCents` | entero | — |
| | `historyCount` | entero | — |
| | `windowDays` | entero | — |
| `velocity` | `orderCount`, `windowMinutes`, `threshold` | entero | — |
| `cross_border_velocity` | `fromCountry`, `toCountry` | texto | — |
| | `elapsedMinutes` | decimal | **2**, fija |
| `unusual_hour` | `bucketStartHour`, `bucketEndHour` | entero | — |
| | `timeZoneId` | texto | — |
| | `observedCount`, `totalCount` | entero | — |
| | `sharePercent` | decimal | **1**, fija |
| `new_buyer_high_value` | `amountCents`, `currencyCode`, `medianCents`, `historyCount` | — | — |
| | `ratio` | decimal | **1**, fija |
| `foreign_country` | `country`, `habitualCountry` | texto | — |
| | `observedCount`, `totalCount` | entero | — |
| | `sharePercent` | decimal | **1**, fija |

Hay que separar dos cosas que es fácil confundir, y confundirlas rompe el dorado:

**El redondeo se hereda de la prosa, modo incluido.** `ratio` y `sharePercent` a un decimal,
`elapsedMinutes` a dos, que es lo que `{ratio:0.0}`, `{share:0.0}` y `{elapsed:0.##}` ya hacían. El
**modo** se declara y es `MidpointRounding.AwayFromZero`: el formato `0.0` de .NET lleva `2.25` a
`2.3`, mientras que `decimal.Round(2.25m, 1)` sin modo —que usa `ToEven`— lo lleva a `2.2`. Copiar
la escala y olvidar el modo haría diferir el dorado exactamente en los empates, que son los casos
que nadie mira. Esto es lo que vuelve a `e3-v2` un cambio de **representación y nada más**, y por
lo tanto lo que hace que la igualdad del punto 1 sea exacta y que los textos dorados de
`ExplanationGoldenTests` salgan **byte por byte idénticos**.

**La escala del texto canónico no se hereda de nada: es una declaración nueva.** Dos creencias
cómodas y falsas, las dos verificadas contra el runtime:

- `{elapsed:0.##}` **no** fija dos decimales. `#` omite los ceros finales, así que el motor viene
  escribiendo `90`, no `90.00`.
- `decimal.Round(4m, 1)` devuelve `4`, no `4.0`. `decimal.Round` redondea; no fija escala.

Por eso la cadena canónica **escribe cada decimal con su cantidad de decimales fija, como texto**:
el valor se calcula con `Math.Round(valor, n, MidpointRounding.AwayFromZero)` y se serializa con
`ToString("F<n>", CultureInfo.InvariantCulture)` a través de `Utf8JsonWriter.WriteRawValue`, de modo
que `4.0` se escriba `4.0` y `90.00` se escriba `90.00`. Guardar la precisión cruda sería «más
información» y a cambio dejaría la identidad de cada evaluación colgando de la escala que arrastre
la división que produjo el número. Esa escala fija mueve el fingerprint, que se mueve igual.

**Antes de escribir una línea del motor, la tarea verifica los formatos con un programa mínimo** y
pega la salida en la entrega: `0.0`, `0.##`, `decimal.Round` sin modo, y `Math.Round` con
`AwayFromZero` seguido de `F1` y `F2`. Estas afirmaciones ya fueron falsas una vez en este mismo
brief; no se heredan de memoria.

**`elapsedMinutes` es el único campo que el corpus no puede certificar, y por eso lleva prueba
aparte.** Nace como `double` —el motor formatea `elapsed.TotalMinutes`, no un `decimal`—, así que
`e3-v2` tiene que convertir antes de redondear, y esa conversión no la cubre el dorado: **los 300
instantes de `demo-orders.v2.json` son minutos enteros**, sin segundos, de modo que
`elapsed.TotalMinutes` siempre da un entero y `{elapsed:0.##}` nunca escribió un decimal sobre este
corpus. `ratio` y `sharePercent` sí tienen decimales reales en el corpus y quedan cubiertos por el
diferencial; éste no. Entonces el campo lleva **su propia prueba unitaria sobre `TimeSpan`**, fuera
del corpus, con al menos un valor de fracción periódica —100 segundos son `1.6666...` minutos— y un
empate en el tercer decimal, comparando el camino nuevo contra lo que `{0.##}` escribe sobre el
mismo `double`.

**El orden de los campos en la cadena canónica se declara y no se cambia nunca**: `rule`, `weight`,
después los campos de la tabla en el orden en que están escritos ahí, omitiendo los nulos, y
`detail` al final cuando existe —lo que solo pasa en filas `e3-v1` leídas de vuelta, porque el motor
no lo escribe nunca más—. `RiskSignalSerializer` lo documenta como lo documenta hoy.

`RuleConfig` gana `E3V2` con `Version = "e3-v2"` y **los mismos umbrales**: lo único que cambia es
cómo se escribe la señal. `RuleConfig.ForVersion(string)` resuelve una versión a su configuración y
falla ante una desconocida.

#### 3. La corrección de arrastre: la configuración de la fila, no la fija

`RuleConfig.E3V1` está clavado en cinco lugares —línea 174 de
`backend/src/Salvo.Application/Explanations/RequestExplanationHandler.cs`, línea 60 de
`backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`, línea 13 de
`backend/src/Salvo.Application/Risk/EvaluateLocalRiskHandler.cs`, línea 27 de
`backend/src/Salvo.Application/Risk/RunScoringHandler.cs` y línea 172 de
`backend/src/Salvo.Application/Dashboard/GetDashboardHandler.cs`— más la línea 20 de
`backend/src/Salvo.Api/Program.cs`. Hoy es inocuo porque hay una sola versión; con
dos vivas deja de serlo, y de las peores maneras: explicar una evaluación `e3-v1` con los umbrales
de otra versión produce un párrafo correcto sobre la evaluación equivocada.

- Los que **escriben** evaluaciones nuevas —`EvaluateLocalRiskHandler`, `RunScoringHandler`— usan la
  versión vigente.
- Los que **leen** una fila —`RequestExplanationHandler`, el proveedor— resuelven la configuración
  **por la versión de esa fila**.
- `backend/src/Salvo.Api/Program.cs` valida `AlertPolicy.E4V1` contra **todas** las configuraciones
  conocidas, no contra una.

#### 4. Lo que pasa con las filas `e3-v1` que ya están escritas

Un snapshot de alerta no se reescribe nunca (decisión 33), así que una base que cruza el cambio de
versión conserva alertas cuyo snapshot es prosa. Esto no se puede tapar y se resuelve diciéndolo:

- **La consola compone la frase desde los campos cuando están, y muestra `detail` cuando no.**
  `SignalList`, en `frontend/src/app/alerts/[id]/evaluation-blocks.tsx`, deja de pintar la prosa
  inglesa del motor y pasa a componer
  en castellano desde los campos; para una fila `e3-v1` cae al `detail` heredado. Un test lee una
  fila `e3-v1` **real** —la del dorado del punto 1, no una escrita a mano— por el camino nuevo.
- **Pedir una explicación de una evaluación `e3-v1` falla con un código, no con una excepción.**
  `RequestExplanationHandler` ya tiene el camino: atrapa `SignalDetailNotRecognizedException` y
  asienta un fallo con nombre en vez de reventar. Se conserva y se extiende a «esta fila no tiene
  campos y este build no lee prosa», con su test sobre una fila `e3-v1` real.

Esto **no** es el defecto de `E7D` otra vez, y la diferencia importa: allá el arreglo no llegaba a lo
ya guardado **y nadie lo decía**. Acá el límite es una consecuencia de la decisión 33, está dicho,
tiene test, y en una base recién sembrada —que es la que un revisor corre— no existe.

#### 5. `ExplanationFacts` se enumera a mano, y hay un test por campo

Hoy el conjunto de hechos se llena con `signal.Numbers`, que salen del tokenizador **sobre la
prosa**. Sin prosa no hay tokens, y el conjunto se vacía sin que nada se ponga rojo: la plantilla
determinista escribe solo cifras que también llegan por otro lado, así que los dorados seguirían
verdes y el `NOT_GROUNDED_NUMBER` empezaría a rechazar texto correcto **de un modelo real**. Es
exactamente el defecto de la Etapa 7, con el signo dado vuelta.

Entonces:

- `SignalFacts.Numbers` se construye **enumerando los campos numéricos** de la tabla del punto 2,
  cada uno con su escala declarada.
- **Todo campo en centavos aporta además sus lecturas en unidades**, igual que
  `ExplanationFacts` hace hoy con `input.AmountCents`. Sin eso, «la mediana fue 86,85 BRL» es una
  cifra verdadera que el verificador rechaza.
- `medianCents` **entra como campo**. Hoy la mediana vive solo en la prosa y el conjunto la tiene
  por el tokenizador; si se pierde, un modelo que la escriba es rechazado y **ningún dorado lo
  nota**, porque la plantilla no la escribe.
- Un test afirma, para cada regla y cada campo numérico, que el valor del campo está en
  `ExplanationFacts`. Otro afirma que **el conjunto construido desde los campos es igual al
  construido desde la prosa** para las mismas evaluaciones del dorado. Ese segundo es el oráculo
  de D1 aplicado al conjunto de hechos.

#### 6. El contrato, recapturado entero

`detail` es contrato, no prosa: viaja en `AlertSignalView`
(`backend/src/Salvo.Application/Alerts/AlertViews.cs`, línea 5), lo proyecta
`backend/src/Salvo.Application/Alerts/AlertProjection.cs` (línea 210), lo exige `projectSignal` en
`frontend/src/lib/api/guards.ts` (línea 193) —**la guarda descarta lo que
no conoce**, así que un campo nuevo no llega a pantalla sin editarla— y lo pinta
`frontend/src/app/alerts/[id]/evaluation-blocks.tsx`. El panel del dashboard **no** entra:
`DashboardSignalView` es `(Rule, AlertCount)` y nunca llevó `detail`. Es la lección de `E7A`, otra vez, y el hueco por el que esa
tarea se rompió a mitad de camino.

Entra todo el camino:

- `AlertSignalView` (`backend/src/Salvo.Application/Alerts/AlertViews.cs`) con los campos nuevos y
  `detail` opcional. `DashboardSignalView` no se toca.
- Recaptura de `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, con
  `OpenApiDriftTests` verde.
- `frontend/src/lib/api/guards.ts` y `frontend/src/lib/api/guards.test.ts`: una señal sin campos
  **y sin `detail`** se descarta; una señal
  `e3-v1` con `detail` y sin campos pasa; una `e3-v2` con campos y sin `detail` pasa.
- `frontend/src/test/fixtures.ts` y `frontend/src/test/boundary.test.ts`.
- `frontend/src/lib/format.ts`: la composición en castellano de la frase de cada regla, junto a
  `ruleLabel`, que es donde ya vive el vocabulario de reglas de la consola. **`format.ts` no tiene
  hoy ningún test propio**, así que la tarea crea `frontend/src/lib/format.test.ts`; ahí vive el
  test de legibilidad de las seis reglas que pide la corrección de la regla de dominio. **En castellano y sin
  diccionario**: `E9C` extrae estos literales a los dos diccionarios, y adelantarlo acá sería
  hacer dos veces el mismo trabajo.
- `frontend/src/app/alerts/[id]/divergence.ts`: ya compara score y señales sin mirar `detail` —lo arregló `E9A` justamente
  porque esta tarea lo iba a disparar—. Se **verifica** que tras el rescoreo con `e3-v2` ninguna
  alerta muestra el aviso, y se agrega el test que lo afirma si no existe.

#### 7. El borrado, en el último commit

`SignalFacts.Parse`, `ParseAll`, los seis `[GeneratedRegex]` y los seis `Read*` se borran en el
**último** commit de la tarea, con `SignalFacts` construyéndose desde `RiskSignal` por copia de
campos. `SignalDetailNotRecognizedException` **se conserva**: sigue siendo el fallo con nombre del
punto 4.

Si algo obliga a conservar el extractor, la tarea **para y consulta**. Conservarlo «por las dudas»
es dejar seis expresiones regulares de una prosa que ya nadie escribe, pudriéndose en silencio.

#### 8. Los dorados que cambian, y por qué

- **Los fingerprints dorados cambian a propósito**: la versión y la cadena canónica son material
  del hash. Cada valor nuevo se registra con su motivo en el mismo commit, y no se toca ninguno
  «para que pase».
- **Los textos de `ExplanationGoldenTests` NO cambian.** Si cambian, algo del punto 2 está mal, y
  esa es la señal más barata que tiene la tarea. La única excepción posible es `e7-v3`, abajo.
- `RiskEvaluationTests`, `EvaluationMetricsEndpointTests` y `DashboardEndpointTests` fijan cifras de
  **score**, que no se mueven. Si alguna se mueve, es un defecto, no una actualización.

### Fuera

- **`e7-v3` y la mediana en el texto.** La plantilla alimentada con los mismos campos produce los
  mismos bytes, y la contrapositiva de la decisión 58 prohíbe subir la versión sin cambio de texto.
  Hay una razón legítima —escribir «es 23,2 veces la mediana del comercio, que fue 86,85 BRL»— y
  queda **para decisión del coordinador**; por defecto, afuera. El campo `medianCents` entra igual,
  porque lo pide el punto 5.
- El portugués y `SALVO_LANGUAGE`: `E9C`.
- La accesibilidad, las capturas, el guion, el README y el artículo: `E9D`.
- Migraciones de base: ninguna. `SignalsJson` es una columna de texto y su contenido cambia sin que
  el esquema se mueva.
- Tocar el corpus, el seed, el panel del dashboard o las métricas.
- `/orders`, el proveedor que se porta mal, y Anthropic.

### Paths autorizados

**Backend**

- `backend/src/Salvo.Domain/Risk/**`
- `backend/src/Salvo.Domain/Explanations/**`
- `backend/src/Salvo.Application/Explanations/**`
- `backend/src/Salvo.Application/Alerts/AlertViews.cs` y
  `backend/src/Salvo.Application/Alerts/AlertProjection.cs`
- `backend/src/Salvo.Application/Risk/EvaluateLocalRiskHandler.cs` y
  `backend/src/Salvo.Application/Risk/RunScoringHandler.cs`
- `backend/src/Salvo.Application/Dashboard/GetDashboardHandler.cs`, **solo** por la resolución de
  `RuleConfig` de su línea 172
- `backend/src/Salvo.Infrastructure/Explanations/**`
- `backend/src/Salvo.Api/Program.cs` y `backend/src/Salvo.Api/AlertEndpoints.cs`
- `backend/tests/**`, incluido el directorio nuevo
  `backend/tests/Salvo.Api.IntegrationTests/Goldens/`

**Frontend**

- `frontend/src/app/alerts/[id]/evaluation-blocks.tsx`
- `frontend/src/app/alerts/[id]/divergence.ts` y
  `frontend/src/app/alerts/[id]/divergence.test.ts`
- `frontend/src/app/alerts/[id]/page.test.tsx`
- `frontend/src/lib/api/guards.ts` y `frontend/src/lib/api/guards.test.ts`
- `frontend/src/lib/api/contract.ts`
- `frontend/src/lib/api/messages.ts` y `frontend/src/lib/api/messages.test.ts`, si aparece un código
  de fila nuevo (decisión 57)
- `frontend/src/lib/format.ts` y `frontend/src/lib/format.test.ts`, que la tarea crea
- `frontend/src/test/fixtures.ts` y `frontend/src/test/boundary.test.ts`
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, **solo recaptura**

**Otros**

- `Coordination/Handoffs/Claude.md`
- `scripts/smoke-ui.sh`, solo si una comprobación mira `detail`. Hoy la única que toca señales lee
  `signal.rule`, así que probablemente no haga falta.

Todo comportamiento modificado lleva su test, como exige `AGENTS.md`.

### Paths reservados por otros trabajos

- `frontend/src/app/**` en su mayor parte, el directorio de diccionarios que `E9C` cree, y
  `.env.example`: `E9C`.
- `README.md`, `docs/**`, `scripts/capturas.sh`, `scripts/demo.sh` y `docs/guion-demo.md`: `E9D`.

`AGENTS.md` y `DesignAgent/Salvo-Blueprint.md` ya fueron corregidos por el coordinador en el commit
anterior de esta misma rama. **No se vuelven a tocar**: si algo más de esos dos documentos
contradice la tarea, se para y se consulta.

**Cuidado con la compuerta**: `scripts/check-docs.sh` verifica que cada test que el README nombra
exista. El README cita `ExplanationIsolationTests.NoTextTheEngineDidNotWriteReachesTheProvider`,
`ExplanationIsolationTests.InvertingEveryLabelChangesNoSummary`,
`ExplanationGroundingTests.AnInventedFigureIsRefusedAndNoTextIsStored` y
`TemporalRiskEngineTests.AddingFutureOrdersCannotChangeEarlierAssessments`, entre otros.
**Renombrar cualquiera de esos tests rompe la compuerta**, y el README está reservado por `E9D`. Si
hay que renombrar uno, la tarea para y consulta.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Migraciones: **no**.
- Levantar la API para recapturar el OpenAPI: autorizado.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR.
- **Acciones destructivas: ninguna.** El borrado del extractor es borrado de código dentro de
  archivos que quedan. Si absorber `SignalFacts` en `RiskSignal` dejara un archivo entero vacío, la
  tarea lo deja escrito en la entrega y el borrado lo hace el coordinador: `rm` está denegado y es
  regla del usuario.
- Commits locales: autorizados, y se pide commitear por partes, **con el dorado en un commit
  anterior al del cambio de motor y el borrado del extractor en el último**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E9B-SENALES-TIPADAS.md` sin faltantes antes de empezar.
- [ ] El extractor lee `amountCents`, `currencyCode`, `medianCents` y `timeZoneId` en un commit
      **anterior** al de la captura, con su test.
- [ ] El dorado `signal-facts.v2.json` existe en un commit **anterior** al que toca el motor, cubre
      las **seis** reglas y **todas** las columnas de la tabla del punto 2.
- [ ] La salida del programa que verifica los formatos está en la entrega, y coincide con lo que el
      punto 2 afirma.
- [ ] `elapsedMinutes` tiene prueba unitaria sobre `TimeSpan` fuera del corpus, con una fracción
      periódica y un empate.
- [ ] Un test compone la frase de las seis reglas desde la fila almacenada, sin nada más que la
      fila.
- [ ] El test permanente afirma la igualdad campo a campo sobre las 300 evaluaciones, y el score y
      el conjunto de reglas de cada una no cambiaron.
- [ ] `ExplanationGoldenTests` pasa **sin tocar los textos**.
- [ ] Los fingerprints dorados cambiaron, y cada valor nuevo está registrado con su motivo.
- [ ] `ExplanationFacts` contiene el valor de cada campo numérico de cada regla, con un test por
      campo, y la mediana en centavos **y en unidades**.
- [ ] El conjunto de hechos desde campos es igual al conjunto desde prosa, sobre el dorado.
- [ ] Una fila `e3-v1` real se ve en la consola por el camino nuevo, y pedirle explicación devuelve
      un fallo con código, no una excepción.
- [ ] Una señal sin campos y sin `detail` la descarta la guarda.
- [ ] Tras el rescoreo con `e3-v2`, **ninguna alerta muestra el aviso de divergencia**.
- [ ] `RuleConfig` se resuelve por la versión de la fila en todo camino de lectura.
- [ ] `SignalFacts.Parse` y sus seis expresiones regulares no existen en el árbol final.
- [ ] `OpenApiDriftTests` verde y el contrato recapturado.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.
- [ ] Handoff con las falsaciones documentadas.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9B-SENALES-TIPADAS.md` | Brief válido |
| `git log --oneline` del extractor, del dorado y del motor | En ese orden |
| Programa mínimo sobre `0.0`, `0.##`, `decimal.Round`, `F1` y `F2` | Coincide con el punto 2 |
| `elapsedMinutes` sobre `TimeSpan`, fuera del corpus | Igual a lo que `{0.##}` escribe |
| Frase de las seis reglas compuesta desde la fila | Legible, sin intérprete |
| Diferencial campo a campo, 300 evaluaciones | Cero desvíos |
| `ExplanationGoldenTests` | Textos idénticos |
| Test por campo sobre `ExplanationFacts` | Todos presentes |
| Fila `e3-v1` en la consola y en el pedido de explicación | Prosa visible; fallo con código |
| Guarda con señal sin campos y sin `detail` | Descartada |
| Rescoreo con `e3-v2` | Sin aviso de divergencia |
| `grep -rn "GeneratedRegex" backend/src/Salvo.Domain/Explanations/` | Sin resultados |
| `OpenApiDriftTests` | Verde |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E9B-SENALES-TIPADAS` | Con las falsaciones |

**Falsaciones exigidas.** Cada una se rompe a propósito, se corre el test, se anota el error, y se
deshace:

1. Emitir `ratio` con la escala cruda de la división → el diferencial del dorado falla.
2. Sacar `medianCents` del conjunto de hechos → el test por campo falla, y un texto con la mediana
   es rechazado por `NOT_GROUNDED_NUMBER`.
3. Dejar `RuleConfig.E3V1` fijo en `RequestExplanationHandler` → el test de resolución por fila
   falla.
4. Hacer que la guarda acepte una señal sin campos y sin `detail` → `guards.test.ts` falla.
5. Borrar el dorado → el test permanente falla en vez de regenerarlo.

## Decisiones delegadas

- El nombre exacto de cada campo en el cable, dentro de la tabla del punto 2.
- La forma del archivo dorado, mientras sea legible y falle por diferencia.
- Cómo se compone la frase de cada regla en la consola, en castellano.
- El nombre del código de fallo de una evaluación sin campos.
- Si `SignalFacts` sobrevive como tipo o se absorbe en `RiskSignal`, mientras el proveedor y
  `ExplanationFacts` no cambien de forma.

## Detenerse y consultar si

- algo obliga a conservar el extractor;
- un texto de `ExplanationGoldenTests` cambia;
- una cifra de score, del dashboard o de las métricas se mueve;
- hace falta una migración;
- hace falta renombrar un test que el README nombra;
- el diferencial del dorado difiere en más de un campo y la explicación no es evidente;
- aparece un motivo para subir a `e7-v3` distinto de escribir la mediana;
- los formatos no se comportan como dice el punto 2;
- la conversión `double → decimal` de `elapsedMinutes` difiere de lo que la prosa escribía;
- algo más de `AGENTS.md` o del Blueprint contradice la tarea.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- El diferencial del dorado: cuántas evaluaciones, cuántos campos, cuántos desvíos.
- La salida del programa que verifica los formatos, y el resultado de la prueba de
  `elapsedMinutes`.
- La lista de fingerprints dorados que cambiaron, con el valor viejo y el nuevo.
- Las cinco falsaciones, con el error exacto de cada una.
- Comandos y resultados exactos.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
