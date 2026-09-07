# Salvo — Task brief `E9C2-ACCESIBILIDAD`

## Identificación

- Work ID: `E9C2-ACCESIBILIDAD`
- Etapa: 9
- Tipo: `implementación` **en dos fases, con una parada obligatoria en el medio**
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-07
- Rama/worktree: `claude/e9c2-accesibilidad`
- Commit base: `a994855`, el `merge-base` real de `claude/e9c2-accesibilidad` con `main`. **Esta
  línea se commitea en la rama, no en `main`**: es la lección de `E8B`.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E9C1-IDIOMA` integrada (merge `0d65f9a`). Va después a propósito: la accesibilidad
  crea texto nuevo, y con los diccionarios ya puestos ese texto nace en los dos idiomas en vez de
  nacer en castellano y traducirse enseguida.

## Resultado esperado

**La etapa no promete cumplir un nivel de WCAG. Promete que alguien recorrió la consola sin ver la
pantalla y anotó qué no funcionó.** Esa es una afirmación que casi ningún proyecto de portafolio
puede hacer, y es toda la razón de que esta tarea exista separada.

Lo automatizable se comprueba con herramientas y entra en la compuerta, para que no se pudra. Lo que
ninguna herramienta puede contestar —si una analista que no ve la pantalla puede emitir un veredicto
sin haber entendido mal lo que estaba por hacer— lo contesta una persona con un lector de pantalla.

**El punto de partida no es un desastre y el brief no finge que lo sea.** La consola ya tiene
`<main>`, `<header>` y `<nav aria-label>`; las secciones llevan `aria-labelledby`; las tablas llevan
`<caption class="sr-only">`; los resultados de acción llevan `role="status"` y `role="alert"`; y la
severidad se escribe en palabras a propósito, con el comentario que dice por qué. Esto es una pasada
de refinamiento sobre algo que ya se pensó, no un rescate.

## Contexto obligatorio

- `Coordination/Tasks/E9-DISENO.md` (**v2**), decisión **D7** entera: la tarea produce una lista con
  severidad **antes** de corregir, y el coordinador decide cuáles entran.
- **`Coordination/Tasks/E9C2-recorrido-voiceover.md`**: el guión que el coordinador corre en la
  parada del medio. Leerlo antes de la fase 1, porque dice qué se va a mirar y con qué criterio.
- `Coordination/Handoffs/Claude.md`, entrada de `E9C1`: los diccionarios, y que todo literal nuevo
  nace en los dos.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 9», el ítem «Pasada de accesibilidad».
- Código, abierto antes de escribir nada:
  - `frontend/eslint.config.mjs` y `frontend/package.json`.
  - `frontend/src/app/layout.tsx` y `frontend/src/components/console-header.tsx`: los puntos de
    referencia que ya existen.
  - `frontend/src/components/severity-badge.tsx` y `failure-notice.tsx`.
  - `frontend/src/app/alerts/alert-table.tsx`, `frontend/src/app/alerts/[id]/review-form.tsx` y
    `frontend/src/app/alerts/[id]/review-panel.tsx`.
  - `frontend/src/lib/i18n/es.ts` y `pt.ts`.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Fase 1 — Lo automatizable, y la lista

**1. Averiguar primero qué hay.** `eslint-config-next` ya trae un subconjunto de reglas de
accesibilidad. **La tarea empieza midiendo cuáles están activas**, y solo agrega lo que falte. Sumar
un plugin que ya está corriendo es superficie sin beneficio, y este proyecto tiene la regla de no
agregar dependencias porque sí.

**2. Las dependencias que falten, con nombre y versión exacta.** Se autorizan, excepcionalmente y
solo éstas: `eslint-plugin-jsx-a11y` si no está cubierto, y `axe-core` con su enlace a Vitest para
comprobar el árbol renderizado. **Versiones exactas**, como todo el resto de `package.json`, y el
handoff dice qué aporta cada una que la otra no.

**3. Corregir lo que las herramientas encuentren**, y dejarlas corriendo en `npm run check` para que
la deriva se detecte en vez de prometerse — el molde de `OpenApiDriftTests` y de `check-docs.sh`.

**4. Preparar el estado que el recorrido necesita.** Una base recién sembrada no sirve para el
guión: **ninguna alerta tiene explicación escrita**, y **el aviso de divergencia no aparece**. Los
pasos 11 y 12 del recorrido quedarían verificando nada, y peor: producirían hallazgos falsos sobre
avisos que la pantalla no tenía por qué mostrar.

La fase 1 entrega, con sus pasos escritos en el handoff para que el coordinador los repita:

- **Una alerta con explicación ya escrita**, que es un `POST` y sale barato.
- **Una alerta con aviso de divergencia**, si se puede producir sin deformar el corpus. El camino es
  insertar un pedido **anterior** a uno ya alertado, para el mismo comercio, de modo que su baseline
  cambie y el rescoreo mueva el score contra el snapshot congelado. **No se puede desde `/import`
  con las muestras que hay**: el corpus termina el 2026-08-28 y la única fila válida de
  `docs/muestras/import-con-errores.csv` es del 29, para un comprador sin historia en ese comercio.
  Si conseguirlo exige tantear montos, **se declara no producible y el recorrido salta el paso 11**.
  Un aviso no verificado y dicho vale más que un paso que fabrica hallazgos falsos.
- Los avisos de «explicación desactualizada» y «escrita por otra plantilla» **no** se piden: exigen
  dos versiones vivas y quedan declarados como no verificados.

**5. Entregar la lista y PARAR.** Esto no es opcional y es la forma de la tarea:

- La lista va en el handoff, con **una fila por hallazgo**: pantalla, qué pasa, severidad
  —`alta` si impide completar una tarea, `media` si la vuelve confusa, `baja` si es incomodidad—, y
  el arreglo propuesto en una línea.
- **La tarea se detiene ahí.** No corrige nada de lo que la fase 2 vaya a tocar, no adivina qué va a
  encontrar el recorrido, y no empieza a poner `aria-live` por las dudas.
- El coordinador corre el recorrido con VoiceOver, suma sus hallazgos a la lista, decide cuáles
  entran, y despacha la fase 2 con esa decisión escrita.

### Fase 2 — Lo que el recorrido encontró

Se despacha **después** de la parada, con la lista aprobada en la mano. Entra únicamente lo que el
coordinador eligió. Cada corrección lleva su test, y **todo literal nuevo nace en los dos
diccionarios**: una clave que falte en uno es error de compilación, como estableció `E9C1`.

Lo que el recorrido casi seguro va a tocar, dicho para que la fase 1 no lo corrija por adelantado y
para que la fase 2 no se sorprenda: el anuncio del resultado de una acción de servidor, el foco
después de emitir un veredicto, si los avisos que **aparecen** llegan al lector cuando aparecen, y
si las celdas de la tabla se leen con el nombre de su columna.

### Fuera

- **Prometer un nivel de WCAG.** La etapa afirma lo que hizo: una pasada automática y un recorrido
  humano. Ni una palabra más.
- Rediseñar pantallas. Si un hallazgo solo se arregla cambiando el diseño, se anota con su severidad
  y se deja **afuera y dicho**.
- El README, el guion de demo, las capturas y el artículo: `E9D`. **Las capturas se regeneran allá**,
  así que cualquier cambio visible que esta tarea produzca las desactualiza, y eso es esperado.
- Tocar el backend, el motor, el corpus, la migración o el contrato. Si un hallazgo exige tocar el
  contrato, la tarea para y consulta.
- Un tercer idioma, y cualquier cambio a los diccionarios que no sea agregar claves nuevas.

### Paths autorizados

- `frontend/src/**`
- `frontend/eslint.config.mjs`, `frontend/package.json` y `frontend/package-lock.json`
- `frontend/vitest.config.mts` y `frontend/src/test/**`
- `scripts/smoke-ui.sh`, solo si un hallazgo se puede afirmar desde el navegador
- `Coordination/Handoffs/Claude.md`

**No** entran, y son las reservas que rompieron tareas anteriores por sub-reservar:
`frontend/openapi/**` ni `frontend/src/lib/api/schema.d.ts` —el contrato no cambia—, ni nada de
`backend/`.

### Paths reservados por otros trabajos

- `README.md`, `docs/**`, `tools/capturas/**`, `scripts/capturas.sh` y `scripts/demo.sh`: `E9D`.

**Cuidado con la compuerta**: `scripts/check-docs.sh` verifica que cada ruta y cada nombre de test
que el README cita exista. Renombrar un test que el README nombra la rompe, y el README está
reservado. Si hay que renombrar uno, la tarea para y consulta.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- **Dependencias nuevas: sí, y solo las del punto 2**, con versión exacta y justificación por
  separado. Es la única tarea de la etapa que las tiene permitidas.
- Migraciones: **no**.
- Levantar la consola y la API para probar: autorizado.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR.
- **Acciones destructivas: ninguna.** `rm` está denegado y es regla del usuario.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

### De la fase 1

- [ ] `/brief-check Coordination/Tasks/E9C2-ACCESIBILIDAD.md` sin faltantes antes de empezar.
- [ ] El handoff dice **qué reglas de accesibilidad ya estaban activas** antes de agregar nada.
- [ ] Cada dependencia nueva tiene versión exacta y una línea que dice qué aporta que la otra no.
- [ ] Las comprobaciones corren dentro de `npm run check` y por lo tanto en la compuerta.
- [ ] El estado del recorrido está preparado y sus pasos escritos: una alerta con explicación, y
      una con divergencia **o** la declaración de que no es producible.
- [ ] La lista de hallazgos existe, con severidad y arreglo propuesto por fila.
- [ ] **La tarea se detuvo** y no corrigió nada fuera de lo que las herramientas encontraron.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.

### De la fase 2

- [ ] Entró exactamente lo que el coordinador eligió, y lo que quedó afuera está dicho con su
      severidad.
- [ ] Cada corrección lleva su test.
- [ ] Todo literal nuevo está en los **dos** diccionarios.
- [ ] La consola sigue funcionando en los dos idiomas: la pasada `pt` del smoke sigue verde.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9C2-ACCESIBILIDAD.md` | Brief válido |
| Reglas activas antes de tocar nada | Enumeradas en el handoff |
| `npm run check` | Incluye las comprobaciones nuevas y pasa |
| Estado del recorrido | Explicación escrita; divergencia producida o declarada imposible |
| Lista de hallazgos | Una fila por hallazgo, con severidad |
| Parada tras la fase 1 | La rama no tiene correcciones que el recorrido no pidió |
| Literales nuevos | En `es.ts` **y** en `pt.ts` |
| `./scripts/smoke-ui.sh` | Verde en `es` y en la pasada `pt` |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E9C2-ACCESIBILIDAD` | Con la lista completa |

**Falsaciones exigidas.** Una sola, pero de la clase que este proyecto pide siempre: **romper a
propósito una de las comprobaciones nuevas y ver que la compuerta se pone roja.** Por ejemplo,
sacarle el nombre accesible a un botón. Si la compuerta sigue verde, la comprobación no comprueba
nada, y eso es peor que no tenerla: promete una verificación que no existe.

En la fase 2, cada corrección se falsa igual: se deshace, se corre su test, y el test tiene que
fallar.

## Decisiones delegadas

- Qué comprobaciones automáticas se agregan, mientras la fase 1 diga cuáles ya estaban.
- Cómo se enlaza `axe-core` con Vitest, y sobre qué pantallas corre.
- El orden y el formato de la lista de hallazgos, mientras cada fila lleve severidad.
- Si algún hallazgo automático se arregla mejor en un componente compartido que en cada pantalla.

## Detenerse y consultar si

- las herramientas encuentran algo que exige tocar el contrato o el backend;
- un hallazgo solo se arregla rediseñando una pantalla;
- una dependencia nueva arrastra otras o no se puede fijar en versión exacta;
- hay que renombrar un test que el README nombra;
- **siempre, al terminar la fase 1**: la parada no es opcional.

## Entrega requerida

### De la fase 1

- Resumen del resultado y archivos modificados.
- **Qué reglas ya estaban activas**, y qué agregó cada dependencia nueva.
- **Los pasos para dejar la base en el estado que el recorrido necesita.**
- **La lista de hallazgos**, con severidad y arreglo propuesto.
- La falsación de la compuerta, con su error exacto.
- Estado: `Parcial — en espera del recorrido con lector de pantalla`.
- Handoff en `Coordination/Handoffs/Claude.md`.

### De la fase 2

- Qué entró, qué quedó afuera y por qué.
- Las falsaciones de cada corrección.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
