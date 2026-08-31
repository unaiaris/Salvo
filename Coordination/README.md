# Salvo — Coordinación entre Codex y Claude

> Estado: activo cuando existe trabajo paralelo
> Fuente de progreso integrado: `../DesignAgent/Salvo-Progress.md`

## Objetivo

Permitir trabajo paralelo sin duplicar la fuente de verdad, pisar archivos o confundir una rama
terminada con una etapa integrada.

## Archivos

| Archivo | Propósito | Responsable de escritura |
| --- | --- | --- |
| `Workboard.md` | Tareas, ownership, dependencias e integración | Coordinador |
| `Handoffs/Codex.md` | Entregas producidas por Codex | Codex |
| `Handoffs/Claude.md` | Entregas producidas por Claude | Claude |
| `Task-Brief-Template.md` | Resultado, alcance, autorizaciones y verificación de cada tarea | Coordinador |
| `../DesignAgent/Salvo-Progress.md` | Estado canónico por etapa | Coordinador tras verificar |
| `../DesignAgent/Salvo-Blueprint.md` | Decisiones de producto/arquitectura | Coordinador tras aprobación |

## Regla de fuente única

No se mantienen copias Codex/Claude del Blueprint o Progress. Ambos agentes leen los mismos
documentos. `AGENTS.md` y `CLAUDE.md` adaptan el comportamiento de cada herramienta.

## Flujo recomendado

1. El coordinador crea un Work ID y completa un task brief compartido con resultado, alcance,
   autorizaciones, paths, commit base y criterios de aceptación.
2. Se asigna propietario y se comprueba que no haya solapamiento.
3. Cada agente trabaja en una rama/worktree independiente.
4. El agente ejecuta sus verificaciones y escribe su handoff.
5. El coordinador revisa y decide el orden de integración.
6. Después de cada merge ejecuta las pruebas afectadas.
7. Tras la integración conjunta ejecuta la compuerta de etapa.
8. Solo entonces actualiza Workboard, Progress y, si corresponde, Blueprint.

## Reglas de concurrencia

- Nunca ejecutar dos agentes sobre el mismo worktree.
- Nunca asignar el mismo archivo a dos tareas activas salvo una secuencia explícita.
- Preferir particiones por módulo o path, no por líneas del mismo archivo.
- Registrar dependencias entre tareas; no asumir que una rama ve cambios no integrados de otra.
- Si dos tareas requieren un archivo central, serializar ese archivo o reservar la edición final al
  coordinador.
- Solución, paquetes NuGet, migraciones EF Core, OpenAPI, lockfiles y configuración central suelen
  ser puntos de serialización.

## Convención de ramas

- Codex: `codex/<work-id>-<slug>`.
- Claude: `claude/<work-id>-<slug>`.

La rama y el commit base deben aparecer en el task brief y en el handoff.

## Integración segura

- No hacer force push ni reescribir historia compartida.
- No resolver silenciosamente conflictos semánticos.
- Integrar primero dependencias, luego consumidores.
- Después del merge, volver a ejecutar pruebas: el éxito aislado de dos ramas no garantiza que su
  combinación sea válida.
- El usuario conserva la decisión final sobre cambios de arquitectura, alcance y acciones externas.

## Qué significa “misma información”

- Diseño aprobado: Blueprint.
- Estado integrado: Progress.
- Trabajo asignado: Workboard.
- Resultado no integrado: handoff del agente.
- Reglas técnicas: AGENTS.
- Adaptación para Claude: CLAUDE + ClaudeAgent.

Esta separación evita que una nota de trabajo temporal se convierta accidentalmente en una decisión
canónica.
