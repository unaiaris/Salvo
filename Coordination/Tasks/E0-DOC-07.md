# Salvo — Task brief `E0-DOC-07`

## Identificación

- Work ID: `E0-DOC-07`
- Etapa: 0
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-01
- Rama/worktree: `claude/e0-doc-07-project-and-models`
- Commit base: el commit de esta rama que incorpora este brief y los borradores del coordinador
- Modelo y esfuerzo acordados: Sonnet 5 · `medium`
- Dependencias: `E0-DOC-06` integrada y verificada (merge `50eb2de`).

## Resultado esperado

El repositorio documenta cómo se elige modelo y esfuerzo, y cómo se configura un Proyecto de
Claude.ai sin violar la regla de fuente única. `Coordination/Task-Brief-Template.md` incorpora el
campo de modelo y esfuerzo, y las referencias cruzadas de la documentación quedan coherentes.
Ningún comportamiento de aplicación cambia y la compuerta full-stack sigue verde.

## Contexto obligatorio

- Sección del Blueprint: §14 Mapa de documentación. El coordinador agrega ahí la fila de
  `Claude-Model-Policy.md` al integrar; esta tarea **no** toca el Blueprint.
- Entrada/checklist del Progress: Etapa 0 — Documentación e instrucciones.
- `Coordination/README.md`, regla de fuente única y tabla de responsables de escritura.
- `ClaudeAgent/README.md`, tabla de archivos del kit y sección «Uso con Claude.ai sin Claude Code»,
  que hoy contradice parcialmente el nuevo `Salvo-Project-Instructions.md`.
- `DesignAgent/Salvo-MOC.md`, índice de documentación.
- `Coordination/Task-Brief-Template.md`, sección Identificación.

Los dos documentos nuevos vienen redactados por el coordinador y ya están en el árbol de trabajo de
esta rama. La tarea es revisarlos, corregirlos si contradicen el repositorio, y conectarlos.

## Alcance

### Dentro

- Revisar `ClaudeAgent/Claude-Model-Policy.md` (borrador del coordinador). Verificar que no
  contradiga `AGENTS.md` ni `Coordination/README.md`, y que la tabla de modelos coincida con la
  documentación oficial vigente. Si un dato está desactualizado, corregirlo y declararlo.
- Revisar `DesignAgent/Salvo-Project-Instructions.md` (reescritura del coordinador). Verificar que
  la tabla de tres superficies y el bloque para copiar sean coherentes con `Coordination/README.md`
  y con las decisiones invariantes de `AGENTS.md`.
- Agregar a la sección Identificación de `Coordination/Task-Brief-Template.md` el campo
  `- Modelo y esfuerzo acordados:` entre `Commit base:` y `Dependencias:`.
- Actualizar `ClaudeAgent/README.md`: agregar `Claude-Model-Policy.md` a la tabla de archivos del
  kit, y reemplazar la sección «Uso con Claude.ai sin Claude Code» por una remisión a
  `DesignAgent/Salvo-Project-Instructions.md`, para no mantener dos listas de adjuntos que se
  contradicen.
- Actualizar `DesignAgent/Salvo-MOC.md` con las entradas nuevas o corregidas.
- Registrar la entrega en `Coordination/Handoffs/Claude.md` **usando el comando `/handoff`**, no a
  mano.

### Fuera

- `DesignAgent/Salvo-Blueprint.md`: lo actualiza el coordinador al integrar.
- `DesignAgent/Salvo-Progress.md` y `Coordination/Workboard.md`: estado canónico, del coordinador.
- `AGENTS.md` y `CLAUDE.md`: la política de modelos documenta un criterio de coordinación; no es
  una regla permanente de implementación y no debe duplicarse allí.
- `.mcp.json` y cualquier configuración de servidores MCP. Se evaluó y quedó descartada por ahora.
- Cualquier archivo de `backend/`, `frontend/`, `scripts/` o `.claude/`.
- Instalar o actualizar dependencias.

### Paths autorizados

- `ClaudeAgent/Claude-Model-Policy.md`
- `ClaudeAgent/README.md`
- `DesignAgent/Salvo-Project-Instructions.md`
- `DesignAgent/Salvo-MOC.md`
- `Coordination/Task-Brief-Template.md`
- `Coordination/Handoffs/Claude.md`

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales permitidas: sí, únicamente en los paths autorizados.
- Instalación o actualización de dependencias: no autorizada.
- Escrituras externas: ninguna. No `git push`, no abrir PR.
- Acciones destructivas: ninguna.
- Commits locales en la rama: autorizados.
- Consulta de documentación oficial de Anthropic por web: autorizada y requerida para validar la
  tabla de modelos.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E0-DOC-07.md` ejecutado antes de empezar y sin faltantes.
- [ ] `ClaudeAgent/Claude-Model-Policy.md` existe, es coherente con `AGENTS.md` y su tabla de
      modelos coincide con la documentación vigente.
- [ ] `DesignAgent/Salvo-Project-Instructions.md` distingue las tres superficies y establece que un
      adjunto es una foto fechada.
- [ ] `Coordination/Task-Brief-Template.md` incluye el campo de modelo y esfuerzo en Identificación.
- [ ] `ClaudeAgent/README.md` no mantiene una lista de adjuntos que contradiga a
      `Salvo-Project-Instructions.md`.
- [ ] `DesignAgent/Salvo-MOC.md` refleja los documentos nuevos.
- [ ] Ningún archivo fuera de los paths autorizados aparece en `git status`.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E0-DOC-07`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E0-DOC-07.md` | Brief válido, sin faltantes |
| Consulta de documentación oficial de modelos | Tabla confirmada o corregida, con la fuente citada |
| `grep -n "Modelo y esfuerzo" Coordination/Task-Brief-Template.md` | Campo presente en Identificación |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `git diff --check` | Pasa |
| `/handoff E0-DOC-07` | Entrada agregada al final de `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Redacción concreta de las correcciones a los borradores del coordinador.
- Ubicación exacta de las filas nuevas en las tablas de `ClaudeAgent/README.md` y `Salvo-MOC.md`.
- Texto de la remisión que reemplaza «Uso con Claude.ai sin Claude Code».

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- los borradores del coordinador contradicen `AGENTS.md`, el Blueprint o `Coordination/README.md`
  en algo que no se resuelva con una corrección menor;
- la documentación oficial muestra un catálogo de modelos incompatible con la tabla del borrador;
- aparece solapamiento con otra tarea o cambios ajenos en paths autorizados;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados exactos.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
