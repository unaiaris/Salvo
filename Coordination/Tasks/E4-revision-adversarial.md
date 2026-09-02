# Salvo — Revisión adversarial del diseño propuesto de Etapa 4

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-02
> Revisor: Claude
> Objeto: `E4-diseno-propuesto.md` (base `main` en `1b52013`)
> Método: lectura de Blueprint §4.3, §7, §11 y bitácora, `AGENTS.md`, Progress, y del código real de
> `Salvo.Domain`, `Salvo.Application`, `Salvo.Infrastructure/Persistence` y `backend/tests`. Los
> escenarios se ejecutaron contra `TemporalRiskEngine` y `RuleConfig.E3V1` reales, y contra EF Core
> 10.0.11 con SQLite, desde programas efímeros fuera del repo. No se editó ningún archivo versionado.

## Hallazgos, por severidad

### 1. D3 y D4, alta. Una alerta por pedido pierde el fraude que un backfill revela, o lo entierra al fondo de la cola

Secuencia verificada con `TemporalRiskEngine.Score` y `RuleConfig.E3V1`:

| Paso | Corpus | Score de `ORD_Z` | Señales |
| --- | --- | --- | --- |
| 1 | 10 pedidos UY del comercio, 1 pedido previo del comprador hace 30 días, `ORD_Z` en BR por 300 | 60 | `amount_anomaly` 40, `foreign_country` 20 |
| 2 | Backfill: 3 pedidos del mismo comprador en AR, 9, 6 y 3 minutos antes | 100 | + `velocity` 30, + `cross_border_velocity` 40 |

**Rama A.** La analista revisó en el paso 1 y marcó `CONFIRMED_SAFE`. En el paso 2 la tabla de D3
dice "No crear. Queda registrada la evaluación nueva". Nada la muestra en el feed, en el dashboard ni
en el detalle. El único rastro es un contador transitorio en el resumen del run. Fraude perdido.

**Rama B.** La alerta sigue `OPEN`. El feed ordena por `riskScoreSnapshot`, así que este pedido se
presenta como `MEDIUM` 60 debajo de cualquier alerta nueva de 70 u 80, cuando es el pedido más
peligroso del corpus. Prioridad invertida.

**Alternativa.** Índice único parcial sobre `orderId WHERE status = 'OPEN'` en lugar de único total,
y permitir una alerta nueva sobre un pedido ya revisado solo cuando la banda de severidad de la
evaluación vigente supera la de la alerta anterior, enlazada con `supersedesAlertId`. Para la rama B,
que feed y detalle lean el score vigente junto al snapshot y ordenen por el vigente. Cuesta una
columna nullable, un `HasFilter` en la configuración EF y un join en la lectura. A cambio desaparece
la "limitación declarada" del README.

### 2. D2, alta. El fingerprint rebota y deja la "evaluación vigente" apuntando a un resultado obsoleto

Las órdenes nunca se borran, pero un baseline sí puede volver exactamente al estado anterior.
Verificado:

| Run | Corpus | Score de `ORD_X` | Fingerprint |
| --- | --- | --- | --- |
| 1 | 2 pedidos previos del comprador, cold start | 0, sin señales | `B766C1F0…` |
| 2 | + 1 pedido retroactivo de 100 | 40, `amount_anomaly` | `94940BC7…` |
| 3 | + 3 pedidos retroactivos de 500, la mediana sube a 300 | 0, sin señales | `B766C1F0…` |

En el run 3 la fila se omite por fingerprint repetido. La evaluación vigente, definida como "última
por `(createdAt, id)`", sigue siendo la del run 2 con score 40 mientras el motor dice 0. El resumen
reporta "0 evaluaciones nuevas", que el diseño presenta como evidencia de idempotencia, cuando en
realidad el estado cambió.

Con alertas es peor: 0 → 60 crea alerta, luego 60 → 0 se omite, y snapshot, vigente y feed quedan
coherentes entre sí y todos equivocados. Esto además anula la mitigación natural del hallazgo 3,
porque comparar snapshot contra vigente no detecta nada.

**Alternativa.** Registrar cada corrida en `ScoringRun` con `run_evaluations(runId, orderId,
evaluationId)`, y definir vigente como la evaluación que referenció la última corrida, se haya
insertado o reusado. Cuesta una tabla pequeña. Variante más barata: `lastConfirmedAt` en
`RiskEvaluation` que se actualiza al reusar una fila, y vigente por ese campo; cuesta 300 updates
por corrida.

### 3. D3, alta. Una alerta abierta que no se actualiza lleva a reportar fraude sobre una premisa ya falsa

Verificado con `new_buyer_high_value`:

| Paso | Corpus | Score de `ORD_Y` | Detail que ve la analista |
| --- | --- | --- | --- |
| 1 | 3 pedidos del comercio de otros compradores, `ORD_Y` de un comprador sin historia por 300 | 70 | "The buyer has no prior merchant orders and 300 UYU cents is 3.0x the merchant median" |
| 2 | Backfill: 3 compras previas del mismo comprador, 60, 50 y 40 días antes, de 300 | 0 | sin señales |

Este es el caso típico de una importación inicial incompleta. La alerta queda `OPEN` como `HIGH`
con un texto que afirma que el comprador es nuevo. La analista lo lee, no tiene ningún indicio de
que el corpus cambió, y marca `REPORTED_FRAUD` a un cliente habitual. La alerta además nunca se
cierra sola, así que suma para siempre en `amountAtRisk`.

**Alternativa.** No mutar el snapshot, que es correcto, pero que `GET /api/alerts/{id}` devuelva la
evaluación vigente junto al snapshot con un indicador de divergencia, y que `POST /review` rechace
o advierta cuando la banda vigente es menor que la del snapshot. Cuesta un join y depende de
arreglar el hallazgo 2.

### 4. D6, alta. La transacción es atómica pero el chequeo "solo una alerta OPEN se revisa" no lo es

La promesa de "conflicto, no sobrescritura silenciosa" es falsa tal como está escrita. Verificado
con EF Core 10.0.11 y SQLite en archivo, dos `DbContext` como dos requests:

```
request1 lee OPEN; request2 lee OPEN; ambos pasan el chequeo 'solo OPEN'
request1 commit: CONFIRMED_SAFE
request2 commit: REPORTED_FRAUD (sin excepción)
estado final: REPORTED_FRAUD; reviews=2 [OPEN->CONFIRMED_SAFE, OPEN->REPORTED_FRAUD]
```

SQLite serializa las escrituras, pero no protege un check-then-act hecho en memoria. El resultado
viola D6 dos veces: el veredicto se sobrescribe y quedan dos `AlertReview` con
`previousStatus = OPEN`.

Con `IsConcurrencyToken()` sobre `status` el mismo experimento termina en
`DbUpdateConcurrencyException`, estado final `CONFIRMED_SAFE` y un solo review:

```
request1 lee OPEN; request2 lee OPEN; ambos pasan el chequeo 'solo OPEN'
request1 commit: CONFIRMED_SAFE
request2 rechazado: DbUpdateConcurrencyException
estado final: CONFIRMED_SAFE; reviews=1 [OPEN->CONFIRMED_SAFE]
```

Un `UNIQUE(alert_reviews.alert_id)` es el respaldo a nivel base: como los estados son terminales,
una alerta solo puede tener un review en el MVP. Cuesta dos líneas de configuración y mapear la
excepción a 409.

**Evidencia adicional.** El test propuesto de "conflicto de revisión" no puede reproducir esta
carrera con la fábrica actual, porque `SalvoApiFactory` comparte una única conexión `:memory:`
entre todos los scopes (`backend/tests/Salvo.Api.IntegrationTests/SalvoApiFactory.cs:14`). El test
pasará en secuencial y el bug llegará igual. Hace falta una base en archivo o `mode=memory&cache=shared`
para ese test.

### 5. D2, media. Contradice la decisión 4 y §7 del Blueprint al definir la identidad de toda `RiskEvaluation`, no solo la local

§7 define `RiskEvaluation` con `source`, `status: PENDING | APPROVED | DENIED | ERROR`, `score`
nullable y `externalEvaluationId` (`DesignAgent/Salvo-Blueprint.md:347`). La verificación de E6
exige que "el estado pendiente pueda finalizar". Con D2, la fila es append-only, el fingerprint no
incluye `status`, y para una evaluación externa `score` y `signalsJson` son nulos.

Secuencia: E6 inserta `(orderId, EXTERNAL_MOCK, null, null)` en `PENDING`; llega el callback
`APPROVED`; o se muta la fila y se rompe append-only, o se inserta otra fila con el mismo fingerprint
y el índice único la rechaza. El diseño dice que lo externo está fuera de alcance, pero fija ahora
una identidad que E6 no podrá cumplir.

**Alternativa.** Acotar D2 a `source = LOCAL` con índice único parcial, dejar `ruleConfigVersion` y
el fingerprint nullable para otras fuentes, y anotar en la bitácora que el ciclo de vida externo se
decide en E6. Cuesta escribir la restricción bien ahora.

### 6. Alcance, media. "Consume el motor de E3" es ambiguo y la lectura obvia rompe el flujo principal

`EvaluateLocalRiskHandler` lanza `InvalidOperationException` cuando algún pedido no tiene etiqueta
(`backend/src/Salvo.Application/Risk/EvaluateLocalRiskHandler.cs:18`). Las importaciones nunca
escriben etiquetas.

Secuencia: `POST /api/order-imports` con un solo pedido, luego `POST /api/risk-evaluations:run`,
resultado 500. Ese es exactamente el paso 1 del flujo de §3.

El brief tiene que decir que el caso de uso de persistencia invoca `TemporalRiskEngine.Score`
directamente y que las métricas con etiquetas quedan en su propio camino.

### 7. D4, media. La protección "por construcción" no cubre a la propia corrida de scoring

El diseño no dice si una corrida es una transacción ni sobre qué estado se evalúa "crear alerta". Si
la implementación sigue el patrón actual de un `SaveChangesAsync` por método de puerto
(`backend/src/Salvo.Infrastructure/Persistence/EfOrderDataStore.cs:56`) y crea alertas a partir de
las evaluaciones nuevas del run:

1. la corrida inserta 300 evaluaciones;
2. falla antes de las alertas;
3. el reintento omite las 300 por fingerprint;
4. ninguna alerta se crea jamás para ese corpus.

Alerta perdida sin fraude perdido en los datos, invisible.

Dos exigencias que faltan: la corrida es un único `SaveChanges`, y el predicado de creación es
"evaluación vigente marcada sin alerta" sobre estado persistido, no sobre el delta del run. Además,
`AddRange` de 300 filas contra el índice único falla entero si una ya existe, así que hay que
preconsultar fingerprints; y dos corridas concurrentes terminan en violación de unicidad que debe
mapearse a 409 y no a 500.

### 8. D2, baja. El fingerprint hashea el texto de `detail`, y hay dos órdenes "canónicos" distintos

Cualquier cambio de redacción de un detail sin subir `ruleConfigVersion` produce 300 evaluaciones
"nuevas" en la siguiente corrida, indistinguibles de un cambio de corpus. El diseño lo lista como
riesgo; lo concreto es que hoy nada lo detecta, porque `RiskEvaluationTests` fija la distribución de
scores pero no los textos.

Además el motor emite señales en orden fijo de regla
(`backend/src/Salvo.Domain/Risk/TemporalRiskEngine.cs:45`) y un test lo declara canónico
(`backend/tests/Salvo.Domain.Tests/TemporalRiskEngineTests.cs:244`), mientras D2 canonicaliza por
nombre alfabético. Si `signalsJson` se guarda en un orden y el fingerprint se calcula en otro, un
recálculo desde la fila almacenada no coincide.

**Alternativa.** La serialización canónica vive en Domain, no en `System.Text.Json` con opciones
por defecto; se persiste exactamente la cadena hasheada; y un test dorado fija los fingerprints del
corpus demo. Cuesta un test.

**Lo que resistió.** Cultura y reserialización de números. Corrí el mismo corpus bajo `en-US`,
`es-UY`, `pt-BR`, `de-DE` y `ar-SA` y el fingerprint fue idéntico, porque todos los details usan
`string.Create(CultureInfo.InvariantCulture, …)` y el identificador de zona horaria se conserva
como `America/Montevideo` en este runtime.

### 9. D5, baja. La alerta no registra qué `AlertPolicy` la clasificó, y el piso de banda duplica el umbral

Si mañana existe `AlertPolicy e4-v2` con otras bandas, todas las alertas históricas, incluidas las
revisadas, cambian de severidad retroactivamente y los conteos por severidad del dashboard dejan de
ser reproducibles. Y si `RuleConfig e3-v2` baja `FlagThreshold` a 50, un score 55 genera alerta sin
banda definida.

**Alternativa.** Persistir `alertPolicyVersion` en `Alert`, que no es persistir severidad sino la
versión de la función, y que `AlertPolicy.Validate()` exija piso igual a `config.FlagThreshold`.
Cuesta una columna y una comprobación.

### 10. D6, baja. La repetición idempotente descarta información

"Misma revisión hacia el mismo estado terminal devuelve éxito sin cambios" descarta en silencio una
`note` distinta. Sin identidad de revisor, una segunda persona que confirma el fraude con otra nota
no deja rastro.

**Alternativa.** 409 si el estado coincide pero la nota difiere, o aceptar reviews adicionales sin
transición. Cuesta una comparación.

## Decisiones que resistieron

**D1 es sólida.** `Order` no tiene mutadores, la unicidad `(merchantId, merchantReferenceId)` más
`HasSameBusinessFactsAs` garantizan que un reimport no cambie hechos, y con D3 el `status` de la
única alerta es el único estado de revisión posible del pedido, así que no hay ambigüedad. Todo
consumidor registrado, el paso 8 de §3, el dashboard de §4.4 y `amountAtRisk`, se calcula desde
`Alert` con un join. Intenté hacerla fallar con evaluaciones externas y con re-scoring y no
encontré secuencia que produzca estado incorrecto. Un detalle de redacción para la bitácora: D1
llama al veredicto "potencialmente revisable" y D6 lo hace terminal.

**D5 resiste en su núcleo.** Con los pesos de `e3-v1` los únicos scores alcanzables por encima del
umbral son 60, 70, 80, 90 y 100; todas las bandas son alcanzables y ninguna es ambigua. Sobre el
corpus demo produce 13 `MEDIUM`, 0 `HIGH` y 5 `CRITICAL`. La única grieta es la de versionado del
hallazgo 9.

**D6 en su núcleo transaccional también resiste.** Un solo `SaveChangesAsync` con `UPDATE` más
`INSERT` es atómico en EF Core sobre SQLite. Lo que falla es la condición, no la transacción.

## Contradicciones con la bitácora

- Decisión 4 y §7 contra D2: hallazgo 5.
- §4.3 línea 201, "una alerta por evaluación local marcada", contra D3. El diseño ya lo declara y
  propone corregirlo; consistente con el paso 5 de §3.
- §4.3 línea 205, "actualiza alerta y pedido", contra D1. Declarado y correcto.
- Decisiones 9, 17, 21 y 27: sin conflicto. El desempate de "vigente" por `id` aleatorio no es
  determinista al estilo de E3, pero con el hallazgo 2 resuelto deja de importar.

## Resumen para el brief

| # | Decisión | Severidad | Acción mínima |
| --- | --- | --- | --- |
| 1 | D3/D4 | Alta | Índice único parcial sobre alertas `OPEN`; feed y detalle leen score vigente; re-alerta por escalada de banda |
| 2 | D2 | Alta | `ScoringRun` + `run_evaluations`, o `lastConfirmedAt`; redefinir "vigente" |
| 3 | D3 | Alta | Detalle devuelve vigente junto al snapshot con indicador de divergencia |
| 4 | D6 | Alta | `IsConcurrencyToken()` en `status` + `UNIQUE(alert_reviews.alert_id)`; test con base en archivo |
| 5 | D2 | Media | Acotar fingerprint e índice a `source = LOCAL` |
| 6 | Alcance | Media | El run llama a `TemporalRiskEngine.Score`, no a `EvaluateLocalRiskHandler` |
| 7 | D4 | Media | Corrida en un `SaveChanges`; predicado de alerta sobre estado persistido; preconsulta de fingerprints |
| 8 | D2 | Baja | Serialización canónica en Domain; test dorado de fingerprints del corpus demo |
| 9 | D5 | Baja | `alertPolicyVersion` en `Alert`; validar piso contra `FlagThreshold` |
| 10 | D6 | Baja | 409 si la nota difiere en una repetición |

## Cómo se verificó

- Programa de consola en el scratchpad referenciando
  `backend/src/Salvo.Domain/Salvo.Domain.csproj`, ejecutando los escenarios A, B y C y la prueba
  de culturas con `TemporalRiskEngine.Score` y `RuleConfig.E3V1`.
- Programa de consola con `Microsoft.EntityFrameworkCore.Sqlite` 10.0.11 y base SQLite en archivo,
  dos `DbContext` como dos requests, con y sin `IsConcurrencyToken()`.
- Los únicos artefactos generados dentro del repo son `bin/` y `obj/` de `Salvo.Domain`, ignorados
  por Git. No se modificó ningún archivo versionado ni el estado canónico.
