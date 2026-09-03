# Salvo — Task brief `E5C-IMPORT-DASHBOARD`

## Identificación

- Work ID: `E5C-IMPORT-DASHBOARD`
- Etapa: 5
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-03
- Rama/worktree: `claude/e5c-import-dashboard`
- Commit base: el commit de `main` que incorpora este brief
- Modelo y esfuerzo acordados: Opus 5 · `high`. El coordinador recomendó `Sonnet 5 · high` —brief
  cerrado, patrones ya establecidos por `E5B`, sin invariantes nuevas— y el usuario optó por no
  arriesgar en la tarea que cierra la etapa. La pieza que motivó la cautela es `scripts/smoke-ui.sh`:
  si no puede hacerse determinista ni garantizar la limpieza de procesos tras dos intentos,
  detenerse y consultar.
- Dependencias: `E5A-API-LECTURA` (merge `5f48db0`) y `E5B-ALERTAS-UI` (merge `278e100`), ambas
  integradas y verificadas.

## Resultado esperado

El recorrido queda cerrado de punta a punta: se importan pedidos, se ejecuta la corrida desde la
propia consola, se ve el resultado agregado en un dashboard que declara de qué corrida habla, y todo
eso se verifica con un smoke ejecutable en los tres escenarios que exige el Blueprint —con datos, sin
datos y con la API caída—. Con esto la Etapa 5 queda completa.

## Contexto obligatorio

- `Coordination/Tasks/E5-DISENO.md` (v2), decisiones **D1, D3, D5, D10, D13, D14, D15, D16 y D17**.
- `Coordination/Tasks/E5-revision-adversarial.md`, hallazgos **1, 6, 7, 10, 14, 15 y 18**.
- `DesignAgent/Salvo-Blueprint.md`: §3, §4.4 y decisiones 37–43 de la bitácora.
- `DesignAgent/Salvo-Progress.md`, entrada «Etapa 5 — UI y dashboard».
- `AGENTS.md`.
- Contrato que se consume: `backend/src/Salvo.Application/Dashboard/DashboardViews.cs`,
  `Metrics/EvaluationMetricsViews.cs`, `backend/src/Salvo.Api/DashboardEndpoints.cs`,
  `EvaluationMetricsEndpoints.cs`, `SystemEndpoints.cs`, `OrderEndpoints.cs` y
  `RiskEvaluationEndpoints.cs`.
- Patrones ya establecidos y que se reutilizan **sin reescribir**: `frontend/src/lib/api/` completo
  —cliente `server-only`, guardas que proyectan, catálogo de mensajes— y
  `frontend/src/components/`. `E5B` los dejó funcionando; esta tarea los extiende.
- `Coordination/Handoffs/Claude.md`, entradas de `E5A` y `E5B`, en particular sus «Riesgos o
  pendientes».

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**
Los handoffs de `E5A` y `E5B` declararon pendientes §4.4 del Blueprint y la línea de Recharts de
`AGENTS.md`, ambas corregidas desde `c759d58`.

## Alcance

### Dentro

**`/import`**

- Carga del corpus demo, visible **solo** si `GET /api/system/capabilities` devuelve
  `demoDataEnabled: true`. Con `false` no se muestra el botón y no se llama al endpoint.
- Importación de archivo con `POST /api/order-imports`: formato CSV o JSON, y presentación de
  `ImportOrdersResult` —totales, importados, duplicados, inválidos— con **errores por fila** y el
  aviso de truncado cuando `errorsTruncated` es verdadero.
- **Acción «Ejecutar scoring»**, que invoca `POST /api/risk-evaluations:run` y presenta el
  `ScoringRunSummary`: secuencia, pedidos, evaluaciones creadas y reusadas, alertas abiertas y
  omitidas. Sin este paso no hay evaluaciones ni alertas: importar no procesa, y la pantalla lo dice.
- Los códigos `413`, `415` y `400` de la importación producen mensajes propios.

**`/dashboard`**

- Cabecera de procedencia: corrida vigente y, si `ordersPendingScoring > 0`, aviso con enlace a
  `/import`.
- Alertas abiertas por severidad, incluidas las bandas en cero.
- **Monto en riesgo desglosado por moneda**, en filas. **Nunca un total.** Lo mismo para el fraude
  reportado, presentado por separado.
- Tasa de marcado.
- **Riesgo temporal como SVG renderizado en el servidor**, sin librería de gráficos. Barras
  semanales con `<title>`, `<desc>` y una tabla equivalente para lectores de pantalla. El componente
  es de servidor: esa es la razón de la decisión 43 y no debe convertirse en cliente.
- Señales principales.
- **Sección de calidad**, visible solo con `demoDataEnabled: true`, consumiendo
  `GET /api/evaluation-metrics`, con el **rótulo fijo y no ocultable**:

  > La fixture demo fue construida para que las reglas recuperen sus propias etiquetas. Estas
  > métricas prueban el pipeline de evaluación —división temporal, holdout sin retuning, cálculo
  > correcto—, no la calidad del criterio de detección.

- `409 METRICS_UNAVAILABLE` produce su propio estado, con enlace a «Ejecutar scoring».

**Estados**

- Cuatro por ruta, y los **tres estados vacíos** distinguidos también en el dashboard: sin pedidos,
  con pedidos y sin corrida, y con corrida y sin alertas abiertas.

**Correcciones de arrastre**

- `frontend/src/lib/format.ts`: la etiqueta de `amount_anomaly` dice «Monto atípico para el
  comprador», pero la regla usa la mediana del **comprador o la del comercio** según la historia
  disponible, y el detalle contradice al título cuando cae al comercio. Debe quedar neutral respecto
  del alcance; el detalle ya dice contra qué mediana se comparó.
- `frontend/src/lib/api/messages.ts`: agregar `SCORING_RUN_CONFLICT` y `METRICS_UNAVAILABLE`, que
  quedaron sin mensaje propio en `E5B` porque no había pantalla que los produjera.
- `frontend/package.json`: el script `dev` es `next dev` a secas y en Next.js 16 eso usa Turbopack,
  mientras `build` fija `--webpack`. Alinear `dev` con `build`, o dejar constancia razonada de por
  qué no.
- `frontend/src/lib/api/health.ts`: quedó sin uso tras `E5B`, con URL relativa y sin timeout
  explícito, lo que roza la regla de `AGENTS.md`. Decidir: eliminarlo con su test, o adaptarlo al
  cliente `server-only`. Justificar en el handoff.

**Deriva entre la API y el documento OpenAPI capturado**

- La compuerta detecta hoy la deriva entre `frontend/openapi/salvo-openapi.json` y los tipos
  generados, **pero no** entre la API real y ese documento. Agregar un test de integración en
  `backend/tests/**` que compare el documento servido por `WebApplicationFactory` contra el archivo
  versionado y falle si difieren. Es el cierre que propuso el handoff de `E5B`.

**`scripts/smoke-ui.sh`**

- Levanta la API con `DemoData__Enabled=true` y `next start`, hace seed y corrida, y comprueba con
  `curl` las cuatro rutas en tres escenarios: **con datos**, **base vacía** y **API apagada**,
  buscando textos fijos que distingan cada estado.
- Espera activamente a que cada proceso esté listo en vez de dormir un tiempo fijo, y **libera los
  procesos y los puertos al terminar, también si falla**. Un smoke que deja un proceso colgado en el
  5100 rompe la ejecución siguiente.
- Usa una base temporal propia; **no toca `salvo.db`** del desarrollador.
- No se agrega a `scripts/check.sh` en esta tarea: se ejecuta y se documenta su salida en el handoff.
  Integrarlo a la compuerta se decide al cerrar la etapa.

**Tests**

- La sección de calidad y el botón de demo no se renderizan con `demoDataEnabled: false`, y en ese
  caso no se llama a `/api/evaluation-metrics`.
- El rótulo de la fixture está presente siempre que la sección se muestra.
- El monto en riesgo se renderiza por moneda y no existe ningún total.
- Los tres estados vacíos del dashboard producen textos distintos.
- `METRICS_UNAVAILABLE` y `SCORING_RUN_CONFLICT` producen sus mensajes.
- Los errores por fila de la importación se listan, y el truncado se avisa.
- El SVG del gráfico incluye `<title>`, `<desc>` y la tabla equivalente.
- Ningún componente cliente nuevo recibe objetos de API: `src/test/boundary.test.ts` debe cubrir
  también las rutas nuevas.
- El test de deriva de OpenAPI falla si se altera el documento versionado.

### Fuera

- Modificar el motor, `RuleConfig`, el fingerprint, las migraciones o la semántica de alertas.
- Estructurar las señales del motor e internacionalización: **es tarea de la Etapa 8**, ya acordada.
- Enriquecer la fixture con casos duros: Etapa 8.
- Conversión de divisas.
- Autenticación e identidad de revisor.
- Playwright o cualquier dependencia nueva. Si algo pareciera exigirla, detenerse y consultar.
- Actualizaciones en vivo, WebSockets o sondeo automático.
- `DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md` y el Blueprint: estado canónico, del
  coordinador.

### Paths autorizados

- `frontend/**`
- `scripts/**`
- `backend/tests/**` — solo para el test de deriva de OpenAPI

### Paths reservados por otros trabajos

- Ninguno. Esta tarea **reserva** `frontend/**`, `scripts/**` y `backend/tests/**`.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths autorizados.
- Instalación de dependencias: **no**. Ninguna.
- Modificar `backend/src/**`: **no**. Si faltara algo del contrato, detenerse y consultar.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna. El smoke usa base temporal propia y no borra `salvo.db`.
- Commits locales en la rama: autorizados, y **se pide commitear por partes** —capa de datos,
  pantallas, smoke— en vez de un único commit final. `E5B` se cortó por un error de servidor y sin
  checkpoint se habría perdido todo.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E5C-IMPORT-DASHBOARD.md` sin faltantes antes de empezar.
- [ ] `/import` ejecuta la corrida y muestra su resumen.
- [ ] Con `demoDataEnabled: false` no aparecen ni el botón de demo ni la sección de calidad, y no se
      llama a `/api/evaluation-metrics`.
- [ ] El rótulo de la fixture es fijo y no ocultable.
- [ ] El monto en riesgo se muestra por moneda, sin total, igual que el fraude reportado.
- [ ] El gráfico es SVG de servidor, con `<title>`, `<desc>` y tabla equivalente.
- [ ] Los tres estados vacíos se distinguen en el dashboard.
- [ ] Las cuatro correcciones de arrastre están hechas y justificadas.
- [ ] El test de deriva de OpenAPI existe y **se verificó que falla** al alterar el documento.
- [ ] `scripts/smoke-ui.sh` corre los tres escenarios y libera procesos y puertos aun si falla.
      **Pegar su salida en el handoff.**
- [ ] `boundary.test.ts` cubre las rutas nuevas.
- [ ] `npm run build` sigue pasando con la API apagada y las rutas de datos siguen dinámicas.
- [ ] Sin cambios en `backend/src/**` ni dependencias nuevas.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E5C-IMPORT-DASHBOARD`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E5C-IMPORT-DASHBOARD.md` | Brief válido |
| `scripts/smoke-ui.sh` | Tres escenarios verdes; sin procesos ni puertos colgados |
| Test de deriva de OpenAPI | Pasa; y falla al alterar el documento versionado |
| Tests de capacidades | Sin demo no hay sección de calidad ni llamada al endpoint |
| Test del rótulo de la fixture | Presente siempre que la sección se muestre |
| Test de monto por moneda | Filas por moneda, sin total |
| Tests de estados vacíos del dashboard | Tres textos distintos |
| `boundary.test.ts` | Cubre `/import` y `/dashboard` |
| `SALVO_API_BASE_URL=http://127.0.0.1:9 npm run build` | Pasa; rutas de datos como `ƒ` |
| `npm run check --prefix frontend` | Typecheck, lint y Vitest verdes |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados; nada de `backend/src/` |
| `/handoff E5C-IMPORT-DASHBOARD` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Diseño visual del dashboard y del gráfico, respetando contraste y accesibilidad.
- Forma concreta del SVG: escalas, ejes, etiquetas.
- Cómo se presenta el barrido de umbrales colapsado.
- Estructura del formulario de importación y cómo se listan los errores por fila.
- Mecanismo de espera y limpieza en `smoke-ui.sh`.
- Cómo se compara el documento OpenAPI en el test de integración.
- Redacción exacta de los mensajes, en castellano y accionables.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- falta algo del contrato y pareciera necesario tocar `backend/src/**`;
- hace falta cualquier dependencia nueva;
- el smoke no puede hacerse determinista o no puede limpiar sus procesos con garantía;
- el gráfico no puede quedar como componente de servidor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos, incluida la salida de `smoke-ui.sh` y la
  comprobación de que el test de deriva falla cuando debe.
- Justificación de las cuatro correcciones de arrastre, en particular qué se hizo con `health.ts`.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
