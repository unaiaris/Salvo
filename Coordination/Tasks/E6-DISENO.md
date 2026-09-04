# Salvo — Diseño de la Etapa 6: proveedor antifraude externo

> Estado: **v2**, con los quince hallazgos de la revisión adversarial incorporados
> Fecha: 2026-09-04
> Sustituye a la v1. El informe queda en `Coordination/Tasks/E6-revision-adversarial.md`
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §3 paso 9, §5, §7 y «Etapa 6»

## Qué cambió respecto de la v1

| Cambio | Origen |
| --- | --- |
| La fila se **reserva antes** de llamar al proveedor, y se correlaciona por referencia además de por identificador | Hallazgo 1 |
| `ERROR` deja de ser terminal para los fallos posteriores al envío; un timeout **conserva** `PENDING` | Hallazgo 2 |
| El disparador de demo es un endpoint de la API, no un botón que compone el payload | Hallazgo 3 |
| Único sobre `(provider, deduplicationKey)`; recibo y transición en una sola unidad de trabajo; `DUPLICATE` desaparece | Hallazgo 4 |
| Tabla de transiciones completa, con `NO_OP`, `SUPERSEDED` y `CONFLICTING` | Hallazgo 5 |
| Lista de tests explícita, y la **reconciliación pasa a `E6A`** | Hallazgo 6 |
| La migración se acota: dos columnas fuera y dos restricciones endurecidas, nada más | Hallazgo 7 |
| La superficie es `/alerts/[id]` más una acción de demo en `/import`; **no** hay `/orders` | Hallazgo 8 + decisión del usuario |
| Endpoints de solicitud y lectura, idempotentes, con tabla de códigos | Hallazgo 9 |
| El mock deriva su resultado de una **función documentada con bandas declaradas** | Hallazgo 10 |
| `GetStatusAsync` acepta búsqueda por referencia; umbral configurable con 0 por defecto; unidad de trabajo por fila; `settledBy` | Hallazgo 11 |
| Tres citas corregidas | Hallazgo 12 |
| Endurecimientos del endpoint de callback | Hallazgo 13 |
| Lista de artefactos a tocar y copia de la base antes de migrar | Hallazgo 14 |
| `KOIN_MODE=sandbox` falla al arrancar; vocabulario de los tres «pendiente»; el score externo no se compara con el local | Hallazgo 15 |

---

## Resumen de decisiones

| # | Decisión |
| --- | --- |
| D1 | La evaluación externa es su propia entidad, en `Salvo.Domain/External/`, sin compartir tipos con `Risk/` |
| D2 | Dos fases: reservar la fila y commitear **antes** de hablar con el proveedor |
| D3 | Máquina de estados con tabla de transiciones completa y explícita |
| D4 | Un fallo posterior al envío **no** cierra la evaluación; solo la cierra lo que se sabe definitivo |
| D5 | Recibo y transición son una sola unidad de trabajo; el duplicado se detecta por violación de unicidad |
| D6 | Doble correlación: por identificador externo y por referencia del pedido |
| D7 | La reconciliación es explícita, va en `E6A`, y su unidad de trabajo es por fila |
| D8 | El mock es determinista por una función documentada con bandas declaradas |
| D9 | El disparador de demo es un endpoint de la API bajo bandera; el cliente elige **qué**, nunca **qué estado** |
| D10 | El endpoint de callback falla cerrado y se endurece |
| D11 | La evaluación externa no abre alertas |
| D12 | La divergencia se expone; el score externo **no** se compara numéricamente con el local |
| D13 | La migración se acota a dos columnas y dos restricciones |
| D14 | Partición en `E6A-PROVEEDOR` y `E6B-CALLBACK-UI` |

---

## D1 — Entidad propia, tipos propios

La v1 argumentó bien por qué separar y lo dejó a medias: proponía que `ExternalEvaluation`
conviviera con las enumeraciones de `Risk/`. **Compartir la enumeración reintroduce a nivel de tipo
la mezcla que la separación combate a nivel de tabla.**

`Salvo.Domain/External/` nace con sus propios tipos: `ExternalProvider`,
`ExternalEvaluationStatus`, y sus nombres de cable. No referencia `Salvo.Domain/Risk/`.

**`ExternalEvaluation`:**

| Campo | Nota |
| --- | --- |
| `id`, `orderId` | |
| `provider` | `EXTERNAL_MOCK` \| `KOIN_SANDBOX` |
| `referenceId` | La referencia estable del pedido, `(merchantId, merchantReferenceId)`. **Se escribe en la reserva**, antes de llamar al proveedor |
| `externalEvaluationId` | Nullable hasta que el proveedor lo asigne |
| `status` | `PENDING \| APPROVED \| DENIED \| ERROR`. Token de concurrencia |
| `score` | Nullable: un proveedor puede no devolverlo |
| `errorCode` | Solo con `status = ERROR`. Catálogo cerrado |
| `lastErrorCode` | Para una fila `PENDING` que arrastra un fallo. **Distinto de `errorCode`** |
| `attemptCount` | **Sondeos de reconciliación**, no reintentos: con una fila por intento sería siempre 1 |
| `settledBy` | `SYNC \| CALLBACK \| RECONCILIATION`. Procedencia, exigida por la decisión 39 |
| `requestedAt`, `updatedAt`, `settledAt` nullable | |

**Índices y restricciones:**

- Único parcial: `(order_id, provider) WHERE status = 'PENDING'`.
- Único parcial: `(provider, external_evaluation_id) WHERE external_evaluation_id IS NOT NULL`.
- Índice sobre `(provider, reference_id)` para la correlación tardía.
- `ck`: terminal ⇔ `settled_at IS NOT NULL`; `error_code IS NOT NULL ⇒ status = 'ERROR'`;
  `settled_by IS NOT NULL ⇔ settled_at IS NOT NULL`.

`ExternalEvaluation` queda **fuera** de `AppendOnlyEntitiesExposeNoPublicMutator` —es mutable a
propósito— y **dentro** de `PersistedRiskEntitiesCarryNoGroundTruthLabel`.

### Corrección de tres citas de la v1

La decisión pertinente de la bitácora es la **29**, no la 30. El test que afirma «sin mutadores» es
`RiskEvaluationIdentityTests.AppendOnlyEntitiesExposeNoPublicMutator`, no `ArchitectureSmokeTests`.
Y **`E5B` no dejó ningún bloque reservado** en el detalle de alerta: hay una grilla de dos bloques,
snapshot y vigente. Esta etapa agrega un tercero; no lo encuentra hecho.

## D2 — Reservar antes de llamar

La secuencia natural —llamar, recibir, insertar— abre dos ventanas verificadas:

- Un callback que llega **antes del commit** no encuentra fila, queda `UNMATCHED`, y **nada vuelve a
  mirarlo**.
- Dos solicitudes concurrentes pasan las dos el chequeo «no hay `PENDING`», llaman las dos al
  proveedor, y aunque una inserción falle, **el proveedor ya creó dos evaluaciones**. Es el
  check-then-act del hallazgo 4 de la Etapa 4, una capa más arriba.

**El flujo correcto tiene tres pasos:**

1. **Reservar.** Insertar `PENDING` con `referenceId`, `externalEvaluationId = null`, y
   `SaveChangesAsync`. El único parcial serializa las solicitudes concurrentes **cuando todavía no
   hay nada del lado del proveedor**: el `409` ocurre sin efectos externos.
2. **Llamar.** `EvaluateAsync` con timeout explícito. Actualizar la fila con `status` como token:
   identificador, estado, score, `settledAt`, `settledBy = SYNC`.
3. **Vincular tarde.** Antes de responder, buscar recibos `UNMATCHED` del mismo proveedor cuyo
   `externalEvaluationId` o `referenceId` coincidan, y aplicar su transición bajo la misma monotonía.
   La reconciliación también barre los `UNMATCHED` con más de N minutos.

Con el mock en proceso, **la primera ventana no se puede reproducir a mano**. Por eso el test es
obligatorio: un proveedor de prueba que invoque el caso de uso del callback **desde dentro** de
`EvaluateAsync` y recién entonces devuelva `PENDING`.

## D3 — Tabla de transiciones completa

| Origen | Mensaje | Efecto en la fila | Recibo | HTTP |
| --- | --- | --- | --- | --- |
| `PENDING` | `APPROVED` / `DENIED` / `ERROR` | Transiciona, `settledAt`, `settledBy` | `APPLIED` | 200 |
| `PENDING` | `PENDING` | `lastErrorCode` / `attemptCount` si aplica | `NO_OP` | 200 |
| Terminal | El **mismo** terminal | Ninguno | `NO_OP` | 200 |
| Terminal | `PENDING` | Ninguno — llegó fuera de orden | `SUPERSEDED` | 200 |
| Terminal | **Otro** terminal | Ninguno — el proveedor se contradice | `CONFLICTING` | 200 |
| — | Sin fila correlacionable | Ninguno | `UNMATCHED` | 202 |

Dos precisiones que la v1 confundía:

- **Fuera de orden ≠ contradicción.** `PENDING` después de `APPROVED` es la red; `DENIED` después de
  `APPROVED` es el proveedor diciendo dos cosas distintas. La v1 los trataba igual, «con marca de
  descarte», y se habría tragado la contradicción en silencio. `CONFLICTING` se muestra en la
  consola: «el proveedor envió un veredicto contradictorio».
- **Un reenvío del mismo estado con otro instante no es duplicado** —la clave difiere— y tiene que
  ser `NO_OP`, no conflicto.

Un duplicado nunca devuelve `409`: invitaría al proveedor a reintentar para siempre.

## D4 — Lo que cierra una evaluación y lo que no

La v1 mapeaba todo fallo a `ERROR` terminal. **Está mal**, y de una forma que el mock no puede
exponer porque no tiene red.

Un timeout es **indeterminado**: el proveedor pudo haber registrado la evaluación. Con `ERROR`
terminal, el callback posterior encuentra una fila terminal, la monotonía lo descarta y **el
veredicto real se pierde**; y la «evaluación nueva» crea una **segunda** evaluación en el proveedor,
con su costo y dos callbacks en camino.

| Fallo | Momento | Resultado |
| --- | --- | --- |
| Conexión rechazada, DNS | **Antes** de enviar | `ERROR` terminal, `errorCode = UNREACHABLE` |
| Rechazo definitivo del proveedor (`4xx` de validación) | Respuesta recibida | `ERROR` terminal, `errorCode = PROVIDER_REJECTED` |
| `TIMEOUT`, `5xx`, respuesta ilegible | **Después** de enviar | Sigue `PENDING`, `lastErrorCode`, sin `settledAt` |

La fila `PENDING` con fallo la cierra **la reconciliación**, que es exactamente para lo que existe.
Pasa a `ERROR` solo tras N reconciliaciones infructuosas o por acción explícita.

`AGENTS.md` ya lo pedía: «retries solo donde sean semánticamente seguros». Y §5.3 exige «política de
timeout, retry, backoff y reconciliación»: **esta es esa política**.

Ningún tipo del cliente HTTP cruza fuera de Infrastructure. El `errorCode` sale de un catálogo
cerrado, nunca del mensaje crudo del proveedor.

## D5 — Recibo y transición, una sola unidad de trabajo

**`CallbackReceipt`:** `id`, `provider`, `deduplicationKey`, `externalEvaluationId` nullable,
`referenceId` nullable, `status` (`APPLIED | NO_OP | SUPERSEDED | CONFLICTING | UNMATCHED`),
`receivedAt`, `processedAt`, `replayCount`, `lastSeenAt`.

- **Único sobre `(provider, deduplication_key)`**, no sobre la clave sola.
- **`DUPLICATE` no existe como estado.** Un duplicado es la *ausencia* de una segunda fila. La v1 era
  autocontradictoria: no se puede tener un único sobre la clave y a la vez una fila que la repita.
- El duplicado se detecta por **violación de unicidad** —códigos SQLite 2067 y 1555, como ya hace
  `EfAlertStore`—, responde `200` y actualiza `replayCount` y `lastSeenAt` sobre la fila existente.
- **Recibo y transición se persisten en un único `SaveChangesAsync`.** Si no, una transición que
  pierde la carrera deja el recibo vivo, el reintento del proveedor se ve como duplicado, responde
  `200`, y **la transición se pierde para siempre**.
- Si la unidad pierde por concurrencia, se revierte entera, el manejador **relee una vez**,
  reclasifica según D3 y persiste. Si vuelve a perder: `503` con `Retry-After`; el reintento converge
  porque para entonces la fila ya es terminal.

**La clave de deduplicación es texto canónico**, fijado en el diseño para que dos implementaciones
produzcan la misma: `provider|externalEvaluationId|status|providerInstant`. Nunca incluye
`receivedAt`. Si el proveedor no envía instante, la clave colapsa a proveedor + identificador +
estado, lo cual es inocuo porque el segundo mensaje sería `NO_OP`.

**No se persiste el payload externo completo**, según §7. Solo los campos correlacionantes. Logs
redactados.

## D6 — Doble correlación

El callback correlaciona por `(provider, externalEvaluationId)` y, si no hay coincidencia, por
`(provider, referenceId)`. El Blueprint §5.1 ya listaba las dos cosas por separado —«`referenceId`
estable por pedido» y «`externalEvaluationId` para correlación»— y la v1 usaba solo la segunda.

El mock debe devolver la referencia del pedido en su callback, igual que se le exigirá al adaptador
real.

## D7 — Reconciliación explícita, en `E6A`

`POST /api/external-evaluations:reconcile`. Recorre las `PENDING` con más de un umbral de
antigüedad, llama al proveedor por cada una y aplica las transiciones. Devuelve un resumen como la
corrida de scoring: consultadas, resueltas, sin cambios, con error, `UNMATCHED` vinculados.

- **El umbral es configurable, con 0 por defecto.** Con la latencia cero del mock, un umbral fijo
  haría que la demo no encontrara nada durante minutos.
- **La unidad de trabajo es por fila**: un `SaveChangesAsync` por evaluación, para que un conflicto
  no revierta el barrido entero.
- **Va en `E6A`, no en `E6B`.** Es la gemela de la solicitud —mismo puerto, mismo token, sin HTTP
  entrante— y es la única forma de probar «el estado pendiente puede finalizar» sin callback. Con la
  partición de la v1, `E6A` integrada dejaba toda fila `PENDING` sin salida.

**El puerto cambia**, y es un cambio de §5.2 que va a la bitácora:

```csharp
Task<ExternalEvaluationResult> GetStatusAsync(
    ExternalEvaluationLookup lookup,   // { ExternalEvaluationId?, ReferenceId }
    CancellationToken cancellationToken);
```

La firma del Blueprint recibe solo `externalEvaluationId`, y **no puede reconciliar** la fila que
quedó entre las dos fases de D2 ni la del timeout de D4, donde el identificador nunca llegó.

La reconciliación y el callback compiten por la misma fila. El token de concurrencia de D1 hace que
una pierda limpiamente.

## D8 — El mock, determinista y con bandas declaradas

Un hash uniforme sobre cuatro caminos daría ~75 pendientes entre 300: setenta y cinco disparos
manuales para que la demo termine. Y las divergencias caerían al azar.

El resultado se deriva de una **función documentada** del sufijo numérico de `merchantReferenceId`,
módulo 100, con bandas declaradas en el diseño:

| Resto | Camino |
| --- | --- |
| 0–74 | `APPROVED` |
| 75–89 | `DENIED` |
| 90–96 | `PENDING`, se cierra por callback o reconciliación |
| 97–99 | `ERROR` |

Sobre `demo-orders.v1.json` eso da **225 aprobados, 45 denegados, 21 pendientes y 9 con error**. El
brief fija esos conteos y un test los clava, como el dorado de fingerprints.

Así **quien escribe la fixture controla el resultado**, que es lo que permite que la Etapa 8 plante
divergencias a propósito en vez de encontrarlas por azar.

`GetStatusAsync` de una `PENDING` devuelve el estado terminal de la **misma** función, para que la
reconciliación la cierre. La latencia simulada es configurable y cero por defecto.

`KOIN_MODE` gobierna el registro, con `mock` por defecto. **`KOIN_MODE=sandbox` falla al arrancar**
con un mensaje explícito: §5.3 lista siete requisitos que no se cumplen. Fallar al arrancar es mejor
que registrar un adaptador que no existe.

## D9 — El disparador de demo es un endpoint de la API

La v1 decía «el callback lo dispara la consola con un botón». Las dos únicas formas de hacerlo
rompen algo:

- **Por el rewrite de Next**: el secreto tendría que estar en el navegador, prohibido por
  `AGENTS.md`.
- **Como acción de servidor**: el secreto vive en el proceso de Next, y §10 del Blueprint dice
  «Next.js no recibe claves de proveedores».

Y peor: si el cliente **compone el payload**, cualquiera con la consola abierta cierra cualquier
evaluación `PENDING` en el estado que elija. La monotonía protege a las terminales; a las pendientes
no las protege nada.

`POST /api/demo-data/external-callbacks:deliver`, registrado **solo bajo `DemoData:Enabled`**, con el
mismo patrón que el seed y las métricas. Recibe `{ externalEvaluationId }` o «todas las pendientes»,
le pide al mock su resultado determinista, y lo inyecta por **el mismo caso de uso** del callback:
mismo recibo, misma deduplicación, misma monotonía.

**El cliente elige qué evaluación, nunca qué estado.** Ningún secreto sale de la API.
`GET /api/system/capabilities` le dice a la consola si el disparador existe.

## D10 — El endpoint de callback falla cerrado

`POST /api/external-callbacks/{provider}`:

- Secreto compartido en cabecera, comparado con `CryptographicOperations.FixedTimeEquals`.
- **Secreto no configurado o vacío ⇒ `401` a toda petición**, o la ruta no se mapea. Nunca «sin
  secreto, pasa».
- `{provider}` validado contra el catálogo; desconocido ⇒ `404`.
- Límite explícito de tamaño de cuerpo.
- **Tolera campos desconocidos** en el payload: un proveedor que agrega un campo no debe romper la
  recepción.
- Variable de entorno declarada en `.env.example` y en §9 del Blueprint.

Se declara en el código y en el README que **este no es el mecanismo real**: §5.3 exige verificar el
mecanismo oficial de Koin, que típicamente es una firma sobre el cuerpo. El secreto compartido
demuestra que el problema está identificado y modelado, no resuelto para producción.

## D11 — La evaluación externa no abre alertas

`Alert.riskEvaluationId` tiene un único total y `riskScoreSnapshot` es obligatorio; una evaluación
externa puede no traer score. Y el predicado de `E4B` compara bandas derivadas del score local.
Enchufar la fuente externa obligaría a rediseñar escalada, severidad e índice.

No es una limitación: es la tesis. **El proveedor opina y el comercio decide.**

## D12 — La divergencia se expone y no se aritmetiza

Cuando el criterio local y el externo difieren, la consola muestra **las dos opiniones con sus
procedencias**, sin combinarlas en un veredicto.

**El score externo no se compara numéricamente con el local.** Son escalas distintas de sistemas
distintos; «externo 45 contra local 60» no significa nada. Lo que se contrasta son los **veredictos**:
aprobado contra denegado.

Vocabulario: hay tres «pendiente» distintos en el sistema —alerta `OPEN` esperando veredicto humano,
evaluación externa `PENDING` esperando al proveedor, y pedido sin corrida de scoring—. La UI los
nombra distinto y nunca usa «pendiente» a secas.

Para la Etapa 7: el bloque externo entra en `AlertDetail` como **sub-objeto nullable propio**
—`externalEvaluation: { … } | null`—, de modo que `explanation` y sus hermanos se agreguen al lado
sin tocarlo. Y el veredicto externo puede **citarse como opinión del proveedor, nunca usarse como
fundamento** de una explicación generada.

## D13 — La migración, acotada

La v1 afirmaba que la restricción de completitud «deja de ser condicional» y el índice «deja de
necesitar filtro», como si fueran gratis. No lo son: quitar el filtro rompe un test que lo afirma
sobre `sqlite_master`, y hacer la completitud incondicional propaga cambios de nulabilidad a seis
archivos de aplicación y a todos los tests con `row.Score!.Value`.

**Alcance de la migración de `E6A`, y nada más:**

1. Eliminar `external_evaluation_id` y `error_code` de `risk_evaluations`. **Verificado ejecutando**:
   328 filas antes y después, los 328 fingerprints byte a byte idénticos, `integrity_check` ok,
   claves foráneas intactas.
2. En la **misma** reconstrucción, endurecer `ck_risk_evaluations_source` a `source = 'LOCAL'` y
   `ck_risk_evaluations_status` a `('APPROVED', 'DENIED')`. Verificado que ninguna fila usa otro
   valor.
3. Quitar los miembros externos de `RiskEvaluationSource` y `RiskEvaluationStatus`. El fingerprint
   solo hashea `"LOCAL"`, así que el test dorado no se entera.
4. Crear `external_evaluations` y `callback_receipts`.

**El índice parcial y la nulabilidad quedan como están.**

`source` **permanece** en `RiskEvaluation`: está dentro del material que se hashea, y quitarla
invalidaría las 328 evaluaciones.

**Antes de migrar, copia de `salvo.db`**: EF ejecuta `PRAGMA foreign_keys = 0` fuera de transacción
y lo advierte.

**Artefactos que la etapa obliga a tocar y que el brief debe listar**: `RiskEvaluationIdentityTests`
(dos aserciones se caen), `ArchitectureSmokeTests`, `OpenApiDriftTests` (exige recaptura),
`frontend/openapi/salvo-openapi.json` y `schema.d.ts` (regenerar), `messages.ts` y su test, las
fixtures del frontend, `boundary.test.ts` y los textos de `smoke-ui.sh`.

## D14 — Partición

| Ítem | Alcance | Depende de |
| --- | --- | --- |
| `E6A-PROVEEDOR` | Entidad, tipos propios, migración, `IAntifraudProvider`, `MockAntifraudProvider`, reserva en dos fases, degradación, **reconciliación**, endpoints de solicitud y lectura | — |
| `E6B-CALLBACK-UI` | `CallbackReceipt`, endpoint de callback autenticado, vinculación tardía de `UNMATCHED`, disparador de demo, bloque en `/alerts/[id]`, acción en `/import`, divergencia | `E6A` integrada |

---

## Endpoints y códigos

| Ruta | Semántica |
| --- | --- |
| `POST /api/orders/{orderId}/external-evaluations` | Idempotente. Con `PENDING` vigente devuelve esa fila y `applied: false`. Con terminal vigente devuelve esa fila, salvo pedido explícito de una nueva, permitido solo si la última es `ERROR` |
| `GET /api/orders/{orderId}/external-evaluations` | Historial del pedido |
| `GET /api/external-evaluations/{id}` | Una evaluación |
| `POST /api/external-evaluations:reconcile` | Barrido, con resumen |
| `POST /api/external-callbacks/{provider}` | Autenticado, para llamadores externos |
| `POST /api/demo-data/external-callbacks:deliver` | Solo con `DemoData:Enabled` |

| `code` | HTTP |
| --- | --- |
| `EXTERNAL_EVALUATION_PENDING` | 409 |
| `EXTERNAL_EVALUATION_NOT_FOUND` | 404 |
| `PROVIDER_NOT_REGISTERED` | 404 |
| `CALLBACK_UNAUTHORIZED` | 401 |
| `RECONCILIATION_CONFLICT` | 409 |

**Un fallo del proveedor no es un `5xx` para la consola.** La fila queda en `ERROR` o `PENDING` y la
respuesta es `200` con la fila.

## Tests obligatorios

- Duplicado exacto: `200`, una transición, un recibo.
- Fuera de orden `APPROVED → PENDING`: sigue `APPROVED`, recibo `SUPERSEDED`.
- Contradicción `APPROVED → DENIED`: sigue `APPROVED`, recibo `CONFLICTING`.
- **Callback antes del commit**: proveedor de prueba que llama al callback desde dentro de
  `EvaluateAsync`. Con el flujo ingenuo la fila queda `PENDING` para siempre.
- **Carrera reconciliación–callback sobre base en archivo**, con `SalvoApiFactory.WithFileDatabase`:
  una transición, un recibo.
- Timeout y luego `GetStatusAsync = DENIED`: **una sola fila**, en `DENIED`.
- `ERROR` y solicitud nueva: segunda fila permitida. Con `PENDING` vigente: `409`.
- Secreto ausente o incorrecto: `401` y **cero** recibos.
- Proveedor que lanza: `POST /api/risk-evaluations:run` responde igual y el test dorado no cambia.
- **Diferencial**: `GET /api/dashboard`, `GET /api/alerts?sort=SCORE_DESC` y
  `GET /api/evaluation-metrics` byte a byte idénticos antes y después de crear evaluaciones externas
  en los cuatro estados.
- Conteos del mock sobre la fixture: 225 / 45 / 21 / 9.

## Qué queda fuera

- `KoinSandboxProvider` y toda llamada real. §5.3 no se cumple.
- Device fingerprint oficial y callback público en Internet.
- Combinar el criterio local y el externo en un veredicto único.
- Alertas originadas por evaluación externa.
- Reintentos automáticos, backoff o trabajos en segundo plano.
- Una ruta `/orders`. Queda como candidata de Etapa 8.
- Modificar el motor, `RuleConfig`, el fingerprint, la semántica de alertas o el dashboard.
- Explicabilidad (Etapa 7) y fixture enriquecida (Etapa 8).

## Cambios de estado canónico que exige este diseño

1. **Blueprint §5.1**: la correlación es doble, por identificador y por referencia.
2. **Blueprint §5.2**: nueva firma de `GetStatusAsync`.
3. **Blueprint §7**: `RiskEvaluation` pierde `externalEvaluationId` y `errorCode` y su `source` queda
   en `LOCAL`; se agregan `ExternalEvaluation` y `CallbackReceipt` con su forma real.
4. **Blueprint §4**: sección de evaluación externa con la tabla de transiciones.
5. **Blueprint §9**: variable del secreto del callback en `.env.example`.
6. **`AGENTS.md`**: la regla sobre `risk_evaluations.status` se reescribe, no se elimina.
7. **Bitácora**, entradas 44 en adelante.
