# Salvo — Task brief `E6B-CALLBACK-UI`

## Identificación

- Work ID: `E6B-CALLBACK-UI`
- Etapa: 6
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-04
- Rama/worktree: `claude/e6b-callback-ui`
- Commit base: el commit de `main` que incorpora este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. La tarea es ancha pero no profunda: la tabla de
  transiciones, la atomicidad y la doble correlación ya están decididas, y los patrones existen. Lo
  que justifica Opus y no Sonnet son los dos tests de concurrencia: la carrera
  callback–reconciliación y el callback previo al commit. Si alguno no puede volverse determinista
  tras dos intentos, detenerse y consultar.
- Dependencias: `E6A-PROVEEDOR`, integrada en `main` mediante `bca2c46` y verificada con la
  compuerta y el smoke.

## Resultado esperado

Una evaluación externa puede cerrarse por callback, y ese callback es seguro: duplicado no repite
efectos, fuera de orden no retrocede, contradicción no se traga en silencio, y sin secreto no entra.
La analista pide una evaluación externa desde el detalle de una alerta, ve la opinión del proveedor
junto a la propia y entiende cuándo discrepan. Con esto la Etapa 6 queda completa.

## Contexto obligatorio

- `Coordination/Tasks/E6-DISENO.md` (v2), decisiones **D3, D5, D6, D9, D10, D11, D12 y D14**.
- `Coordination/Tasks/E6-revision-adversarial.md`, hallazgos **1, 3, 4, 5, 8, 13 y 15**.
- `DesignAgent/Salvo-Blueprint.md`: §4.5 con la tabla de transiciones, §5, §7 y §9.
- `DesignAgent/Salvo-Progress.md`, entrada «Etapa 6 — Proveedor antifraude mock».
- `AGENTS.md`, en particular las reglas de evaluación externa y la de secretos sin
  `NEXT_PUBLIC_`.
- `Coordination/Handoffs/Claude.md`, entrada de `E6A`, en particular sus «Decisiones y supuestos».
- Código de `E6A`: `backend/src/Salvo.Domain/External/**`, `Salvo.Application/External/**`
  —sobre todo `ExternalProviderExchange`, que es el único punto donde una respuesta se vuelve
  transición—, `Salvo.Infrastructure/External/**` y `Persistence/EfExternalEvaluationStore.cs`,
  `Salvo.Api/ExternalEvaluationEndpoints.cs`.
- Patrones a reutilizar **sin reescribir**: `frontend/src/lib/api/` completo, `components/`,
  `src/app/alerts/[id]/` y `src/app/import/`; el registro condicional por bandera de
  `OrderEndpoints` y `EvaluationMetricsEndpoints`; el mapeo de violación de unicidad de
  `EfAlertStore`; `SalvoApiFactory.WithFileDatabase` y su diccionario `Settings`.

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**

## Alcance

### Dentro

**Dominio e infraestructura**

- `CallbackReceipt` en `Salvo.Domain/External/`: `id`, `provider`, `deduplicationKey`,
  `externalEvaluationId` nullable, `referenceId` nullable, `status`
  (`APPLIED | NO_OP | SUPERSEDED | CONFLICTING | UNMATCHED`), `replayCount`, `lastSeenAt`,
  `receivedAt`, `processedAt` nullable. **No existe `DUPLICATE`**: un duplicado es la ausencia de una
  segunda fila.
- Migración con **único sobre `(provider, deduplication_key)`**, no sobre la clave sola, más un
  índice por `(provider, reference_id)` para la vinculación tardía.
- La clave de deduplicación es **texto canónico fijado**:
  `provider|externalEvaluationId|status|providerInstant`. Nunca incluye el instante de recepción.

**Aplicación — el callback**

- Un caso de uso único, compartido por el endpoint autenticado y por el disparador de demo.
- **Recibo y transición se persisten en un único `SaveChangesAsync`.** Si no, una transición que
  pierde la carrera deja el recibo vivo, el reintento se ve como duplicado, responde `200` y la
  transición se pierde para siempre.
- El duplicado se detecta por **violación de unicidad** —códigos SQLite 2067 y 1555, como
  `EfAlertStore`—, responde `200` y actualiza `replayCount` y `lastSeenAt`.
- Ante `DbUpdateConcurrencyException`: revertir entero, **releer una vez**, reclasificar según la
  tabla de §4.5 y persistir. Si vuelve a perder: `503` con `Retry-After`.
- **Correlación doble**: por `(provider, externalEvaluationId)` y, si no hay, por
  `(provider, referenceId)`.
- Transiciones exactamente según la tabla de §4.5 del Blueprint. En particular:
  **fuera de orden ≠ contradicción**. `PENDING` tras un terminal es `SUPERSEDED`; **otro** terminal
  tras un terminal es `CONFLICTING`, se registra y **se muestra**, no se traga.

**Vinculación tardía de `UNMATCHED`**

- Al terminar la solicitud de evaluación —el punto de extensión que dejó `E6A`— y en cada barrido de
  reconciliación, buscar recibos `UNMATCHED` del mismo proveedor cuyo `externalEvaluationId` o
  `referenceId` coincidan, y aplicar su transición bajo la misma monotonía.
- Es lo que cierra la ventana del hallazgo 1: un callback que llegó antes de que la fila existiera.

**API**

- `POST /api/external-callbacks/{provider}`, para llamadores externos:
  - Secreto compartido en cabecera, comparado con `CryptographicOperations.FixedTimeEquals`.
  - **Falla cerrado**: secreto no configurado o vacío ⇒ `401` a toda petición, o la ruta no se mapea.
    Nunca «sin secreto, pasa».
  - `{provider}` validado contra el catálogo; desconocido ⇒ `404`.
  - Límite explícito de tamaño de cuerpo.
  - **Tolera campos desconocidos** en el payload.
  - Variable de entorno declarada en `.env.example`.
- `POST /api/demo-data/external-callbacks:deliver`, **solo bajo `DemoData:Enabled`**, con el patrón
  de `OrderEndpoints` y `EvaluationMetricsEndpoints`. Recibe `{ externalEvaluationId }` o «todas las
  pendientes», le pide al mock su resultado determinista y lo inyecta por **el mismo caso de uso**.
  **El cliente elige qué evaluación, nunca qué estado.** Ningún secreto sale de la API.
- `GET /api/system/capabilities` gana un campo que indica si el disparador existe.
- `AlertDetail` gana `externalEvaluation` como **sub-objeto nullable propio**, no campos sueltos, de
  modo que la Etapa 7 agregue hermanos sin tocarlo.

**Consola**

- **Tercer bloque en `/alerts/[id]`.** Hoy hay dos, snapshot y vigente, en una grilla de dos
  columnas: **no existe ningún bloque reservado**; hay que hacerle lugar.
  - Estado del proveedor, con su procedencia (`SYNC`, `CALLBACK`, `RECONCILIATION`) y su instante.
  - Acción «solicitar evaluación externa» cuando no hay ninguna.
  - Con `demoDataEnabled`, acción de entregar el callback pendiente.
  - Divergencia: cuando el veredicto local y el externo difieren, se muestran **las dos opiniones con
    sus procedencias**, sin combinarlas. **Los scores no se comparan numéricamente entre sí**: son
    escalas de sistemas distintos. Se contrastan los veredictos.
  - Una contradicción del proveedor (`CONFLICTING`) se muestra: «el proveedor envió un veredicto
    contradictorio».
- **Acción en `/import`**, solo con `demoDataEnabled`: pedir evaluación externa del corpus vigente, y
  entregar todos los callbacks pendientes. Cubre a los pedidos sin alerta sin agregar una ruta.
- **Vocabulario**: hay tres «pendiente» distintos —alerta esperando veredicto humano, evaluación
  externa esperando al proveedor, y pedido sin corrida—. Se nombran distinto y **nunca se usa
  «pendiente» a secas**.
- `messages.ts` incorpora los códigos de la Etapa 6, incluido `EXTERNAL_EVALUATION_SETTLED` que
  `E6A` agregó y que hoy cae en el mensaje genérico.
- **No se agrega ninguna ruta nueva.** `/orders` queda como candidata de Etapa 8.

**Contrato y verificación**

- Recapturar `frontend/openapi/salvo-openapi.json` y regenerar `schema.d.ts`.
- `boundary.test.ts` cubre los componentes nuevos: ningún componente cliente recibe objetos de API.
- `scripts/smoke-ui.sh` gana comprobaciones del bloque externo en el escenario con datos.

**Tests**

- Duplicado exacto: `200`, **una** transición, **un** recibo, `replayCount` incrementado.
- Fuera de orden `APPROVED → PENDING`: sigue `APPROVED`, recibo `SUPERSEDED`.
- Contradicción `APPROVED → DENIED`: sigue `APPROVED`, recibo `CONFLICTING`, visible en la consola.
- Mismo terminal otra vez: `NO_OP`, `200`.
- **Callback antes del commit**: un `IAntifraudProvider` de prueba que invoque el caso de uso del
  callback **desde dentro** de `EvaluateAsync` y recién entonces devuelva `PENDING`. Con la
  vinculación tardía la fila termina en el estado del callback; sin ella queda `PENDING` para
  siempre.
- **Carrera callback–reconciliación sobre base en archivo**: una transición, un recibo.
- Secreto ausente, vacío o incorrecto: `401` y **cero** recibos.
- Proveedor desconocido en la ruta: `404`.
- Payload con campos desconocidos: se acepta.
- El disparador de demo no existe con `DemoData:Enabled = false`.
- El disparador de demo **no acepta un estado del cliente**: verificable porque su contrato no tiene
  ese campo.
- Diferencial: dashboard, feed y métricas idénticos antes y después de los callbacks.

### Fuera

- `KoinSandboxProvider` y cualquier llamada real.
- Alertas originadas por evaluación externa.
- Combinar el criterio local y el externo en un veredicto.
- Comparar numéricamente el score externo con el local.
- Una ruta `/orders`.
- Reintentos automáticos, backoff o trabajos en segundo plano.
- Modificar el motor, `RuleConfig`, el fingerprint, la corrida de scoring, la semántica de alertas o
  el dashboard operativo.
- Explicabilidad (Etapa 7) y fixture enriquecida (Etapa 8).

### Paths autorizados

- `backend/src/Salvo.Domain/External/**`
- `backend/src/Salvo.Application/External/**`
- `backend/src/Salvo.Application/Alerts/**`, solo para el sub-objeto `externalEvaluation`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `frontend/**`
- `scripts/**`

### Paths reservados por otros trabajos

- Ninguno. Esta tarea reserva los anteriores.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Crear la migración con `dotnet ef migrations add`: autorizado.
- **Copia de `salvo.db` antes de aplicar la migración.**
- Levantar la API para recapturar el OpenAPI: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna.
- Commits locales en la rama: autorizados, y **se pide commitear por partes**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E6B-CALLBACK-UI.md` sin faltantes antes de empezar.
- [ ] Recibo y transición se persisten en un único `SaveChangesAsync`.
- [ ] No existe el estado `DUPLICATE`; el duplicado se detecta por violación de unicidad.
- [ ] La tabla de transiciones de §4.5 está implementada completa, con `SUPERSEDED` y `CONFLICTING`
      distinguidos.
- [ ] **El test del callback antes del commit falla sin la vinculación tardía.** Documentar cómo se
      comprobó.
- [ ] El endpoint de callback **falla cerrado** sin secreto configurado.
- [ ] El disparador de demo no acepta un estado del cliente, y no existe sin `DemoData:Enabled`.
- [ ] Ningún secreto en el proceso de Next ni con prefijo `NEXT_PUBLIC_`.
- [ ] Ningún componente cliente recibe objetos de API; `boundary.test.ts` cubre lo nuevo.
- [ ] La consola no usa «pendiente» a secas para los tres significados.
- [ ] `npm run build` sigue pasando con la API apagada y las rutas siguen dinámicas.
- [ ] `scripts/smoke-ui.sh` verde, con las comprobaciones nuevas. **Pegar su salida en el handoff.**
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E6B-CALLBACK-UI`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E6B-CALLBACK-UI.md` | Brief válido |
| Test de duplicado exacto | `200`, una transición, un recibo |
| Tests de `SUPERSEDED` y `CONFLICTING` | Fila intacta, recibos distintos |
| Test del callback antes del commit | La fila termina en el estado del callback; falla sin vinculación tardía |
| Carrera callback–reconciliación sobre base en archivo | Una transición, un recibo |
| Test del secreto | `401` y cero recibos |
| Test diferencial | Dashboard, feed y métricas idénticos |
| `SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build` | Pasa; rutas de datos como `ƒ` |
| `scripts/smoke-ui.sh` | Verde, con las comprobaciones nuevas |
| `npm run api:types:check --prefix frontend` | Al día |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E6B-CALLBACK-UI` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Cómo se reacomoda la grilla del detalle para el tercer bloque.
- Diseño visual del bloque externo y de la divergencia, respetando contraste y accesibilidad.
- Forma concreta del payload del callback y del disparador de demo.
- Cómo se simulan las carreras en los tests.
- Redacción exacta de los mensajes, en castellano y accionables.
- Nombre del campo de capacidades del disparador.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- recibo y transición no pueden persistirse juntos sin romper otra cosa;
- el test del callback antes del commit no puede volverse determinista;
- hace falta una dependencia nueva o una ruta nueva;
- el secreto no puede mantenerse fuera del proceso de Next;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos y resultados exactos, incluida la salida de `smoke-ui.sh` y la comprobación de que el
  test del callback antes del commit falla sin la vinculación tardía.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
