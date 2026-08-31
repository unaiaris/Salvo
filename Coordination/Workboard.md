# Salvo — Workboard Codex–Claude

> Estado: sin tareas de implementación activas
> Última actualización: 2026-08-31
> Responsable: coordinador de la etapa

## Estados

`Propuesta` → `Asignada` → `En curso` → `Lista para integrar` → `Integrada` → `Verificada`

Estados alternativos: `Bloqueada`, `Cancelada`.

Una tarea no cambia a `Integrada` o `Verificada` por decisión del agente que la implementa.

## Trabajo activo

No hay tareas activas. La Etapa 1 todavía no comenzó.

## Cola próxima

| Work ID | Etapa | Objetivo | Propietario | Estado | Dependencias | Paths previstos |
| --- | --- | --- | --- | --- | --- | --- |
| E1-FOUNDATION | 1 | Fundaciones ASP.NET Core + Next.js y compuerta conjunta | Codex | Propuesta | Aprobación de inicio | Solución, paquetes, EF Core, OpenAPI, frontend y configuración central |

La Etapa 1 contiene archivos difíciles de paralelizar —solución, paquetes NuGet, `package.json`,
lockfile, EF Core, OpenAPI y configuración—, por lo que conviene completar primero su scaffold de
manera secuencial. El trabajo paralelo será más seguro desde módulos con contratos ya integrados.

## Historial integrado

| Work ID | Etapa | Resultado | Agente | Estado | Evidencia |
| --- | --- | --- | --- | --- | --- |
| E0-DOC-01 | 0 | Auditoría original y propuesta B2B | Codex | Verificada | Blueprint/bitácora |
| E0-DOC-02 | 0 | Documentación canónica y `AGENTS.md` | Codex | Verificada | `DesignAgent/*.md` |
| E0-DOC-03 | 0 | Tracker y entrevista Koin | Codex | Verificada | Progress + Interview Prep |
| E0-DOC-04 | 0 | Kit Claude y coordinación multiagente | Codex | Verificada | `CLAUDE.md`, `ClaudeAgent/`, `Coordination/` |
| E0-DOC-05 | 0 | Instrucciones concisas y task brief compartido | Codex | Verificada | `AGENTS.md`, `CLAUDE.md`, `Coordination/Task-Brief-Template.md` |

## Plantilla de fila activa

| Work ID | Etapa | Objetivo | Propietario | Estado | Rama | Base | Paths reservados | Dependencias | Actualizado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |  |  |  |  |

## Reglas del tablero

- El coordinador asigna IDs y ownership.
- Un path reservado no se asigna en paralelo.
- Cada tarea registra commit base antes de empezar.
- `Lista para integrar` exige handoff y verificaciones de rama.
- `Verificada` exige merge y compuerta ejecutada sobre el estado integrado.
