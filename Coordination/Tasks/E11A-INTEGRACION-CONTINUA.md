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
- Modelo y esfuerzo acordados: **Opus 5.5 · `high`**. El `brief-check`, con un modelo distinto.
- Dependencias: ninguna. `E11B` —el adaptador de Anthropic— depende de ésta.

## Resultado esperado

**La compuerta se corre sola.** Cada push a `main` y cada pull request corren `scripts/check.sh` en
GitHub Actions, y cada pull request corre además `scripts/smoke-ui.sh`, con las mismas versiones
exactas que el repositorio fija y sin ningún secreto.

### Por qué va primero, y por qué dentro de esta etapa

La Etapa 11 va a tocar la pieza más delicada del proyecto: el camino por el que un texto escrito por
un modelo llega a la base. Antes de tocarla, la compuerta tiene que correr **sola**, en cada push,
sin depender de que alguien se acuerde. Hoy vive en `scripts/check.sh` y la corre el coordinador a
mano. Cualquiera que abra el repositorio ve diez etapas de disciplina y ninguna corrida automática.

Esta tarea no pasa por el diseño de la etapa porque no cambia el producto. `E11-DISENO` cubre el
adaptador.

## Contexto obligatorio

- `scripts/check.sh` y `scripts/smoke-ui.sh`, **enteros**: qué asumen que ya está instalado y qué
  instalan ellos. El flujo instala exactamente lo que los scripts asumen, y nada más.
- `global.json`, `.nvmrc` y el campo `packageManager` de `frontend/package.json`: son las tres
  versiones que el flujo tiene que respetar.
- `Dockerfile`, el comentario sobre la imagen del SDK que empieza con «`global.json` fija el SDK en
  10.0.400 con `rollForward: disable`». Explica cómo una exigencia fija apoyada en una referencia
  móvil rompió el primer despliegue. **Esta tarea es el mismo problema con otro nombre**: una acción
  de GitHub pedida por `@v4` es una etiqueta que se mueve sola.
- `render.yaml`, el comentario sobre `autoDeployTrigger`.
- `Coordination/Workboard.md`, sección de lecciones.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

1. **El flujo** en `.github/workflows/`. Dispara en `push` a `main` y en `pull_request`. Corre
   `check.sh` en los dos casos, y `smoke-ui.sh` en los pull requests.
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
8. **El despliegue, solo con la compuerta en verde, si Render lo admite.** Hoy `render.yaml` declara
   `autoDeployTrigger: commit`: cualquier commit a `main` se despliega, pase o no la compuerta. Si la
   **documentación oficial de Render** confirma un valor que despliegue solo con los checks en verde,
   se usa y se actualiza el comentario que lo explica. Si no lo confirma, **no se toca** y se dice.

### Fuera

- Cambiar `check.sh` o `smoke-ui.sh`. Si algo falla en CI por el entorno, se ajusta el flujo; si
  hace falta cambiar un script, es hallazgo y consulta.
- Dependabot, CodeQL, publicación de artefactos, matrices de sistemas operativos.
- Cualquier cambio de producto.

### Paths autorizados

- `.github/workflows/**`
- `README.md`, **solo** el cartel de estado.
- `render.yaml`, **solo** `autoDeployTrigger` y su comentario, y solo bajo la condición del punto 8.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11A-INTEGRACION-CONTINUA.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno. `E11B` todavía no tiene brief.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no** se agrega ninguna al proyecto.
- Red: leer documentación oficial de GitHub y de Render, y resolver el SHA de cada acción.
- Escrituras externas: **no**. El agente **no hace push**: la configuración lo deniega a propósito.
  El coordinador sube la rama y pega el resultado de la corrida.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] El flujo corre `check.sh` en push a `main` y en pull request, y `smoke-ui.sh` en pull request.
- [ ] SDK, Node y npm salen de `global.json`, `.nvmrc` y `packageManager`, sin versiones repetidas en
      el flujo.
- [ ] Cada acción está fijada por SHA completo con su versión en un comentario.
- [ ] `permissions: contents: read`, y ningún secreto referenciado.
- [ ] **Una corrida verde** sobre la rama, subida por el coordinador.
- [ ] **Una falsación**: una rama descartable con un test roto a propósito, que tiene que dar
      **rojo**. Un flujo que nunca se vio fallar no está probado. El coordinador la sube y la borra
      después.
- [ ] El cartel de estado en el README, y `./scripts/check-docs.sh` verde.
- [ ] El punto 8 resuelto en una de sus dos formas, con la URL oficial que lo sostiene.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` en local | Verde |
| La corrida del flujo sobre la rama | Verde, con la duración de cada paso |
| La corrida sobre la rama de falsación | **Roja**, en el paso que corre los tests, nombrando el test roto |
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
