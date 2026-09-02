# Salvo — Workboard Codex–Claude

> Estado: E4A-PERSISTENCIA verificada; E4B-ALERTAS asignada y pendiente de ejecución
> Última actualización: 2026-09-02
> Responsable: coordinador de la etapa

## Estados

`Propuesta` → `Asignada` → `En curso` → `Lista para integrar` → `Integrada` → `Verificada`

Estados alternativos: `Bloqueada`, `Cancelada`.

Una tarea no cambia a `Integrada` o `Verificada` por decisión del agente que la implementa.

## Trabajo activo

| Work ID | Etapa | Objetivo | Propietario | Estado | Rama | Base | Paths reservados | Dependencias | Actualizado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| E4B-ALERTAS | 4 | Alertas con escalada, revisión transaccional y control de concurrencia | Claude | Asignada | `claude/e4b-alertas` | commit de `main` que incorpora el brief | `backend/src/Salvo.Domain/Alerts/**`, `backend/src/Salvo.Application/Alerts/**`, `backend/src/Salvo.Infrastructure/**`, `backend/src/Salvo.Api/**` | `E4A-PERSISTENCIA` integrada | 2026-09-02 |

Modelo y esfuerzo acordados para `E4B-ALERTAS`: Opus 5 · `high`.

## Cola próxima

No hay tareas en cola. La Etapa 5 no se inicia sin aprobación explícita del usuario.

## Historial integrado

| Work ID | Etapa | Resultado | Agente | Estado | Evidencia |
| --- | --- | --- | --- | --- | --- |
| E0-DOC-01 | 0 | Auditoría original y propuesta B2B | Codex | Verificada | Blueprint/bitácora |
| E0-DOC-02 | 0 | Documentación canónica y `AGENTS.md` | Codex | Verificada | `DesignAgent/*.md` |
| E0-DOC-03 | 0 | Tracker y entrevista Koin | Codex | Verificada | Progress + Interview Prep |
| E0-DOC-04 | 0 | Kit Claude y coordinación multiagente | Codex | Verificada | `CLAUDE.md`, `ClaudeAgent/`, `Coordination/` |
| E0-DOC-05 | 0 | Instrucciones concisas y task brief compartido | Codex | Verificada | `AGENTS.md`, `CLAUDE.md`, `Coordination/Task-Brief-Template.md` |
| E1-FOUNDATION | 1 | Fundaciones ASP.NET Core + Next.js y compuerta conjunta | Codex | Verificada | PR #1, merge `897cec7` y compuerta sobre `main` |
| E2-CONTRACT-DATA | 2 | Contrato, importación, migración y datos sintéticos idempotentes | Codex | Verificada | Merge `4b7bf54`, 24 tests .NET y compuerta sobre `main` |
| E3-MOTOR-DETERMINISTA | 3 | Baseline temporal, seis reglas, scoring y evaluación sin fuga | Codex | Verificada | Merge `809ff75`, 42 tests .NET y compuerta sobre `main` |
| E0-DOC-06 | 0 | Tooling de Claude Code: comandos `/gate`, `/handoff`, `/brief-check`, permisos y política de `.claude/` | Claude | Verificada | Merge `50eb2de`, compuerta full-stack verde sobre `main` |
| E0-DOC-07 | 0 | Política de modelo y esfuerzo, e instrucciones del Proyecto de Claude.ai | Claude | Verificada | Merge `a3f8ac4`, compuerta full-stack verde sobre `main` |
| E4A-PERSISTENCIA | 4 | Persistencia idempotente de evaluaciones locales y corridas de scoring | Claude | Verificada | Merge `1d9ee83`, 71 tests .NET y compuerta verde sobre `main` |

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
