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

## Alcance

### Dentro

#### 1. El oráculo, y va primero

`SignalFacts` existe exactamente para leer la prosa que el motor escribe. Con el corpus v2 sembrado
y el motor **quieto** en `e3-v1`, la base tiene evaluaciones reales de las seis reglas, y eso lo
vuelve el único oráculo capaz de certificar el cambio de motor sin frases inventadas en un test.

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

Las escalas son **exactamente las que la prosa ya fijaba** —`{ratio:0.0}`, `{share:0.0}`,
`{elapsed:0.##}`— y eso no es casualidad ni pereza: hace que `e3-v2` sea un cambio de
**representación y nada más**, y por lo tanto que la igualdad del punto 1 sea exacta y que los
textos dorados de `ExplanationGoldenTests` salgan **byte por byte idénticos**. Guardar la precisión
cruda sería «más información» y a cambio dejaría la identidad de cada evaluación dependiendo de la
escala de una división. No se hace.

«Escala fija» quiere decir escrita siempre con esa cantidad de decimales: `4.0`, no `4`; `90.00`, no
`90`. La escala del `decimal` que se serializa se fija con `decimal.Round(valor, n)` y el
serializador no la recorta.

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
no conoce**, así que un campo nuevo no llega a pantalla sin editarla— y lo pintan
`frontend/src/app/alerts/[id]/evaluation-blocks.tsx` y `frontend/src/app/dashboard/panels.tsx`. Es la lección de `E7A`, otra vez, y el hueco por el que esa
tarea se rompió a mitad de camino.

Entra todo el camino:

- `AlertSignalView` (`backend/src/Salvo.Application/Alerts/AlertViews.cs`) con los campos nuevos y
  `detail` opcional; lo mismo para
  `DashboardSignalView` si el panel del dashboard lo necesita.
- Recaptura de `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, con
  `OpenApiDriftTests` verde.
- `frontend/src/lib/api/guards.ts` y `frontend/src/lib/api/guards.test.ts`: una señal sin campos
  **y sin `detail`** se descarta; una señal
  `e3-v1` con `detail` y sin campos pasa; una `e3-v2` con campos y sin `detail` pasa.
- `frontend/src/test/fixtures.ts` y `frontend/src/test/boundary.test.ts`.
- `frontend/src/lib/format.ts`: la composición en castellano de la frase de cada regla, junto a
  `ruleLabel`, que es donde ya vive el vocabulario de reglas de la consola. **En castellano y sin
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
- `backend/src/Salvo.Application/Dashboard/GetDashboardHandler.cs` y
  `backend/src/Salvo.Application/Dashboard/DashboardViews.cs`, solo por
  la resolución de `RuleConfig` y por `DashboardSignalView` si hace falta
- `backend/src/Salvo.Infrastructure/Explanations/**`
- `backend/src/Salvo.Api/Program.cs` y `backend/src/Salvo.Api/AlertEndpoints.cs`
- `backend/tests/**`, incluido el directorio nuevo
  `backend/tests/Salvo.Api.IntegrationTests/Goldens/`

**Frontend**

- `frontend/src/app/alerts/[id]/evaluation-blocks.tsx`
- `frontend/src/app/alerts/[id]/divergence.ts` y
  `frontend/src/app/alerts/[id]/divergence.test.ts`
- `frontend/src/app/alerts/[id]/page.test.tsx`
- `frontend/src/app/dashboard/panels.tsx` y sus tests, solo si `DashboardSignalView`
  (`backend/src/Salvo.Application/Dashboard/DashboardViews.cs`) cambia
- `frontend/src/lib/api/guards.ts` y `frontend/src/lib/api/guards.test.ts`
- `frontend/src/lib/api/contract.ts`
- `frontend/src/lib/api/messages.ts` y `frontend/src/lib/api/messages.test.ts`, si aparece un código
  de fila nuevo (decisión 57)
- `frontend/src/lib/format.ts` y su test
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
- Commits locales: autorizados, y se pide commitear por partes, **con el dorado en un commit
  anterior al del cambio de motor y el borrado del extractor en el último**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E9B-SENALES-TIPADAS.md` sin faltantes antes de empezar.
- [ ] El dorado `signal-facts.v2.json` existe en un commit **anterior** al que toca el motor, y
      cubre las **seis** reglas.
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
| `git log --oneline` del dorado y del motor | El dorado precede |
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
- aparece un motivo para subir a `e7-v3` distinto de escribir la mediana.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- El diferencial del dorado: cuántas evaluaciones, cuántos campos, cuántos desvíos.
- La lista de fingerprints dorados que cambiaron, con el valor viejo y el nuevo.
- Las cinco falsaciones, con el error exacto de cada una.
- Comandos y resultados exactos.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
