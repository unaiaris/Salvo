# Salvo — Diseño de la Etapa 6: proveedor antifraude externo

> Estado: propuesta v1, pendiente de revisión adversarial y de aprobación del usuario
> Fecha: 2026-09-04
> Base: `main` tras el cierre de la Etapa 5
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §3 paso 9, §5, §7 y «Etapa 6»

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | La evaluación externa vive en su **propia entidad**, no como una fila más de `RiskEvaluation` | Problema 1 |
| D2 | Su ciclo de vida es una máquina de estados con transiciones monótonas y estado terminal | Problema 2 |
| D3 | El callback es replay-safe por `CallbackReceipt`, y **fuera de orden no retrocede** | Problema 3 |
| D4 | Una evaluación externa **no abre alertas** en esta etapa | Problema 4 |
| D5 | La reconciliación es explícita y manual, como la corrida de scoring | §5.1 |
| D6 | El fallo del proveedor es un estado del dominio, no una excepción que se propaga | §5.1 |
| D7 | El mock es determinista, sin red, y produce los cuatro estados | §5.2 |
| D8 | El callback se autentica con un secreto compartido, y se declara que no es el mecanismo real | §5.3 |
| D9 | La divergencia entre el criterio local y el externo se expone, no se resuelve | Tesis del proyecto |
| D10 | Partición en `E6A-PROVEEDOR` y `E6B-CALLBACK-UI` | — |

---

## Los cuatro problemas

### Problema 1 — La Etapa 4 dejó esto abierto a propósito

`RiskEvaluation` es **append-only y sin mutadores públicos**. Su identidad es un fingerprint de
contenido y hay un índice único parcial sobre él. La decisión 30 de la bitácora y el brief de
`E4A-PERSISTENCIA` lo dicen con todas las letras:

> Una evaluación externa tiene `score` y `signalsJson` nulos y un `status` que transiciona de
> `PENDING` a `APPROVED` por callback. Ese ciclo de vida es **mutable** y se decide en la Etapa 6.
> Fijar ahora una identidad append-only para todas las fuentes dejaría a E6 sin salida: o rompe
> append-only, o choca contra el índice único.

Ese día no elegimos. Hoy hay que elegir, y las opciones tienen costos distintos:

| Opción | Costo |
| --- | --- |
| Mutar las filas externas | El invariante «append-only» pasa a ser condicional y deja de poder verificarse de un vistazo. `ArchitectureSmokeTests` no puede afirmar «sin mutadores» si hay mutadores para una fuente |
| Una fila por transición, en la misma tabla | Conserva append-only, pero «la evaluación vigente» pasa a depender de dos definiciones distintas según la fuente: por `run_evaluations` para la local, por «última fila» para la externa |
| **Entidad separada** | Migración nueva y una entrada de bitácora que corrige §7 del Blueprint |

### Problema 2 — Los estados asíncronos no son un `enum`, son una máquina

`PENDING → APPROVED` no es la única transición posible. También hay `PENDING → DENIED`,
`PENDING → ERROR`, y —el caso que rompe implementaciones ingenuas— **un callback de `PENDING` que
llega después de uno de `APPROVED`**, porque la red no garantiza orden.

### Problema 3 — Un callback duplicado y un callback fuera de orden son problemas distintos

La idempotencia por clave de deduplicación resuelve el **duplicado exacto**: el mismo mensaje dos
veces. No resuelve **dos mensajes distintos del mismo asunto que llegan al revés**. Se necesitan dos
mecanismos, no uno.

### Problema 4 — Si la evaluación externa abriera alertas, rompería la Etapa 4

`Alert.riskEvaluationId` tiene un único **total** y `riskScoreSnapshot` es obligatorio. Una
evaluación externa puede no traer score. Y el predicado de creación de `E4B` compara bandas de
severidad derivadas del score local. Enchufar la fuente externa a ese predicado obligaría a
rediseñar la escalada, la severidad y el índice, en una etapa que no tiene ese alcance.

---

## D1 — La evaluación externa es su propia entidad

**`RiskEvaluation` queda como lo que realmente es: el resultado de las reglas locales.** Append-only,
siempre con fingerprint, siempre reproducible a partir del corpus y la configuración.

**`ExternalEvaluation` es una entidad nueva**, con ciclo de vida propio:

- `id`, `orderId`, `provider` (`EXTERNAL_MOCK` | `KOIN_SANDBOX`)
- `externalEvaluationId` nullable hasta que el proveedor lo asigne
- `status`: `PENDING | APPROVED | DENIED | ERROR`
- `score` nullable — un proveedor puede no devolverlo
- `errorCode` sanitizado, nullable
- `requestedAt`, `settledAt` nullable, `updatedAt`
- `attemptCount`
- `status` como **token de concurrencia**, igual que en `Alert`

Único parcial: **una evaluación externa no terminal por pedido y proveedor**
(`WHERE status = 'PENDING'`). Un pedido puede tener varias evaluaciones externas a lo largo del
tiempo, pero solo una esperando respuesta.

### Por qué separar y no discriminar

1. **La tesis del proyecto es que el score local y la evaluación del proveedor no se mezclan.**
   Guardarlos en la misma tabla con una columna que dice cuál es cada uno es exactamente la mezcla
   que la tesis advierte, a nivel de almacenamiento. La separación hace estructural lo que hoy es
   una convención.
2. **Los dos objetos tienen naturalezas incompatibles.** Uno es una función pura del corpus:
   idempotente, reproducible, con identidad de contenido. El otro es una conversación con un sistema
   remoto: mutable, reintentable, con identificadores de correlación y acuses de recibo. Meterlos en
   una tabla obliga a que la mitad de las columnas y la mitad de las restricciones estén siempre
   nulas o siempre condicionadas.
3. **Elimina un riesgo ya declarado.** `AGENTS.md` dice hoy «ninguna lectura de estado vigente
   consulta `risk_evaluations.status` a secas: siempre vía `run_evaluations`». Esa regla existe
   **porque** se planeaba mezclar. Con la separación, el peligro desaparece en vez de vigilarse.
4. **La restricción `ck_risk_evaluations_local_completeness` deja de ser condicional** y el índice
   único de fingerprint deja de necesitar filtro.

### Lo que **no** se toca

`source` **permanece en `RiskEvaluation` y sigue valiendo `LOCAL`**. Está dentro del material que se
hashea en el fingerprint:

```
SHA-256( orderId | source | ruleConfigVersion | score | signalsCanonical )
```

Quitarla o cambiar su serialización invalidaría las 328 evaluaciones existentes y rompería el test
dorado. Se queda como está. Las columnas `external_evaluation_id` y `error_code`, siempre nulas,
**se eliminan** en la migración de esta etapa; la de `source` no.

Esto exige corregir §7 del Blueprint y agregar entradas de bitácora.

## D2 — Máquina de estados con transiciones monótonas

```
                 ┌─────────► APPROVED ─┐
   (solicitud)   │                     │
       └──► PENDING ────────► DENIED ──┼──► terminal
                 │                     │
                 └─────────► ERROR ────┘
```

- `PENDING` es el único estado no terminal.
- Los tres terminales **no transicionan a nada**, ni entre ellos.
- Una respuesta `received` del proveedor se trata como `PENDING`, según §5.1.
- La transición se aplica con el `status` como token de concurrencia: dos actualizaciones simultáneas
  terminan en conflicto, no en sobrescritura silenciosa. Es el mismo mecanismo que `AlertReview`, y
  por la misma razón.

**`ERROR` es terminal a propósito.** Un error del proveedor no se reintenta sobre la misma fila: se
solicita una evaluación **nueva**, que es otra fila con su propio `attemptCount`. Así el historial
conserva que hubo un fallo en vez de borrarlo con un reintento exitoso.

## D3 — Dos mecanismos, porque son dos problemas

**Duplicado exacto.** `CallbackReceipt` con único sobre `deduplicationKey`:

- `id`, `provider`, `deduplicationKey` único, `externalEvaluationId`, `receivedAt`, `processedAt`
  nullable, `status` (`ACCEPTED | DUPLICATE | REJECTED | UNMATCHED`)
- La clave la provee el proveedor o, si no la provee, se deriva de forma estable del contenido
  correlacionante —`externalEvaluationId` más estado más instante del proveedor—, nunca de la hora
  de recepción.
- Un duplicado devuelve **`200`**, se registra como `DUPLICATE` y no vuelve a aplicar la transición.
  Un `409` invitaría al proveedor a reintentar para siempre.

**Fuera de orden.** El acuse de recibo no lo resuelve: son mensajes distintos. La regla es la
monotonía de D2: **una transición hacia un estado no terminal sobre una evaluación ya terminal se
descarta**, se registra el recibo como `ACCEPTED` con una marca de descarte, y se responde `200`.

**Callback sin correlación.** Un `externalEvaluationId` que no existe se registra como `UNMATCHED` y
responde `202`: se recibió y se guardó, pero no se aplicó. No es un error del proveedor y no debe
provocar reintentos, pero tampoco puede silenciarse.

**No se persiste el payload externo completo.** Solo los campos correlacionantes, según §7 del
Blueprint. Los logs van redactados.

## D4 — La evaluación externa no abre alertas

En esta etapa, la alerta sigue siendo un artefacto de las reglas locales. La evaluación externa:

- se muestra en el detalle de la alerta, en el **bloque que `E5B` dejó reservado**;
- se muestra en el listado de pedidos, para pedidos sin alerta;
- **no** participa del predicado de creación, ni de la escalada, ni de la severidad.

No es una limitación: es la tesis. El proveedor opina y el comercio decide, y lo que se demuestra es
la **superficie de integración**, no un motor de decisión combinado. Combinar los dos criterios es
una decisión de producto que no está tomada y que no corresponde a esta etapa.

## D5 — La reconciliación es explícita

`POST /api/external-evaluations:reconcile` recorre las evaluaciones `PENDING` con más de un umbral
de antigüedad, llama a `GetStatusAsync` por cada una, y aplica las transiciones que correspondan.
Devuelve un resumen igual que la corrida de scoring: consultadas, resueltas, sin cambios, con error.

Es manual y no un trabajo en segundo plano, por la misma razón que la corrida de scoring lo es: el
MVP no tiene planificador, la demo tiene que ser reproducible, y un proceso periódico escondería
justamente el estado que se quiere mostrar.

**La reconciliación y el callback pueden competir por la misma fila.** El token de concurrencia de D2
es lo que hace que una de las dos pierda limpiamente.

## D6 — El fallo del proveedor es un estado, no una excepción

- Timeout explícito en toda llamada, según `AGENTS.md`.
- Un timeout, una conexión rechazada o un `5xx` producen `status = ERROR` con un `errorCode`
  **sanitizado y de un catálogo cerrado** —`TIMEOUT`, `UNREACHABLE`, `PROVIDER_ERROR`,
  `INVALID_RESPONSE`—, nunca el mensaje crudo del proveedor.
- **La evaluación local no se toca jamás.** El pedido conserva su score, su alerta y su veredicto.
- Ningún tipo del SDK o del cliente HTTP cruza fuera de Infrastructure, según §6 del Blueprint.

## D7 — El mock es determinista y sin red

`MockAntifraudProvider` deriva su respuesta de forma determinista del pedido —por ejemplo, de un
hash estable de la referencia— y produce los cuatro caminos:

| Camino | Para qué |
| --- | --- |
| `APPROVED` síncrono | El caso feliz |
| `DENIED` síncrono | Divergencia con el criterio local cuando el local aprobó |
| `PENDING` con callback posterior | El caso que obliga a correlacionar |
| `ERROR` | Degradación |

Determinista significa que **el mismo pedido produce siempre el mismo camino**, para que la demo y
los tests sean reproducibles. La latencia simulada, si existe, es configurable y cero por defecto.

`KOIN_MODE` gobierna qué implementación se registra, con `mock` por defecto. `KoinSandboxProvider`
**no se implementa** en esta etapa: §5.3 lista siete requisitos que no se cumplen, empezando por
credenciales que no existen.

## D8 — El callback se autentica, y se declara qué falta

`POST /api/external-callbacks/{provider}` exige un secreto compartido en cabecera, comparado con
**comparación de tiempo constante**. Sin él o con uno incorrecto: `401`, y **no** se crea recibo.

Se declara explícitamente, en el código y en el README, que **este no es el mecanismo real**: §5.3
exige «verificación del mecanismo oficial de autenticación/origen del callback» de Koin, que
típicamente es una firma sobre el cuerpo. El secreto compartido demuestra que el problema está
identificado y modelado, no que esté resuelto para producción.

En la demo local el callback lo dispara la propia consola con un botón, porque no hay HTTPS público.
Eso se declara como limitación, no se disfraza.

## D9 — La divergencia se expone, no se resuelve

Cuando el proveedor externo y las reglas locales difieren —el local aprueba y el externo deniega, o
al revés— la interfaz lo **muestra como divergencia**, con las dos opiniones y sus procedencias.
No hay una regla que combine ambas en un veredicto.

Es coherente con lo que ya hace `E5B` con el snapshot y la evaluación vigente: cuando dos fuentes de
verdad discrepan, Salvo muestra las dos y deja decidir a la persona.

## D10 — Partición

| Ítem | Alcance | Depende de |
| --- | --- | --- |
| `E6A-PROVEEDOR` | `ExternalEvaluation`, `IAntifraudProvider`, `MockAntifraudProvider`, migración, solicitud de evaluación, degradación, `GET`/`POST` de evaluación externa | — |
| `E6B-CALLBACK-UI` | `CallbackReceipt`, endpoint de callback autenticado, reconciliación, y la superficie en la consola: bloque en el detalle, disparadores y divergencia | `E6A` integrada |

---

## Qué queda fuera

- `KoinSandboxProvider` y cualquier llamada real a Koin. §5.3 no se cumple.
- Device fingerprint oficial.
- Callback público en Internet.
- Combinar el criterio local y el externo en un veredicto único.
- Alertas originadas por evaluación externa.
- Reintentos automáticos, backoff o trabajos en segundo plano.
- Modificar el motor, `RuleConfig`, el fingerprint, la semántica de alertas de E4B o el dashboard
  operativo de E5A.
- Explicabilidad (Etapa 7) y enriquecimiento de la fixture (Etapa 8).

## Cambios de estado canónico que exige este diseño

1. **Blueprint §7**: `RiskEvaluation` deja de tener `source` externo, `externalEvaluationId` y
   `errorCode`; se agrega `ExternalEvaluation`. `source` permanece por el fingerprint.
2. **Blueprint §4**: una sección nueva de evaluación externa, con la máquina de estados.
3. **`AGENTS.md`**: la regla sobre `risk_evaluations.status` se puede relajar, pero **no se elimina**;
   se reescribe explicando que la fuente externa vive en otra tabla.
4. **Bitácora**, entradas 44 en adelante.

## Preguntas abiertas para la revisión adversarial

1. ¿Separar la entidad contradice algo del Blueprint más allá de §7, o rompe algún test existente?
2. ¿Se puede eliminar `external_evaluation_id` y `error_code` de `risk_evaluations` sin tocar el
   fingerprint ni el test dorado? ¿La migración es segura sobre una base con datos?
3. Con `ERROR` terminal y una fila nueva por reintento, ¿el único parcial sobre `PENDING` permite el
   reintento, o lo bloquea?
4. ¿Qué pasa si un callback llega para una evaluación cuya fila fue creada por una solicitud que
   todavía no terminó de persistirse? ¿Hay una carrera entre `EvaluateAsync` y el callback?
5. La clave de deduplicación derivada del contenido, ¿es estable si el proveedor reenvía el mismo
   estado con distinto instante? ¿Y si reenvía estados distintos con el mismo instante?
6. ¿El botón que dispara el callback en la demo abre alguna vía para que un cliente cualquiera
   provoque transiciones arbitrarias?
