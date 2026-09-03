# Salvo — Workboard Codex–Claude

> Estado: Etapa 5 en curso. `E5C-IMPORT-DASHBOARD` asignada; es el último ítem de la etapa
> Última actualización: 2026-09-02
> Responsable: coordinador de la etapa

## Estados

`Propuesta` → `Asignada` → `En curso` → `Lista para integrar` → `Integrada` → `Verificada`

Estados alternativos: `Bloqueada`, `Cancelada`.

Una tarea no cambia a `Integrada` o `Verificada` por decisión del agente que la implementa.

## Trabajo activo

| Work ID | Etapa | Objetivo | Propietario | Estado | Rama | Base | Paths reservados | Dependencias | Actualizado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| E5C-IMPORT-DASHBOARD | 5 | Importación con corrida, dashboard, gráfico y smoke de recorrido | Claude | Asignada | `claude/e5c-import-dashboard` | commit de `main` que incorpora el brief | `frontend/**`, `scripts/**`, `backend/tests/**` | `E5A` y `E5B` integradas | 2026-09-03 |

Modelo y esfuerzo acordados para `E5C-IMPORT-DASHBOARD`: Opus 5 · `high`. El coordinador
recomendó `Sonnet 5 · high`; el usuario optó por no arriesgar en el ítem que cierra la etapa.

## Cola próxima

No hay tareas en cola. La Etapa 6 no se inicia sin aprobación explícita del usuario.

Candidatas registradas para la Etapa 8, acordadas con el usuario:

- **Señales estructuradas e internacionalización.** El motor emite campos tipados en vez de prosa
  (sube a `e3-v2` e invalida los fingerprints a propósito); la UI compone el texto y el portugués
  pasa a ser un diccionario más. Hoy los detalles de las señales están en inglés dentro del
  fingerprint y traducir solo la cáscara sería cosmético.
- **Enriquecer la fixture con casos duros.** El corpus demo tiene un solo arquetipo de fraude
  —monto atípico desde país extranjero—: las 18 alertas llevan `amount_anomaly` y `foreign_country`,
  y tres de las seis reglas nunca abren una alerta. Además `score ≥ 60 ⇔ isFraudLabel`, por lo que
  F1 vale 1,00 y las métricas prueban el pipeline, no el criterio.
- Pasada de accesibilidad con lector de pantalla real.
- Decidir si `scripts/smoke-ui.sh` se integra a la compuerta.

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
| E4B-ALERTAS | 4 | Alertas con escalada, revisión transaccional y control de concurrencia | Claude | Verificada | Merge `c35878b`, 109 tests .NET, compuerta verde sobre `main` y 18 alertas (13 `MEDIUM`, 5 `CRITICAL`) verificadas en `salvo.db` |
| E5A-API-LECTURA | 5 | Superficie de lectura: dashboard, métricas, capacidades y orden del feed | Claude | Verificada | Merge `5f48db0`, 127 tests .NET, compuerta verde sobre `main`; falsación del test diferencial documentada y agregados verificados contra `salvo.db` |
| E5B-ALERTAS-UI | 5 | Cliente tipado, feed de alertas, detalle y revisión | Claude | Verificada | Merge `278e100`, 97 tests de frontend y 127 .NET, compuerta verde sobre `main`; build con la API apagada y recorrido manual de `/alerts` con datos reales |

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
