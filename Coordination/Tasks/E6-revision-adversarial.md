# Salvo — Revisión adversarial del diseño propuesto de Etapa 6

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-04
> Revisor: Claude
> Objeto: `Coordination/Tasks/E6-DISENO.md` v1 (base `main` en `9daa6cc`)
> Método: lectura de Blueprint §3, §4, §5, §6, §7, §9, §10, §11 y bitácora completa, `AGENTS.md`,
> `E4-DISENO.md`, `E5-DISENO.md`, los briefs `E4A`, `E4B`, `E5A`, `E5B` y `E5C`, y del código real de
> `Salvo.Domain/Risk`, `Salvo.Domain/Alerts`, `Salvo.Domain/Orders`, `Salvo.Application/Risk`,
> `Salvo.Application/Alerts`, `Salvo.Application/Dashboard`, `Salvo.Application/Metrics`,
> `Salvo.Application/Orders`, `Salvo.Api`, `Salvo.Infrastructure/Persistence` con sus tres
> migraciones y el snapshot del modelo, `backend/tests` completo, `frontend/src/lib/api`,
> `frontend/src/app/alerts`, `frontend/src/app/import`, `frontend/src/test`, `next.config.ts`,
> `scripts/check.sh` y `scripts/smoke-ui.sh`. Las cifras se verificaron con consultas de solo lectura
> sobre `backend/src/Salvo.Api/salvo.db` (ignorada por Git; 7 corridas). La pregunta 2 se respondió
> **ejecutando la migración de verdad** sobre una copia del repositorio y de la base en el
> scratchpad de la sesión. No se editó ningún archivo versionado salvo este informe.

## Hallazgos, por severidad

### 1. D3 y pregunta 4, alta. Correlar solo por `externalEvaluationId` deja una ventana en la que el callback no puede vincularse, y nada lo vincula después

La entidad de D1 tiene `externalEvaluationId` «nullable hasta que el proveedor lo asigne» y D3
correlaciona **únicamente** por ese identificador (`E6-DISENO.md:156-158`). No tiene `referenceId`,
aunque el Blueprint §5.1 lista las dos cosas por separado —«`referenceId` estable por pedido» y
«`externalEvaluationId` para correlación» (`DesignAgent/Salvo-Blueprint.md:253-254`)— y la
checklist de la Etapa 6 exige «Correlación por referencias» (`DesignAgent/Salvo-Progress.md:207`).
El pedido ya tiene esa referencia estable: `Order.Reference` es `(merchantId, merchantReferenceId)`
(`backend/src/Salvo.Domain/Orders/Order.cs:76`).

La secuencia natural de implementación —llamar a `EvaluateAsync`, recibir `PENDING` con el
identificador, construir la fila, `SaveChangesAsync`— abre dos ventanas:

- **Callback antes del commit.** Entre que el proveedor responde y la fila se persiste, un callback
  con ese identificador no encuentra fila, se registra como `UNMATCHED`, responde `202` y, por
  decisión explícita del diseño, «no debe provocar reintentos». Después la fila se persiste en
  `PENDING` y **ningún paso vuelve a mirar los recibos `UNMATCHED`**. La única salida es la
  reconciliación de D5, que es manual y además espera «más de un umbral de antigüedad».
- **Dos solicitudes concurrentes sobre el mismo pedido.** Las dos pasan el chequeo «no hay
  `PENDING`» antes de que ninguna inserte, las dos llaman al proveedor, una inserción falla por el
  único parcial y responde `409`, pero el proveedor ya creó dos evaluaciones y enviará dos
  callbacks: el segundo queda `UNMATCHED` para siempre. Es el mismo check-then-act del hallazgo 4
  de la revisión de E4, una capa más arriba.

Con el mock en proceso y el callback disparado por un botón, la primera ventana **no se puede
reproducir a mano en la demo**, que es exactamente la razón por la que llegaría a producción sin que
nadie la viera.

Corrección concreta, en dos fases y con doble correlación:

1. **Reservar antes de llamar.** Insertar la fila `PENDING` con `referenceId = Order.Reference`,
   `externalEvaluationId = null` y `SaveChangesAsync` **antes** de tocar al proveedor. El único
   parcial de D1 serializa las solicitudes concurrentes cuando todavía no hay nada en el proveedor:
   el `409` se produce sin efectos externos.
2. Llamar a `EvaluateAsync` con timeout y actualizar la fila con `status` como token: identificador,
   estado, score, `settledAt`.
3. **Vincular tarde.** Antes de responder, buscar recibos `UNMATCHED` del mismo proveedor cuyo
   `referenceId` o `externalEvaluationId` coincidan y aplicar su transición bajo la misma monotonía.
   La reconciliación también barre los `UNMATCHED` con más de N minutos.
4. El callback correlaciona por `(provider, externalEvaluationId)` y, si no hay coincidencia, por
   `(provider, referenceId)`; el proveedor devuelve la referencia del pedido en su callback, y el
   mock debe hacerlo también.

Test que lo demuestra: un `IAntifraudProvider` de prueba que, **desde dentro** de `EvaluateAsync`,
invoque el caso de uso del callback con el veredicto final y recién entonces devuelva `PENDING`. Con
el flujo ingenuo el recibo queda `UNMATCHED` y la fila `PENDING` para siempre; con las dos fases la
fila termina en el estado del callback.

### 2. D6 y D2, alta. `ERROR` terminal tras un `TIMEOUT` pierde el veredicto del proveedor y duplica la evaluación

D6 mapea `TIMEOUT`, `UNREACHABLE`, `PROVIDER_ERROR` e `INVALID_RESPONSE` a `status = ERROR`
(`E6-DISENO.md:190-194`) y D2 hace `ERROR` terminal: «un error del proveedor no se reintenta sobre la
misma fila: se solicita una evaluación nueva» (`E6-DISENO.md:136-138`).

Un timeout es un resultado **indeterminado**: el proveedor pudo haber registrado la evaluación y va
a responder por callback o a `GetStatusAsync`. Con `ERROR` terminal, el callback que llega después
encuentra una fila terminal y la monotonía de D2 lo descarta (terminal → terminal, que además el
diseño no define; hallazgo 5): el veredicto real se pierde. Y la «evaluación nueva» que D2 prescribe
crea una **segunda** evaluación en el proveedor para el mismo pedido, con costo y con dos callbacks
en camino. `AGENTS.md:127` dice «retries solo donde sean semánticamente seguros», y §5.3 exige una
«política de timeout, retry, backoff y reconciliación» (`Salvo-Blueprint.md:292`): esta es la
política, y está al revés.

El mock no puede exponerlo porque no tiene red y D7 fija latencia cero por defecto. El diseño
enviaría una máquina de estados correcta para la demo y equivocada para el adaptador real, que el
post-MVP tendría que rehacer junto con su migración.

Corrección concreta: separar «no se envió» de «se envió y no se sabe».

| Fallo | Momento | Estado resultante |
| --- | --- | --- |
| `UNREACHABLE` (conexión rechazada, DNS) | Antes de enviar | `ERROR` terminal |
| Rechazo definitivo del proveedor (validación, `4xx`) | Respuesta recibida | `ERROR` terminal |
| `TIMEOUT`, `INVALID_RESPONSE`, `5xx` | Después de enviar | Sigue `PENDING`, con `lastErrorCode` y `attemptCount + 1` |

La fila `PENDING` con error la cierra la reconciliación por referencia (hallazgo 11), que es para lo
que existe; pasa a `ERROR` solo tras N reconciliaciones fallidas o por acción explícita. Tests con un
proveedor que inyecta fallos, con el patrón de `ConflictingScoringRunStore`
(`backend/tests/Salvo.Api.IntegrationTests/ScoringRunPersistenceTests.cs:370-408`): uno que lanza
`TimeoutException` tras «aceptar» y luego responde `DENIED` a `GetStatusAsync` debe terminar con
**una sola fila** en `DENIED`.

### 3. D8 y pregunta 6, alta. El botón de demo no puede pasar por el endpoint de callback autenticado

`next.config.ts:9-13` reescribe `/api/:path*` hacia la API: **toda** ruta de la API es alcanzable
desde el navegador a través del servidor Next, incluidos el callback y la reconciliación que E6
agregue. D8 dice que «en la demo local el callback lo dispara la propia consola con un botón»
(`E6-DISENO.md:226`). Hay exactamente dos maneras de hacerlo y las dos rompen algo:

- Si el botón llama al endpoint de callback **a través del rewrite**, el secreto tiene que estar en
  el navegador: `NEXT_PUBLIC_`, prohibido por `AGENTS.md:120`.
- Si el botón es una acción de servidor —el patrón de `frontend/src/app/import/actions.ts:127-160`—
  el secreto vive en el proceso de Next. Blueprint §10: «Next.js no recibe claves de proveedores»
  (`Salvo-Blueprint.md:472`). Y el mismo secreto en dos procesos.

En cualquiera de las dos, si la acción **compone el payload** —estado, score, instante— cualquier
persona con la consola abierta cierra cualquier evaluación `PENDING` en el estado que elija. La
monotonía protege a las terminales; a las pendientes no las protege nada. El determinismo de D7 queda
anulado por diseño: una demo legítima y una manipulada producen el mismo estado.

Corrección concreta: el disparador de demo es un endpoint de la **API**, registrado solo bajo
`DemoData:Enabled` con el patrón de `backend/src/Salvo.Api/OrderEndpoints.cs:32-39` y
`EvaluationMetricsEndpoints.cs:21-24`, por ejemplo `POST /api/demo-data/external-callbacks:deliver`
con `{ externalEvaluationId }` o «todas las pendientes». Ese endpoint le pide a
`MockAntifraudProvider` su resultado determinista para esa evaluación y lo inyecta por **el mismo caso
de uso** del callback: mismo recibo, misma deduplicación, misma monotonía. El cliente elige **qué**
evaluación, nunca **qué estado**; ningún secreto sale de la API; `CapabilitiesResponse`
(`backend/src/Salvo.Api/SystemEndpoints.cs:19-26`) le dice a la consola si el disparador existe. El
endpoint autenticado `POST /api/external-callbacks/{provider}` queda para llamadores externos y lo
ejercitan solo los tests de integración, con el secreto fijado por `SalvoApiFactory`. Debe fallar
cerrado: secreto no configurado o vacío → `401` a toda petición, o ruta no mapeada.

### 4. D3, alta. Un único sobre `deduplicationKey` no admite una fila `DUPLICATE`, y recibo y transición tienen que ser una sola unidad de trabajo

D3 define `CallbackReceipt` con «`deduplicationKey` único» y a la vez «un duplicado devuelve `200`,
se registra como `DUPLICATE`» (`E6-DISENO.md:144-149`). Las dos cosas no pueden ser ciertas: una
segunda fila con la misma clave viola el índice único. O el único desaparece y la deduplicación pasa
a ser una consulta previa —el check-then-act de siempre—, o `DUPLICATE` no es una fila.

El problema de fondo es la atomicidad. Si el recibo se inserta como `ACCEPTED` y la transición se
aplica en otro `SaveChangesAsync`, y esa transición falla —D5 dice que callback y reconciliación
compiten por la misma fila, y el token de concurrencia hace perder a uno—, el recibo sobrevive y la
transición no. El reintento del proveedor trae la misma clave, se clasifica `DUPLICATE`, responde
`200` y **la transición se pierde para siempre**. Es la forma exacta del hallazgo 4 de E4.

Corrección concreta:

- Único sobre `(provider, deduplication_key)`, no sobre la clave sola.
- Recibo y transición en **un único** `SaveChangesAsync`.
- El duplicado exacto se detecta por la violación de unicidad —`SqliteExtendedErrorCode` 2067 o
  1555, como ya hace `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs:257-261`— y
  responde `200` sin fila nueva; opcionalmente un `UPDATE` de `replay_count` y `last_seen_at` sobre
  la existente.
- Si la unidad pierde la carrera (`DbUpdateConcurrencyException`), se revierte entera; el
  manejador **relee una vez** y reclasifica —mismo terminal: `NO_OP`; otro terminal: `CONFLICTING`—,
  persiste y responde `200`. Si vuelve a perder, `503` con `Retry-After`: el reintento converge
  porque para entonces la fila es terminal.
- Estados del recibo: `APPLIED | NO_OP | SUPERSEDED | CONFLICTING | UNMATCHED`. `DUPLICATE` no
  existe: es la ausencia de una segunda fila.

### 5. D2 y D3, media. La máquina de estados no define terminal → terminal, y «fuera de orden» no es lo mismo que «contradicción»

D2 dice «los tres terminales no transicionan a nada, ni entre ellos» y D3 solo trata «una transición
hacia un estado no terminal sobre una evaluación ya terminal» (`E6-DISENO.md:152-154`). Quedan sin
definir dos casos que van a ocurrir en la primera semana:

- **Mismo terminal otra vez.** Una respuesta síncrona `APPROVED` cuya fila ya es terminal y el
  callback `APPROVED` que el proveedor envía igual; o un reenvío con otro instante, que por la clave
  de D3 **no** es duplicado (pregunta 5). Tiene que ser un `NO_OP` con `200`, no un conflicto.
- **Otro terminal.** `DENIED` después de `APPROVED` es una contradicción del proveedor, no un mensaje
  fuera de orden. «`ACCEPTED` con marca de descarte» la tragaría en silencio.

Con el hallazgo 2, además, `PENDING → PENDING` con `lastErrorCode` y `attemptCount` es una
autotransición legítima que el token sobre `status` no distingue de una escritura concurrente sin
cambios; es aceptable, pero hay que decirlo.

Corrección concreta: una tabla de transiciones en el diseño —origen × destino → efecto sobre la fila,
estado del recibo, código HTTP—, con la contradicción como `CONFLICTING`: fila intacta, `200`, log
redactado y visible en la consola como «el proveedor envió un veredicto contradictorio». Y
restricciones de consistencia como `ck_alerts_review_consistency`
(`backend/src/Salvo.Infrastructure/Persistence/Configurations/AlertConfiguration.cs:32-34`):
terminal ⇔ `settled_at IS NOT NULL`; `error_code IS NOT NULL ⇒ status = 'ERROR'`, con
`last_error_code` aparte para el `PENDING` con fallos.

### 6. D10, media. El diseño no fija criterios de aceptación ni tests, y la reconciliación está en el ítem equivocado

`E4-DISENO.md` y `E5-DISENO.md` enumeran los tests de cada ítem; `E6-DISENO.md` no enumera ninguno,
y el Blueprint fija la verificación de la etapa: «callbacks duplicados no repiten efectos y el estado
pendiente puede finalizar» (`Salvo-Blueprint.md:536`). Lista mínima, cada uno atado a un hallazgo:

- Duplicado exacto: `200`, una transición, un recibo (hallazgo 4).
- Fuera de orden `APPROVED` → `PENDING`: sigue `APPROVED`, recibo `SUPERSEDED` (D2).
- Contradicción `APPROVED` → `DENIED`: sigue `APPROVED`, recibo `CONFLICTING` (hallazgo 5).
- Callback antes del commit, con el proveedor de prueba que llama al callback desde `EvaluateAsync`
  (hallazgo 1).
- Carrera reconciliación–callback sobre **base en archivo**, con `SalvoApiFactory.WithFileDatabase`
  y el patrón de compuerta de `AlertReviewTests.cs:105-143`: una transición, un recibo.
- `ERROR` y solicitud nueva: segunda fila permitida; con `PENDING` vigente, `409` (pregunta 3).
- Timeout y luego `GetStatusAsync` = `DENIED`: una sola fila, en `DENIED` (hallazgo 2).
- Secreto ausente o incorrecto: `401` y cero recibos (D8).
- Proveedor que lanza: `POST /api/risk-evaluations:run` responde igual y el test dorado de
  fingerprints no cambia (`AGENTS.md:128`).
- **Diferencial**: `GET /api/dashboard`, `GET /api/alerts?sort=SCORE_DESC` y
  `GET /api/evaluation-metrics` byte a byte idénticos antes y después de crear evaluaciones externas
  en los cuatro estados, con el molde de `DashboardEndpointTests.cs:26-41`.
- `ExternalEvaluation` **fuera** de `AppendOnlyEntitiesExposeNoPublicMutator` y **dentro** de
  `PersistedRiskEntitiesCarryNoGroundTruthLabel`.

Partición: D10 pone la reconciliación en `E6B`. Es la gemela de la solicitud —mismo puerto, mismo
token, ningún HTTP entrante— y la única forma de probar «el estado pendiente puede finalizar» sin
callback. Con `E6A` integrada tal como está partida, toda fila `PENDING` queda sin salida hasta
`E6B`. Mover la reconciliación a `E6A`; `E6B` queda con endpoint de callback, recibos, disparador de
demo y consola.

### 7. D1, media. «Deja de ser condicional» y «deja de necesitar filtro» tienen un radio que el diseño no declara, y deja enumeraciones colgadas

D1 afirma que la restricción de completitud «deja de ser condicional» y que «el índice único de
fingerprint deja de necesitar filtro» (`E6-DISENO.md:101-102`), como si fueran consecuencias
gratuitas. Contra el código:

- Quitar el filtro del índice rompe
  `TheFingerprintIndexIsUniqueOnlyForLocallyProducedEvaluations`, que afirma
  `WHERE source = 'LOCAL'` sobre `sqlite_master`
  (`backend/tests/Salvo.Api.IntegrationTests/ScoringRunPersistenceTests.cs:249-251`).
- Hacer la completitud incondicional es `NOT NULL` sobre cuatro columnas: otra reconstrucción de
  tabla. Y si el dominio la acompaña, `int? Score`, `string? SignalsJson`, `string? RuleConfigVersion`
  y `string? EvaluationFingerprint` (`backend/src/Salvo.Domain/Risk/RiskEvaluation.cs:52-60`) se
  propagan a `RunScoringHandler.cs:74-75`, `AlertProjection.cs:96`, `IEvaluationMetricsReader.cs:15-19`,
  `EfEvaluationMetricsReader.cs:36`, `GetEvaluationMetricsHandler.cs:31` y a todos los tests con
  `row.Score!.Value`.
- Lo que queda colgado sin que el diseño lo nombre: `ck_risk_evaluations_source` sigue admitiendo
  `EXTERNAL_MOCK` y `KOIN_SANDBOX`
  (`backend/src/Salvo.Infrastructure/Persistence/Configurations/RiskEvaluationConfiguration.cs:16-18`),
  igual que `RiskEvaluationSource.ExternalMock` y `KoinSandbox` (`RiskEvaluationSource.cs:8-13`) y
  `RiskEvaluationStatus.Pending` y `Error` (`RiskEvaluationStatus.cs:9-15`). Si `ExternalEvaluation`
  reutiliza `RiskEvaluationStatus`, las dos entidades comparten enumeración: la mezcla que D1 combate
  a nivel de tabla, reintroducida a nivel de tipo.

Corrección concreta, mínima y explícita para `E6A`: eliminar las dos columnas (seguro, pregunta 2) y,
en la **misma** reconstrucción, endurecer `ck_risk_evaluations_source` a `source = 'LOCAL'` y
`ck_risk_evaluations_status` a `('APPROVED', 'DENIED')`; verificado que ninguna fila usa otro valor
(307 `APPROVED`, 21 `DENIED`, 328 `LOCAL`). Quitar de las enumeraciones los miembros externos y sus
nombres de cable: el fingerprint solo hashea `"LOCAL"` (`RiskEvaluationFingerprint.cs:30-32`), así
que el test dorado no se entera. `ExternalProvider` y `ExternalEvaluationStatus` nacen en
`Salvo.Domain/External/`, sin compartir nada con `Risk/`. El índice parcial y la nulabilidad quedan
como están, o su endurecimiento se lista como ítem aparte con sus ediciones de tests.

### 8. D4 y D10, media. «El listado de pedidos» no existe en la consola, y los disparadores no tienen pantalla

D4 dice que la evaluación externa «se muestra en el listado de pedidos, para pedidos sin alerta»
(`E6-DISENO.md:168`). `frontend/src/app/` tiene `alerts`, `dashboard` e `import`; `GET /api/orders`
se usa una sola vez y solo para contar (`frontend/src/lib/api/alerts.ts:88-94`). El Blueprint §4.4
no lista ninguna ruta `/orders`. Y `E6B` promete «disparadores» sin decir desde dónde se solicita una
evaluación para un pedido que no tiene alerta.

Corrección concreta, a elegir en el brief: (a) agregar `/orders` —ruta dinámica, guardas, tests de
frontera, textos del smoke, §4.4 corregido—, que es una pantalla entera no presupuestada; o (b) en
E6 el bloque externo y la acción «solicitar evaluación externa» viven en `/alerts/[id]`, y una
acción de demo «evaluar el corpus vigente con el proveedor» en `/import`, visible solo con
`demoDataEnabled`, cubre a los pedidos sin alerta. Recomiendo (b) y dejar `/orders` para E8 si se
quiere.

### 9. D10, media. La solicitud de evaluación no tiene endpoint definido ni es idempotente

`E6A` incluye «solicitud de evaluación» y «`GET`/`POST` de evaluación externa» sin rutas ni
semántica. `AGENTS.md:111` exige que repetir «callback» no duplique efectos y el mismo principio vale
para la solicitud: D1 dice «un pedido puede tener varias evaluaciones externas a lo largo del
tiempo», así que un doble clic son dos llamadas al proveedor y dos evaluaciones en Koin.

Corrección concreta:

- `POST /api/orders/{orderId}/external-evaluations`, idempotente: con `PENDING` vigente responde
  `200` con esa fila y `applied: false`, como `AlertReviewResult.Applied`; con `APPROVED` o
  `DENIED` vigente, `200` con esa fila salvo pedido explícito de una nueva, permitido solo cuando
  la última es `ERROR`.
- `GET /api/orders/{orderId}/external-evaluations` y `GET /api/external-evaluations/{id}`.
- Tabla de códigos con mensaje y recuperación, como exige E5 D13 y ya implementa
  `frontend/src/lib/api/messages.ts:29-198`; el test de E5 anticipa uno
  (`messages.test.ts:151-158`): `EXTERNAL_EVALUATION_PENDING` `409`,
  `EXTERNAL_EVALUATION_NOT_FOUND` `404`, `PROVIDER_NOT_REGISTERED` `404`,
  `CALLBACK_UNAUTHORIZED` `401`, `RECONCILIATION_CONFLICT` `409`. Un fallo del proveedor **no** es
  un `5xx` para la consola: la fila queda en `ERROR` o `PENDING` y la respuesta es `200` con la fila.
- `attemptCount`: con una fila por reintento vale siempre 1. Definirlo como sondeos de
  reconciliación, o quitarlo.

### 10. D7, media. «Determinista» no dice qué distribución produce sobre la fixture, y la demo depende de eso

D7 deriva el resultado «de un hash estable de la referencia» (`E6-DISENO.md:200`). Un hash uniforme
sobre cuatro caminos da ~75 pedidos `PENDING` entre 300: 75 callbacks manuales para que la demo
termine. Las divergencias de D9 —local aprueba, externo deniega— caen al azar, y la candidata de E8
«enriquecer la fixture con casos duros» (`Coordination/Workboard.md:33-45`) cambiaría la distribución
sin que nadie lo decida.

Corrección concreta: derivar el resultado de una función **documentada** de `merchantReferenceId`,
por ejemplo el sufijo numérico módulo 100 con bandas declaradas —`APPROVED` 0–74, `DENIED` 75–89,
`PENDING` 90–96, `ERROR` 97–99—, de modo que quien escriba la fixture controle el resultado; fijar en
el brief los conteos que eso produce sobre `demo-orders.v1.json` y clavarlos en un test como el
dorado; garantizar al menos N divergencias deliberadas; que `GetStatusAsync` de un `PENDING`
devuelva el resultado terminal de la misma función, para que la reconciliación lo cierre; y que el
disparador del hallazgo 3 pueda cerrar todas las pendientes de una vez.

### 11. D5 y §5.2, media. La reconciliación necesita más contrato del que el Blueprint define

`IAntifraudProvider.GetStatusAsync(string externalEvaluationId, …)`
(`Salvo-Blueprint.md:271-272`) no puede reconciliar una fila sin identificador: la que quedó entre
las dos fases del hallazgo 1, o la del timeout del hallazgo 2 donde el identificador nunca llegó.
Hace falta consulta por referencia —`GetStatusAsync(ExternalEvaluationLookup { ExternalEvaluationId?,
ReferenceId })`— o reemitir `EvaluateAsync` con la misma referencia confiando en la idempotencia del
proveedor. Es un cambio de §5.2 y va a la bitácora.

Además: el «umbral de antigüedad» de D5 contra la «latencia cero por defecto» de D7 hace que en la
demo la reconciliación no encuentre nada durante los primeros minutos; el umbral debe ser
configurable con 0 por defecto para el mock. La unidad de trabajo debe ser **por fila** —un
`SaveChangesAsync` por evaluación— para que un conflicto no revierta el barrido entero. Y para la
procedencia que exige la decisión 39, la fila debería registrar `settledBy: SYNC | CALLBACK |
RECONCILIATION`.

### 12. Problema 1 y D4, baja. Tres citas del diseño no coinciden con el código ni con los documentos

- «La decisión 30 de la bitácora y el brief de E4A lo dicen con todas las letras»
  (`E6-DISENO.md:30`): la cita es de `E4-DISENO.md:66-69`. La decisión 30
  (`Salvo-Blueprint.md:601`) es la serialización canónica; la pertinente es la 29 (`:600`). El brief
  de E4A solo dice «el resto se define para no rehacer el schema en E6»
  (`Coordination/Tasks/E4A-PERSISTENCIA.md:45-47`).
- «`ArchitectureSmokeTests` no puede afirmar «sin mutadores»» (`E6-DISENO.md:42`): ese test es
  `RiskEvaluationIdentityTests.AppendOnlyEntitiesExposeNoPublicMutator`
  (`backend/tests/Salvo.Domain.Tests/RiskEvaluationIdentityTests.cs:126-137`);
  `ArchitectureSmokeTests.cs` no contiene esa afirmación.
- «El bloque que `E5B` dejó reservado» (`E6-DISENO.md:167`): no existe. El detalle es una grilla de
  dos bloques, snapshot y vigente (`frontend/src/app/alerts/[id]/page.tsx:69-75`); el brief de E5B no
  lo menciona y `E5-DISENO.md:411` lo prometió sin que se construyera. Es inocuo, pero el brief de
  E6B tiene que decir «agregar un tercer bloque», no «llenar el reservado».

### 13. D8, baja. Endurecimientos que faltan en el endpoint de callback

- La variable del secreto no existe: ni en `.env.example:10-15` ni en Blueprint §9
  (`Salvo-Blueprint.md:446-455`). Nombrarla y documentar que vacía significa cerrado.
- `{provider}` en la ruta se valida contra los proveedores registrados; un valor desconocido es
  `404` sin recibo, no `UNMATCHED`.
- Límite de tamaño de cuerpo pequeño y explícito; un callback pesa bytes.
- **Tolerar campos desconocidos.** La importación rechaza `UNKNOWN_FIELD` a propósito; un proveedor
  real agrega campos sin avisar y el callback no puede caerse por eso.
- Comparación con `CryptographicOperations.FixedTimeEquals`, sobre bytes de la misma longitud.
- Declarar que `202` para `UNMATCHED` es semántica interna: todo proveedor trata cualquier `2xx`
  como entregado.

### 14. D1 y D10, baja. Artefactos que la etapa obliga a tocar y el diseño no lista

- `RiskEvaluationIdentityTests.cs:35-36` afirma `ExternalEvaluationId` y `ErrorCode` nulos: deja
  de compilar con la migración. Verificado en la copia: con esas dos líneas quitadas, todo vuelve a
  pasar.
- `ArchitectureSmokeTests.PersistedRiskEntitiesCarryNoGroundTruthLabel` (`:45-62`) debe incluir
  `ExternalEvaluation` y `CallbackReceipt`.
- `OpenApiDriftTests` falla hasta recapturar el documento (`npm run api:capture`, `api:types`).
- Las fixtures del frontend (`frontend/src/test/fixtures.ts:39-75`) tienen que llevar la clave
  nueva de `AlertDetail`: `projectNullable` devuelve `undefined` ante una clave ausente
  (`frontend/src/lib/api/guards.ts:154-163`) y el detalle entero cae en `malformed`. Todos los tests
  de `src/app/alerts/[id]` se rompen hasta entonces.
- `scripts/smoke-ui.sh` gana textos del bloque externo; `AGENTS.md:50` se reescribe;
  `.env.example` y README declaran D8.
- Operación: la migración reconstruye la tabla y EF Core emite `PRAGMA foreign_keys = 0` **fuera**
  de la transacción; la herramienta lo advierte (capturado en la copia: «cannot be executed in a
  transaction… would be left in a partially applied state»). El brief debe exigir copia de
  `salvo.db` antes de `database update`, con la regla del brief de E4A de no borrar ni recrear la
  base local.

### 15. D7 y D9, baja. `KOIN_MODE=sandbox` sin adaptador, tres «PENDING» y la escala del score externo

- `KOIN_MODE=sandbox` sin `KoinSandboxProvider` tiene que **fallar al arrancar**, como hace
  `AlertPolicy.E4V1.Validate` en `backend/src/Salvo.Api/Program.cs:20`, nunca caer al mock en
  silencio.
- En el detalle van a convivir el `PENDING` externo, el `explanationStatus: PENDING` de E7
  (`Salvo-Blueprint.md:378`) y el `OPEN` de la alerta. Cada uno con su procedencia en el texto.
- La escala del `score` externo es desconocida. D9 compara **estados**, no scores; el score externo
  se muestra con el rótulo del proveedor y nunca al lado del 0–100 local como si fueran comparables.
  Los nombres de cable coinciden —`DENIED` local es `IsFlagged`
  (`backend/src/Salvo.Domain/Risk/RiskEvaluation.cs:117`; `RiskEvaluationWireNames.cs:13-16`)—,
  así que el texto de la UI tiene que llevar la fuente siempre.

## Decisiones que resistieron

**D1, la entidad separada, es correcta y el argumento de contaminación se sostiene contra el
código.** Toda lectura de estado vigente pasa por `run_evaluations` y de ahí a `risk_evaluations`:
el dashboard (`EfDashboardReader.cs:84-98`), el orden del feed (`EfAlertStore.cs:78-92`), la
evaluación vigente (`EfAlertStore.cs:228-250`, `EfScoringRunStore.cs:119-142`), las métricas
(`EfEvaluationMetricsReader.cs:26-38`) y la tasa de marcado (`GetDashboardHandler.cs:105-110`). La
preconsulta de fingerprints filtra `Source == Local` (`EfScoringRunStore.cs:32-37`). Con la tabla
separada, ninguna de esas consultas puede ver una fila externa aunque alguien olvide la regla de
`AGENTS.md:50`. Intenté encontrar un camino de contaminación y no lo hay. La migración es segura
(pregunta 2).

**Problema 4 y D4 son exactos.** `alerts.risk_score_snapshot` es obligatorio
(`AlertConfiguration.cs:57-59`), `risk_evaluation_id` tiene único total (`:102-104`) y clave foránea a
`risk_evaluations` (`:114-117`), y el predicado de creación compara bandas de `AlertPolicy` sobre el
score local (`RunScoringHandler.cs:144-155`). Enchufar la fuente externa exigiría rediseñar los tres.
Excluirla es lo correcto y es la tesis.

**D2 en su núcleo, el token sobre `status`, es el mecanismo probado de E4**
(`AlertConfiguration.cs:72-77`, `EfAlertStore.cs:122-147`, `AlertSchemaTests.cs:57-123`). Convive bien
con el único parcial sobre `PENDING`: la transición saca la fila del índice y una solicitud nueva
puede entrar. Lo que falla es la tabla de transiciones, no el token.

**D5 manual es coherente** con la decisión 39 y con la corrida de scoring que la consola dispara
(`frontend/src/app/import/corpus-actions.tsx:42-65`).

**D6 en su frontera es correcta.** `Salvo.Domain.csproj` no referencia paquetes, el mock no necesita
ninguno y `ArchitectureSmokeTests.cs:10-30` lo vigila; `Directory.Packages.props` no cambia.

**D9 es coherente** con el patrón de divergencia de E5 (`frontend/src/app/alerts/[id]/divergence.ts`).

**Las cifras del diseño son exactas.** `salvo.db` tiene 328 evaluaciones, todas `LOCAL`, con
`external_evaluation_id` y `error_code` nulos en las 328.

## Contradicciones con el Blueprint, la bitácora y `AGENTS.md`

- §7 `RiskEvaluation` (`Salvo-Blueprint.md:356-366`) contra D1: declarado por el diseño. Falta
  declarar que `status` de `LOCAL` se reduce a `APPROVED | DENIED`.
- §5.1 `referenceId` (`:253`) y checklist «Correlación por referencias» (`Salvo-Progress.md:207`)
  contra una entidad sin referencia: hallazgo 1.
- §5.2 `GetStatusAsync(string externalEvaluationId)` (`:271-272`) contra la reconciliación de filas
  sin identificador: hallazgo 11.
- §5.3 «política de timeout, retry, backoff y reconciliación» (`:292`) y `AGENTS.md:127` contra
  `ERROR` terminal por timeout: hallazgo 2.
- §9 (`:446-455`) y `.env.example` sin secreto de callback contra D8: hallazgo 13.
- §10 «Next.js no recibe claves de proveedores» (`:472`) y `AGENTS.md:120` contra el botón de D8:
  hallazgo 3.
- §10 «callbacks persistidos antes de responder `2xx` e idempotentes ante replay» (`:476`) y
  `AGENTS.md:126`: D3 cumple lo primero y solo cumple lo segundo con el hallazgo 4.
- `AGENTS.md:111` «repetir … no duplica efectos» contra la solicitud sin idempotencia: hallazgo 9.
- §4.4 sin ruta `/orders` contra «listado de pedidos»: hallazgo 8.
- Bitácora 4 (`:575`) y 29 (`:600`): coherentes con D1 y la refuerzan. La cita a la 30: hallazgo 12.
- §12 «scores local y externo permanecen separados» (`:561`): D1 lo hace estructural.

## Respuestas a las seis preguntas abiertas

**1. ¿Separar la entidad contradice algo más allá de §7, o rompe algún test?** No contradice ninguna
decisión de la bitácora: la 4 y la 29 la exigen. Contradice tres cosas del Blueprint que el diseño no
lista: §5.1 (`referenceId`, hallazgo 1), §5.2 (firma de `GetStatusAsync`, hallazgo 11) y §9 (sin
variable de secreto, hallazgo 13). Tests que se tocan: `RiskEvaluationIdentityTests.cs:35-36` deja de
compilar; `ScoringRunPersistenceTests.cs:249-251` solo si se quita el filtro del índice;
`ArchitectureSmokeTests.PersistedRiskEntitiesCarryNoGroundTruthLabel` debe crecer;
`OpenApiDriftTests` exige recaptura; y las fixtures del frontend deben llevar la clave nueva
(hallazgo 14). El test dorado no se toca.

**2. ¿Se pueden eliminar `external_evaluation_id` y `error_code` sin tocar el fingerprint ni el test
dorado, y es segura la migración con datos?** Sí, y no es una opinión: se ejecutó.

- El fingerprint hashea `orderId | source | ruleConfigVersion | score | signalsCanonical`
  (`RiskEvaluationFingerprint.cs:30-32`); ninguna de las dos columnas entra. El test dorado lee
  `MerchantReferenceId`, `Score`, `Status` y `EvaluationFingerprint`
  (`ScoringRunPersistenceTests.cs:63-76`).
- Sobre una copia de `main` con las dos propiedades quitadas del dominio y de la configuración,
  `dotnet ef migrations add` generó dos `DropColumn`. EF Core 10 sobre SQLite los ejecuta como
  **reconstrucción de tabla**: `CREATE TABLE ef_temp_risk_evaluations` con las nueve columnas
  restantes y las seis restricciones, `INSERT … SELECT`, `PRAGMA foreign_keys = 0`,
  `DROP TABLE`, `RENAME`, `PRAGMA foreign_keys = 1`, y recreación de `ix_risk_evaluations_order` y de
  `ux_risk_evaluations_local_fingerprint` con su `WHERE source = 'LOCAL'`.
- Aplicada a una copia de `salvo.db`: 328 filas antes y después; el fingerprint de `ORD_000001` es
  `0d7470c3…edb0`, el del test dorado, antes y después; los 328 fingerprints ordenados por `id` son
  byte a byte idénticos; `PRAGMA integrity_check` = `ok`; `PRAGMA foreign_key_check` = 0 filas; las
  claves foráneas de `alerts.risk_evaluation_id` y `run_evaluations.evaluation_id` siguen apuntando a
  `risk_evaluations`; 21 alertas y 2.141 referencias intactas; `__EFMigrationsHistory` con cuatro
  entradas.
- `dotnet ef migrations has-pending-model-changes`: sin cambios. `dotnet test` filtrado sobre la
  copia: 9 tests de dominio (identidad, arquitectura) y 4 de integración (dorado, índice parcial,
  tablas de la migración, corrida repetida) en verde.
- Dos salvedades: el `PRAGMA foreign_keys = 0` corre fuera de transacción y EF lo advierte —copia de
  seguridad de `salvo.db` antes de migrar—; y las dos aserciones de
  `RiskEvaluationIdentityTests.cs:35-36` hay que quitarlas o no compila.

**3. Con `ERROR` terminal y fila nueva por reintento, ¿el único parcial permite el reintento o lo
bloquea?** Lo permite: `ERROR` no está en `WHERE status = 'PENDING'`, así que la fila nueva entra.
Lo que bloquea es una `PENDING` **atascada**: sin expiración, sin reintento automático y con la
reconciliación manual, un pedido cuyo proveedor nunca contestó no admite ninguna solicitud nueva
hasta que alguien reconcilie y el mock devuelva algo terminal (hallazgo 10). Y con el hallazgo 2 la
pregunta cambia de forma: tras un timeout el reintento correcto **no** es una fila nueva sino la
reconciliación por referencia sobre la misma fila; la fila nueva queda para el `ERROR` definitivo y
tiene que pedirse explícitamente (hallazgo 9).

**4. ¿Qué pasa si un callback llega antes de que la fila esté persistida? ¿Hay carrera?** La hay, y
es el hallazgo 1. Con correlación solo por `externalEvaluationId` el recibo queda `UNMATCHED` y nada
lo vincula después; con dos solicitudes concurrentes el proveedor recibe dos evaluaciones. Se cierra
reservando la fila `PENDING` con `referenceId` **antes** de llamar al proveedor, correlacionando
también por referencia, y vinculando los `UNMATCHED` al actualizar la fila y en la reconciliación. Con
el mock en proceso la carrera no se ve en la demo; el test que la demuestra es un proveedor de prueba
que dispara el callback desde dentro de `EvaluateAsync`.

**5. ¿La clave derivada del contenido es estable si el proveedor reenvía el mismo estado con distinto
instante? ¿Y estados distintos con el mismo instante?** Con `externalEvaluationId + estado + instante`:

- Mismo estado, distinto instante → **claves distintas**, así que el reenvío no es duplicado. No es
  un problema si terminal → mismo terminal es `NO_OP` (hallazgo 5); es un problema si el diseño lo
  trata como conflicto o como «descarte».
- Estados distintos, mismo instante → **claves distintas**, y decide la monotonía: `PENDING` tras
  `APPROVED` se descarta; `DENIED` tras `APPROVED` es contradicción (`CONFLICTING`), no «fuera de
  orden».
- Sin instante del proveedor la clave colapsa a identificador + estado: dos mensajes distintos con el
  mismo estado se ven como uno, lo cual es inocuo porque el segundo sería `NO_OP` de todos modos.
- La clave debe incluir `provider`, nunca `receivedAt`, y el diseño debe fijar la regla de derivación
  como texto canónico (`provider|externalEvaluationId|status|providerInstant`) para que dos
  implementaciones del mismo callback produzcan la misma clave.

**6. ¿El botón de demo abre una vía para transiciones arbitrarias?** Sí, en las dos formas en que el
diseño lo permite, y por dos razones distintas: el secreto tendría que salir de la API, y si el
cliente compone el payload elige el estado de cualquier `PENDING`. Deja de abrirla con el
disparador de demo del hallazgo 3: endpoint de la API bajo `DemoData:Enabled`, resultado tomado del
mock, inyectado por el mismo caso de uso del callback; el cliente elige la evaluación, nunca el
estado. Las terminales están protegidas por la monotonía en cualquier caso; las pendientes solo con
esto.

## Riesgos de rehacer trabajo en E7 y E8

- **E7.** `AlertDetail` vuelve a crecer con `explanation`, `recommendedAction` y
  `explanationStatus`. Si el bloque externo entra en `AlertDetail` como sub-objeto nullable propio
  —`externalEvaluation: { … } | null`— E7 agrega hermanos sin tocarlo, y las guardas que proyectan
  (`guards.ts:33-45`) absorben el despliegue en cualquier orden. El proveedor de explicaciones «usa
  únicamente señales suministradas» (`DesignAgent/Salvo-Portability.md`): el veredicto externo puede
  citarse como opinión del proveedor, nunca como fundamento; conviene decidirlo ahora en D9 para que
  E7 no lo decida solo. Y el vocabulario de tres «PENDING» del hallazgo 15.
- **E8.** La candidata `e3-v2` invalida los fingerprints a propósito; E6 no los toca (pregunta 2),
  así que no interfiere. La candidata de fixture enriquecida **sí** interfiere con D7 si el mock
  hashea la referencia (hallazgo 10): decidir la función ahora es lo que permite que E8 plante
  divergencias a propósito. El README debe «distinguir mock, sandbox y producción»
  (`Salvo-Blueprint.md:566`): la declaración de D8 sobre el callback y la de D7 sobre
  `KoinSandboxProvider` son ese texto. La decisión sobre `/orders` (hallazgo 8) también es de E8 si
  no es de E6.
- **Post-MVP.** La máquina de estados del hallazgo 2 y el puerto del hallazgo 11 son lo que el
  adaptador real necesita; decidirlos ahora evita una migración de `external_evaluations` cuando
  aparezcan las credenciales.

## Resumen para el brief

| # | Decisión | Severidad | Acción mínima |
| --- | --- | --- | --- |
| 1 | D3 / pregunta 4 | Alta | Reservar la fila `PENDING` con `referenceId` antes de llamar al proveedor; correlar por identificador o por referencia; vincular `UNMATCHED` al actualizar y en la reconciliación; test con proveedor que llama al callback desde `EvaluateAsync` |
| 2 | D6 / D2 | Alta | `TIMEOUT`, `INVALID_RESPONSE` y `5xx` tras enviar dejan la fila `PENDING` con `lastErrorCode`; `ERROR` terminal solo para «no se envió» y rechazos definitivos; la reconciliación cierra |
| 3 | D8 / pregunta 6 | Alta | Disparador de demo como endpoint de la API bajo `DemoData:Enabled`, resultado del mock, mismo caso de uso; ningún secreto en Next; el callback autenticado falla cerrado |
| 4 | D3 | Alta | Único `(provider, deduplication_key)`; recibo y transición en un `SaveChangesAsync`; duplicado por violación de unicidad; relectura ante conflicto; sin estado `DUPLICATE` |
| 5 | D2 / D3 | Media | Tabla de transiciones completa: terminal → mismo terminal `NO_OP`, terminal → otro terminal `CONFLICTING`; restricciones de consistencia |
| 6 | D10 | Media | Lista de tests, incluida la carrera sobre base en archivo y el diferencial de dashboard, feed y métricas; reconciliación a `E6A` |
| 7 | D1 | Media | Migración mínima: dos columnas fuera, `ck_risk_evaluations_source` y `_status` endurecidos, enumeraciones limpias; índice parcial y nulabilidad intactos o como ítem aparte |
| 8 | D4 / D10 | Media | Decidir `/orders` o bloque y acción en `/alerts/[id]` más acción de demo en `/import`; §4.4 |
| 9 | D10 | Media | Endpoints de solicitud y lectura; idempotencia; tabla de códigos; definir o quitar `attemptCount` |
| 10 | D7 | Media | Función documentada de `merchantReferenceId` con bandas; conteos sobre la fixture fijados en test; `GetStatusAsync` del mock resuelve `PENDING` |
| 11 | D5 / §5.2 | Media | `GetStatusAsync` por referencia; umbral 0 para el mock; unidad de trabajo por fila; `settledBy` |
| 12 | Problema 1 / D4 | Baja | Corregir las citas: decisión 29, `RiskEvaluationIdentityTests`, «tercer bloque» |
| 13 | D8 | Baja | Variable del secreto; `{provider}` validado; límite de cuerpo; tolerar campos desconocidos; `FixedTimeEquals` |
| 14 | D1 / D10 | Baja | Listar los artefactos a tocar; copia de `salvo.db` antes de migrar |
| 15 | D7 / D9 | Baja | `KOIN_MODE=sandbox` falla al arrancar; procedencia en cada «pendiente»; el score externo no se compara con el local |

## Cómo se verificó

- Lectura completa de los archivos listados en el encabezado; toda cita `archivo:línea` se abrió y
  se corresponde con `main` en `9daa6cc`.
- Consultas `sqlite3 -readonly` sobre `backend/src/Salvo.Api/salvo.db`: evaluaciones por `source`
  y por `status`, nulidad de `external_evaluation_id` y `error_code`, corridas, referencias, alertas
  por estado, historial de migraciones y DDL de `risk_evaluations` y sus índices. La base no se
  modificó.
- Experimento de migración, íntegramente fuera del repositorio, en el scratchpad de la sesión:
  `git archive HEAD | tar -x` sobre un directorio nuevo; copia de `salvo.db`; edición de
  `RiskEvaluation.cs`, `RiskEvaluationConfiguration.cs` y `RiskEvaluationIdentityTests.cs` en esa
  copia; `dotnet restore --locked-mode`, `dotnet tool restore`, `dotnet ef migrations add
  DropUnusedExternalColumns`, `dotnet build --configuration Release`, `dotnet ef migrations script
  20260902204944_AlertsAndReview DropUnusedExternalColumns`, `dotnet ef database update
  --connection "Data Source=<copia>"`, volcado de fingerprints antes y después con `cmp`,
  `pragma_integrity_check`, `pragma_foreign_key_check`, `pragma_foreign_key_list` de `alerts` y
  `run_evaluations`, `dotnet ef migrations has-pending-model-changes` y `dotnet test` filtrado a
  `DemoCorpusProducesTheGoldenFingerprints`, `RiskEvaluationIdentityTests`,
  `TheFingerprintIndexIsUniqueOnly*`, `ArchitectureSmokeTests`,
  `InitialMigrationCreatesExpectedTablesAndIndexes` y `RepeatedRunOverTheSameCorpus*`: 13 tests, 0
  fallos. La advertencia de EF sobre `PRAGMA foreign_keys` quedó en el registro de aplicación.
- Los hallazgos 1, 2, 4 y 5 describen comportamiento de un diseño que todavía no tiene código: se
  verificaron contra los mecanismos existentes que el diseño dice reutilizar (token de concurrencia,
  índices parciales, detección de unicidad, patrón de gating de demo) y contra los documentos, no
  contra una implementación. Se señalan como razonamiento sobre el diseño, no como bugs observados.
- No se ejecutó ninguna compuerta sobre el repositorio ni se generó ningún artefacto dentro de él; el
  único archivo escrito es este informe. No hubo `git push` ni PR.
