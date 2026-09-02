# Salvo — Kit de trabajo para Claude

> Estado: activo; Claude Code 2.1.257 instalado y verificado en este entorno
> Última actualización: 2026-09-01
> Entrada automática: `../CLAUDE.md`

## Propósito

Esta carpeta adapta el proyecto para Claude sin duplicar el Blueprint. Las decisiones, el estado y
los requisitos siguen siendo compartidos con Codex.

## Archivos

| Archivo | Uso |
| --- | --- |
| `../CLAUDE.md` | Memoria raíz cargada por Claude Code |
| `Claude-Workflow.md` | Protocolo permanente específico de Claude |
| `Claude-Model-Policy.md` | Criterio para elegir modelo y esfuerzo por tipo de tarea |
| `../Coordination/Task-Brief-Template.md` | Plantilla compartida para asignar una tarea acotada |
| `Claude-Handoff-Template.md` | Plantilla de entrega al terminar |

Los avances reales se registran en `../Coordination/Handoffs/Claude.md`, no en las plantillas.

## Responsabilidad de cada fuente

- `DesignAgent/Salvo-Blueprint.md`: producto y arquitectura aprobados.
- `AGENTS.md`: reglas técnicas, seguridad, autonomía y calidad.
- `DesignAgent/Salvo-Progress.md`: etapa integrada y evidencia canónica.
- `Coordination/Workboard.md`: asignación, ownership y estado de integración.
- `Coordination/Task-Brief-Template.md`: resultado, alcance, paths y verificación de la tarea.
- `CLAUDE.md` y `Claude-Workflow.md`: adaptación operativa específica de Claude.
- Handoff: evidencia producida; nunca cambia por sí solo el estado integrado.

El task brief puede concretar una tarea, pero no contradecir Blueprint o `AGENTS.md`. Ante una
contradicción material, Claude debe detenerse y reportarla.

## Uso con Claude Code

1. Instalar y autenticar Claude Code siguiendo la documentación oficial de Anthropic.
2. Abrir Claude desde la raíz del repositorio, no desde `ClaudeAgent/`.
3. Ejecutar `/memory` para comprobar que `CLAUDE.md` y sus imports fueron cargados.
4. Darle un task brief completo basado en `../Coordination/Task-Brief-Template.md`.
5. Trabajar en la rama/worktree asignada.
6. Cerrar con un handoff completo.

No ejecutar `/init`: el `CLAUDE.md` del proyecto ya existe y fue diseñado para compartir contexto
con Codex.

### Entorno verificado

Claude Code 2.1.257 está instalado en este entorno. El 2026-09-01 se ejecutó `/memory` y confirmó
que `CLAUDE.md` y sus cinco imports (`AGENTS.md`, `DesignAgent/Salvo-Overview.md`,
`DesignAgent/Salvo-Progress.md`, `Coordination/Workboard.md`, `ClaudeAgent/Claude-Workflow.md`)
cargan correctamente, cerrando el riesgo abierto en el handoff `E0-DOC-04`.

### Comandos de sesión

`.claude/commands/` versiona tres comandos que automatizan el ritual descrito en
`Coordination/README.md` y `Claude-Workflow.md` sin reimplementarlo:

| Comando | Uso |
| --- | --- |
| `/gate` | Ejecuta `./scripts/check.sh` desde la raíz y reporta la compuerta full-stack en verde o en rojo, sin ejecutar sus pasos sueltos ni reimplementarlos. |
| `/handoff [work-id]` | Arma una entrada siguiendo `Claude-Handoff-Template.md` con rama, commit base y commit final obtenidos de Git, y la agrega al final de `Coordination/Handoffs/Claude.md` sin sobrescribir entradas previas. |
| `/brief-check [ruta]` | Valida un task brief contra `Coordination/Task-Brief-Template.md`, `AGENTS.md` y el Blueprint; ante cualquier falta se detiene y la reporta en vez de completarla. |

`.claude/settings.json` preautoriza únicamente los comandos verificables y reversibles listados en
el brief `E0-DOC-06` (restore/build/test de .NET, `npm run check`/`build`, `./scripts/check.sh` y
lecturas de Git) y deniega explícitamente `git push`, `git reset --hard` y reescritura de historia,
borrado de archivos, y la lectura de `.env` y sus variantes distintas de `.env.example`. No amplía
la política de autonomía de `AGENTS.md`; solo la expresa como permisos verificables. Overrides
personales van en `.claude/settings.local.json`, que queda fuera de control de versiones.

## Uso con Claude.ai sin Claude Code

Configuración, lista de adjuntos y límites del Proyecto de Claude.ai:
`DesignAgent/Salvo-Project-Instructions.md`. No mantener aquí una segunda lista de adjuntos: ese
documento es la única fuente para esta superficie.

## Referencia oficial

Claude Code carga `CLAUDE.md` como memoria de proyecto y admite imports mediante `@ruta`.
Consultar la documentación oficial antes de cambiar esta estructura:

- [How Claude remembers your project](https://code.claude.com/docs/en/memory)
- [Claude Code best practices](https://code.claude.com/docs/en/best-practices)
