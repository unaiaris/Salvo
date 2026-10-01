# Salvo — Task brief `E11A0-HUSO-HORARIO`

## Identificación

- Work ID: `E11A0-HUSO-HORARIO`
- Etapa: 11 — la IA de verdad
- Tipo: `corrección`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-10-01
- Rama/worktree: `claude/e11a0-huso`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. El coordinador acepta que este campo no lleve SHA, por la lección de
  `E8B`: el commit que lo escribiera en `main` movería la punta y lo volvería falso.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Sonnet 5.5 · `high`**, la fila «Implementación con brief cerrado» de
  `ClaudeAgent/Claude-Model-Policy.md`: la causa ya está diagnosticada hasta la línea. El
  `brief-check`, con un modelo distinto.
- Dependencias: ninguna. **`E11A-INTEGRACION-CONTINUA` depende de ésta**: sin esta corrección, su
  primera corrida en un runner sale roja por un defecto de producto.

## Resultado esperado

**Una fecha de calendario se muestra el mismo día en cualquier máquina**, y la suite del frontend
corre en un huso horario distinto del del negocio, para que un defecto de esta clase no pueda volver a
esconderse detrás del huso de la máquina de desarrollo. En la instancia pública, la tabla semanal del
dashboard y el eje del gráfico vuelven a decir el mismo día.

## El defecto

**Cómo apareció.** El cuarto `brief-check` de `E11A` simuló el runner de GitHub, que corre en UTC, y
un test del frontend falló: `src/app/dashboard/page.test.tsx`, «dibuja el riesgo temporal como SVG
con título, descripción y tabla equivalente», espera «24 ago» y obtiene «23 ago». Pasa con el huso
local de la máquina de desarrollo y con `TZ=America/Montevideo`; falla solo con `TZ=UTC`.

**La causa.** `formatCalendarDate`, en `frontend/src/lib/format.ts`, construye
`new Date(year, month - 1, day)` —medianoche **en el huso del proceso**— y la formatea con un
formateador que tiene `timeZone: BUSINESS_TIME_ZONE`. En una máquina en Montevideo los dos husos
coinciden. En una en UTC, la medianoche UTC son las 21:00 del día anterior en Montevideo, y la fecha
retrocede un día.

**La ironía, que el comentario tiene que dejar escrita.** El comentario de la función explica por qué
leer la fecha como medianoche UTC la correría un día hacia atrás «para cualquiera al oeste de
Greenwich». La solución que adoptó solo es correcta en una máquina que está en el huso del negocio: el
razonamiento del comentario se aplica a su propia solución.

**Está en vivo.** En https://salvo-k6wk.onrender.com/dashboard la tabla semanal rotula las 18 semanas
en **domingo** —26 abr. 2026 … 23 ago. 2026—, mientras el eje del SVG, que recorta el texto con
`weekStart.slice(5)`, las marca en **lunes**. La misma semana, dos días distintos, en la misma página.
El contenedor corre en UTC, y ni el `Dockerfile` ni `render.yaml` declaran un huso.

**El alcance del defecto, verificado.** `new Date(año, mes, día)` aparece **una sola vez** en el código
de producto del frontend. `formatInstant` y `formatDate` parsean instantes absolutos y son correctos en
cualquier huso. El backend pasa sus 295 tests en UTC: `RuleConfig` resuelve el huso del negocio
explícitamente.

## Contexto obligatorio

- `DesignAgent/Salvo-Blueprint.md`, §11, «Etapa 11 — La IA de verdad».
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 11 — La IA de verdad»: el que esta tarea cierra es
  el **primero**, el de `E11A0`.
- `frontend/src/lib/format.ts`, entero: el comentario de cabecera sobre el huso fijo, y
  `formatCalendarDate` con su comentario.
- `frontend/src/app/dashboard/risk-chart.tsx`: los dos lugares que usan la fecha —la tabla, con
  `formatCalendarDate`, y el eje, con `weekStart.slice(5)`—.
- `frontend/vitest.config.mts` y `frontend/vitest.setup.ts`.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

1. **Corregir `formatCalendarDate`.** Una fecha de calendario no tiene huso: se construye y se
   formatea **en el mismo huso**, cualquiera sea el del proceso. La forma exacta es delegada.
2. **Un test unitario de `formatCalendarDate`** que no dependa del huso de la máquina.
3. **La suite del frontend corre en un huso declarado y distinto del del negocio.** UTC sirve: es el de
   los runners y el de la instancia pública. Así, una máquina de desarrollo en Montevideo deja de
   esconder esta clase de defecto.
4. **El comentario de la función** dice qué pasó, en una o dos oraciones, incluida la ironía.

### Fuera

- **Fijar `TZ` en el `Dockerfile`, en `render.yaml` o en el flujo de CI.** Daría verde y escondería el
  defecto en vez de corregirlo. Está prohibido.
- El backend, y los otros formateadores.
- Cambiar cómo el eje del gráfico muestra la fecha.

### Paths autorizados

- `frontend/src/lib/format.ts`, **solo** `formatCalendarDate` y su comentario.
- `frontend/src/lib/format.test.ts`.
- `frontend/vitest.config.mts` y `frontend/vitest.setup.ts`, **solo** para declarar el huso de la suite.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11A0-HUSO-HORARIO.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

`E11A-INTEGRACION-CONTINUA` reserva `.github/workflows/**`, el cartel del README y `render.yaml`.
Ninguno se toca acá.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no**.
- Red: **no**.
- Escrituras externas: **no**. El agente no hace push.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] `formatCalendarDate` devuelve el mismo día con el proceso en UTC, en `America/Montevideo` y en
      un huso al este de Greenwich.
- [ ] Un test unitario lo afirma sin depender del huso de la máquina.
- [ ] La suite del frontend declara su huso, distinto del del negocio, y **la declaración surte efecto
      antes de que se cree la primera fecha**: lo demuestra la falsación.
- [ ] **Falsación**: revirtiendo **solo** la corrección, `npm run test` falla **en la máquina del
      coordinador, que está en Montevideo**, nombrando el test. Eso prueba que la suite dejó de
      depender del huso de la máquina. Después se restaura.
- [ ] `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes.
- [ ] **Al integrar** —lo observa el coordinador, o el coordinador de la etapa desde afuera—: en
      `/dashboard` de la instancia pública, la tabla rotula las semanas en lunes, igual que el eje.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `npm run test` con la corrección | Verde, en el huso declarado de la suite |
| `npm run test` con la corrección revertida, en la máquina del coordinador | **Rojo**, nombrando el test |
| `./scripts/check.sh` | Verde |
| `./scripts/smoke-ui.sh` | Verde |
| `/dashboard` de la instancia pública, después del despliegue | La tabla y el eje dicen el mismo día |

## Decisiones delegadas

- La forma exacta de la corrección.
- Dónde se declara el huso de la suite —configuración o archivo de setup—, mientras la falsación
  demuestre que surte efecto.

## Detenerse y consultar si

- **otro test falla al cambiar el huso de la suite**: es probablemente otro defecto de la misma clase, y
  se reporta en vez de arreglarse acá;
- la corrección exige tocar algo fuera de los paths autorizados.

## Entrega requerida

- Resumen, archivos tocados, y la falsación con su salida.
- Supuestos, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
