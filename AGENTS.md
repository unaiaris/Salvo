# Salvo — Instrucciones permanentes para Codex

<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your
training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's
directory; in monorepos the `next` package may not be visible from the repo root) before writing any
code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at
`node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it or changing its markers may
cause Next.js to add it again.

<!-- END:nextjs-agent-rules -->

## Propósito

Salvo es una consola antifraude B2B de portfolio para un comercio electrónico ficticio. El objetivo
es demostrar dominio antifraude, full-stack e integración robusta con proveedores externos.

La fuente de verdad de producto y arquitectura es `DesignAgent/Salvo-Blueprint.md`. Si una tarea
cambia alcance o arquitectura, actualizar primero su bitácora y después los documentos derivados.

## Estado actual

- Etapa 1 integrada y verificada sobre `main`.
- Etapa 2 integrada en `main` mediante `4b7bf54` y verificada con la compuerta full-stack.
- Etapa 3 integrada en `main` mediante `809ff75` y verificada con la compuerta full-stack.
- Etapa 4 completada. Se ejecutó en dos ítems: `E4A-PERSISTENCIA`, integrada mediante `1d9ee83`, y
  `E4B-ALERTAS`, integrada mediante `c35878b`. Ambas verificadas con la compuerta full-stack sobre
  `main`.
- Etapa 5 aprobada por el usuario y en curso. Se ejecuta en tres ítems: `E5A-API-LECTURA`,
  `E5B-ALERTAS-UI` y `E5C-IMPORT-DASHBOARD`, en ese orden y con la anterior integrada.
- Las decisiones de diseño de la Etapa 5 son las entradas 37 a 43 de la bitácora del Blueprint. El
  diseño v2 y su revisión adversarial viven en `Coordination/Tasks/E5-DISENO.md` y
  `Coordination/Tasks/E5-revision-adversarial.md`.
- El dashboard operativo no lee `OrderEvaluationLabel` por ningún camino, ni directo ni indirecto.
  La calidad del criterio es otra superficie, tras `DemoData:Enabled`.
- Ninguna lectura de estado vigente consulta `risk_evaluations.status` a secas: siempre vía
  `run_evaluations` de la corrida vigente.
- Las decisiones de diseño de la Etapa 4 son las entradas 28 a 36 de la bitácora del Blueprint. El
  diseño y su revisión adversarial viven en `Coordination/Tasks/E4-DISENO.md` y
  `Coordination/Tasks/E4-revision-adversarial.md`.
- El veredicto de una alerta es terminal: nada reabre una alerta revisada. Una escalada crea una
  alerta nueva enlazada por `supersedesAlertId`.
- No incorporar UI, `amountAtRisk` ni orden de feed: son Etapa 5 y requieren aprobación explícita.
- No iniciar una etapa nueva sin petición o aprobación explícita del usuario.
- El estado operativo, checklists y evidencias viven en `DesignAgent/Salvo-Progress.md`.

## Decisiones invariantes

- El riesgo local lo calculan reglas deterministas, puras y auditables.
- La IA nunca decide fraude, severidad ni bloqueo; solo puede redactar explicaciones.
- Anthropic es el proveedor de IA previsto, pero su integración se difiere hasta que el núcleo
  funcione sin IA.
- El MVP usa `IAntifraudProvider` con implementación mock. Koin sandbox es post-MVP y requiere
  aprobación, onboarding y credenciales.
- El score local, la evaluación externa y la alerta son entidades/conceptos separados.
- El dataset es 100% sintético. No incorporar PII o información financiera real.
- Sin autenticación, la aplicación es solo local y no se despliega con rutas mutables públicas.
- NL→SQL, auth, observabilidad, Postgres y deploy están fuera del MVP inicial.

## Stack y versiones

- Backend: .NET 10 LTS, C# 14 y ASP.NET Core.
- Frontend: Node.js 24.20.0, npm 11.19.0, Next.js App Router, React y TypeScript estricto.
- Persistencia: EF Core + SQLite local.
- Contrato HTTP: OpenAPI generado por ASP.NET Core y cliente TypeScript tipado.
- Tests: xUnit e integración ASP.NET Core en backend; Vitest/Testing Library en frontend.
- UI: Tailwind. Los gráficos se dibujan en SVG renderizado en el servidor; sin librería de
  gráficos (decisión 43).
- `global.json`, `Directory.Packages.props` y `.nvmrc` quedaron fijados en la Etapa 1.
- Fijar versiones exactas y versionar `package-lock.json`; no usar versiones flotantes ni `@latest`
  en instrucciones reproducibles.
- Usar ESLint directamente; no usar el comando eliminado `next lint`.
- Antes de fijar versiones de .NET, EF Core o Next.js, consultar la documentación vigente y ejecutar
  smoke tests con las versiones exactas elegidas.

## Arquitectura

- Backend monolítico modular y cliente web separado; no crear microservicios.
- `backend/src/Salvo.Domain`: entidades, value objects, reglas, scoring y métricas puras.
- `backend/src/Salvo.Application`: casos de uso, puertos y contratos internos.
- `backend/src/Salvo.Infrastructure`: EF Core, repositorios y adaptadores mock/externos.
- `backend/src/Salvo.Api`: endpoints, validación de transporte, OpenAPI y composición.
- `frontend/src`: UI Next.js, componentes y cliente HTTP tipado.
- `Salvo.Domain` no referencia ASP.NET Core, EF Core ni SDKs externos.
- La UI no contiene reglas de fraude ni accede directamente a la DB.
- Endpoints y Route Handlers de proxy no duplican casos de uso.
- Los tipos y errores específicos de proveedores no atraviesan la frontera de infraestructura.

## Reglas de dominio

- `amountCents` es un entero positivo y siempre se acompaña de `currencyCode`.
- Procesar pedidos en orden temporal.
- El baseline de un pedido usa únicamente historia anterior; nunca el presente o futuro.
- `isFraudLabel` solo se usa para evaluación, nunca como feature.
- Pesos, ventanas, mínimos y umbral viven en un `RuleConfig` central e inmutable.
- Cada señal incluye un `detail` legible.
- Repetir seed, scoring, creación de alertas o callback no duplica efectos.
- Una revisión actualiza entidades relacionadas en una transacción de DB.
- No cambiar automáticamente pedidos ya revisados durante un re-scoring normal.

## Seguridad y privacidad

- Validar DTOs en la API y volver a validar invariantes al construir tipos de dominio.
- Usar Zod en el frontend solo donde aporte validación temprana; nunca tratarla como validación
  autoritativa.
- Secretos únicamente en el backend ASP.NET Core; nunca exponer claves mediante `NEXT_PUBLIC_`.
- No leer ni mostrar `.env` o almacenes de credenciales; usar `.env.example` para conocer variables.
- No escribir secretos en documentación, fixtures, tests, logs o mensajes.
- No almacenar ni registrar PAN, CVV, documentos reales o links sensibles.
- Usar identificadores pseudónimos de comprador.
- Sanitizar errores externos y evitar logs de request/response completos por defecto.
- Persistir callbacks de forma segura antes de responder `2xx`; tolerar replays.
- Toda red externa usa timeout explícito; retries solo donde sean semánticamente seguros.
- La caída de IA o proveedor externo no detiene el scoring local.

## Calidad

- C# con nullable habilitado, analyzers activos y warnings tratados como errores.
- No usar el operador de supresión `!` para ocultar nulabilidad sin una justificación local.
- TypeScript estricto y sin `any`. Si un borde es desconocido, usar `unknown` y validarlo.
- Funciones de dominio puras, pequeñas y sin dependencias de framework.
- Tests sin red; mockear Anthropic y Koin.
- Añadir tests para cada comportamiento y regresión modificada.
- `dotnet build` y `dotnet test` verifican backend; `npm run check` verifica typecheck, ESLint y
  tests frontend.
- La compuerta global debe ejecutar verificaciones y builds de producción de ambas toolchains.
- Verificar seed idempotente y evaluación sin fuga temporal.
- Mantener estados vacíos, carga, error y accesibilidad básica.

## Autonomía y aprobaciones

- Para explicar, revisar, diagnosticar o planificar: inspeccionar y reportar; no editar archivos
  salvo que la petición incluya corregirlos.
- Para implementar, cambiar o corregir: realizar los cambios locales dentro del alcance y ejecutar
  verificaciones no destructivas sin pedir confirmación adicional.
- Leer archivos, inspeccionar Git, editar paths autorizados y ejecutar tests locales son acciones
  permitidas dentro de una tarea de implementación.
- Pedir confirmación antes de escrituras externas, acciones destructivas, uso de credenciales o una
  ampliación material de alcance/arquitectura.
- Si una ambigüedad admite una suposición segura que no cambia el alcance, continuar y declararla en
  la entrega; consultar solo cuando la elección cambie materialmente el resultado.

## Forma de trabajo

- Antes de editar, leer la sección relevante del Blueprint y comprobar el estado del repo.
- Tomar Git, Progress y las compuertas como evidencia; no reutilizar artefactos ignorados sin
  validarlos contra el brief de la tarea.
- Para una tarea coordinada, partir de un brief completo basado en
  `Coordination/Task-Brief-Template.md`.
- Explicar impacto y archivos afectados cuando la tarea sea material.
- Trabajar por etapas pequeñas y verification gates.
- No tocar archivos fuera del alcance ni sobrescribir cambios del usuario.
- No mover, borrar o esconder `DesignAgent/` para crear el scaffold.
- Al terminar, informar archivos cambiados, verificaciones ejecutadas y riesgos restantes.

## Coordinación con Claude

- Antes de trabajo paralelo, leer `Coordination/README.md` y `Coordination/Workboard.md`.
- Nunca ejecutar Codex y Claude sobre el mismo worktree ni asignarles los mismos paths.
- El coordinador es el único que modifica Workboard y Progress durante trabajo paralelo.
- Codex registra entregas paralelas en `Coordination/Handoffs/Codex.md`; Claude usa su propio log.
- Una rama terminada está “lista para integrar”, no “integrada”. El estado canónico cambia solo tras
  merge y verificación conjunta.
- Registrar rama, commit base, commit final, archivos y comandos en cada handoff.
- Serializar lockfiles, migraciones y configuración central salvo que exista una partición segura.

## Documentación

- `AGENTS.md`: reglas permanentes de Codex.
- `DesignAgent/Salvo-Blueprint.md`: fuente de verdad y bitácora.
- `DesignAgent/Salvo-Progress.md`: estado vivo y evidencias por etapa.
- `CLAUDE.md` y `ClaudeAgent/`: adaptación para Claude sin duplicar la fuente de verdad.
- `Coordination/`: ownership, trabajo paralelo y handoffs.
- `README.md`: presentación pública; debe distinguir mock, sandbox y producción.
- Tests: comportamiento ejecutable.
- No duplicar instrucciones extensas en `CLAUDE.md`; si se añade, mantenerlo pequeño y coherente.
