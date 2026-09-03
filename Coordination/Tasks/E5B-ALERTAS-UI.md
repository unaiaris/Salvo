# Salvo — Task brief `E5B-ALERTAS-UI`

## Identificación

- Work ID: `E5B-ALERTAS-UI`
- Etapa: 5
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-03
- Rama/worktree: `claude/e5b-alertas-ui`
- Commit base: el commit de `main` que incorpora este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. Las trampas del framework están cubiertas por
  criterios de aceptación demostrables, no por la capacidad del modelo. Si `experimental.taint` o la
  verificación de deriva de tipos sin API no tienen solución limpia tras dos intentos, detenerse y
  consultar antes de insistir.
- Dependencias: `E5A-API-LECTURA`, integrada en `main` mediante `5f48db0` y cerrada en `4c0256d`.

## Resultado esperado

Una analista abre la consola, ve la cola de alertas ordenada por riesgo vigente, entra a una,
entiende **por qué se abrió** y **qué dice el corpus ahora**, y emite un veredicto que queda
registrado —o recibe una explicación en castellano de por qué no se pudo—. Ningún campo que la API
no proyecte llega al navegador, y la compuerta pasa sin la API levantada.

## Contexto obligatorio

- `Coordination/Tasks/E5-DISENO.md` (v2), decisiones **D7, D8, D11, D12, D13, D14 y D17**.
- `Coordination/Tasks/E5-revision-adversarial.md`, hallazgos **2, 3, 8, 9, 11, 12, 13, 16 y 17**.
  Son el origen de las exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
- `DesignAgent/Salvo-Blueprint.md`: §4.3, §4.4 y decisiones 37–43 de la bitácora.
- `DesignAgent/Salvo-Progress.md`, entrada «Etapa 5 — UI y dashboard».
- `AGENTS.md`: reglas de UI, calidad y seguridad. En particular: TypeScript estricto, timeout
  explícito en toda red externa, y nada de secretos en variables `NEXT_PUBLIC_`.
- Contrato que se consume, ya integrado: `backend/src/Salvo.Application/Alerts/AlertViews.cs`,
  `AlertSortOrder.cs`, `ScoringRunReference.cs`, y `backend/src/Salvo.Api/AlertEndpoints.cs` para
  los códigos de error exactos.
- Código existente: `frontend/` completo —hoy `src/app/page.tsx`, `src/lib/api/health.ts` y su
  test—, `frontend/next.config.ts` y `scripts/check.sh`.

## Alcance

### Dentro

**Tipos generados desde OpenAPI**

- Se agrega `openapi-typescript` como devDependency **con versión fijada**, y un script npm que
  genera los tipos desde `/openapi/v1.json`. Es la única dependencia nueva autorizada de la etapa,
  aprobada en el diseño v2 (D7).
- El archivo generado **se versiona**, y `check.sh` verifica que esté al día sin necesitar la API
  levantada: se compara contra un documento OpenAPI capturado, o se regenera y se diffea. El
  mecanismo concreto es decisión delegada; el requisito es que la compuerta detecte la deriva y que
  **no dependa de un puerto abierto**.

**Cliente de API**

- Módulo con `import "server-only"`. En esta etapa el navegador **no** llama a la API.
- **URLs absolutas** desde `SALVO_API_BASE_URL`. El `fetch` con ruta relativa del patrón de
  `health.ts` lanza `TypeError` en Node: el rewrite de `next.config.ts` solo existe para peticiones
  del navegador.
- **Timeout explícito** con `AbortSignal.timeout(...)` en toda llamada, según `AGENTS.md`.
- Lector de `application/problem+json` que extrae `status`, `title`, `detail` y la extensión `code`.
- **Guardas que proyectan, no que comprueban.** Cada guarda construye un objeto nuevo con las claves
  conocidas, exige las obligatorias e **ignora las desconocidas**. La guarda de `health.ts` es un
  predicado sobre el mismo objeto y deja pasar todo lo demás: ese patrón no se extiende.

**Rutas**

- `/alerts` — feed. Usa `sort=SCORE_DESC` y pide **una sola página de hasta 200**. Muestra la
  procedencia (`currentRun`) en cabecera. Severidad con **texto además de color**.
- `/alerts/[id]` — detalle. Snapshot y evaluación vigente como **dos bloques visualmente distintos**,
  cada uno con su procedencia: el snapshot con su `createdAt`, el vigente rotulado con la corrida
  («Vigente desde la corrida #3, …»). Si se muestra `evaluatedAt`, se rotula «calculada por primera
  vez el…», porque es la fecha de su primera inserción y no la de la corrida vigente.
- Un armazón mínimo de aplicación: cabecera y navegación, con los enlaces a `/import` y `/dashboard`
  presentes aunque esas rutas las construya `E5C`.

**Rutas dinámicas**

- Cada ruta de datos declara `export const dynamic = 'force-dynamic'`.
- **Criterio verificable**: `npm run build` pasa con `SALVO_API_BASE_URL` apuntando a un puerto
  cerrado, y la salida lista las rutas como dinámicas (`ƒ`), no estáticas (`○`). Sin esto, Next
  hornea el estado de error en build y lo sirve para siempre.

**Revisión**

- Acción de servidor que revalida la ruta y relee el estado persistido. **Sin estado optimista.**
- **La nota sobrevive al conflicto.** React 19 restablece los campos no controlados de un
  `<form action>` al terminar la acción, también con error; sin mitigación, tras un `409` la
  analista pierde lo que escribió. Campos controlados, o devolver lo enviado en el estado de la
  acción y aplicarlo como `defaultValue`.
- `maxLength` de 2000 en la nota, y aun así se mapea `NOTE_TOO_LONG`.
- `AlertReviewResult.applied === false` significa «ya estaba exactamente así»: se dice eso, no
  «revisión registrada».

**Divergencia**

- `divergence.hasBandDivergence` verdadero: aviso **destacado** que dice en palabras qué cambió, y
  casilla de reconocimiento que habilita el envío. Espeja el `409`
  `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED`; el `409` se maneja igual, porque otra pestaña puede cambiar
  el estado entre el render y el envío.
- `snapshot.evaluationId` distinto de `currentEvaluation.evaluationId` **sin** divergencia de banda:
  aviso **secundario**, sin casilla. Ser más estricto que la API crearía un `200` inalcanzable para
  cualquier otro cliente.
- `currentEvaluation` nulo: el texto dice «no hay evaluación vigente para este pedido», **nunca** «la
  evaluación vigente es 0», y la revisión procede sin reconocimiento, como hace la API.

**Códigos de error**

| `code` | HTTP | Qué ofrece la UI |
| --- | --- | --- |
| `ALERT_NOT_FOUND` | 404 | Volver al feed |
| `INVALID_STATUS` | 400 | Error de formulario |
| `NOTE_TOO_LONG` | 400 | Error de formulario |
| `ALERT_ALREADY_REVIEWED` | 409 | Recargar y ver el veredicto vigente |
| `ALERT_REVIEW_NOTE_CONFLICT` | 409 | Recargar y ver la nota registrada |
| `ALERT_DIVERGENCE_NOT_ACKNOWLEDGED` | 409 | Volver al aviso de divergencia |
| `ALERT_REVIEW_CONFLICT` | 409 | Recargar |
| `INVALID_SORT`, `INVALID_PAGE`, `INVALID_PAGE_SIZE` | 400 | Error interno; no deberían ocurrir desde la UI |

Nunca se muestra «Error 409» ni el `detail` crudo como único texto. El mensaje principal es de la
UI, en castellano y accionable; el `detail` puede ir como información secundaria.

**Frontera servidor–cliente**

- Los componentes cliente reciben **primitivas**: `alertId`, `status`, `hasBandDivergence`, textos ya
  formateados. **Nunca objetos de API.** En React Server Components toda prop de un componente
  cliente se serializa entera dentro del HTML.
- Se habilita `experimental.taint` y se marca la respuesta cruda del módulo `server-only` con
  `experimental_taintObjectReference`.

**Estados y accesibilidad**

- Cuatro estados por ruta: con datos, cargando, vacío y con error.
- Los **tres estados vacíos** de D3 se distinguen y se redactan distinto: sin pedidos, con pedidos y
  sin corrida —`currentRun` nulo—, y con corrida y sin alertas abiertas.
- Contraste suficiente, foco visible, encabezados de tabla asociados, avisos anunciados a lectores
  de pantalla, todo alcanzable por teclado.
- Formato de montos e instantes con **configuración regional fija** y zona `America/Montevideo`,
  para que los tests no dependan de la máquina.

**Tests**

- **Un componente cliente no recibe ningún objeto de API.** Con `fetch` simulado que devuelve un
  campo extra (`isFraudLabel: true`), afirmar que no llega ni al render ni a las props serializadas.
  El test viejo de «el HTML no contiene `isFraudLabel`» pasaría **vacuamente**, porque la API no
  emite ese campo: no sirve y se reemplaza.
- Las guardas descartan claves desconocidas y rechazan las obligatorias faltantes.
- Cada código de error produce su mensaje, y ninguno cae en un texto genérico.
- La nota sobrevive a un `409`.
- El aviso de divergencia bloquea el envío hasta reconocerlo; el aviso secundario no lo bloquea.
- `currentEvaluation` nulo no produce «la evaluación vigente es 0».
- `applied === false` no dice «revisión registrada».
- Los tres estados vacíos producen textos distintos.
- El cliente aborta por timeout y eso produce el estado de error, no una excepción sin manejar.

### Fuera

- `/import` y `/dashboard`, el gráfico y la sección de calidad. Son `E5C-IMPORT-DASHBOARD`.
- `scripts/smoke-ui.sh`. Es `E5C`.
- Consumir `GET /api/system/capabilities`. Es `E5C`, que es quien decide qué ocultar.
- **Todo `backend/`.** Si algo del contrato faltara, detenerse y consultar; no se modifica la API.
- Playwright o cualquier dependencia distinta de `openapi-typescript`.
- Autenticación e identidad de revisor.
- Actualizaciones en vivo, WebSockets o sondeo automático.
- Reabrir alertas revisadas.
- `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` y el Blueprint: estado canónico, del
  coordinador.

### Paths autorizados

- `frontend/**`
- `scripts/check.sh` — solo para el paso de verificación de tipos generados
- `frontend/package.json` y `frontend/package-lock.json` — solo para `openapi-typescript`

### Paths reservados por otros trabajos

- Ninguno. Esta tarea **reserva** `frontend/**` y `scripts/check.sh` completos.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths autorizados.
- Instalación de dependencias: **solo `openapi-typescript`**, con versión fijada. Cualquier otra,
  consultar antes.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna.
- Commits locales en la rama: autorizados.
- Levantar la API localmente para generar los tipos: autorizado.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E5B-ALERTAS-UI.md` sin faltantes antes de empezar.
- [ ] `npm run build` pasa con `SALVO_API_BASE_URL` en un puerto cerrado, y las rutas aparecen como
      dinámicas (`ƒ`). **Documentar la salida del build en el handoff.**
- [ ] El cliente es `server-only`, usa URL absoluta y fija timeout en toda llamada.
- [ ] Las guardas proyectan: se verifica con un test que inyecta una clave desconocida.
- [ ] Ningún componente cliente recibe un objeto de API, verificado por test con campo extra
      inyectado. **Documentar que ese test falla si se le pasa el objeto entero**, igual que se hizo
      con el test diferencial de `E5A`.
- [ ] Los tipos generados están versionados y la compuerta detecta la deriva sin API levantada.
- [ ] Los ocho códigos de la tabla producen mensajes distintos y accionables.
- [ ] La nota sobrevive a un `409`.
- [ ] Divergencia de banda bloquea; cambio de evaluación sin banda avisa sin bloquear.
- [ ] Los tres estados vacíos se distinguen.
- [ ] Severidad con texto además de color en feed y detalle.
- [ ] Sin cambios en `backend/`.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E5B-ALERTAS-UI`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E5B-ALERTAS-UI.md` | Brief válido |
| `npm run build` con puerto cerrado | Pasa; rutas listadas como `ƒ` |
| Test de campo extra inyectado | No llega al render ni a las props; y falla si se pasa el objeto entero |
| Test de guardas | Clave desconocida descartada; obligatoria faltante rechazada |
| Tests de códigos de error | Ocho mensajes distintos |
| Test de nota tras `409` | La nota se conserva |
| Tests de divergencia | Banda bloquea; sin banda avisa |
| Tests de estados vacíos | Tres textos distintos |
| `npm run check --prefix frontend` | Typecheck, lint sin advertencias y Vitest verdes |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados; nada de `backend/` |
| `/handoff E5B-ALERTAS-UI` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Estructura de carpetas dentro de `frontend/src`, y cómo se organizan cliente, guardas y vistas.
- Forma concreta de las guardas y del lector de `problem+json`.
- Mecanismo de verificación de deriva de tipos en `check.sh`, siempre que no requiera la API.
- Diseño visual, tipografía y paleta, respetando contraste y severidad con texto.
- Cómo se conserva la nota tras un conflicto.
- Redacción exacta de los mensajes, siempre en castellano y accionables.
- Cómo se simula `fetch` en los tests.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- falta algo en el contrato de la API y pareciera necesario tocar `backend/`;
- hace falta una dependencia además de `openapi-typescript`;
- las rutas no pueden hacerse dinámicas sin renunciar a algo del diseño;
- `experimental.taint` no está disponible o rompe el build;
- el test de la frontera servidor–cliente no puede construirse de forma determinista;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos, incluida la salida de `next build` mostrando las
  rutas dinámicas, y la comprobación de que el test de frontera falla cuando debe.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
