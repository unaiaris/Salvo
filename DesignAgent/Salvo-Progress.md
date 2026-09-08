# Salvo — Seguimiento de implementación

> Estado del documento: activo
> Última actualización: 2026-09-08
> Fuente de alcance: [[Salvo-Blueprint]]
> Regla: actualizar este archivo al comenzar y cerrar cada etapa

## Estado general

| Campo | Valor |
| --- | --- |
| Estado del proyecto | **MVP cerrado.** Las nueve etapas completadas y verificadas. **Etapa 10 en ejecución**: la instancia pública |
| Etapa completada | Etapa 9 — Corpus, idiomas y cierre (`E9A`, `E9B`, `E9C1`, `E9C2` y `E9D` integradas) |
| Próxima etapa | Etapa 10 — La instancia pública. Es la primera fuera del núcleo local |
| Estado de la próxima etapa | En ejecución. Diseño v2 aprobado tras revisión adversarial (`e1233ba`); `E10A-CONTENEDOR-Y-MEDICION` despachada |
| Bloqueo actual | Ninguno |
| Dependencias externas | Ninguna para el núcleo local |
| Anthropic | Previsto para después del núcleo; decisión aparte, preparada por D11 |
| Koin sandbox | Post-MVP; sujeto a onboarding y credenciales |
| Coordinación Codex–Claude | `E10A-CONTENEDOR-Y-MEDICION` asignada a Opus 5 · `high`; `E10B` y `E10C` propuestas |

**Este bloque se actualiza en cada cierre de etapa y en cada alta de tarea.** Quedó desfasado
durante toda la Etapa 7 porque los cierres actualizaron el registro de actividad y los checklists
pero no el tablero; un `brief-check` lo encontró.

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
| 5 | UI y dashboard | Completada | Recorrido completo y estados vacíos/error | Merges `5f48db0`, `278e100` y `8198fd5`; 129 tests .NET y 153 de frontend; `check.sh` y `smoke-ui.sh` verdes sobre `main` (21 comprobaciones, 0 fallas) |
| 6 | Proveedor antifraude mock | Completada | Callbacks duplicados sin efectos repetidos y pendientes que finalizan | Merges `bca2c46` y `a412693`; 196 tests .NET y 172 de frontend; compuerta y smoke verdes (29 comprobaciones) |
| 7 | Explicabilidad | Completada | Funciona sin red; el texto verificado sobre la salida no cambia ninguna superficie de decisión | Merges `82f2487`, `ac11015`, `0d117dd` y `b4aac6b`; compuerta y smoke verdes sobre `main` |
| 8 | El argumento del proyecto | Completada | README, diagramas, capturas y guion de demo; cada afirmación contrastada contra el código | Merges `1243d54` y `6ae7750`; `check-docs.sh` incorporado a la compuerta; seis capturas revisadas una por una |
| 9 | Corpus, idiomas y cierre | Completada | Seis reglas y tres bandas alcanzables; F1 deja de valer 1,00 | Merges `41343c1` y `4f7daf9`. F1 holdout 0,632 y calibración 0,688; las seis reglas disparan y las tres bandas existen; el motor escribe campos y el extractor de prosa ya no existe. y `0d65f9a`. El idioma es del despliegue y entra en la identidad de la explicación; el castellano sigue siendo el valor por defecto. La consola pasa de 6 a 31 reglas de accesibilidad más `axe-core` en la compuerta. `E9D` despachada |
| 10 | La instancia pública | En ejecución | Cualquiera la usa desde el navegador, gratis, sin instalar nada | Diseño v2 (`e1233ba`) tras 13 hallazgos, 5 altos. `E10A` mide si es posible y **puede terminar diciendo que no** |
| Post-MVP | Koin sandbox, auth, observabilidad | Pendiente | Aprobación independiente por capacidad | Pendiente |

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
tercera. (Sección histórica de la Etapa 4: las Etapas 5, 6 y 7 se completaron después.)

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
- [x] Importación, errores por fila y ejecución de la corrida de scoring.
- [x] Feed de alertas.
- [x] Detalle, divergencia y revisión.
- [x] Dashboard con monto por moneda y gráfico SVG de servidor.
- [x] Rutas dinámicas: el build pasa sin API levantada.
- [x] Estados vacíos —los tres—, carga, error y accesibilidad estructural.
- [x] `scripts/smoke-ui.sh` con los tres escenarios.

### Etapa 6 — Proveedor externo mock

- [x] Entidad externa separada, con tipos propios.
- [x] Reserva en dos fases antes de llamar al proveedor.
- [x] Mock determinista con bandas declaradas.
- [x] Degradación: un fallo posterior al envío no cierra la evaluación.
- [x] Reconciliación explícita.
- [x] Callback autenticado, replay-safe y con transiciones monótonas.
- [x] Vinculación tardía de recibos sin correlación.
- [x] Superficie en la consola con la divergencia expuesta.

### Etapa 7 — Explicabilidad

- [x] Entidad propia con identidad por evaluación y restricción `READY ⇔ summary`.
- [x] `SignalFacts` y `ExplanationFacts` en el dominio; tokenizador declarado.
- [x] Grounding verificado sobre la salida, en el caso de uso, con test de mutación.
- [x] Ciclo de vida que no puede quedar trabado: reserva, asentamiento incondicional, reintento
      sobre la misma fila, pendiente vencida retomable y tope de intentos.
- [x] Proveedor determinista por defecto, con salida estructurada.
- [x] Al input no entra ningún texto que no escriba el motor; proveedor espía que lo afirma.
- [x] Interceptor de comandos, diferencial ampliado y diferencial de etiquetas sobre el texto.
- [x] La revisión registra qué explicación tenía delante.
- [x] Columnas y códigos que un proveedor real necesita, puestos desde el principio.
- [x] Verificar que la app funcione sin API key y que un `AI_PROVIDER` desconocido falle al arrancar.

- [x] Bloque de explicación en el detalle, con el aviso de desactualizada y los botones.
- [x] Rótulos de los códigos, `boundary.test.ts` con el sub-objeto contaminado y los textos de
      `smoke-ui.sh`. Los cuatro códigos de conflicto fueron a `messages.ts` y los nueve de fallo a
      `format.ts`: son valores de un campo dentro de un `200`, no rechazos de una petición.
- [x] `EXPLANATION_READY` movido de `guards.ts` a `contract.ts` como `EXPLANATION_STATUS`.
- [x] Pulido del texto y del aviso, con la plantilla en `e7-v2` para que el arreglo alcance a lo ya
      escrito (decisión 58).
- [x] La consola ofrece redactar con la plantilla vigente cuando el texto lo escribió una anterior,
      sin alarma y conservando la fila previa.

- [ ] Decidir aparte si se activa Anthropic.

### Etapa 8 — El argumento del proyecto

- [x] README de portfolio completo, con las cifras del corpus en un bloque marcado.
- [x] Diagramas de arquitectura y de flujo: cuatro, en Mermaid versionado, vistos renderizados.
- [x] Cada afirmación de hecho del README contrastada contra el código, y `scripts/check-docs.sh`
      dentro de la compuerta para que la deriva se detecte en vez de prometerse.
- [x] Capturas del recorrido: seis, regenerables con `./scripts/capturas.sh` sobre una base nueva.
- [x] Guion de demo de diez minutos, con `./scripts/demo.sh` para ensayar sin consumir la base.
- [x] **Instalación limpia, hecha después de integrar `E8B`**: un clon del repositorio en un
      directorio nuevo, sin ninguna base de datos —`salvo.db` está ignorada—, levantó todo desde
      cero con `dotnet restore`, `npm ci --prefix frontend`, `check.sh`, `smoke-ui.sh` y
      `capturas.sh`, las cuatro verdes. **Con una salvedad declarada**: el navegador de Playwright
      ya estaba en la caché de la máquina —Playwright la guarda fuera del proyecto—, así que la
      descarga inicial de unos 150 MB es el único paso que este clon no ejercitó. Pasó no es lo
      mismo que se probó.


### Etapa 10 — La instancia pública

- [ ] El contenedor corre consola y API, y **el camino frío está medido** contra los umbrales que ya
      existen en el smoke. Puede terminar diciendo que la etapa no es posible.
- [ ] El rewrite de `/api/:path*` borrado: hoy publica la API entera a un `/api/api/` de distancia.
- [ ] La instancia se reinicia sola, avisa en pantalla que es compartida y efímera, y tiene tope de
      pedidos.
- [ ] La decisión 8 revisada en sus **seis** lugares por el coordinador, con la 70 escrita.
- [ ] Publicada, con el link en el README, en el artículo y en la guía.

### Etapa 9 — Corpus, idiomas y cierre

- [x] Fixture enriquecida con siete arquetipos. **Las 42 celdas predichas se cumplieron: cero
      desvíos**, y la predicción está commiteada antes que la fixture.
- [x] El seed versionado, con `GET /api/demo-data/seed-preview` que avisa antes del clic.
- [x] Panel de denegados por el proveedor sin alerta local: 43 pedidos, entre ellos los tres
      arquetipos invisibles.
- [x] **F1 sobre el holdout: 0,632** (matriz 6/3/4/87), y 0,688 sobre calibración. El barrido sigue
      eligiendo 60, así que no hay discrepancia que explicar.
- [x] Las seis reglas disparan y las tres bandas existen: la distribución de scores ahora tiene 70 y
      80, que el corpus v1 nunca alcanzaba.
- [x] Comercios en el corredor UTC−3. Los `merchantId` son los del v1 a propósito: renombrar uno
      anula el guardián de conflicto. `MER_US_MARKET` es hoy el comercio argentino, y `E9D` lo
      explica.
- [x] Señales estructuradas (`e3-v2`), con `detail` conservado como campo heredado. **El
      diferencial contra lo que el extractor leía de la prosa no tuvo un solo desvío**: 56
      evaluaciones, 87 señales, las seis reglas, y la captura commiteada antes que el motor. Los
      textos dorados salieron idénticos sin tocar `ExplanationGoldenTests`, que es la prueba de que
      fue un cambio de representación y nada más.
- [x] Idioma del despliegue: `SALVO_LANGUAGE`, el idioma en la identidad de la explicación —quinta
      columna del índice de identidad, tercera del parcial de pendientes, y filtro en las tres
      costuras de consulta— y la consola compuesta desde dos diccionarios tipados donde **una clave
      faltante es error de compilación**. `es` es el valor por defecto y el de la demostración. La
      comprobación central pasa de punta a punta: con la explicación castellana escrita, el
      despliegue en portugués escribe una fila nueva y la castellana queda intacta.
- [x] Pasada de accesibilidad. **Automática, entera**: de 6 reglas a 31, más `axe-core` sobre el
      árbol renderizado, las dos dentro de la compuerta; encontraron un defecto real —una aclaración
      que no pertenecía a ningún término dentro de una lista de definiciones— y siete hallazgos con
      severidad, de los cuales entraron cinco. **Con lector de pantalla, parcial**: se recorrieron
      la portada y el encabezado de la cola de alertas, sin hallazgos, y se interrumpió ahí. El
      contraste de color **no lo comprueba nada** y queda dicho. Ningún documento afirma que alguien
      recorrió la consola entera sin ver la pantalla.
- [x] Códigos de error de fila traducidos (`describeRecordError`), en los dos idiomas y con test.
- [x] Repaso final. Toda cifra que un documento afirma sale de **una corrida del 2026-09-07** y no
      de otro documento; cuatro párrafos de prosa que describían el corpus v1 reescritos; las seis
      capturas regeneradas; nueve deudas escritas con nombre y ninguna pagada. Y el aviso del
      dashboard, que afirmaba lo contrario de lo que mostraba, corregido tras revisar las capturas
      una por una.

Fuera de la etapa, dichos: la ruta `/orders`, un proveedor simulado que se porte mal, Anthropic, el
desempate de la cola por identificador aleatorio, y pintar `explanationId` en el panel de revisión.

## Decisiones y dependencias abiertas

| Tema | Estado | Momento de decisión | Nota |
| --- | --- | --- | --- |
| SDK .NET y versiones exactas de dependencias | Resuelta | Etapa 1 | .NET 10.0.400 y dependencias directas fijadas; locks verificados |
| Schema y contratos E2 | Resuelta | Preparación de Etapa 2 | Decisiones 15–20 del Blueprint + brief `E2-CONTRACT-DATA` |
| Baseline, reglas y calibración E3 | Resuelta | Preparación de Etapa 3 | Decisiones 21–27 del Blueprint + brief `E3-MOTOR-DETERMINISTA` |
| Anthropic real | Diferida | Decisión aparte | La Etapa 7 cerró con proveedor determinista. Hoy `AI_PROVIDER=anthropic` se niega a arrancar, con o sin clave |
| Acceso Koin sandbox | Diferida | Post-MVP | Requiere onboarding, private key y `org_id` |
| Auth/multi-tenant | Diferida | Post-MVP | Necesaria antes de publicación mutable |
| Deploy/Postgres | Diferida | Post-MVP | No condiciona la demo local |
| Primer trabajo paralelo Codex–Claude | Cerrada sin ocurrir | — | Codex construyó las Etapas 0 a 3 y Claude las 4 a 7, siempre en secuencia. El protocolo paralelo existe y no se usó |

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
| 2026-09-03 | 5 | Inicio de E5C-IMPORT-DASHBOARD | Brief y rama `claude/e5c-import-dashboard` desde `bb4cf62`; Opus 5 · high acordado tras recomendar Sonnet | En curso |
| 2026-09-03 | 5 | Importación con corrida, dashboard con SVG de servidor, test de deriva de OpenAPI y smoke de recorrido | Cinco commits; 129 tests .NET y 153 de frontend; smoke falsado de tres maneras y test de deriva falsado dos veces | Lista para integrar |
| 2026-09-03 | 5 | Integración y cierre de la Etapa 5 | Merge de `claude/e5c-import-dashboard` + `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes sobre `main`: 21 comprobaciones, 0 fallas | Completada |
| 2026-09-04 | 5 | Prueba manual de importación en los dos formatos | Cuatro caminos cubiertos: importado, duplicado, rechazado por registro y por documento. Las seis reglas del motor dispararon en datos reales y aparecieron las tres bandas de severidad | Completada |
| 2026-09-04 | Preparación E6 | Diseño v1 de la Etapa 6 | Entidad separada, máquina de estados, callback replay-safe | Superado por la v2 |
| 2026-09-04 | Preparación E6 | Revisión adversarial del diseño con Fable 5.1 · `xhigh` | 15 hallazgos, 4 de severidad alta; la pregunta sobre la migración se respondió ejecutándola sobre una copia de la base | Completada |
| 2026-09-04 | Preparación E6 | Diseño v2 con los quince hallazgos incorporados y partición en E6A/E6B | Aprobación del usuario + decisiones 44–50; §4.5, §5.1, §5.2, §7 y §9 del Blueprint corregidos | Aprobada |
| 2026-09-04 | 6 | Inicio de E6A-PROVEEDOR | Brief y rama `claude/e6a-proveedor` desde `8d5faf7`; Opus 5 · high acordado | En curso |
| 2026-09-04 | 6 | Entidad externa, migración, mock determinista, reserva en dos fases y reconciliación | Cinco commits; 160 tests .NET; falsación del test de concurrencia y del diferencial documentadas | Lista para integrar |
| 2026-09-04 | 6 | Integración y verificación canónica de E6A | Merge `bca2c46` + `check.sh` y `smoke-ui.sh` verdes sobre `main`; los 328 fingerprints recalculados desde el material original coinciden con los almacenados | Completada |
| 2026-09-04 | 6 | Inicio de E6B-CALLBACK-UI | Brief y rama `claude/e6b-callback-ui` desde `d5da100`; Opus 5 · high acordado | En curso |
| 2026-09-04 | 6 | Recibos, callback autenticado, vinculación tardía, disparador de demo y superficie en la consola | Seis commits; 196 tests .NET y 172 de frontend; smoke de 21 a 29 comprobaciones; falsación del callback previo al commit y del test de frontera | Lista para integrar |
| 2026-09-04 | 6 | Integración y cierre de la Etapa 6 | Merge `a412693` + `check.sh` y `smoke-ui.sh` verdes sobre `main`: 29 comprobaciones, 0 fallas | Completada |
| 2026-09-05 | Preparación E7 | Diseño v1 de la Etapa 7 | Nueve decisiones y seis preguntas abiertas para la revisión | Superado por la v2 |
| 2026-09-05 | Preparación E7 | Revisión adversarial del diseño con Fable 5.1 · `xhigh` | 12 hallazgos, 3 de severidad alta, verificados contra `salvo.db` y contra el código en `cc4e1c3`; la validación numérica de la v1 rechazaba el ejemplo del propio diseño | Completada |
| 2026-09-05 | Preparación E7 | Diseño v2 con los doce hallazgos incorporados y partición en E7A/E7B | Aprobación del usuario + decisiones 51–56; §4.3, §4.7, §7, §9 y §11 del Blueprint, `Salvo-Portability.md`, `AGENTS.md` y `Workboard.md` corregidos | Aprobada |
| 2026-09-05 | 7 | Inicio de `E7A-EXPLICACIONES` | Brief y rama `claude/e7a-explicaciones` desde `5318518`; Opus 5 · `high` acordado | En curso |
| 2026-09-05 | 7 | Entidad propia, hechos y tokenizador en Domain, grounding verificado sobre la salida, proveedor determinista y ciclo de vida sin trabas | Nueve commits; el interceptor de comandos, el diferencial ampliado, el diferencial de etiquetas sobre el texto y el proveedor espía, con falsación documentada en cada caso. El test dorado encontró que la plantilla escribía el año como `2.026` y que la validación lo aceptaba por la regla de ambigüedad de miles: un año es rótulo, no cantidad | Lista para integrar |
| 2026-09-05 | 7 | Integración y verificación canónica de `E7A-EXPLICACIONES` | Merge `82f2487` + `check.sh` y `smoke-ui.sh` verdes sobre `main`; el contrato de alertas obligó a autorizar la proyección mínima en `guards.ts` y `fixtures.ts`, fuera de la reserva original del brief | Completada |
| 2026-09-05 | 7 | Inicio de `E7B-EXPLICACIONES-UI` | Brief y rama `claude/e7b-explicaciones-ui` desde `af93f01`; Opus 5 · `high` acordado tras corregir el coordinador un precedente que había citado mal | En curso |
| 2026-09-05 | 7 | Bloque de explicación en el detalle, aviso de desactualizada, `explanationId` en la revisión y códigos con rótulo | Seis commits; 239 tests .NET y 197 de frontend; smoke de 29 a 37 comprobaciones, con un escenario que pide la explicación por la API y lee la página. El smoke encontró que el rótulo del botón viajaba como prop y quedaba en el payload RSC de toda página, hubiera botón o no | Lista para integrar |
| 2026-09-05 | 7 | Integración y cierre de la Etapa 7 | Merge `ac11015` + `check.sh` y `smoke-ui.sh` verdes sobre `main`: 37 comprobaciones, 0 fallas | Completada |
| 2026-09-06 | 7 | Recorrido manual de la explicación en la consola | Cuatro observaciones: el aviso posterior a generar repetía la leyenda del bloque, la plantilla escribía «56,0 veces» y «Coincidieron», y la referencia compuesta quedó demostrada con dos pedidos `ORD_000011` de comercios distintos | Completada |
| 2026-09-06 | 7 | `E7C-PULIDO-EXPLICACION` | Cuatro commits; el dorado partido en las dos ramas del cambio —`ORD_000011` con decimal, `ORD_000171` sin él—, `explanation-action.ts` con sus primeros cuatro tests, y la plantilla subida a `e7-v2` con un test de convivencia de versiones. `brief-check` había corregido antes un criterio de aceptación invertido | Lista para integrar |
| 2026-09-06 | 7 | Integración de `E7C-PULIDO-EXPLICACION` | Merge `0d117dd` + `check.sh` y `smoke-ui.sh` verdes sobre `main` | Completada |
| 2026-09-06 | 7 | El recorrido manual muestra que el bump de plantilla no alcanzó al texto guardado | Lectura y escritura no coincidían en cuál era la explicación vigente: `FindAsync` busca con `templateVersion` y `GetExplanationsAsync` no lo filtra, así que la consola escondía el botón sobre una fila de la versión anterior | Completada |
| 2026-09-06 | 7 | `E7D-PLANTILLA-VIGENTE` | Cuatro commits; `WrittenByAnotherTemplate` calculado contra el proveedor registrado y no contra una constante, el botón convertido en un tipo de tres pedidos —`first`, `currentTemplate`, `retry`— para que solo el reintento mande `regenerate: true`, y el test de extremo a extremo que faltaba. Sin migración ni columna nueva | Lista para integrar |
| 2026-09-06 | 7 | Integración y cierre definitivo de la Etapa 7 | Merge `b4aac6b`; verificado a mano sobre `ORD_900004`: la fila `e7-v1` queda intacta con sus instantes originales y la `e7-v2` se escribe al lado con el texto corregido | Completada |
| 2026-09-06 | Preparación E8 | Alcance acordado con el usuario y creación de la Etapa 9 | La Etapa 8 queda en README, diagramas, capturas y guion; fixture enriquecida, señales estructuradas, portugués y accesibilidad pasan a la 9, con la decisión de incluir falsos negativos y positivos ya tomada | Aprobada |
| 2026-09-06 | Preparación E8 | Diseño v1 de la Etapa 8 | Nueve decisiones y seis preguntas abiertas | Superado por la v2 |
| 2026-09-06 | Preparación E8 | Revisión adversarial con Fable 5.1 · `xhigh` | 12 hallazgos, 5 altos, más un inventario de lo falso en once documentos. Encontró que no hay Chrome en la máquina, que el id de una alerta es un GUID y que `salvo.db` no es el corpus demo | Completada |
| 2026-09-06 | Preparación E8 | Sincronización canónica de once documentos | La cabecera del Blueprint declaraba «Etapa 4 en ejecución»; el §3 decía que importar procesa; `AGENTS.md` decía dos veces «No iniciar la Etapa 7» | Completada |
| 2026-09-06 | Preparación E8 | Diseño v2 (`a7e3b97`) y briefs `E8A` y `E8B` (`95d5db3`) | Playwright aprobado por nombre y motivo en `tools/capturas/`; muestras a versionar en `docs/muestras/`; Opus 5 · `high` acordado para las dos tareas | Aprobada |
| 2026-09-06 | 8 | Inicio de `E8A-README-DIAGRAMAS` | Rama `claude/e8a-readme-diagramas` desde `95d5db3`. El `brief-check` encontró tres faltas del coordinador, incluida una sincronización canónica declarada y hecha a medias | En curso |
| 2026-09-06 | 8 | README reescrito como argumento, cuatro diagramas y `scripts/check-docs.sh` | Ocho commits. El verificador de documentos entró en la compuerta y se falsó en dos direcciones. Los cuatro diagramas necesitaron **tres renderizados**: el primero pasó la sintaxis y encontró acentos faltantes y un dibujo ilegible; el segundo encontró una superposición que la corrección misma había introducido; el tercero confirmó | Lista para integrar |
| 2026-09-06 | 8 | Integración de `E8A-README-DIAGRAMAS` | Merge `1243d54` + `check.sh` —que ahora incluye `check-docs.sh`— y `smoke-ui.sh` verdes sobre `main` | Completada |
| 2026-09-06 | 8 | `E8B-DEMO-CAPTURAS` | Seis commits. Playwright en `tools/capturas/` con lockfile propio: `frontend/package-lock.json` intacto. La aserción previa a cada disparo detuvo la primera corrida en la toma 5 —la pantalla traduce el código de error y no lo muestra— y `check-docs.sh` cazó un nombre de test inventado en el guion | Lista para integrar |
| 2026-09-06 | 8 | Integración de `E8B-DEMO-CAPTURAS` | Merge `6ae7750` + compuerta y smoke verdes. Las seis capturas revisadas una por una por el coordinador: salen de una base nueva —18 alertas, sin banda `ALTA`— y la de importación de un envío real del formulario | Completada |
| 2026-09-07 | Preparación E9 | Diseño v1 de la Etapa 9 | Once decisiones y seis preguntas abiertas | Superado por la v2 |
| 2026-09-07 | Preparación E9 | Revisión adversarial con Fable 5.1 · `xhigh` | 11 hallazgos, 5 altos, más una primera parte sobre criterio de dominio. Corrigió el arquetipo central —una cuenta tomada que se envía a la víctima no monetiza nada— e invirtió el orden de la etapa: el corpus actual dispara tres de las seis reglas, así que el extractor de `SignalFacts` es el único oráculo capaz de certificar los campos tipados | Completada |
| 2026-09-07 | Preparación E9 | Diseño v2 y estado canónico | Decisiones 62–64; §4.1, §7, §9 y §11 del Blueprint; el `13,8 %` corregido a `16,7 %` en cuatro documentos | Aprobada |
| 2026-09-07 | 9 | `E9A-FIXTURE` | Nueve commits. La tabla de arquetipos predicha se commiteó **antes** que la fixture y las 42 celdas cayeron sin un solo desvío; de 2.400 comprobaciones campo a campo difirió una, y era la transcripción en Python la equivocada, no el motor. Dos hallazgos de construcción: un patrón de reparto con periodicidades alineadas clavaba a cada comercio en las mismas cinco horas, y la primera versión concentraba casi todos los arquetipos en un solo comprador cuyos propios montos altos le subían la mediana | Lista para integrar |
| 2026-09-08 | 9 | `E9D-CIERRE` | Siete commits. Toda cifra publicada sale de una corrida real y fechada, no de un handoff. Reescribió además cuatro párrafos de prosa que ningún `grep` de cifras encontraba —«el corpus alcanza tres de las seis reglas», «cuando el motor los emita, el extractor se borra» en futuro cuando ya no existía— y dejó nueve deudas escritas con nombre | Lista para integrar |
| 2026-09-08 | 9 | Revisión de capturas por el coordinador | Encontró la peor afirmación falsa que quedaba, y estaba **dentro del producto**: el aviso del dashboard decía que la fixture fue construida para que las reglas recuperen sus propias etiquetas, a veinte píxeles de un F1 de 63,2 %, en la captura embebida en el README. No fue falta de `E9D`: el brief le reservó `frontend/src/**` fuera de alcance y ese aviso no lo cita ningún documento. Corregido en los dos idiomas, en el glosario y en las dos anclas que lo comprobaban | Completada |
| 2026-09-08 | 9 | Integración de `E9D-CIERRE` | Merge `c2e9a65` + compuerta y smoke verdes. **Cierra el MVP**: las nueve etapas completadas y verificadas | Completada |
| 2026-09-07 | 9 | `E9C2-ACCESIBILIDAD`, fase 1 | La tarea **se detuvo en el medio, que es su forma**, y la parada se respetó: un solo archivo de producto tocado en toda la fase. Midió que las seis reglas heredadas solo comprueban que un atributo `aria-*` esté bien escrito, y ninguna mira si un control se puede operar. Las dos dependencias ya estaban en el árbol como transitivas y se declararon en su versión exacta; no entró una tercera. Encontró un defecto real de marcado y entregó siete hallazgos con severidad | Parcial |
| 2026-09-07 | 9 | Recorrido con VoiceOver | Lo corrió el coordinador. **Parcial**: portada y encabezado de la cola de alertas, sin hallazgos, e interrumpido ahí por lo tedioso del ejercicio. No cambió nada: los siete hallazgos ya estaban medidos y ninguno dependía del recorrido para existir. Se ajustó la afirmación de la etapa en vez de estirarla | Completada |
| 2026-09-07 | 9 | `E9C2-ACCESIBILIDAD`, fase 2 | Seis commits, uno por hallazgo, con las tres claves nuevas en los dos diccionarios en un commit **anterior** al código que las usa. Entraron los cinco aprobados; el contraste y el encabezado único quedaron afuera y dichos | Lista para integrar |
| 2026-09-07 | 9 | Integración de `E9C2-ACCESIBILIDAD` | Merge `835a76a` + compuerta y smoke verdes. Verificado por el coordinador contra el árbol: la región viva y el `tabIndex={-1}` del veredicto, la `caption` del barrido, y las claves en `es.ts` y `pt.ts` | Completada |
| 2026-09-07 | 9 | `E9C1-IDIOMA` | Cinco commits de implementación sobre 97 archivos. El `brief-check` con Fable · `xhigh` frenó el despacho **tres** veces: la reserva de paths no cubría las tres costuras de consulta del almacén ni `Application/Alerts/**`, `guards.ts` quedaba como excepción cuando la recaptura lo deja sin compilar, y el décimo código de fallo estaba mandado a `CONSOLE_CODES` citando la decisión 57 al revés. La segunda vuelta descubrió además que un script de edición del coordinador había fallado a mitad y descartado un bloque entero de correcciones sin avisar | Lista para integrar |
| 2026-09-07 | 9 | Integración de `E9C1-IDIOMA` | Merge `0d65f9a` + compuerta y smoke verdes (71 comprobaciones, eran 52). Verificado por el coordinador contra el árbol: `ExplanationGoldenTests` sin un byte de diferencia, las tres costuras filtrando por idioma, el idioma en los dos índices, `SpanishNumberFormat` renombrado a `ExplanationNumberFormat`, y `BUSINESS_TIMEZONE` fuera de `.env.example`. El décimo código quedó como `LegacySignalFormat`, que dice qué pasó en vez de culpar a un proveedor que nunca se llamó | Completada |
| 2026-09-07 | 9 | `E9B-SENALES-TIPADAS` | Once commits. El `brief-check` con Fable · `xhigh` frenó el despacho dos veces: la primera por una contradicción con `AGENTS.md` que nadie había listado y por dos afirmaciones sobre precisión que el runtime desmiente, la segunda por dos observaciones menores. La medición de la fixture mostró que sus 300 instantes son minutos enteros, así que el dorado no podía certificar la fracción de `elapsedMinutes` y ese campo se probó aparte. En ejecución apareció que el extractor inventaba un `scope` constante en `new_buyer_high_value` | Lista para integrar |
| 2026-09-07 | 9 | Integración de `E9B-SENALES-TIPADAS` | Merge `4f7daf9` + compuerta y smoke verdes (52 comprobaciones). Verificado por el coordinador contra el árbol: cero referencias a `Parse`, `ExplanationGoldenTests` y `divergence.ts` sin un byte de diferencia, el redondeo en el constructor con `AwayFromZero` y la escala escrita con `F{n}`, y el dorado escribiendo a `Path.GetTempPath()` en vez de sobre sí mismo | Completada |
| 2026-09-07 | 9 | Integración de `E9A-FIXTURE` | Merge `41343c1` + compuerta y smoke verdes. F1 holdout 0,632 y calibración 0,688, verificados por el coordinador contra las matrices que los tests fijan | Completada |

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
5. ejecutar `scripts/smoke-ui.sh` sobre el estado integrado. No forma parte de `scripts/check.sh`
   —cuesta compilar las dos toolchains y arrancar dos procesos— pero es lo único que verifica el
   recorrido completo con datos, sin datos y con la API caída, que el Blueprint exige;
6. pulsar *Sync* en la integración de GitHub del Proyecto de Claude.ai, para que el conocimiento del
   Proyecto deje de ir por detrás de `main`. La sincronización no es automática al hacer push;
7. señalar la próxima etapa sin iniciarla automáticamente.

### Excepción para trabajo paralelo

Cuando Codex y Claude trabajan en ramas/worktrees distintos:

1. el coordinador actualiza Workboard y Progress antes de despachar;
2. cada agente modifica solo sus paths y escribe en su handoff;
3. los agentes no cambian el estado canónico desde sus ramas;
4. el coordinador integra en el orden indicado por dependencias;
5. Progress se actualiza únicamente después del merge y la compuerta conjunta.

Ver `Coordination/README.md` y `Coordination/Workboard.md`.
