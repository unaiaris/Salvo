# Salvo — Handoffs de Codex

Registro de entregas producidas por Codex. El estado canónico sigue en el Workboard y Progress.

## E0-DOC-04 — Kit Claude y coordinación multiagente

### Identificación

- Estado: integrado
- Etapa: 0
- Agente: Codex
- Fecha: 2026-08-30

### Resultado

- `CLAUDE.md` raíz con imports de contexto compartido.
- Carpeta `ClaudeAgent/` con workflow, task brief y handoff.
- Carpeta `Coordination/` con protocolo, Workboard y logs separados.
- Política de fuente única y reglas para ramas/worktrees paralelos.

### Verificación

- Estructura Markdown y enlaces internos.
- Ausencia de secretos.
- Ningún código de aplicación ni dependencia añadidos.

### Riesgos o pendientes

- Claude Code no está instalado actualmente en este entorno.
- La Etapa 1 no debe paralelizarse hasta fijar scaffold y configuración central.

## E0-DOC-05 — Instrucciones y task brief compartido

### Identificación

- Estado: integrado
- Etapa: 0
- Agente: Codex
- Fecha: 2026-08-31

### Resultado

- `AGENTS.md` centraliza autonomía, aprobaciones y reglas compartidas.
- `CLAUDE.md` queda como adaptador conciso sin duplicar el workflow.
- `Coordination/Task-Brief-Template.md` sirve a Codex y Claude con resultado, alcance,
  autorizaciones, aceptación y evidencia.
- Terminología y estados de entrega quedan alineados con el Workboard.

### Verificación

- Referencias internas y nombres de archivos revisados.
- Terminología de arquitectura y estados buscada en todos los Markdown.
- Formato del diff validado sin errores.

### Riesgos o pendientes

- La carga real de imports de `CLAUDE.md` requiere un smoke test cuando Claude Code esté instalado.

## E1-FOUNDATION — Fundaciones reproducibles

### Identificación

- Estado: verificada
- Etapa: 1
- Agente: Codex
- Fecha: 2026-08-31
- Rama: `codex/e1-foundation`
- Commit base: `ca94c4752073170939796f3174f58ccf4b190624`
- Commit final: `d5a35fb90a72f87b813c40d36f2ce6eb31d150ab`
- Merge en `main`: `897cec7`

### Resultado

- Solución .NET 10 modular con Domain, Application, Infrastructure, API y proyectos de tests.
- API mínima con health, OpenAPI y EF Core/SQLite comprobado en integración.
- Frontend Next.js App Router con TypeScript estricto, ESLint, Tailwind, Vitest y rewrite a la API.
- SDKs, dependencias directas y lockfiles fijados; `.env.example` no contiene secretos.
- Compuerta raíz `scripts/check.sh` para ambas toolchains.

### Verificación

- `dotnet restore Salvo.slnx --locked-mode`: pasa.
- `dotnet build Salvo.slnx --configuration Release --no-restore`: 0 warnings, 0 errores.
- `dotnet test Salvo.slnx --configuration Release --no-build --no-restore`: 3/3 tests pasan.
- `npm run check --prefix frontend`: typecheck, ESLint y 3/3 tests pasan.
- `npm run build --prefix frontend`: Next.js 16.3.3 Webpack pasa.
- `./scripts/check.sh`: compuerta completa verde.
- `./scripts/check.sh` repetido sobre `main` en `897cec7`: compuerta completa verde.
- Smokes HTTP: API health, OpenAPI 3.1.1, frontend y proxy API responden `200`.
- `git diff --check`: pasa; no hay `.env` ni DB SQLite versionadas.

### Riesgos o pendientes

- Turbopack no se pudo validar por la restricción de puertos del entorno; Webpack es la ruta de
  build de producción configurada y verificada.
- La Etapa 2 no comenzó y requiere petición o aprobación independiente.

## E2-CONTRACT-DATA — Contrato y datos sintéticos

### Identificación

- Estado: lista para integrar
- Etapa: 2
- Agente: Codex
- Fecha: 2026-08-31
- Rama: `codex/e2-contract-data`
- Commit base: `193f7aacfe822160bbd9dc6c52815d970a93df4e`
- Commit de implementación: `8b79010`
- Push/merge: no realizados

### Resultado

- `Order` protege invariantes, normalización e inmutabilidad; `OrderEvaluationLabel` permanece
  separado y no forma parte del contrato público.
- Importación CSV/JSON converge en un único caso de uso con errores parciales, límites, duplicados
  idempotentes y conflictos de referencias inmutables.
- EF Core crea `orders` y `order_evaluation_labels` mediante `InitialOrderContractData`, con claves,
  checks, índices y timestamps UTC ISO canónicos.
- El seed embebido fija 300 pedidos pseudónimos y 300 labels —18 positivos— con GUID y tiempos
  reproducibles; repetirlo no modifica el estado.
- La API expone `POST /api/order-imports` y mapea `POST /api/demo-data/seed` solo cuando
  `DemoData:Enabled` está activo.
- La ayuda memoria documenta el speech y el recorrido real por Domain, Application,
  Infrastructure, API, migración, fixture y tests.

### Archivos principales

- Dominio: `backend/src/Salvo.Domain/Orders/**` y `Evaluation/OrderEvaluationLabel.cs`.
- Aplicación: `backend/src/Salvo.Application/Orders/**`.
- Infraestructura: parsers, persistencia, migración, seed y fixture bajo
  `backend/src/Salvo.Infrastructure/**`.
- API: `Program.cs`, `OrderEndpoints.cs` y configuración `DemoData`.
- Tests: `OrderTests.cs`, `OrderImportEndpointTests.cs`, `OrderPersistenceTests.cs`,
  `DemoSeedTests.cs` y actualización de la factory/migración de integración.
- Tooling: `Directory.Packages.props`, `.config/dotnet-tools.json`, lockfiles y `scripts/check.sh`.

### Verificación

- `dotnet restore Salvo.slnx --locked-mode`: pasa.
- `dotnet tool restore`: restaura `dotnet-ef` 10.0.11.
- `dotnet build Salvo.slnx --configuration Release --no-restore`: 0 warnings, 0 errores.
- `dotnet ef migrations has-pending-model-changes ... --no-build`: ningún cambio pendiente.
- `dotnet test Salvo.slnx --configuration Release --no-build --no-restore`: 24/24 pasan
  —16 integración, 8 dominio—.
- `npm run check --prefix frontend`: 3/3 Vitest, typecheck y ESLint pasan.
- `npm run build --prefix frontend`: Next.js 16.3.3 Webpack pasa.
- `./scripts/check.sh`: compuerta full-stack verde.
- Auditoría de fixture y fuentes: sin campos/patrones de PII o pago; los errores no reflejan valores
  recibidos y OpenAPI no expone `isFraudLabel`.

### Riesgos o pendientes

- Falta push, revisión, merge y repetición de la compuerta sobre `main`; E2 aún no está integrada ni
  completada canónicamente.
- La API no migra ni carga seed al arrancar por diseño. Una DB local efímera de E1 con el antiguo
  checkpoint requiere que el desarrollador la aparte o elimine explícitamente antes de aplicar la
  primera migración; ningún comando de E2 borra datos automáticamente.
- Etapa 3, reglas, scoring, alertas, Anthropic y Koin no fueron iniciados.
