# Salvo — Diseño de la Etapa 4: Alertas y casos de uso (v2)

> Estado: propuesta del coordinador tras revisión adversarial, pendiente de aprobación final
> Fecha: 2026-09-02
> Base: `main` en `1b52013`
> Reemplaza a la v1. Los diez hallazgos de `E4-revision-adversarial.md` están incorporados.

La Etapa 4 se ejecuta en dos ítems de trabajo: `E4A-PERSISTENCIA` y `E4B-ALERTAS`. La etapa del
Blueprint sigue siendo la 4; la partición es de ejecución, no de alcance.

## Decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | `Order` permanece inmutable; el estado de revisión vive en `Alert` | v1, resistió el ataque |
| D2 | `RiskEvaluation` append-only con identidad por fingerprint, acotada a `source = LOCAL` | v1 + hallazgos 5 y 8 |
| D3 | `ScoringRun` persistido; la evaluación vigente es la que referenció la última corrida | hallazgo 2 |
| D4 | Como máximo una alerta `OPEN` por pedido; re-alerta solo por escalada de banda | hallazgo 1 |
| D5 | Una alerta abierta nunca se actualiza; la divergencia se expone, no se oculta | v1 + hallazgo 3 |
| D6 | Severidad derivada del score, con `alertPolicyVersion` persistida | v1 + hallazgo 9 |
| D7 | La revisión escribe dos entidades en una transacción, con token de concurrencia | v1 + hallazgos 4 y 10 |
| D8 | La corrida de scoring invoca `TemporalRiskEngine` directamente | hallazgo 6 |

---

## D1 — `Order` permanece inmutable

El veredicto de un analista no es un hecho del pedido. El pedido ocurrió con un importe, un país y
un horario: eso es inmutable y verificable contra la fuente. Que alguien lo haya marcado como fraude
es un juicio del proceso de revisión, producido después y por otra parte del sistema.

Además, ningún pedido puede revisarse sin alerta previa, y solo se alerta lo que supera el umbral.
El estado de revisión, por construcción, solo existe para pedidos alertados.

`amountAtRisk`, el dashboard de §4.4 y el paso 8 de §3 se calculan desde `Alert` con un join.

**Precisión de redacción**: en el MVP el veredicto de una revisión es **terminal**. Una alerta
revisada no se reabre; una escalada posterior crea una alerta nueva (D4), no modifica la anterior.

§4.3 del Blueprint debe corregirse: la revisión es transaccional sobre la alerta y su registro de
auditoría, no sobre el pedido.

---

## D2 — Identidad de la evaluación por contenido, solo para `LOCAL`

`RiskEvaluation` es append-only. Su identidad es un fingerprint determinista:

```
evaluationFingerprint = SHA-256( orderId | source | ruleConfigVersion | score | signalsCanonical )
```

### Serialización canónica

- Vive en `Salvo.Domain`, no en `System.Text.Json` con opciones por defecto.
- Usa **el orden de reglas que ya emite el motor** —`amount_anomaly`, `velocity`,
  `cross_border_velocity`, `unusual_hour`, `new_buyer_high_value`, `foreign_country`— y no un orden
  alfabético. `TemporalRiskEngine.ScoreOne` lo fija y `ScoreIsCappedAndSignalsRemainInCanonicalOrder`
  lo declara canónico. Un orden distinto haría que un recálculo desde la fila almacenada no
  coincida con su propio fingerprint.
- La cadena exacta que se hashea es la que se persiste en `signalsJson`. No se re-serializa nunca.
- Un test dorado fija los fingerprints del corpus demo. Cualquier cambio de redacción de un `detail`
  sin subir `ruleConfigVersion` rompe ese test de forma ruidosa, en vez de producir 300 evaluaciones
  falsamente nuevas.

### Alcance de la identidad

El fingerprint y su índice único son **parciales, solo para `source = 'LOCAL'`**.
`evaluationFingerprint` y `ruleConfigVersion` son nullable para otras fuentes.

Una evaluación externa tiene `score` y `signalsJson` nulos y un `status` que transiciona de
`PENDING` a `APPROVED` por callback. Ese ciclo de vida es mutable y se decide en la Etapa 6. Fijar
ahora una identidad append-only para todas las fuentes dejaría a E6 sin salida: o rompe append-only,
o choca contra el índice único.

---

## D3 — `ScoringRun` y la definición de evaluación vigente

### El problema que resuelve

Un baseline puede volver exactamente a un estado anterior. Verificado: score 0 → 40 → 0. En la
tercera corrida el fingerprint coincide con el de la primera y la fila se omite, pero la evaluación
"última por fecha" sigue siendo la de 40 mientras el motor dice 0. El resumen reporta cero altas y
lo presenta como idempotencia, cuando el estado cambió.

### Modelo

`ScoringRun`: `id`, `ruleConfigVersion`, `startedAt`, `completedAt`, `orderCount`,
`evaluationsCreated`, `evaluationsReused`, `alertsCreated`, `alertsSkippedOpen`,
`alertsSkippedReviewed`.

`RunEvaluation`: `runId`, `orderId`, `evaluationId`. Único por `(runId, orderId)`.

**La evaluación vigente de un pedido es la que referenció la última corrida**, se haya insertado o
reusado. La definición deja de depender de timestamps y el rebote desaparece.

El resumen de la corrida deja de ser un objeto transitorio y pasa a ser evidencia consultable: la
segunda corrida sobre el mismo corpus registra `evaluationsCreated = 0`.

### Atomicidad de la corrida

- La corrida completa es **un único `SaveChangesAsync`**. Si falla, no queda ni la corrida, ni las
  evaluaciones, ni las alertas.
- Antes de insertar se **preconsultan los fingerprints existentes**. `AddRange` de 300 filas contra
  un índice único falla entero si una sola ya existe.
- Dos corridas concurrentes que colisionen en unicidad se mapean a `409`, nunca a `500`.

---

## D4 — Una alerta `OPEN` por pedido, con escalada

### Índices

- `Alert.orderId`: único **parcial**, `WHERE status = 'OPEN'`.
- `Alert.riskEvaluationId`: único total. Una evaluación origina como máximo una alerta.
- `Alert.supersedesAlertId`: FK nullable a la alerta anterior en una escalada.

### Predicado de creación

Evaluado sobre **estado persistido**, nunca sobre el delta de la corrida. Se crea alerta cuando:

1. la evaluación vigente del pedido está marcada; **y**
2. el pedido no tiene ninguna alerta `OPEN`; **y**
3. el pedido no tiene alertas revisadas, **o** la banda de severidad de la evaluación vigente
   supera la banda de la última alerta.

### Por qué la escalada

Sin ella, esta secuencia pierde un fraude: la analista revisa y marca `CONFIRMED_SAFE` con score 60;
después un backfill retroactivo revela tres compras del mismo comprador en otro país minutos antes,
y el score sube a 100. Con una alerta por pedido y sin escalada, nada vuelve a mostrar ese pedido.

La escalada exige subir de banda, no solo de puntos: evita reabrir por ruido y solo vuelve a llamar
la atención cuando la naturaleza del riesgo cambió.

---

## D5 — La alerta abierta no se actualiza, pero la divergencia se expone

El snapshot de la alerta documenta por qué se abrió y no se sobrescribe: es el registro auditable de
la decisión que la analista está por tomar.

Pero ocultar que el corpus cambió es peligroso. Caso verificado: una alerta se abre con
`new_buyer_high_value` y el detalle *"el comprador no tiene pedidos previos"*; un backfill revela
tres compras anteriores y el score cae a 0. La analista lee un texto que ya es falso y reporta
fraude sobre un cliente habitual.

Por eso:

- `GET /api/alerts/{id}` devuelve el snapshot **y** la evaluación vigente, con un indicador de
  divergencia de banda.
- `GET /api/alerts` devuelve ambos scores, para que el feed pueda ordenar por el vigente y no
  invierta prioridades.
- `POST /api/alerts/{id}/review` exige `acknowledgedDivergence: true` cuando las bandas difieren.
  No bloquea la revisión —eso impediría decisiones legítimas— pero impide decidir sin haberlo visto.

---

## D6 — Severidad derivada, versión persistida

`AlertPolicy e4-v1`, inmutable, con la misma forma que `RuleConfig`:

| Rango de score | Severidad |
| --- | --- |
| 60–69 | `MEDIUM` |
| 70–89 | `HIGH` |
| 90–100 | `CRITICAL` |

La severidad no se persiste: es función pura del `riskScoreSnapshot`, que sí está guardado.

Lo que **sí** se persiste es `Alert.alertPolicyVersion`. Sin eso, una futura `e4-v2` con otras
bandas reclasificaría retroactivamente todas las alertas históricas, incluidas las ya revisadas, y
los conteos por severidad del dashboard dejarían de ser reproducibles. Guardar la versión de la
función no es guardar el valor derivado.

`AlertPolicy.Validate()` exige que el piso de la banda más baja sea igual a `RuleConfig.FlagThreshold`.
Si mañana el umbral baja a 50, la validación falla al arrancar en vez de dejar un score 55 sin banda.

Sobre el corpus demo, con `e3-v1`, los scores alcanzables por encima del umbral son 60, 70, 80, 90 y
100, y la distribución da 13 `MEDIUM`, 0 `HIGH` y 5 `CRITICAL`.

---

## D7 — Revisión transaccional con token de concurrencia

### La transacción

Un único `SaveChangesAsync`:

1. `UPDATE Alert` — `status`, `reviewedAt`;
2. `INSERT AlertReview` — `alertId`, `previousStatus`, `newStatus`, `note`, `reviewedAt`.

### La condición, que es lo que fallaba

Verificar en memoria que la alerta está `OPEN` y después commitear **no protege nada**. SQLite
serializa las escrituras pero no impide un check-then-act: dos requests leen `OPEN`, ambos pasan el
chequeo, ambos commitean, el segundo pisa al primero y quedan dos `AlertReview` con
`previousStatus = OPEN`.

- `Alert.status` se configura con `IsConcurrencyToken()`. El segundo commit produce
  `DbUpdateConcurrencyException`, que se mapea a `409`.
- `UNIQUE(alert_reviews.alert_id)` como respaldo a nivel base: los estados son terminales, así que
  una alerta tiene como máximo un review en el MVP.

### Idempotencia y conflicto

| Situación | Respuesta |
| --- | --- |
| Alerta `OPEN`, transición válida | `200`, alerta revisada |
| Mismo estado terminal, misma nota | `200`, sin cambios |
| Mismo estado terminal, nota distinta | `409` |
| Estado terminal distinto al pedido | `409` |
| Bandas divergentes sin `acknowledgedDivergence` | `409` |

### El test que hoy no puede escribirse

`SalvoApiFactory` comparte una única `SqliteConnection(":memory:")` entre todos los scopes. Un test
de carrera sobre esa fábrica pasaría en secuencial y el bug llegaría igual. El test de conflicto
necesita una base en archivo o `mode=memory&cache=shared`.

Sin autenticación, `AlertReview` no puede atribuir la decisión a una persona. Queda declarado.

---

## D8 — La corrida invoca el motor, no el handler de métricas

`EvaluateLocalRiskHandler` lanza `InvalidOperationException` si algún pedido no tiene etiqueta, y
las importaciones nunca escriben etiquetas. Importar un pedido y correr el scoring daría `500` — y
ese es el paso 1 del flujo principal de §3.

El caso de uso de persistencia invoca `TemporalRiskEngine.Score` directamente. El camino de métricas
con ground truth, que sí exige etiquetas completas, permanece separado y sin cambios.

---

## Partición en dos ítems de trabajo

### `E4A-PERSISTENCIA`

- Dominio: `RiskEvaluation`, `RiskEvaluationSource`, serialización canónica de señales, cálculo de
  fingerprint.
- Dominio: `ScoringRun`, `RunEvaluation`.
- Aplicación: caso de uso de corrida de scoring, invocando `TemporalRiskEngine.Score`.
- Infraestructura: configuraciones EF Core, migración con índice único parcial sobre
  `(evaluationFingerprint) WHERE source = 'LOCAL'`, únicos de `RunEvaluation`.
- API: `POST /api/risk-evaluations:run`, `GET /api/orders` con paginación y sin exponer
  `isFraudLabel` bajo ninguna circunstancia.
- Tests: dos corridas seguidas dan `evaluationsCreated = 0`; el escenario de rebote 0 → 40 → 0
  mantiene la vigente correcta; test dorado de fingerprints del corpus demo; estabilidad ante
  culturas; fallo parcial no deja corrida a medias.

**Compuerta**: `./scripts/check.sh` verde y los tests de idempotencia y rebote pasando.

### `E4B-ALERTAS`

- Dominio: `Alert`, `AlertStatus`, `AlertSeverity`, `AlertReview`, `AlertPolicy` con `Validate()`.
- Aplicación: creación de alertas dentro de la corrida —extiende el caso de uso de E4A dentro del
  mismo `SaveChanges`— y caso de uso de revisión.
- Infraestructura: configuraciones, migración con único parcial sobre `orderId WHERE status = 'OPEN'`,
  único de `riskEvaluationId`, único de `alert_reviews.alert_id`, `IsConcurrencyToken` en `status`.
- API: `GET /api/alerts`, `GET /api/alerts/{id}`, `POST /api/alerts/{id}/review`.
- Tests: escalada de banda tras backfill crea alerta nueva enlazada; caída de score deja divergencia
  visible; revisión sin `acknowledgedDivergence` da `409`; carrera de revisión sobre base en archivo
  da `409` y un solo `AlertReview`; repetición idempotente y sus variantes.

**Compuerta**: `./scripts/check.sh` verde y la carrera de revisión reproducida.

`E4B` depende de `E4A` integrada.

---

## Fuera de alcance de la Etapa 4

- UI. Es la Etapa 5, incluido el orden del feed y `amountAtRisk`.
- `IAntifraudProvider`, evaluaciones externas y `CallbackReceipt`. Etapa 6.
- `explanation`, `recommendedAction` y `explanationStatus` de `Alert`. Etapa 7; las columnas no se
  crean todavía.
- Autenticación e identidad de revisor. Post-MVP.

---

## Entradas propuestas para la bitácora del Blueprint

| # | Decisión | Razón |
| --- | --- | --- |
| 28 | `Order` permanece inmutable; el estado de revisión vive en `Alert` y §4.3 se corrige | Separar hechos verificables del juicio operativo |
| 29 | `RiskEvaluation` es append-only con identidad por fingerprint, acotada a `source = LOCAL` | El motor depende del corpus; el ciclo externo es mutable y se decide en E6 |
| 30 | La serialización canónica vive en Domain y usa el orden de reglas del motor | Un recálculo desde la fila almacenada debe reproducir su propio fingerprint |
| 31 | `ScoringRun` se persiste y define la evaluación vigente | Un baseline puede volver a un estado anterior y hacer rebotar el fingerprint |
| 32 | Como máximo una alerta `OPEN` por pedido; re-alerta solo por escalada de banda | Evitar duplicados sin perder el fraude que revela un backfill |
| 33 | Una alerta abierta no se actualiza, pero la divergencia se expone y se reconoce al revisar | El snapshot es auditable; ocultar el cambio induce decisiones falsas |
| 34 | La severidad se deriva del score y se persiste `alertPolicyVersion` | Evitar reclasificación retroactiva de alertas históricas |
| 35 | La revisión escribe alerta y auditoría en una transacción, con `status` como token de concurrencia | Una transacción atómica no protege un check-then-act |
| 36 | La corrida de scoring invoca `TemporalRiskEngine`; el camino de métricas queda separado | `EvaluateLocalRiskHandler` exige etiquetas que una importación no produce |

---

## Riesgos que permanecen declarados

- Sin autenticación, `AlertReview` no atribuye la decisión a una persona.
- El test dorado de fingerprints fija el texto de los `detail`: cambiar redacción exige actualizarlo
  conscientemente. Es deliberado.
- `POST /api/risk-evaluations:run` recalcula el corpus completo. Con 300 pedidos es trivial; el
  diseño no define comportamiento para volúmenes donde deje de serlo.
- La escalada por banda no cubre un aumento de riesgo que no cruce una banda.
