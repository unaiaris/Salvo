# Salvo — Task brief `E11A-INTEGRACION-CONTINUA`

## Identificación

- Work ID: `E11A-INTEGRACION-CONTINUA`
- Etapa: 11 — la IA de verdad
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-30
- Rama/worktree: `claude/e11a-ci`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. No se declara de antemano: el commit que lo escribiera en `main`
  movería la punta y volvería falso el campo. Es la lección de `E8B`, y el coordinador acepta que
  este campo no lleve SHA; la verificación la hacen el primer commit de la rama y el handoff.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5.5 · `high`**, la fila «ambigüedad real dentro del alcance» de
  `ClaudeAgent/Claude-Model-Policy.md`: el punto 8 depende de lo que diga la documentación de Render.
  El `brief-check`, con un modelo distinto.
- Dependencias: ninguna. `E11B` —el adaptador de Anthropic— depende de ésta.

## Resultado esperado

**La compuerta se corre sola.** Cada push, a cualquier rama, corre `scripts/check.sh` en GitHub
Actions; cada push a `main` corre además `scripts/smoke-ui.sh`; y el flujo completo se puede lanzar a
mano sobre cualquier rama. Con las mismas versiones exactas que el repositorio fija y sin ningún
secreto.

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

1. **El flujo** en `.github/workflows/`, con cuatro disparadores, cada uno con su motivo:
   - **`push` a cualquier rama** corre `check.sh`. Es lo que produce la evidencia de esta tarea —la
     corrida verde de la rama y la roja de la falsación— sin abrir pull requests, porque este proyecto
     integra por merge local.
   - **`push` a `main`** corre además `smoke-ui.sh`. Lo que queda en `main` es lo que se despliega, y
     ahí el recorrido importa.
   - **`workflow_dispatch`** corre el flujo completo a mano, desde la pestaña Actions, sobre cualquier
     rama. Es como el coordinador ve el recorrido verde **antes** de integrar.
   - **`pull_request`** corre los dos, por si algún día se usan. No es el camino de este proyecto.
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
     La caché de capas de Docker hace que un commit de documentos reconstruya en medio minuto. El
     riesgo real no es el cupo: es **desplegar un `main` en rojo**.
   - **Si la documentación oficial de Render** confirma un valor que despliegue solo con los checks en
     verde, se usa.
   - **Si no lo confirma**, se queda `commit`.
   - **Nunca `off`**: con la compuerta corriendo sola, un despliegue a mano es un paso más que alguien
     tiene que recordar, y la medición no lo justifica.
   - **En los dos casos se reescribe el comentario**, con las cifras medidas y la decisión tomada, para
     que deje de prescribir algo que el proyecto decidió no hacer.

### Fuera

- Cambiar `check.sh` o `smoke-ui.sh`. Si algo falla en CI por el entorno, se ajusta el flujo; si
  hace falta cambiar un script, es hallazgo y consulta.
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
  El coordinador sube las ramas, lanza el flujo a mano, y pega los resultados.
- **La falsación la prepara el agente**: una segunda rama local, `claude/e11a-falsacion`, cortada de
  `claude/e11a-ci` después del último commit de la tarea, con **un solo commit** que invierte un
  assert de un test existente y lo nombra en el mensaje. El coordinador la sube, ve el rojo, y la
  borra local y remota: **el borrado es suyo**.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] Los cuatro disparadores del punto 1, cada uno con lo que corre.
- [ ] SDK, Node y npm salen de `global.json`, `.nvmrc` y `packageManager`, sin versiones repetidas en
      el flujo.
- [ ] Cada acción está fijada por SHA completo con su versión en un comentario.
- [ ] `permissions: contents: read`, y ningún secreto referenciado.
- [ ] **Una corrida verde de `check.sh`** al subir la rama `claude/e11a-ci`.
- [ ] **Una corrida verde del flujo completo** —`check.sh` y `smoke-ui.sh`— lanzada a mano sobre esa
      misma rama.
- [ ] **Una falsación**: la rama `claude/e11a-falsacion`, con un assert invertido, tiene que dar
      **rojo**, en el paso de los tests, nombrando ese test. Un flujo que nunca se vio fallar no está
      probado.
- [ ] El cartel de estado en el README, y `./scripts/check-docs.sh` verde.
- [ ] El punto 8 resuelto en una de sus dos formas, con la URL oficial que lo sostiene.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` en local | Verde |
| El push de `claude/e11a-ci` | `check.sh` verde, con la duración de cada paso |
| El flujo lanzado a mano sobre `claude/e11a-ci` | `check.sh` y `smoke-ui.sh` verdes |
| El push de `claude/e11a-falsacion` | **Roja**, en el paso de los tests, nombrando el test invertido |
| `./scripts/check-docs.sh` | Verde con el cartel agregado |

## Decisiones delegadas

- Caché de NuGet y de npm: sí o no, con el motivo.
- Un solo job o dos: con el motivo.
- La redacción del cartel y del comentario de `render.yaml`.

## Detenerse y consultar si

- `check.sh` o `smoke-ui.sh` fallan en CI por algo que exige cambiar el script;
- el flujo necesita un secreto para algo;
- la documentación de Render sobre el punto 8 es ambigua;
- aparece cualquier paso que exija crear una cuenta, una credencial o aceptar términos.

## Entrega requerida

- Resumen, archivos tocados, y las dos corridas —la verde y la roja— con su enlace.
- Qué se decidió sobre el punto 8, con la fuente oficial.
- Supuestos, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
