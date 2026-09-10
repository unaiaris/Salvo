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
- Etapa 5 completada. Se ejecutó en tres ítems: `E5A-API-LECTURA` (`5f48db0`), `E5B-ALERTAS-UI`
  (`278e100`) y `E5C-IMPORT-DASHBOARD`, todos integrados y verificados con la compuerta full-stack y
  con `scripts/smoke-ui.sh` sobre el estado integrado.
- El recorrido completo se verifica con `scripts/smoke-ui.sh`, que no forma parte de
  `scripts/check.sh`: se ejecuta tras cada integración de etapa, junto con la compuerta.
- El gráfico del dashboard es SVG de servidor. Ningún componente del dashboard es de cliente y
  `boundary.test.ts` exige que su lista de cruces servidor–cliente sea vacía.
- Etapa 6 completada. Se ejecutó en dos ítems: `E6A-PROVEEDOR` (`bca2c46`) y `E6B-CALLBACK-UI`
  (`a412693`), ambos verificados con la compuerta y el smoke.
- Etapa 7 completada. Se ejecutó en cuatro ítems: `E7A-EXPLICACIONES` (`82f2487`),
  `E7B-EXPLICACIONES-UI` (`ac11015`), `E7C-PULIDO-EXPLICACION` (`0d117dd`) y
  `E7D-PLANTILLA-VIGENTE` (`b4aac6b`), todos verificados con la compuerta y el smoke.
- La explicación de una alerta es una entidad propia cuya identidad es la evaluación, y cada cifra y
  cada regla de su texto se verifican contra la evaluación **antes** de guardarlo: un texto que no
  pasa esa comprobación no se persiste, no se registra y no se muestra.
- Al input de un modelo no entra ningún texto que no escriba el motor. La verificación vive en el
  caso de uso, entre el puerto y el almacenamiento, nunca en el adaptador.
- Cambiar el texto que produce una plantilla sube su versión, porque la versión forma parte de la
  identidad de la fila.
- Etapa 8 completada. Se ejecutó en dos ítems: `E8A-README-DIAGRAMAS` (`1243d54`) y
  `E8B-DEMO-CAPTURAS` (`6ae7750`). El README pasó a ser el argumento del proyecto, y
  `scripts/check-docs.sh` entró en la compuerta: cada ruta y cada nombre de test que un documento
  público cita se comprueba, no se promete.
- Etapa 9 completada: corpus, idiomas y cierre. El corpus de demostración se construye para que
  las reglas **se equivoquen** (decisión 65); el idioma es del despliegue (decisión 62); y el
  umbral de alerta es una política de negocio y no el resultado del barrido (decisión 63).
- **Etapa 10 completada: la instancia pública existe.** https://salvo-k6wk.onrender.com, en el plan gratuito de Render: una
  sandbox compartida que se reinicia sola, con corpus sintético. La invariante sobre despliegue es la
  decisión 70, que reemplazó a la 8 conservando su motivo. `E10A` (`29677f6`), `E10B` (`23a47cd`) y
  `E10C` (`51eec4c`) integradas y verificadas. **No hay etapa abierta**: lo que venga es post-MVP.
- El SDK se pide **por versión exacta** en el `Dockerfile`, porque `global.json` lo fija con
  `rollForward: disable`. Una exigencia fija no se apoya en una etiqueta móvil: la de `sdk:10.0`
  cambió de parche y rompió el primer build en una máquina sin caché.
- La imagen de la instancia compartida trae la base sembrada **horneada adentro** y la migración al
  arrancar apagada: por eso llega al primer dato en 41 s a 0,1 vCPU en vez de 119,7 s. El reinicio
  por antigüedad es el proceso terminando con **código 75** y la restauración es copiar ese archivo.
  El tope de pedidos vive en la API y el limitador de tasa en la capa de Next: son dos defensas
  distintas y ninguna reemplaza a la otra.
- El orden de la Etapa 9 es obligatorio: la fixture primero y el motor después, porque el corpus
  actual dispara tres de las seis reglas y el extractor de `SignalFacts` es el único oráculo capaz
  de certificar los campos tipados de las otras tres.
- El recibo de un callback y la transición que provoca se persisten en una única unidad de trabajo.
  Un duplicado es la ausencia de una segunda fila, detectada por violación de unicidad.
- El endpoint de callback falla cerrado: sin secreto configurado, `401` a toda petición.
- El disparador de callback de la demo no acepta un estado del cliente. Elige qué evaluación, nunca
  qué resultado.
- No iniciar una etapa nueva sin petición o aprobación explícita del usuario.
- `risk_evaluations` está restringida a `source = 'LOCAL'` y `status IN ('APPROVED','DENIED')` a
  nivel de base. Cualquier estado externo va en `external_evaluations`.
- La taxonomía de fallo del proveedor vive en un solo lugar, `ExternalProviderExchange`: ningún
  adaptador decide por su cuenta si un fallo cierra la evaluación.
- Las decisiones de diseño de la Etapa 6 son las entradas 44 a 50 de la bitácora del Blueprint. El
  diseño v2 y su revisión adversarial viven en `Coordination/Tasks/E6-DISENO.md` y
  `Coordination/Tasks/E6-revision-adversarial.md`.
- No iniciar una etapa nueva sin petición o aprobación explícita del usuario.
- Ningún componente cliente recibe objetos de la API: solo primitivas. Las guardas de
  `frontend/src/lib/api/guards.ts` proyectan, nunca comprueban sobre el mismo objeto.
- Las rutas de datos declaran `dynamic = 'force-dynamic'`. El build debe pasar con la API apagada.
- Las decisiones de diseño de la Etapa 5 son las entradas 37 a 43 de la bitácora del Blueprint. El
  diseño v2 y su revisión adversarial viven en `Coordination/Tasks/E5-DISENO.md` y
  `Coordination/Tasks/E5-revision-adversarial.md`.
- El dashboard operativo no lee `OrderEvaluationLabel` por ningún camino, ni directo ni indirecto.
  La calidad del criterio es otra superficie, tras `DemoData:Enabled`.
- Ninguna lectura de estado vigente consulta `risk_evaluations.status` a secas: siempre vía
  `run_evaluations` de la corrida vigente. La regla se conserva aunque la evaluación externa viva en
  otra tabla: `risk_evaluations` acumula historia y solo la corrida vigente define qué está vigente.
- La evaluación externa vive en `ExternalEvaluation`, con tipos propios en `Salvo.Domain/External/`.
  No comparte tabla ni enumeración con `RiskEvaluation`, que es append-only y siempre `LOCAL`.
- Una fila de evaluación externa se reserva y se persiste **antes** de llamar al proveedor.
- Un fallo posterior al envío deja la fila en `PENDING` con `lastErrorCode`; no la cierra.
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
- La IA nunca decide fraude, severidad ni bloqueo; solo puede redactar explicaciones, y el texto
  que redacta no puede escribir en ninguna superficie de decisión.
- Que una explicación use solo las señales suministradas se verifica sobre la salida y se rechaza
  el texto que no lo cumple; no se confía al prompt.
- Anthropic es el proveedor de IA previsto, pero su integración se difiere hasta que el núcleo
  funcione sin IA.
- El MVP usa `IAntifraudProvider` con implementación mock. Koin sandbox es post-MVP y requiere
  aprobación, onboarding y credenciales.
- El score local, la evaluación externa y la alerta son entidades/conceptos separados.
- El dataset es 100% sintético. No incorporar PII o información financiera real.
- Sin autenticación **no se despliega nada que reciba datos de una persona real**. La excepción es
  una instancia pública de demostración que cumpla las tres condiciones de la decisión 70: datos
  sintéticos a los que vuelve en cada reinicio, aviso en pantalla de que es compartida y efímera, y
  un reinicio que no depende de que nadie se acuerde.
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
- Toda señal se lee sin intérprete: `e3-v1` por su `detail` en prosa, `e3-v2` por sus campos
  con nombre. Un test compone la frase de cada regla desde la fila almacenada.
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
- Al input de un modelo de lenguaje no entra ningún texto que no escriba el motor: quedan fuera
  los campos importados, los identificadores, las notas escritas por personas y las respuestas de
  proveedores externos. Un identificador normalizado no es seguro por tener formato estricto.
- Un texto rechazado por la validación de grounding no se persiste, no se registra y no llega al
  diagnóstico: se registra el token ofensor, nunca la frase.
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
