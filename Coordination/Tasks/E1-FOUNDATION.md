# E1-FOUNDATION — Fundaciones reproducibles

## Identificación

- Work ID: E1-FOUNDATION
- Etapa: 1
- Tipo: implementación
- Propietario: Codex
- Coordinador: Codex en sesión con el usuario
- Fecha: 2026-08-31
- Rama/worktree: `codex/e1-foundation`
- Commit base: `ca94c4752073170939796f3174f58ccf4b190624`
- Dependencias: aprobación de inicio recibida; SDK .NET 10.0.400 instalado y verificado

## Resultado esperado

El repositorio puede instalarse, compilarse, probarse y arrancarse localmente con una solución
ASP.NET Core/.NET 10, un frontend Next.js y una compuerta reproducible, sin lógica de negocio.

## Contexto obligatorio

- Blueprint: secciones 6, 8, 9, 10 y Etapa 1 de la sección 11.
- Progress: `Próximo bloque de trabajo — Etapa 1`.
- Guías incluidas en `node_modules/next/dist/docs/` para la versión exacta instalada.

## Alcance

### Dentro

- SDKs, solución, proyectos, referencias, configuración central y paquetes exactos.
- API mínima con health check y OpenAPI.
- EF Core/SQLite con una operación mínima comprobada por integración.
- Frontend App Router mínimo con TypeScript estricto, ESLint, Tailwind y Vitest.
- Contrato/proxy mínimo hacia la API, `.env.example` y compuerta full-stack.
- Tests, documentación operativa, Workboard, Progress y handoff de la etapa.

### Fuera

- Entidades definitivas, seed, reglas de fraude, alertas y UI funcional.
- Anthropic, Koin sandbox, autenticación, despliegue y servicios externos.

### Paths autorizados

- `backend/**`, `frontend/**`, `scripts/**`
- `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `Salvo.slnx`
- `.nvmrc`, `.env.example`, `.gitignore`, `README.md`
- `AGENTS.md`, `DesignAgent/Salvo-Progress.md`, `DesignAgent/Salvo-Getting-Started.md`
- `Coordination/**`

### Paths reservados por otros trabajos

- Ninguno; la etapa se ejecuta secuencialmente.

## Acciones autorizadas

- Ediciones locales permitidas: sí, dentro de los paths indicados.
- Instalación o actualización de dependencias: sí, con versiones exactas y evidencia.
- Escrituras externas: no; no publicar, desplegar ni hacer push.
- Acciones destructivas: no; preservar los artefactos ignorados preexistentes.

## Criterios de aceptación

- [x] SDK .NET y Node fijados; paquetes NuGet y npm con versiones exactas.
- [x] Solución y referencias respetan las fronteras de arquitectura.
- [x] API, OpenAPI y frontend arrancan sin errores.
- [x] EF Core/SQLite realiza una operación mínima en un test de integración.
- [x] Nullable, analyzers, warnings como errores y TypeScript estricto pasan.
- [x] Tests backend y frontend pasan sin red.
- [x] Builds de producción pasan.
- [x] Una compuerta raíz ejecuta todas las verificaciones.
- [x] No hay secretos ni archivos DB versionados.
- [x] Evidencias y riesgos quedan registrados.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `dotnet restore Salvo.slnx --locked-mode` | Locks válidos; todos los proyectos restaurados |
| `dotnet build Salvo.slnx --configuration Release --no-restore` | 0 warnings, 0 errores |
| `dotnet test Salvo.slnx --configuration Release --no-build --no-restore` | 3/3 tests pasan |
| `npm run check --prefix frontend` | Typecheck, ESLint y 3/3 tests pasan |
| `npm run build --prefix frontend` | Next.js 16.3.3 Webpack compila y prerenderiza `/` |
| `./scripts/check.sh` | Ambas toolchains pasan en secuencia |
| Smoke de API/OpenAPI/frontend | `/health`, OpenAPI, `/` y proxy `/api/health` responden `200` |

## Decisiones adoptadas

- SDK .NET fijado en 10.0.400 y Node.js en 24.20.0.
- Build Next.js fijado a Webpack porque Turbopack requiere puertos no disponibles en el entorno de
  ejecución; `--webpack` está documentado y soportado por Next.js 16.3.3.
- Compuerta raíz implementada como `scripts/check.sh`, sin introducir otro gestor de tareas.

## Riesgos y pendientes

- Repetir `./scripts/check.sh` después del merge para cambiar el estado canónico a `Verificada`.
- Turbopack queda sin evidencia en este entorno; no bloquea el build Webpack configurado.
- La Etapa 2 permanece fuera de alcance y pendiente de aprobación.

## Decisiones delegadas

- Elegir versiones patch compatibles dentro de .NET 10/Node 24 y registrarlas.
- Elegir nombres internos de proyectos de test y mecanismo simple de compuerta local.
- Ajustar el scaffold generado sin ampliar el alcance funcional.

## Detenerse y consultar si

- una dependencia obliga a cambiar el stack o una frontera del Blueprint;
- se requiere credencial, escritura externa o acción destructiva;
- una compuerta obligatoria no puede verificarse tras agotar alternativas seguras;
- los residuos locales ignorados interfieren materialmente con el scaffold nuevo.

## Entrega requerida

- Resultado, archivos, comandos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Codex.md` antes de integrar en `main`.

Estado de entrega: `Lista para integrar`.
