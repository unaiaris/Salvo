# Salvo — Workboard Codex–Claude

> Estado: Etapa 7 completa. Siete etapas integradas y verificadas; Etapa 8 sin iniciar
> Última actualización: 2026-09-05
> Responsable: coordinador de la etapa

## Estados

`Propuesta` → `Asignada` → `En curso` → `Lista para integrar` → `Integrada` → `Verificada`

Estados alternativos: `Bloqueada`, `Cancelada`.

Una tarea no cambia a `Integrada` o `Verificada` por decisión del agente que la implementa.

## Trabajo activo

No hay tareas activas ni paths reservados.

## Cola próxima

| Work ID | Estado | Modelo y esfuerzo | Depende de |
| --- | --- | --- | --- |
| `E7A-EXPLICACIONES` | `Verificada` (merge `82f2487`) | Opus 5 · `high` | — |
| `E7B-EXPLICACIONES-UI` | `Verificada` (merge `ac11015`) | Opus 5 · `high` | `E7A` |
| `E7C-PULIDO-EXPLICACION` | `Verificada` (merge `0d117dd`) | Opus 5 · `high` | `E7B` |

Dos lecciones de la Etapa 7 para los briefs que vengan:

- **Si una tarea de backend cambia el contrato, cambia también la guarda que lo consume.** `E7A`
  tuvo que salirse de su reserva de paths porque su punto 8 obligaba a que `AlertDetail` ganara el
  sub-objeto y las guardas proyectan hacia el tipo generado. Un contrato contra el que nadie puede
  compilar no es un contrato entregado.
- **No todo código de error va a `messages.ts`.** Ese catálogo es exactamente lo que los endpoints
  emiten como problema, y su test lo afirma. Un código que es el valor de un campo dentro de un
  `200` se rotula en `format.ts`, como `externalErrorLabel`.
- **Un ejemplo concreto en un brief se saca del archivo o de la base en ese momento**, nunca de una
  captura ni de memoria. Dos `brief-check` seguidos encontraron lo mismo: rutas mal citadas en el de
  `E7B`, y en el de `E7C` un pedido ilustrativo que el propio brief prohibía usar en el test, del
  que salió además un criterio de aceptación al revés. El brief se leía coherente; le faltaba
  anclaje.
- **Un arreglo que no alcanza a lo ya guardado no está terminado.** Con identidad por contenido,
  corregir al productor no corrige lo producido: hay que preguntarse siempre qué pasa con las filas
  que ya existen (decisión 58).

Notas para los briefs de la Etapa 8:

- **Copia de `salvo.db` antes de migrar.** EF ejecuta `PRAGMA foreign_keys = 0` fuera de transacción.
- Artefactos que la etapa obliga a tocar: `AlertSchemaTests` (se extiende),
  `ArchitectureSmokeTests` (suma `AlertExplanation`), `OpenApiDriftTests` con recaptura,
  `frontend/openapi/salvo-openapi.json` y `schema.d.ts`, `fixtures.ts` con `explanation: null`
  —sin eso todo el detalle cae en `malformed`—, `messages.ts` y su test, `boundary.test.ts` y los
  textos de `scripts/smoke-ui.sh`.
- Verificar el estado canónico antes de declararlo pendiente.
- Reservar `backend/tests/**` en `E7A`, y `frontend/**` y `scripts/**` en `E7B`.
- `E7A` reserva `salvo-openapi.json` y `schema.d.ts`: `E7B` no recaptura el contrato.

Notas conservadas de la Etapa 6, por si vuelven a aplicar:

- `RiskEvaluationIdentityTests` perdió dos aserciones al separarse la evaluación externa.

Candidatas registradas para la Etapa 8, acordadas con el usuario:

- **Señales estructuradas e internacionalización.** El motor emite campos tipados en vez de prosa
  (sube a `e3-v2` e invalida los fingerprints a propósito); la UI compone el texto y el portugués
  pasa a ser un diccionario más. Hoy los detalles de las señales están en inglés dentro del
  fingerprint y traducir solo la cáscara sería cosmético. **La Etapa 7 deja la semilla**:
  `SignalFacts` en Domain convierte cada `detail` en campos tipados; cuando el motor los emita,
  el extractor se borra y la plantilla y los hechos de grounding quedan intactos.
- **Enriquecer la fixture con casos duros.** El corpus demo tiene un solo arquetipo de fraude
  —monto atípico desde país extranjero—: las 18 alertas llevan `amount_anomaly` y `foreign_country`,
  y tres de las seis reglas nunca abren una alerta. Además `score ≥ 60 ⇔ isFraudLabel`, por lo que
  F1 vale 1,00 y las métricas prueban el pipeline, no el criterio.
  Verificado el 2026-09-04 importando escenarios a mano: `velocity`, `cross_border_velocity` y
  `unusual_hour` sí funcionan, y la banda `ALTA` también, pero el corpus no las alcanza. En
  particular `unusual_hour` es **estructuralmente inalcanzable** con esta fixture: exige una franja
  de seis horas con ≤10% de los pedidos del comercio en treinta días, y la franja más rara de los
  tres comercios está en 13,8%. Una fixture enriquecida debería cubrir las seis reglas y las tres
  bandas.
- Pasada de accesibilidad con lector de pantalla real.
- Traducir los códigos de error de fila que hoy caen al inglés (`describeRecordError`).

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
| E5C-IMPORT-DASHBOARD | 5 | Importación con corrida, dashboard con SVG de servidor, test de deriva de OpenAPI y smoke de recorrido | Claude | Verificada | 129 tests .NET y 153 de frontend; `check.sh` y `smoke-ui.sh` verdes sobre `main`, 21 comprobaciones y 0 fallas; smoke falsado de tres maneras y test de deriva falsado dos veces |
| E6A-PROVEEDOR | 6 | Entidad externa, puerto, mock determinista, reserva en dos fases, degradación y reconciliación | Claude | Verificada | Merge `bca2c46`, 160 tests .NET, compuerta y smoke verdes sobre `main`; falsación del test de concurrencia y del diferencial documentadas; fingerprints recalculados e idénticos |
| E6B-CALLBACK-UI | 6 | Recibos, callback autenticado, vinculación tardía, disparador de demo y superficie en la consola | Claude | Verificada | Merge `a412693`, 196 tests .NET y 172 de frontend, compuerta y smoke verdes sobre `main` con 29 comprobaciones; falsación del callback previo al commit y del test de frontera documentadas |

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
