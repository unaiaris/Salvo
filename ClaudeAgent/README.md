# Salvo — Kit de trabajo para Claude

> Estado: preparado; Claude Code no está instalado actualmente en este entorno
> Última actualización: 2026-08-31
> Entrada automática: `../CLAUDE.md`

## Propósito

Esta carpeta adapta el proyecto para Claude sin duplicar el Blueprint. Las decisiones, el estado y
los requisitos siguen siendo compartidos con Codex.

## Archivos

| Archivo | Uso |
| --- | --- |
| `../CLAUDE.md` | Memoria raíz cargada por Claude Code |
| `Claude-Workflow.md` | Protocolo permanente específico de Claude |
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

## Uso con Claude.ai sin Claude Code

Subir o adjuntar únicamente:

- `DesignAgent/Salvo-Blueprint.md`;
- `DesignAgent/Salvo-Overview.md`;
- `DesignAgent/Salvo-Progress.md`;
- `ClaudeAgent/Claude-Workflow.md`;
- el task brief actual.

Claude.ai no debe inferir que puede editar el repositorio. Los cambios deben volver como propuesta,
patch o handoff y ser integrados por el coordinador.

## Referencia oficial

Claude Code carga `CLAUDE.md` como memoria de proyecto y admite imports mediante `@ruta`.
Consultar la documentación oficial antes de cambiar esta estructura:

- [How Claude remembers your project](https://code.claude.com/docs/en/memory)
- [Claude Code best practices](https://code.claude.com/docs/en/best-practices)
