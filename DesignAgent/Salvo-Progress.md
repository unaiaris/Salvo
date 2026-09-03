# Salvo — Seguimiento de implementación

> Estado del documento: activo
> Última actualización: 2026-09-02
> Fuente de alcance: [[Salvo-Blueprint]]
> Regla: actualizar este archivo al comenzar y cerrar cada etapa

## Estado general

| Campo | Valor |
| --- | --- |
| Estado del proyecto | Etapa 5 — UI y dashboard, en curso; `E5A` integrada |
| Etapa completada | Etapa 4 — Alertas y casos de uso |
| Próxima etapa | Etapa 5 — UI y dashboard |
| Estado de la próxima etapa | Aprobada; se ejecuta en `E5A-API-LECTURA`, `E5B-ALERTAS-UI` y `E5C-IMPORT-DASHBOARD` |
| Bloqueo actual | Ninguno |
| Dependencias externas | Ninguna para el núcleo local |
| Anthropic | Previsto para después del núcleo |
| Koin sandbox | Post-MVP; sujeto a onboarding y credenciales |
| Coordinación Codex–Claude | Sin tareas activas ni paths reservados |

## Leyenda

| Estado | Significado |
| --- | --- |
| `Pendiente` | Todavía no comenzó |
| `En curso` | Hay trabajo activo dentro del alcance acordado |
| `Bloqueada` | No puede continuar sin una decisión o dependencia externa |
| `En verificación` | Implementación terminada; falta integración o cierre de la compuerta canónica |
| `Completada` | Criterios de salida y evidencias registrados |

Solo puede existir una etapa `En curso` a la vez.

## Tablero maestro

| Etapa | Objetivo | Estado | Compuerta principal | Evidencia |
| --- | --- | --- | --- | --- |
| 0 | Documentación e instrucciones | Completada | Docs coherentes y sin código | `AGENTS.md` + `DesignAgent/*.md` |
| 1 | Fundaciones reproducibles | Completada | Builds, tests y arranque de ASP.NET Core + Next.js | Merge `897cec7` + compuerta verde |
| 2 | Contrato y datos sintéticos | Completada | Seed idempotente + parser validado | Merge `4b7bf54` + 24 tests .NET + compuerta verde en `main` |
| 3 | Motor determinista | Completada | Tests por regla + métricas sin fuga | Merge `809ff75` + 42 tests .NET + compuerta verde en `main` |
| 4 | Alertas y casos de uso | Completada | Idempotencia + consistencia transaccional | Merges `1d9ee83` y `c35878b`; 109 tests .NET; compuerta verde en `main`; corpus demo con 18 alertas (13 `MEDIUM`, 5 `CRITICAL`) verificado contra la base |
| 5 | UI y dashboard | Pendiente | Recorrido completo y estados vacíos/error | Pendiente |
| 6 | Proveedor antifraude mock | Pendiente | Estados y callbacks replay-safe | Pendiente |
| 7 | Explicabilidad | Pendiente | Funciona sin red; Anthropic opcional | Pendiente |
| 8 | Calidad y portfolio | Pendiente | Instalación limpia + demo reproducible | Pendiente |
| Post-MVP | Koin sandbox, auth, observabilidad, deploy | Pendiente | Aprobación independiente por capacidad | Pendiente |

## Etapa 1 — Resultado verificado

### Objetivo

Crear una base ejecutable y reproducible, sin lógica de negocio.

### Alcance previsto

- Instalar y fijar un SDK exacto de .NET 10 mediante `global.json`.
- Crear la solución y proyectos `Salvo.Domain`, `Salvo.Application`, `Salvo.Infrastructure`,
  `Salvo.Api` y sus proyectos de tests.
- Configurar nullable, analyzers, warnings como errores, xUnit y tests de integración.
- Configurar ASP.NET Core, OpenAPI, EF Core/SQLite y verificar una operación mínima.
- Crear `.nvmrc` con Node.js 24.20.0 y fijar dependencias npm exactas.
- Crear el frontend Next.js sin mover ni borrar `DesignAgent/`.
- Configurar TypeScript estricto, ESLint directo, Vitest y un smoke test de UI.
- Definir el proxy/cliente tipado para que el frontend consuma la API sin contener negocio.
- Crear `.env.example` sin secretos.
- Definir comandos reproducibles para build, test, lint, arranque y compuerta global.

La fundación se ejecutará primero de forma secuencial. Solución, paquetes, lockfile, EF Core,
OpenAPI y configuración central son puntos de alta colisión; el paralelismo se habilitará después
de que estos contratos estén integrados.

### Archivos esperados

La lista exacta se confirmará al iniciar la etapa. Como mínimo:

- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `Salvo.slnx`
- `backend/src/Salvo.Domain/`
- `backend/src/Salvo.Application/`
- `backend/src/Salvo.Infrastructure/`
- `backend/src/Salvo.Api/`
- `backend/tests/`
- `.nvmrc`
- `.gitignore`
- `.env.example`
- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/tsconfig.json`
- configuración de Next.js, ESLint y Vitest
- schema EF Core mínimo sin entidades definitivas de negocio
- smoke tests backend y frontend

### Fuera de alcance de esta etapa

- Modelos de dominio definitivos.
- Seed de pedidos.
- Reglas de fraude.
- UI funcional.
- Anthropic o llamadas a Koin.

### Criterios de salida

- [x] SDK .NET y Node fijados y documentados.
- [x] Dependencias NuGet y npm con versiones exactas instaladas.
- [x] API y frontend inician sin errores.
- [x] Nullable, analyzers y warnings como errores pasan.
- [x] `dotnet build` pasa.
- [x] `dotnet test` ejecuta tests unitarios y de integración mínimos.
- [x] OpenAPI se genera sin errores.
- [x] EF Core/SQLite realiza una operación mínima verificada.
- [x] TypeScript estricto pasa.
- [x] ESLint pasa mediante CLI directa.
- [x] Vitest ejecuta al menos un smoke test.
- [x] `npm run check` pasa.
- [x] Builds de producción backend y frontend pasan.
- [x] No hay secretos ni archivos DB versionados.
- [x] Riesgos restantes registrados.

### Evidencia integrada

- `./scripts/check.sh`: restore NuGet bloqueado, build Release con 0 warnings/0 errores, 3 tests
  .NET, typecheck, ESLint, 3 tests Vitest y build Next.js 16.3.3 con Webpack, todo verde.
- Smoke API: `GET /health` respondió `200` con el contrato esperado.
- Smoke OpenAPI: `GET /openapi/v1.json` respondió `200`, OpenAPI 3.1.1 y operación `GetHealth`.
- Smoke frontend: `/` respondió `200` y `/api/health` atravesó el rewrite hacia la API.
- Auditoría Git: sin secretos, `.env` ni bases SQLite versionadas; artefactos generados permanecen
  ignorados.
- PR #1 integrado en `main` mediante `897cec7`; `./scripts/check.sh` repetido sobre ese estado con
  todos los pasos verdes.

### Riesgos restantes de Etapa 1

- El build Turbopack no puede validarse bajo la restricción de puertos del entorno; la compuerta usa
  `next build --webpack`, opción soportada por Next.js 16.3.3 y verificada en producción local.

La Etapa 4 quedó integrada y verificada sobre `main` en dos ítems, `E4A-PERSISTENCIA` y
`E4B-ALERTAS`. La verificación se hizo además contra la base real: tres corridas de scoring sobre el
corpus demo, la segunda y la tercera reusando las 300 evaluaciones, y 18 alertas abiertas por la
tercera. Etapa 5 permanece pendiente: requiere diseño, brief y autorización independientes.

## Checklists por etapa

### Etapa 0 — Documentación e instrucciones

- [x] Revisar todos los Markdown originales.
- [x] Aprobar el pivot a consola antifraude B2B.
- [x] Separar score local, evaluación externa y alerta.
- [x] Definir Koin mock en MVP y sandbox post-MVP.
- [x] Registrar Anthropic como proveedor posterior.
- [x] Crear `AGENTS.md`.
- [x] Sincronizar documentos derivados.
- [x] Definir ASP.NET Core para backend y Next.js para presentación.
- [x] Definir EF Core/SQLite y OpenAPI como fronteras de infraestructura/contrato.
- [x] Centralizar autonomía, autorizaciones y task brief compartido para Codex y Claude.
- [x] Verificar que no exista código de aplicación versionado.
- [x] Verificar la carga real de los imports de `CLAUDE.md` con Claude Code instalado.
- [x] Versionar el tooling de Claude Code: comandos, permisos y política de `.claude/`.
- [x] Documentar el criterio de modelo y esfuerzo, y la configuración del Proyecto de Claude.ai.

### Etapa 2 — Contrato y datos

- [x] Aprobar schema detallado.
- [x] Crear migración EF Core.
- [x] Crear fixtures y seed sintéticos.
- [x] Verificar idempotencia del seed.
- [x] Implementar parser CSV/JSON y errores parciales.
- [x] Verificar límites de 5 MiB, 10.000 registros y 1.000 errores detallados.
- [x] Verificar claves, checks, índices, UTC canónico y transacción atómica en SQLite.
- [x] Auditar contratos públicos y fixture para impedir PII y fuga de `isFraudLabel`.

### Etapa 3 — Motor determinista

- [x] Baseline estrictamente temporal.
- [x] Reglas puras y configuración central.
- [x] Scoring determinista.
- [x] Tests positivos, negativos y cold start.
- [x] Evaluación temprana y calibración.

### Etapa 4 — Alertas

- [x] Persistir evaluaciones locales.
- [x] Crear alertas idempotentes.
- [x] Revisar alerta y escribir su auditoría en una transacción DB.
- [x] Proteger estados ya revisados durante re-scoring.

### Etapa 5 — UI y dashboard

- [x] Endpoints de lectura: dashboard, métricas y capacidades.
- [x] Orden del feed en la API, con el `JOIN` antes de paginar.
- [x] Test diferencial: invertir etiquetas no cambia el dashboard.
- [x] Cliente `server-only` con tipos generados desde OpenAPI y guardas que proyectan.
- [ ] Importación, errores por fila y ejecución de la corrida de scoring.
- [x] Feed de alertas.
- [x] Detalle, divergencia y revisión.
- [ ] Dashboard con monto por moneda y gráfico SVG de servidor.
- [x] Rutas dinámicas: el build pasa sin API levantada.
- [ ] Estados vacíos —los tres—, carga, error y accesibilidad.
- [ ] `scripts/smoke-ui.sh` con los tres escenarios.

### Etapa 6 — Proveedor externo mock

- [ ] Implementar `IAntifraudProvider`.
- [ ] Escenarios `approved`, `denied` y `received`.
- [ ] Correlación por referencias.
- [ ] Callback simulado replay-safe.
- [ ] Reconciliación de pendientes.
- [ ] Errores y timeouts simulados.

### Etapa 7 — Explicabilidad

- [ ] Proveedor determinista por defecto.
- [ ] Salida estructurada y grounded.
- [ ] Decidir si se activa Anthropic.
- [ ] Si se aprueba: cliente server-side, mocks y control de costo.
- [ ] Verificar que la app funcione sin API key.

### Etapa 8 — Calidad y portfolio

- [ ] Métricas finales y umbral documentado.
- [ ] README de portfolio completo.
- [ ] Diagramas y capturas.
- [ ] Guion de demo.
- [ ] Revisión de seguridad y accesibilidad.
- [ ] Instalación limpia y compuertas .NET/npm verdes.

## Decisiones y dependencias abiertas

| Tema | Estado | Momento de decisión | Nota |
| --- | --- | --- | --- |
| SDK .NET y versiones exactas de dependencias | Resuelta | Etapa 1 | .NET 10.0.400 y dependencias directas fijadas; locks verificados |
| Schema y contratos E2 | Resuelta | Preparación de Etapa 2 | Decisiones 15–20 del Blueprint + brief `E2-CONTRACT-DATA` |
| Baseline, reglas y calibración E3 | Resuelta | Preparación de Etapa 3 | Decisiones 21–27 del Blueprint + brief `E3-MOTOR-DETERMINISTA` |
| Anthropic real | Diferida | Etapa 7 | Proveedor preferido; el núcleo no depende de él |
| Acceso Koin sandbox | Diferida | Post-MVP | Requiere onboarding, private key y `org_id` |
| Auth/multi-tenant | Diferida | Post-MVP | Necesaria antes de publicación mutable |
| Deploy/Postgres | Diferida | Post-MVP | No condiciona la demo local |
| Primer trabajo paralelo Codex–Claude | Diferida | Después del scaffold | Requiere paths no solapados y task briefs |

## Registro de actividad

| Fecha | Etapa | Cambio | Verificación | Resultado |
| --- | --- | --- | --- | --- |
| 2026-08-30 | 0 | Revisión completa de documentación original | Auditoría de 7 Markdown y entorno | Completada |
| 2026-08-30 | 0 | Pivot B2C → consola antifraude B2B | Aprobación del usuario | Completada |
| 2026-08-30 | 0 | Blueprint, docs derivados, `AGENTS.md` y README | Formato, coherencia y ausencia de código | Completada |
| 2026-08-30 | 0 | Tracker y preparación de entrevista Koin | Revisión editorial de todos los Markdown | Completada |
| 2026-08-30 | 0 | Kit Claude y coordinación multiagente | Imports, enlaces y protocolo revisados | Completada |
| 2026-08-31 | 0 | Arquitectura ASP.NET Core + Next.js y compuertas full-stack | Revisión cruzada de Blueprint, instrucciones y tracker | Completada |
| 2026-08-31 | 0 | Instrucciones Codex/Claude y task brief compartido | Terminología, referencias, límites de autonomía y diff validados | Completada |
| 2026-08-31 | 1 | Inicio de fundaciones reproducibles | Aprobación del usuario, brief y rama aislada | En curso |
| 2026-08-31 | 1 | Scaffold .NET 10 + Next.js 16, SQLite, OpenAPI y tests | Build Release, 3 tests .NET, check frontend y build Webpack | Lista para integrar |
| 2026-08-31 | 1 | Compuerta raíz y smokes full-stack | `./scripts/check.sh`; health, OpenAPI, frontend y proxy HTTP 200 | Verde en rama |
| 2026-08-31 | 1 | PR #1 integrado y verificación conjunta sobre `main` | Merge `897cec7` + `./scripts/check.sh` | Completada |
| 2026-08-31 | Preparación E2 | Schema, contratos, seed, migración y criterios diseñados paso a paso | Aprobación del usuario + brief `E2-CONTRACT-DATA` | Aprobada; no iniciada |
| 2026-08-31 | 2 | Inicio de E2-CONTRACT-DATA | Autorización del usuario + rama `codex/e2-contract-data` desde `193f7aa` | En curso |
| 2026-08-31 | 2 | Contrato, importadores, migración y seed implementados | Commit `8b79010`; 16 tests de integración + 8 de dominio | Lista para integrar |
| 2026-08-31 | 2 | Compuerta full-stack de rama | Restore locked, tooling EF, modelo sin cambios, 24 tests .NET, 3 Vitest y build Next.js | Verde en rama |
| 2026-08-31 | 2 | Integración y verificación canónica | Merge `4b7bf54` + `./scripts/check.sh` sobre `main` | Completada |
| 2026-08-31 | Preparación E3 | Diagnóstico, baseline, reglas, pesos, métricas y brief detallados | Aprobación del usuario | Aprobada |
| 2026-08-31 | 3 | Inicio de E3-MOTOR-DETERMINISTA | Rama `codex/e3-motor-determinista` desde `4053f8f` | En curso |
| 2026-08-31 | 3 | Motor temporal, reglas y evaluación implementados | Commit `c3a765c`; 24 tests de dominio + 18 de integración | Lista para integrar |
| 2026-08-31 | 3 | Compuerta full-stack de rama | Restore locked, EF sin cambios, 42 tests .NET, 3 Vitest y build Next.js | Verde en rama |
| 2026-08-31 | 3 | Integración y verificación canónica | Merge `809ff75` + `./scripts/check.sh` sobre `main` | Completada |
| 2026-09-01 | 0 | Entorno Claude Code verificado | `/memory`: `CLAUDE.md` y sus cinco imports cargados; cierra el riesgo abierto en `E0-DOC-04` | Completada |
| 2026-09-01 | 0 | Inicio de E0-DOC-06 | Brief `Coordination/Tasks/E0-DOC-06.md` y rama `claude/e0-doc-06-tooling` desde `dbce667` | En curso |
| 2026-09-01 | 0 | Tooling implementado y corregido tras revisión del coordinador | Commits `15b2b2a`, `205bdd0` y `bffb018`; `/gate` y `/brief-check` ejecutados en sesión nueva | Lista para integrar |
| 2026-09-01 | 0 | Integración y verificación canónica | Merge `50eb2de` + `./scripts/check.sh` sobre `main` | Completada |
| 2026-09-02 | 0 | Inicio de E0-DOC-07 | Brief `Coordination/Tasks/E0-DOC-07.md` y rama `claude/e0-doc-07-project-and-models` desde `aa002e3` | En curso |
| 2026-09-02 | 0 | Política de modelos y instrucciones del Proyecto conectadas al repositorio | Commits `6afa4c4` y `45381a0`; tabla de modelos validada contra documentación oficial; `/brief-check`, `/gate` y `/handoff` ejecutados | Lista para integrar |
| 2026-09-02 | 0 | Integración y verificación canónica | Merge `a3f8ac4` + `./scripts/check.sh` sobre `main` | Completada |
| 2026-09-02 | Preparación E4 | Diseño de alertas, evaluaciones y revisión transaccional | Aprobación del usuario | Aprobada |
| 2026-09-02 | Preparación E4 | Revisión adversarial del diseño con Fable 5.1 | 10 hallazgos, 4 de severidad alta, verificados contra código real | Completada |
| 2026-09-02 | Preparación E4 | Diseño v2 con los diez hallazgos incorporados y partición en E4A/E4B | Aprobación del usuario + decisiones 28–36 en la bitácora | Aprobada; no iniciada |
| 2026-09-02 | 4 | Inicio de E4A-PERSISTENCIA | Brief y rama `claude/e4a-persistencia` desde `1215a2d` | En curso |
| 2026-09-02 | 4 | Evaluaciones, corridas, fingerprint canónico y endpoints implementados | Commits `73c1b17` y `32bfffc`; 71 tests .NET; rebote 0→40→0 cubierto | Lista para integrar |
| 2026-09-02 | 4 | Integración y verificación canónica de E4A | Merge `1d9ee83` + `./scripts/check.sh` sobre `main` | Completada |
| 2026-09-02 | 4 | Inicio de E4B-ALERTAS | Brief y rama `claude/e4b-alertas` desde `05ddb4a`; Opus 5 · high acordado | En curso |
| 2026-09-02 | 4 | Alertas con escalada, revisión transaccional y token de concurrencia | Commits `c6e945b` y `50a9bc3`; 109 tests .NET; carrera de revisión sobre base en archivo | Lista para integrar |
| 2026-09-02 | 4 | Integración y verificación canónica de E4B | Merge `c35878b` + `./scripts/check.sh` sobre `main` + 18 alertas (13 `MEDIUM`, 5 `CRITICAL`, 0 `HIGH`) verificadas en `salvo.db` | Completada |
| 2026-09-02 | 0 | Preparación de entrevista fuera del control de versiones y política de modelo ampliada | `Salvo-Interview-Prep.md` movido a `_local/` con la sección de valor frente a Koin plegada; referencias vivas limpiadas; `Claude-Model-Policy.md` documenta el cambio de tipo de tarea a mitad de sesión; el protocolo de cierre incluye el *Sync* del Proyecto | Completada |
| 2026-09-03 | Preparación E5 | Diseño v1 de la Etapa 5 | Tres hallazgos propios: dashboard sin endpoint, `amountAtRisk` multimoneda, orden del feed | Superado por la v2 |
| 2026-09-03 | Preparación E5 | Revisión adversarial del diseño con Fable 5.1 · `high` | 19 hallazgos, 5 de severidad alta, verificados contra código y contra la base demo; refutó el argumento de inferencia de la v1 | Completada |
| 2026-09-03 | Preparación E5 | Diseño v2 con los diecinueve hallazgos incorporados y partición en E5A/E5B/E5C | Aprobación del usuario + decisiones 37–43 en la bitácora; §4.4 corregido y Recharts fuera del stack | Aprobada |
| 2026-09-03 | 5 | Inicio de E5A-API-LECTURA | Brief y rama `claude/e5a-api-lectura` desde `3081bb3`; Opus 5 · high acordado | En curso |
| 2026-09-03 | 5 | Superficie de lectura: dashboard, métricas, capacidades y orden del feed | Commits `513a980` y `9a78456`; 127 tests .NET; falsación del test diferencial documentada | Lista para integrar |
| 2026-09-03 | 5 | Integración y verificación canónica de E5A | Merge `5f48db0` + `./scripts/check.sh` sobre `main` + agregados del dashboard verificados contra `salvo.db` | Completada |
| 2026-09-03 | 5 | Inicio de E5B-ALERTAS-UI | Brief y rama `claude/e5b-alertas-ui` desde `35e6b14`; Opus 5 · high acordado | En curso |
| 2026-09-03 | 5 | Cliente tipado, feed, detalle y revisión | Commits `6d087f2`, `1774277` y `a147a8a`; 97 tests de frontend; build con la API apagada y falsación en dos pasos del test de frontera | Lista para integrar |
| 2026-09-03 | 5 | Integración y verificación canónica de E5B | Merge `278e100` + `./scripts/check.sh` sobre `main` + recorrido manual de `/alerts` con datos reales | Completada |

## Protocolo de actualización

Al comenzar una etapa:

1. cambiar su estado a `En curso`;
2. completar alcance y archivos afectados;
3. registrar decisiones o bloqueos conocidos.

Al terminar:

1. cambiar a `En verificación`;
2. ejecutar la compuerta completa;
3. registrar comandos, resultado y riesgos restantes;
4. marcar `Completada` solo si todos los criterios obligatorios pasan;
5. pulsar *Sync* en la integración de GitHub del Proyecto de Claude.ai, para que el conocimiento del
   Proyecto deje de ir por detrás de `main`. La sincronización no es automática al hacer push;
6. señalar la próxima etapa sin iniciarla automáticamente.

### Excepción para trabajo paralelo

Cuando Codex y Claude trabajan en ramas/worktrees distintos:

1. el coordinador actualiza Workboard y Progress antes de despachar;
2. cada agente modifica solo sus paths y escribe en su handoff;
3. los agentes no cambian el estado canónico desde sus ramas;
4. el coordinador integra en el orden indicado por dependencias;
5. Progress se actualiza únicamente después del merge y la compuerta conjunta.

Ver `Coordination/README.md` y `Coordination/Workboard.md`.
