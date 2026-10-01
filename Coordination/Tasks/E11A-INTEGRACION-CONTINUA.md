# Salvo — Task brief `E11A-INTEGRACION-CONTINUA`

## Identificación

- Work ID: `E11A-INTEGRACION-CONTINUA`
- Etapa: 11 — la IA de verdad
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-30 · revisado el 2026-10-01 tras cuatro rondas de `brief-check`
- Rama/worktree: `claude/e11a-ci`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. No se declara de antemano: el commit que lo escribiera en `main`
  movería la punta y volvería falso el campo. Es la lección de `E8B`, y el coordinador acepta que
  este campo no lleve SHA; la verificación la hacen el primer commit de la rama y el handoff.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5.5 · `high`**, la fila «ambigüedad real dentro del alcance» de
  `ClaudeAgent/Claude-Model-Policy.md`: la primera ejecución en Linux puede pedir ajustes del flujo
  que no se pueden prever desde acá. El `brief-check` ya vio dos candidatos —ver «Lo que
  probablemente falle la primera vez»—. El `brief-check`, con un modelo distinto.
- Dependencias: **`E11A0-HUSO-HORARIO`, integrada en `main` antes de darle a esta.** El runner corre
  en UTC, y con el defecto de fechas que corrige `E11A0` la suite del frontend sale roja por un motivo
  de producto, no de entorno. `E11B` —el adaptador de Anthropic— depende de ésta.

## Resultado esperado

**La compuerta se corre sola.** Cada push, a cualquier rama, corre `scripts/check.sh` y
`scripts/smoke-ui.sh` en GitHub Actions, con las mismas versiones exactas que el repositorio fija y
sin ningún secreto. Y `render.yaml` declara que `main` solo se despliega en Render cuando esa corrida
está en verde.

**Lo que esta tarea verifica es el valor declarado; que la plataforma lo cumple lo observa el
coordinador después de integrar**, porque solo se puede ver con el flujo en `main` y desde el panel de
Render. La tarea entrega los pasos de esa observación, y el coordinador la registra en el Progress.

**La observación no se hace sobre el commit de merge.** Ese commit modifica `render.yaml`, y la
documentación de Render dice que «Each push to the linked branch that modifies your Blueprint file
triggers a deploy of any added or modified resources»
(https://render.com/docs/infrastructure-as-code), sin decir si esa sincronización respeta
`checksPass`. Puede desplegar sin esperar, y eso no probaría nada. **La que cuenta es el primer commit
a `main` posterior al merge que no toque `render.yaml`** —en la práctica, el cierre del estado
canónico—: ese despliegue tiene que esperar la corrida verde antes de empezar.

### Por qué va primero, y por qué dentro de esta etapa

La Etapa 11 va a tocar la pieza más delicada del proyecto: el camino por el que un texto escrito por
un modelo llega a la base. Antes de tocarla, la compuerta tiene que correr **sola**, en cada push,
sin depender de que alguien se acuerde. Hoy vive en `scripts/check.sh` y la corre el coordinador a
mano. Cualquiera que abra el repositorio ve diez etapas de disciplina y ninguna corrida automática.

Esta tarea no pasa por el diseño de la etapa porque no cambia el producto. `E11-DISENO` cubre el
adaptador.

## Contexto obligatorio

- `DesignAgent/Salvo-Blueprint.md`, §11, «Etapa 11 — La IA de verdad», y la **decisión 71** de la
  bitácora, que saca la CI remota de la lista de diferidos del §2.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 11 — La IA de verdad»: el que esta tarea cierra
  es el **primero**.
- `scripts/check.sh` y `scripts/smoke-ui.sh`, **enteros**: qué asumen que ya está instalado y qué
  instalan ellos. El flujo instala exactamente lo que los scripts asumen, y nada más.
- `global.json`, `.nvmrc` y el campo `packageManager` de `frontend/package.json`: son las tres
  versiones que el flujo tiene que respetar.
- `Dockerfile`, el comentario sobre la imagen del SDK que empieza con «`global.json` fija el SDK en
  10.0.400 con `rollForward: disable`». Explica cómo una exigencia fija apoyada en una referencia
  móvil rompió el primer despliegue. **Esta tarea es el mismo problema con otro nombre**: una acción
  de GitHub pedida por `@v4` es una etiqueta que se mueve sola.
- `render.yaml`, el comentario sobre `autoDeployTrigger`.
- `Coordination/Workboard.md`, las lecciones: las viñetas que siguen a «Dos lecciones de la Etapa 7
  para los briefs que vengan», debajo de la tabla «Cola próxima».

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

1. **El flujo** en `.github/workflows/`. **Cada `push`, a cualquier rama, corre `check.sh` y
   `smoke-ui.sh`**; `pull_request` corre lo mismo, por si algún día se usan.
   - **Por qué en cualquier rama**: es lo que produce la evidencia de esta tarea —la corrida verde de
     la rama y la roja de la falsación— sin abrir pull requests, porque este proyecto integra por
     merge local.
   - **Por qué el recorrido en cada push, y no solo en `main`**: la única otra forma de verlo antes de
     integrar sería `workflow_dispatch`, y **no sirve para eso**. La documentación de GitHub dice que
     ese evento solo dispara si el archivo del flujo existe en la rama por defecto
     (https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows),
     y mientras el flujo viva en la rama no está en `main`. El costo es tiempo de corrida por push: la
     entrega reporta la duración de cada job, y si el recorrido resulta caro, correrlo solo en `main`
     es una decisión posterior, tomada con ese número.
   - **`workflow_dispatch` se agrega igual**, para relanzar el flujo a mano sobre `main` una vez
     integrado. No es evidencia de esta tarea.
   - **El runner se fija por versión** —la etiqueta con número que la documentación de GitHub liste
     vigente el día—, **nunca `ubuntu-latest`**: es una etiqueta que se mueve sola, la misma clase de
     referencia que el punto 3 prohíbe.
2. **Versiones exactas, por archivo.** El SDK desde `global.json`, Node desde `.nvmrc`, npm en la
   versión que declara `packageManager`. Ninguna versión escrita a mano en el flujo si ya existe en
   un archivo del repositorio: dos declaraciones de lo mismo terminan discrepando.
3. **Acciones fijadas por SHA completo**, con la versión legible en un comentario al lado. Una
   acción pedida por `@v4` es exactamente la clase de referencia móvil que rompió el despliegue de
   la Etapa 10.
4. **Permisos mínimos**: `contents: read` y nada más. El flujo no necesita escribir nada.
5. **Ningún secreto.** Sin `AI_PROVIDER` ni `KOIN_MODE` definidos, el backend arranca con los
   simuladores, que es lo que la compuerta prueba.
6. **El flujo no arregla nada.** No formatea, no commitea, no actualiza dependencias. Reporta.
7. **El cartel de estado** al principio del `README.md`.
8. **El despliegue, y el comentario de `render.yaml` que esta tarea vuelve falso.** Hoy `render.yaml`
   declara `autoDeployTrigger: commit`, y su comentario prescribe pasar a `off` «si el repositorio
   vuelve a tener actividad de desarrollo», por el costo del cupo de 500 minutos de construcción. La
   Etapa 11 **es** actividad de desarrollo, así que el brief tiene que decidir, y decide esto:

   - **La premisa del `off` ya no se sostiene, y lo dice una medición.** Las cuatro construcciones de
     la Etapa 10 tardaron 11,9 s, 2 min 10 s, 28,3 s y 1 min 15 s: unos cuatro minutos de quinientos.
     Están en `DesignAgent/Salvo-Progress.md`, registro de actividad, fila «Duración de las primeras
     construcciones en Render», leídas del panel de despliegues.
     La caché de capas de Docker hace que un commit de documentos reconstruya en medio minuto. El
     riesgo real no es el cupo: es **desplegar un `main` en rojo**.
   - **Se usa `checksPass`.** La especificación del Blueprint
     (https://render.com/docs/blueprint-spec) lo describe así, leído el 2026-10-01: «Trigger a deploy
     only if the linked branch's CI checks pass». Con la compuerta corriendo en cada push, `main` solo
     se despliega en verde.
   - **Una consecuencia que el comentario tiene que decir.** La página de despliegues
     (https://render.com/docs/deploys) dice que con ese valor Render no despliega si «Zero checks are
     detected for the new commit». Hoy no pasa, porque todo push corre la compuerta. Pero si algún día
     el flujo deja de correr para ciertos cambios —un filtro por carpetas, por ejemplo—, esos commits
     a `main` **no se despliegan nunca**, y nada avisa.
   - **Las citas se vuelven a comprobar el día que se ejecute**: el agente abre esas dos páginas, y la
     de infraestructura como código citada en «Resultado esperado», y compara con estas citas. Si alguna cambió o desapareció, se detiene y consulta. El comentario
     de `render.yaml` lleva las dos URL.
   - **Nunca `off`**: con la compuerta corriendo sola, un despliegue a mano es un paso más que alguien
     tiene que recordar, y la medición no lo justifica.
   - **Se reescribe el comentario**, con las cifras medidas y la decisión tomada, para
     que deje de prescribir algo que el proyecto decidió no hacer.

### Lo que probablemente falle la primera vez

El `brief-check` leyó los scripts pensando en una máquina Linux limpia y encontró dos candidatos. No
son certezas: son lo primero que hay que mirar si el primer push sale rojo.

- **`smoke-ui.sh` exige `lsof`** para comprobar los puertos, y un runner puede no traerlo. Instalarlo
  en el runner es **entorno, no una dependencia del proyecto**, y está permitido.
- **Los proyectos de test nunca se restauraron en modo bloqueado fuera de macOS**: el `Dockerfile` solo
  restaura el proyecto de la API. Si un lockfile difiere entre plataformas, es hallazgo y consulta.
- **El npm que trae Node no tiene por qué ser el que declara `packageManager`** (`npm@11.19.0`).
  El flujo instala la versión declarada, leída del archivo, y la comprueba con `npm --version` antes de
  correr los scripts.
- **Un test de fechas que sale corrido un día** no es un problema del runner: es el defecto que corrige
  `E11A0`, y significa que esa tarea no está integrada en la base de la rama. Se detiene y consulta.

### Fuera

- Cambiar `check.sh` o `smoke-ui.sh`. Si algo falla en CI por el entorno, se ajusta el flujo; si
  hace falta cambiar un script, es hallazgo y consulta.
- **Fijar la zona horaria** (`TZ`) en el flujo, el `Dockerfile` o `render.yaml` para que un test
  pase. Esconde el defecto en vez de corregirlo; la corrección es de producto y es `E11A0`.
- Dependabot, CodeQL, publicación de artefactos, matrices de sistemas operativos.
- Cualquier cambio de producto.

### Paths autorizados

- `.github/workflows/**`
- `README.md`, **solo** el cartel de estado.
- `render.yaml`, **solo** `autoDeployTrigger` y su comentario, según el punto 8.
- **La rama de falsación** (`claude/e11a-falsacion`), y **solo en ella**: un único archivo de test
  existente, con **un solo assert invertido**. Esa rama no se integra nunca.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11A-INTEGRACION-CONTINUA.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno. `E11B` todavía no tiene brief.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no** se agrega ninguna al proyecto.
- Red: leer documentación oficial de GitHub y de Render, y resolver el SHA de cada acción.
- Escrituras externas: **no**. El agente **no hace push**: la configuración lo deniega a propósito.
  El coordinador sube las ramas y pega los resultados.
- **La tarea avanza en tres tiempos, y cada uno espera al anterior**, porque el agente no puede
  ejecutar el flujo: el primer push es su primera ejecución.
  1. **El flujo.** El agente deja el flujo, el cartel y el punto 8 en `claude/e11a-ci`, y **se
     detiene**. El coordinador sube la rama y pega el resultado. **Si sale rojo por el entorno**, el
     agente ajusta el flujo con commits nuevos sobre la misma rama, el coordinador vuelve a subirla, y
     se repite **hasta el verde**.
  2. **La falsación, cortada desde el verde.** Recién con la rama en verde, el agente corta
     `claude/e11a-falsacion` **desde ese commit**, con **un solo commit** que invierte un assert de un
     test existente y lo nombra en el mensaje. Cortarla desde el verde garantiza que el rojo tenga un
     solo motivo posible. El coordinador la sube, ve el rojo, y la borra local y remota: **el borrado
     es suyo**.
  3. **El handoff.** Con los enlaces de las dos corridas, el agente escribe el handoff en un último
     commit sobre `claude/e11a-ci`.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] Los disparadores del punto 1: `push` a cualquier rama y `pull_request` corren `check.sh` y
      `smoke-ui.sh`; `workflow_dispatch` existe para relanzar sobre `main`.
- [ ] El runner fijado por versión, nunca `ubuntu-latest`.
- [ ] SDK, Node y npm salen de `global.json`, `.nvmrc` y `packageManager`, sin versiones repetidas en
      el flujo.
- [ ] Cada acción está fijada por SHA completo con su versión en un comentario.
- [ ] `permissions: contents: read`, y ningún secreto referenciado.
- [ ] **Una corrida verde**, `check.sh` y `smoke-ui.sh`, al subir la rama `claude/e11a-ci`.
- [ ] **Una falsación**: la rama `claude/e11a-falsacion`, con un assert invertido, tiene que dar
      **rojo**, y el log de `check.sh` nombra ese test. Un flujo que nunca se vio fallar no está
      probado.
- [ ] El cartel de estado en el README, y `./scripts/check-docs.sh` verde.
- [ ] El punto 8: `autoDeployTrigger: checksPass`, con el comentario reescrito —cifras medidas,
      decisión tomada, y la consecuencia de los commits sin checks— y la URL oficial que lo sostiene.
- [ ] Los pasos para que el coordinador observe en Render, **en el primer commit a `main` posterior al
      merge que no toque `render.yaml`**, que el despliegue **esperó** a los checks, y que el servicio
      tomó el valor nuevo.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` en local | Verde |
| El push de `claude/e11a-ci` | `check.sh` y `smoke-ui.sh` verdes, con la duración de cada job |
| El push de `claude/e11a-falsacion` | **Roja**, y el log de `check.sh` nombra el test invertido |
| **Después de integrar**, en el panel de Render —lo observa el coordinador— | El servicio muestra el disparo condicionado a los checks, y el despliegue del primer commit posterior al merge que no toca `render.yaml` esperó la corrida verde antes de empezar |
| `./scripts/check-docs.sh` | Verde con el cartel agregado |

## Decisiones delegadas

- Caché de NuGet y de npm: sí o no, con el motivo.
- Un solo job o dos: con el motivo.
- La redacción del cartel y del comentario de `render.yaml`.

## Detenerse y consultar si

- la restauración bloqueada (`--locked-mode`) falla en Linux por un lockfile: cambiar lockfiles está
  fuera de esta tarea;
- `check.sh` o `smoke-ui.sh` fallan en CI por algo que exige cambiar el script;
- un test falla en el runner y arreglarlo exige tocar código de producto o un test: eso no es entorno,
  y queda fuera de esta tarea;
- alguna de las tres citas de Render del punto 8 y de «Resultado esperado» cambió o ya no está;
- el flujo necesita un secreto para algo;
- la documentación de Render sobre el punto 8 es ambigua;
- aparece cualquier paso que exija crear una cuenta, una credencial o aceptar términos.

## Entrega requerida

- Resumen, archivos tocados, y las dos corridas —la verde de `claude/e11a-ci` y la roja de la
  falsación— con su enlace y la duración de cada job.
- Qué se decidió sobre el punto 8, con la fuente oficial.
- **Los pasos de la observación en Render**, escritos para que el coordinador los siga después de
  integrar: dónde mirar en el panel que el servicio tomó el valor, cómo se ve un despliegue que espera
  a los checks, y por qué se mira el primer commit posterior al merge y no el merge.
- Supuestos, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
