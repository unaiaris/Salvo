# Salvo — Task brief `E7B-EXPLICACIONES-UI`

## Identificación

- Work ID: `E7B-EXPLICACIONES-UI`
- Etapa: 7
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-05
- Rama/worktree: `claude/e7b-explicaciones-ui`
- Commit base: `8951fa2` — el `HEAD` de `main` tras integrar `E7A-EXPLICACIONES` en el merge
  `82f2487` y cerrar sus registros. Es la base real de `claude/e7b-explicaciones-ui`
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. El coordinador había recomendado `Sonnet 4.5 ·
  high` apoyándose en un precedente inexistente —afirmó que `E6B-CALLBACK-UI` se había corrido con
  Sonnet, y su brief dice `Opus 5 · high`, igual que `E5C`—. La forma del trabajo sí es la de E6B:
  un bloque más en el detalle, una guarda, un aviso que ya tiene molde, fixtures y mensajes, con el
  razonamiento difícil resuelto en E7A. Lo que justifica Opus es la guarda que debe rechazar un
  `summary` fuera de `READY` y la contaminación del sub-objeto en `boundary.test.ts`: construir un
  test y demostrar que falla cuando debe es la pieza que en este proyecto viene pidiendo Opus. Si la
  guarda no puede rechazar el caso que se le pide, o el smoke no puede verificar el bloque,
  detenerse y consultar tras dos intentos.
- Dependencias: **`E7A-EXPLICACIONES` integrada en `main`**, con el OpenAPI recapturado.

## Resultado esperado

La analista ve la explicación de una alerta en su detalle, puede pedirla si no existe y regenerarla
si falló, y ve un aviso cuando la explicación describe una evaluación que ya no es la vigente.
Ningún texto que no haya pasado la validación llega al navegador, ni siquiera cuando la respuesta
de la API viene malformada.

## Contexto obligatorio

- `Coordination/Tasks/E7-DISENO.md` (v2), decisiones **D2, D3, D4, D8, D9 y D10**.
- `Coordination/Tasks/E7-revision-adversarial.md`, hallazgos **6, 8 y 10**.
- `Coordination/Handoffs/Claude.md`, entrada de `E7A-EXPLICACIONES`: la forma exacta del
  sub-objeto `explanation` y de los códigos de error.
- `DesignAgent/Salvo-Blueprint.md`: §4.3, §4.4 y §4.7.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 7 — Explicabilidad»: los primeros diez ítems los
  cerró `E7A`; esta tarea cierra los tres siguientes —el bloque en el detalle con su aviso y sus
  botones, `messages.ts` con `boundary.test.ts` y los textos de `smoke-ui.sh`, y mover
  `EXPLANATION_READY` junto a los demás valores de cable—. El último, activar Anthropic, queda
  fuera y se decide aparte.
- Código existente a reutilizar:
  - `frontend/src/lib/api/guards.ts`: `projectExternalEvaluation` es el molde exacto;
  - `frontend/src/app/alerts/[id]/divergence.ts`: la forma del aviso que precede al contenido;
  - `frontend/src/lib/format.ts` y `frontend/src/lib/api/messages.ts`: rótulos y códigos;
  - el bloque de evaluación externa de E6B en el detalle de alerta, con su botón y su estado;
  - `frontend/src/app/alerts/[id]/**`;
  - `frontend/src/test/fixtures.ts` y `frontend/src/test/boundary.test.ts`;
  - `scripts/smoke-ui.sh`.

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**

## Alcance

### Dentro

**1. Guarda de proyección.** `projectExplanation`, con el molde de `projectExternalEvaluation`:
proyecta campo por campo y **rechaza como `malformed`** un `summary` no nulo cuando `status` es
distinto de `READY`. Esta es la tercera puerta de D4: la base y el manejador cierran las otras dos,
y la guarda cierra la del navegador.

**2. Bloque de explicación en el detalle de alerta.** Estados: sin pedir, `PENDING`, `READY`,
`FAILED`. Con `READY`, el resumen; con `FAILED`, el rótulo del código y el botón de regenerar; con
el tope agotado, el rótulo correspondiente y **sin** botón. Declara que el resumen lo compone una
plantilla determinista, no un modelo.

**3. Aviso de desactualizada.** Con la forma de `divergence.ts`: **precede al contenido**, no lo
oculta. `isOutdated` llega calculado desde la API y no se recalcula en el cliente.

**4. `explanationId` en el formulario de revisión.** Se envía como primitiva oculta, igual que
`alertId` (D10). Si no hay explicación, se envía vacío y la revisión funciona igual.

**5. La guarda y las fixtures ya existen: se extienden, no se crean.** `E7A` dejó
`projectExplanation` en `guards.ts` con el rechazo de `summary` fuera de `READY`, y `fixtures.ts`
con las claves nuevas, porque sin eso el contrato no compilaba. Esta tarea parte de ahí. Queda
pendiente de `E7A`: mover la constante `EXPLANATION_READY` de `guards.ts` junto a los demás valores
de cable.

**6. `messages.ts` y su test** ganan los códigos nuevos: `NOT_GROUNDED_RULE`,
`NOT_GROUNDED_NUMBER`, `TOO_LONG`, `MALFORMED_OUTPUT`, `PROVIDER_UNAVAILABLE`, `PROVIDER_TIMEOUT`,
`PROVIDER_REFUSED`, `CANCELLED`, `ATTEMPT_LIMIT_REACHED`, `EXPLANATION_PENDING`,
`EXPLANATION_ALREADY_READY`, `EXPLANATION_ATTEMPTS_EXHAUSTED`.

**7. `boundary.test.ts`** contamina el sub-objeto nuevo, incluido un `summary` inyectado sobre una
fila `FAILED`.

**8. `scripts/smoke-ui.sh`** gana comprobaciones del bloque en el escenario con datos.

### Fuera

- **La nota de revisión no se precarga nunca con el resumen.** Es la vía más corta para que la
  prosa del modelo termine en la auditoría con firma humana. Prohibido explícitamente por D10.
- Cualquier acción recomendada, insignia de acción o consejo derivado de la severidad dentro del
  bloque de explicación. `recommendedAction` no existe (decisión 53).
- Componentes cliente que reciban objetos de API: siguen recibiendo primitivas (decisión 42).
- Cualquier cambio en el motor, el fingerprint, las alertas, el dashboard o las métricas.
- Cambios en la lógica de generación, validación o ciclo de vida: eso es E7A.
- Una ruta propia de explicaciones. Vive solo en el detalle de alerta.

### Paths autorizados

- `frontend/**`
- `scripts/**`
- `backend/src/Salvo.Api/**` y `backend/tests/**`, **solo** si el handoff de E7A dejó algún ajuste
  menor pendiente y el coordinador lo autoriza por escrito

### Paths reservados por otros trabajos

- Ninguno. Esta tarea reserva los anteriores.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Levantar API y frontend para el smoke: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna. No borrar bases.
- Commits locales en la rama: autorizados, y **se pide commitear por partes**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E7B-EXPLICACIONES-UI.md` sin faltantes antes de empezar.
- [ ] `projectExplanation` rechaza como `malformed` un `summary` no nulo con `status` distinto de
      `READY`. Documentar en el handoff que el test **falla** si la guarda proyecta el campo sin
      mirar el estado.
- [ ] `boundary.test.ts` afirma que ningún componente cliente recibe el objeto de explicación, con
      el sub-objeto contaminado. Documentar la falsación en dos pasos, como en E5 y E6.
- [ ] El formulario de revisión no precarga la nota con el resumen, y hay un test que lo afirma.
- [ ] Revisar una alerta sin explicación funciona exactamente igual que antes.
- [ ] El aviso de desactualizada precede al contenido y no lo oculta.
- [ ] `npm run build --prefix frontend` pasa **con la API apagada** (decisión 41).
- [ ] `./scripts/smoke-ui.sh` verde, con las comprobaciones nuevas, en los tres escenarios: con
      datos, sin datos y con la API caída.
- [ ] `npm run api:types:check --prefix frontend` al día, sin recapturar: el contrato lo gobierna
      E7A.
- [ ] Sin dependencias nuevas.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E7B-EXPLICACIONES-UI`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E7B-EXPLICACIONES-UI.md` | Brief válido |
| Test de la guarda con `summary` sobre `FAILED` | `malformed`; falla si la guarda proyecta sin mirar el estado |
| `boundary.test.ts` con el sub-objeto contaminado | Ningún objeto cruza la frontera; falsado en dos pasos |
| Test de la nota de revisión sin precarga | La nota llega vacía |
| Test de revisión sin explicación | Comportamiento idéntico al anterior |
| `npm run build --prefix frontend` con la API apagada | Build verde |
| `./scripts/smoke-ui.sh` | Verde en los tres escenarios, con las comprobaciones nuevas |
| `npm run api:types:check --prefix frontend` | Al día |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E7B-EXPLICACIONES-UI` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Disposición visual del bloque y su lugar en el detalle.
- Redacción de los rótulos en castellano, coherente con `format.ts` y `messages.ts`.
- Cómo se representa el estado `PENDING` mientras se espera.
- Qué comprobaciones concretas agrega el smoke, siempre que verifiquen contenido y no solo un `200`.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- el contrato que dejó E7A no alcanza para lo que el diseño pide mostrar;
- la guarda no puede rechazar el caso que se le exige sin cambiar el contrato;
- aparece la tentación de recalcular `isOutdated` en el cliente;
- hace falta una dependencia nueva o un componente cliente que reciba un objeto;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos y resultados exactos, incluidas las falsaciones de la guarda y del test de frontera.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
