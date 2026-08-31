# Salvo — Seguimiento de implementación

> Estado del documento: activo
> Última actualización: 2026-08-31
> Fuente de alcance: [[Salvo-Blueprint]]
> Regla: actualizar este archivo al comenzar y cerrar cada etapa

## Estado general

| Campo | Valor |
| --- | --- |
| Estado del proyecto | Etapa 1 — Fundaciones reproducibles completada |
| Etapa completada | Etapa 1 — Fundaciones reproducibles |
| Próxima etapa | Etapa 2 — Contrato y datos sintéticos |
| Estado de la próxima etapa | Schema aprobado; inicio pendiente de autorización |
| Bloqueo actual | Ninguno |
| Dependencias externas | Ninguna para el núcleo local |
| Anthropic | Previsto para después del núcleo |
| Koin sandbox | Post-MVP; sujeto a onboarding y credenciales |
| Coordinación Codex–Claude | Brief E2 documentado como propuesta; sin trabajo activo |

## Leyenda

| Estado | Significado |
| --- | --- |
| `Pendiente` | Todavía no comenzó |
| `En curso` | Hay trabajo activo dentro del alcance acordado |
| `Bloqueada` | No puede continuar sin una decisión o dependencia externa |
| `En verificación` | Implementación terminada; falta pasar la compuerta |
| `Completada` | Criterios de salida y evidencias registrados |

Solo puede existir una etapa `En curso` a la vez.

## Tablero maestro

| Etapa | Objetivo | Estado | Compuerta principal | Evidencia |
| --- | --- | --- | --- | --- |
| 0 | Documentación e instrucciones | Completada | Docs coherentes y sin código | `AGENTS.md` + `DesignAgent/*.md` |
| 1 | Fundaciones reproducibles | Completada | Builds, tests y arranque de ASP.NET Core + Next.js | Merge `897cec7` + compuerta verde |
| 2 | Contrato y datos sintéticos | Pendiente | Seed idempotente + parser validado | Schema y brief aprobados; implementación pendiente |
| 3 | Motor determinista | Pendiente | Tests por regla + métricas sin fuga | Pendiente |
| 4 | Alertas y casos de uso | Pendiente | Idempotencia + consistencia transaccional | Pendiente |
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

El schema de Etapa 2 está aprobado; la implementación permanece pendiente de autorización y no se
inicia automáticamente.

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

### Etapa 2 — Contrato y datos

- [x] Aprobar schema detallado.
- [ ] Crear migración EF Core.
- [ ] Crear fixtures y seed sintéticos.
- [ ] Verificar idempotencia del seed.
- [ ] Implementar parser CSV/JSON y errores parciales.

### Etapa 3 — Motor determinista

- [ ] Baseline estrictamente temporal.
- [ ] Reglas puras y configuración central.
- [ ] Scoring determinista.
- [ ] Tests positivos, negativos y cold start.
- [ ] Evaluación temprana y calibración.

### Etapa 4 — Alertas

- [ ] Persistir evaluaciones locales.
- [ ] Crear alertas idempotentes.
- [ ] Revisar alerta y pedido en una transacción DB.
- [ ] Proteger estados ya revisados durante re-scoring.

### Etapa 5 — UI

- [ ] Importación y errores por fila.
- [ ] Feed de alertas.
- [ ] Detalle y revisión.
- [ ] Dashboard.
- [ ] Estados vacíos, carga, error y accesibilidad.

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
5. señalar la próxima etapa sin iniciarla automáticamente.

### Excepción para trabajo paralelo

Cuando Codex y Claude trabajan en ramas/worktrees distintos:

1. el coordinador actualiza Workboard y Progress antes de despachar;
2. cada agente modifica solo sus paths y escribe en su handoff;
3. los agentes no cambian el estado canónico desde sus ramas;
4. el coordinador integra en el orden indicado por dependencias;
5. Progress se actualiza únicamente después del merge y la compuerta conjunta.

Ver `Coordination/README.md` y `Coordination/Workboard.md`.
