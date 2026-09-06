# Salvo — Revisión adversarial del diseño propuesto de Etapa 8

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-06
> Revisor: Claude (Fable 5.1 · `xhigh`)
> Objeto: `Coordination/Tasks/E8-DISENO.md` v1 (base `main` en `3cbdd07`)
> Método: lectura completa de `README.md`, del Blueprint (§0–§15 y las 58 entradas de la bitácora),
> `AGENTS.md`, `CLAUDE.md`, Overview, MOC, Getting-Started, Portability, Project-Instructions,
> Progress, Workboard, `Coordination/README.md`, `ClaudeAgent/*`, la plantilla de brief, los diseños
> y revisiones de E5–E7, los briefs E5B, E5C y E6A–E7D y el handoff de `E7D`; del código real en
> `Salvo.Api` (Program, endpoints, settings, launchSettings), `Salvo.Application/Alerts`,
> `/External`, `/Explanations`, `/Risk`, `Salvo.Domain/Risk`, `/Alerts`, `/External`,
> `/Explanations`, `Salvo.Infrastructure` (composición, mock, store de alertas, fixture embebida),
> `frontend/src/app/**`, `frontend/src/lib/**`, `next.config.ts`, `package.json`,
> `scripts/check.sh` y `scripts/smoke-ui.sh`; de Git (`git log`, fecha del último commit de cada
> documento) y del entorno de esta máquina (`/Applications`, `claude --version`, Brave, Node). Las
> cifras de la base salen de consultas de solo lectura sobre `backend/src/Salvo.Api/salvo.db`
> (`sqlite3 -readonly`): 328 pedidos, 7 corridas, 21 alertas, 328 evaluaciones externas y 2
> explicaciones. No se ejecutó ninguna compuerta ni ningún test; cada conteo de tests que se cita
> dice de dónde sale. No se editó ningún archivo salvo este informe.

La revisión tiene tres partes, en el orden en que las pediste: el ataque al diseño, el inventario de
lo que hoy es falso, y la pregunta de Playwright. Las seis preguntas abiertas se responden dentro de
los hallazgos y se recogen al final.

---

## Primera mitad — Hallazgos, por severidad

### 1. D5, alta. La opción que el diseño recomienda para las capturas parte de una premisa falsa, y aunque fuera cierta no cumpliría el criterio de D5

D5 dice que la opción recomendada es automatizar las capturas «con el navegador de Chrome conectado
a su máquina, que sí alcanza `localhost:3000`», y que «depende de que la extensión esté disponible»
(`E8-DISENO.md:85-88`). Contra la máquina:

- **No hay Google Chrome instalado.** `/Applications` tiene Safari y **Brave Browser 145.1.87.190**;
  `mdfind` por `com.google.Chrome` no devuelve nada; no hay `chromium` ni `google-chrome` en `PATH`.
  Brave admite extensiones de Chrome, pero que la extensión de Claude funcione ahí no está verificado
  y no lo verifiqué: no es algo que un informe pueda afirmar.
- **Aunque funcionara, no sería reproducible.** El criterio de D5 es que las capturas «se sacan del
  recorrido real» y quedan «anotadas como regenerables en la Etapa 9». Una captura que solo puede
  regenerar una sesión de Claude con una extensión conectada no la regenera el usuario desde una
  terminal, ni la regenera nadie que clone el repositorio. Es exactamente la clase de artefacto que
  «se desactualiza en silencio», el argumento con el que D3 rechaza las imágenes para los diagramas.

La consecuencia no es «capturas a mano»: es un script versionado, que es lo que preguntás en la
tercera parte. Ahí está la recomendación completa. Lo que este hallazgo cierra es la opción B del
diseño: no existe en esta máquina y no cumple el criterio que el propio D5 fija.

### 2. D5 y D6, alta. Ni una captura ni un paso del guion pueden citar una alerta por su URL ni por su posición en la cola: el id es un GUID aleatorio y el desempate de la cola también lo es

El guion dice que cada bloque lleva «el clic que hay que dar» (`E8-DISENO.md:107`) y la primera
toma es «`/alerts` con la cola ordenada» (`E8-DISENO.md:76`). Contra el código:

- El id de una alerta es `Guid.NewGuid()` (`SystemAlertIdGenerator.cs:7`). Dos bases nuevas con el
  mismo corpus producen URLs distintas para la misma alerta. Ninguna URL de `/alerts/[id]` puede ir
  en un guion ni en un pie de captura.
- Todas las alertas de una corrida comparten un único `createdAt`: `RunScoringHandler` toma un solo
  instante, `startedAt`, y lo pasa entero a `OpenAlertsAsync` (`RunScoringHandler.cs:25, 79, 165`).
  La cola ordena por
  score vigente descendente, después `createdAt` descendente y después **`alert.Id`**
  (`EfAlertStore.cs:86-91`). Con el corpus demo los scores son solo 60 y 90 (consulta sobre
  `salvo.db`: 13 alertas en 60, 5 en 90 y ninguna en otro valor), así que **el orden dentro de cada
  banda lo decide un GUID aleatorio** y cambia en cada base nueva. «La primera alerta de la cola» no
  nombra a ninguna alerta en particular.

Lo que sí es estable es `merchantReferenceId`, que la tabla del feed muestra. Corrección: el guion y
los pies de captura nombran pedidos (`ORD_000011`, `ORD_000171`…) y el clic se describe como «la
fila de `ORD_000011`», nunca como «la primera fila». La captura 1 va a mostrar un orden distinto en
cada regeneración dentro de cada banda: hay que decirlo en la nota de regenerables, o aceptar que la
captura 1 se compara a ojo y no byte a byte. Hacer determinista el desempate (por referencia del
pedido) es código de producto y D9 lo excluye de la etapa; queda como hallazgo para la Etapa 9, no
como tarea de E8.

### 3. D5, alta. «Del recorrido real con el corpus demo» no describe la base que hay, y la toma 5 depende de un archivo que no está versionado

`salvo.db` no es el corpus demo. Consultas de solo lectura:

| Qué | En `salvo.db` | Solo corpus demo |
| --- | --- | --- |
| Pedidos | 328 (100 `MER_UY_STORE` + 6 importados, 101 `MER_BR_STORE`, 100 `MER_US_MARKET`, 21 `MER_UY_PHARMA`) | 300 |
| Corridas | 7 | 1 |
| Alertas | 21: 20 `OPEN` (6 `CRITICAL`, 1 `HIGH`, 13 `MEDIUM`) y 1 `REPORTED_FRAUD` | 18 (13 `MEDIUM`, 5 `CRITICAL`, 0 `HIGH`) |
| Evaluaciones externas | 328 (264 `APPROVED`, 54 `DENIED`, 10 `ERROR`), 21 recibos `APPLIED` | ninguna hasta que se piden |
| Explicaciones | 2 (`e7-v1` y `e7-v2`, sobre la misma evaluación) | ninguna |

La banda `HIGH`, la única alerta a 70 y la única con `unusual_hour` (`ORD_959999`) existen porque el
2026-09-04 se importó `import-hora-inusual.json`; los pedidos `ORD_9000xx` vienen de
`import-valido.csv`. Esos archivos viven en `_local/muestras/`, que `.gitignore:59` excluye a
propósito, y cuyo README dice «No versionados». Consecuencias:

- **Las capturas tienen que salir de una base nueva**, como hace el smoke: base temporal, seed,
  corrida. Si salen de `salvo.db`, la captura 1 muestra 20 alertas y una banda que el corpus no
  produce, y el pie «corpus demo» es falso desde el primer día.
- **La toma 5** —`/import` con filas rechazadas— necesita un archivo con filas rechazadas. Hoy el
  único es `_local/muestras/import-con-errores.csv`, que no viaja con el repositorio. Reproducir esa
  captura desde un clon es imposible. Hay que versionar una muestra —por ejemplo `docs/muestras/`—,
  sintética y con la misma tabla de códigos esperados que ya tiene el README de `_local/muestras`.
  Mover las cuatro muestras es una decisión del usuario, porque él las dejó fuera adrede.
- **La toma 6** dice «el aviso de divergencia de banda, si se puede provocar». Hay dos divergencias
  distintas en pantalla y el diseño no dice cuál: la de banda entre snapshot y evaluación vigente
  (`divergence.ts:19-37`, bloquea el veredicto) y la de criterio entre motor local y proveedor
  («Los dos criterios no coinciden», `external-block.tsx:125`). La segunda es determinista y
  abundante: el mock aprueba todo pedido cuyo sufijo módulo 100 sea menor que 75
  (`MockAntifraudProvider.cs:31, 110-113`), y 14 de las 18 alertas del corpus caen ahí
  (`ORD_000011, 027, 043, 059, 107, 123, 139, 155, 171, 203, 219, 235, 251` y `267`, con sufijos 11
  a 71), así que pedir la evaluación externa sobre cualquiera de ellas muestra el aviso en el acto.
  Las otras cuatro no divergen: `ORD_000075`, `187` y `283` reciben `DENIED` en el acto, y
  `ORD_000091` queda `PENDING` y su callback la cierra en `DENIED`. La
  primera **no está en ninguna alerta de `salvo.db`** (consulta: las 21 alertas tienen la misma banda
  en snapshot y vigente) y solo se provoca con una importación retroactiva que cambie el baseline de
  un pedido ya puntuado; la receta existe en
  `AlertCreationTests.AnOpenAlertKeepsItsSnapshotAndExposesTheDivergenceAfterABackfill`
  (`AlertCreationTests.cs:119-127`, con `AlertTestCorpus.DivergenceBase()` y su backfill) y habría
  que convertirla en un archivo de muestra versionado. El diseño tiene que elegir una de las dos y
  nombrarla; «si se puede provocar» sugiere que no se sabía cuál.

### 4. D6, alta. Dos bloques del guion prometen algo que la pantalla no puede dar con la alerta que se venía mostrando, y el guion entero es destructivo sin un reset que hoy no existe

**Minuto 7–8, «la segunda opinión del proveedor y la divergencia».** Sobre una alerta del corpus se
puede mostrar una de las dos cosas, no las dos:

- La divergencia aparece en el acto para los sufijos menores que 75 (arriba). Pero ahí el proveedor
  responde `APPROVED` de forma síncrona, así que no hay callback que entregar: el botón «Entregar
  callback» solo existe con la evaluación `PENDING` (`external-block.tsx:61`).
- El callback solo existe para los sufijos 90–96 (`MockAntifraudProvider.cs:37, 120-133`). Entre las
  18 alertas del corpus hay **una sola** en esa banda, `ORD_000091`, y su resto es impar, así que el
  callback la cierra en `DENIED`: coincide con el motor local y no hay divergencia.

Entonces, o el guion usa dos alertas, o muestra el mecanismo de callback desde `/import` —«Solicitar
evaluación externa del corpus» deja 21 pendientes y «Entregar callbacks» las cierra; pulsarlo dos
veces muestra el replay, «Los 21 callbacks ya se habían recibido» (`actions.ts:231-241`)— y la
divergencia en la alerta que venía mostrando. La segunda opción es mejor demo y no necesita nada
nuevo. El guion tiene que decir cuál.

**Minuto 8–9, «el veredicto terminal y su auditoría».** La auditoría que la pantalla muestra es la
transición, el instante y la nota (`review-panel.tsx:52-90`). **No muestra `explanationId`**, que es
el dato de la decisión 56 —«qué explicación tenía delante la analista»— y la razón por la que se
conserva una explicación desactualizada. El contrato lo expone y la guarda lo proyecta
(`AlertViews.cs:60-70`, `guards.ts:349-363`), pero ningún componente lo pinta. El guion puede
enseñarlo con un `curl GET /api/alerts/{id}` en la terminal C, o el diseño puede aceptar que en
pantalla se ve la mitad. Pintarlo es código de producto y D9 lo excluye: hallazgo, no tarea.

Dos cosas más del mismo bloque: una alerta revisada desaparece de la cola —el feed pide solo
`status=OPEN` (`alerts.ts:39-45`)— y no hay ninguna ruta que liste pedidos o alertas cerradas
(`/orders` es candidata de la Etapa 9). Si en la demo se navega después del veredicto, no se vuelve
a esa alerta salvo por URL. Y revisar con divergencia de banda exige la casilla de reconocimiento
(`review-form.tsx:79-91`); si el guion elige una alerta sin divergencia de banda, ese detalle no se
ve nunca, lo que es aceptable pero conviene saberlo.

**El guion es destructivo y de un solo uso.** El veredicto es terminal (decisión 35), la explicación
escrita no se regenera (`explanation-block.tsx:185-197`), y el seed es idempotente: repetirlo no
devuelve la base a cero. Cada ensayo consume el estado del anterior. Con la API en `5100` sobre
`salvo.db` no hay forma de volver a empezar, y **borrar `salvo.db` no es una opción**: `rm` está
denegado en `.claude/settings.json` y es una regla del usuario. El reset correcto es el que ya usa
el smoke: apuntar la API a otra base con `ConnectionStrings__SalvoDb` y migrarla con
`dotnet ef database update --connection` (`smoke-ui.sh:219-241`). D9 dice que `scripts/` se toca
«solo si el guion de demo necesita un paso que hoy no existe — y si lo necesita, es hallazgo». Lo
necesita: un `scripts/demo.sh` que levante API y consola en los puertos de desarrollo sobre una base
nueva con fecha, sin borrar nada, y deje la base vieja donde estaba. Debe entrar en el alcance de
`E8B` explícitamente.

### 5. D1, media-alta. La estructura del README omite un criterio de aceptación global del Blueprint, y una de sus frases contradice el §1

- **«El README distingue claramente simulación, sandbox y producción»** es un criterio global de
  aceptación (`Salvo-Blueprint.md:704`), una regla permanente de `AGENTS.md` («debe distinguir mock,
  sandbox y producción») y la «Regla de portfolio» de `Salvo-Portability.md:103-106`. Ninguna de las
  ocho secciones de D1 lo nombra. Puede vivir dentro de «Qué es y qué no» o de «Límites declarados»,
  pero tiene que aparecer como sección o subtítulo con ese nombre, porque es lo que un revisor del
  rubro busca primero: qué de todo esto habló alguna vez con Koin. Respuesta hoy: nada;
  `KOIN_MODE=sandbox` y `AI_PROVIDER=anthropic` **se niegan a arrancar**
  (`DependencyInjection.cs:95-106, 128-139`), que es mejor argumento que cualquier frase.
- **«Salvo es el lado del comercio, no un motor antifraude»** (`E8-DISENO.md:43`) contradice el §1
  del Blueprint: Salvo *tiene* un motor de reglas deterministas que calcula `localRiskScore`
  (`Salvo-Blueprint.md:37-40, 49`). Lo que se quiere decir es «no es un **proveedor** antifraude»:
  no evalúa para muchos comercios, es la consola de uno que consume a un proveedor. La distinción
  importa porque el pitch corregido es la frase que abre el README.
- **Blueprint §2 promete «README de portfolio con decisiones, métricas, arquitectura y guion de
  demo»** (`Salvo-Blueprint.md:76`) y D8 degrada las métricas a una línea de límites. Son
  compatibles solo si el bloque marcado de D2 trae la tabla de métricas del holdout con su
  advertencia —la misma `FIXTURE_CAVEAT` de `quality-section.tsx:28-31`—; si no, hay que corregir §2.
- **«Cómo se verifica» merece su lugar** (pregunta 1): el lector objetivo es una entrevista de
  ingeniería, y lo que distingue al proyecto no es qué hace sino cómo se sabe que lo hace. Pero
  merece el lugar si es concreto. Nombres que existen y que el README puede citar sin inventar nada:
  `DashboardEndpointTests.TheDashboardIsIndependentOfGroundTruth` (decisión 38: invertir las
  etiquetas no cambia el dashboard), `ExplanationIsolationTests.ExplainingEveryAlertChangesNoDecisionSurface`
  y `.InvertingEveryLabelChangesNoSummary` y `.NoTextTheEngineDidNotWriteReachesTheProvider`
  (los «cuatro tests» de `Salvo-Blueprint.md:663-664`),
  `ExplanationGroundingTests.AnInventedFigureIsRefusedAndNoTextIsStored`,
  `ExternalCallbackRaceTests.ACallbackThatArrivesBeforeTheIdentifierIsWrittenDownStillSettlesTheEvaluation`,
  `AlertReviewTests.TwoConcurrentReviewsLeaveOneVerdictAndExactlyOneAudit`,
  `TemporalRiskEngineTests.AddingFutureOrdersCannotChangeEarlierAssessments`,
  `DemoSeedTests.SeedIsFullyIdempotentAndContainsOnlySyntheticPseudonymousData`, `OpenApiDriftTests`,
  `boundary.test.ts` («no pasa ningún objeto a un componente cliente desde el detalle») y
  `scripts/smoke-ui.sh` con sus seis escenarios. Y el artefacto más fuerte del proyecto —las tablas
  de falsación de cada handoff, «mutación → test que cae»— hoy no se enlaza desde ningún lado
  público: el mapa de documentación tiene que incluir `Coordination/Handoffs/Claude.md` y
  `Coordination/Tasks/` (diseños y revisiones adversariales).
- **Las cinco decisiones.** El diseño no dice cuáles. Las que un revisor del rubro discutiría, con
  su número: 29 y 31 juntas (evaluación append-only con identidad de contenido, y la corrida vigente
  como lo único que define «vigente»); 33 (la alerta congela un snapshot y expone la divergencia en
  vez de actualizarse); 37 y 38 (el dashboard no lee la etiqueta, y eso se prueba invirtiéndola);
  45 y 47 (reservar antes de llamar, y que un fallo posterior al envío no cierre); 52 y 54 (grounding
  verificado sobre la salida, y ningún texto ajeno al motor entra al modelo). El resto son
  consecuencias de esas.

### 6. D1 §6, media. «Copiados del artículo para revisores» cita un documento que no existe en el repositorio ni en `_local/`

`grep -ri "para revisores\|artículo"` sobre todo el árbol, `_local/` incluido, encuentra tres
menciones: el propio diseño (`E8-DISENO.md:52`), el checklist de la Etapa 9 en el Progress
(`Salvo-Progress.md:269`: «todos los documentos y el artículo para revisores») y una palabra suelta
en la revisión de E7. No hay ningún archivo con ese contenido. Si existe en un chat o en el Proyecto
de Claude.ai, es una foto sin fecha y sin verificar, exactamente lo que D7 prohíbe usar como fuente.
Los límites que sí están escritos y contrastados, para redactar esa sección desde ellos:

- `FIXTURE_CAVEAT` (`quality-section.tsx:28-31`): la fixture recupera sus propias etiquetas; F1 mide
  el pipeline, no el criterio. Confirmado en la base: sobre la corrida 7, las 300 etiquetas y el
  flag `score >= 60` coinciden en los 300 casos (282/282 y 18/18).
- `Workboard.md:79-90`: un solo arquetipo de fraude, tres reglas que el corpus nunca dispara,
  `unusual_hour` estructuralmente inalcanzable (13,8 %).
- `Salvo-Blueprint.md:78-97`: diferido y fuera de alcance; `§5.3:353-362`: qué falta para un sandbox.
- `DependencyInjection.cs:95-106, 128-139`: sin adaptador Anthropic ni Koin; nombrarlos falla al
  arrancar.
- Decisión 53: la IA no recomienda acciones. `console-header.tsx:38-40`: «Datos sintéticos · sin
  autenticación · uso local».

### 7. D2, media. El bloque marcado del README no alcanza a donde viven hoy las cifras del corpus, y el guion las va a citar también

Pregunta 2. D2 dice que «fuera de ese bloque, el README no menciona ninguna cifra del corpus». La
regla es correcta para el README y ciega para todo lo demás. Inventario de cifras del corpus que la
Etapa 9 va a invalidar y que no están en ningún bloque marcado:

| Dónde | Qué dice |
| --- | --- |
| `frontend/src/app/import/page.tsx:66` | «Trescientos pedidos sintéticos con sus etiquetas de fraude» — texto de pantalla, saldrá en la captura 5 |
| `MockAntifraudProvider.cs:21-23` y `ExternalEvaluationIsolationTests.cs:166-171` | 300 referencias → 225 aprobadas, 45 denegadas, 21 pendientes, 9 en error; un test lo fija |
| `ExplanationGoldenTests.cs:33, 52` y `ExplanationFactsTests.cs:18` | Textos dorados sobre `ORD_000011` (score 90, tres reglas) y `ORD_000171` (score 60, ratio `15.0`) |
| `frontend/src/app/dashboard/page.test.tsx:65, 98-101, 195` | «300 pedidos en la corrida», la suma sin unidad `3.942.246` |
| `scripts/smoke-ui.sh:318` | `expect_no_text "/dashboard" "3.942.246"` |
| `Salvo-Blueprint.md:121-122` y decisión 19 (`:728`) | 300 pedidos, 120 días, 18 fraudes y 282 legítimos |
| `Workboard.md:79-90` | 18 alertas con `amount_anomaly` y `foreign_country`; 13,8 % |
| `Salvo-Progress.md:46-48` | 18 alertas (13 `MEDIUM`, 5 `CRITICAL`) y los conteos de tests de cada cierre |

Y dos comentarios de código que ya están mal hoy, antes de la Etapa 9:
`quality-section.tsx:26` («scheduled work for stage 8») y `SignalFacts.cs:22` («the stage 8
candidate») nombran a la Etapa 8 para lo que el Blueprint movió a la 9 (`Salvo-Blueprint.md:675-684`).
Un README que diga «la Etapa 9 enriquece la fixture» al lado de un comentario que dice «stage 8» es
la contradicción que D7 existe para cazar.

Qué hacer con esto sin salirse de E8: D2 mantiene la regla para el README y **`E8A` entrega este
inventario como parte de su tabla de verificación**, para que la Etapa 9 empiece con la lista y no
con un `grep`. El guion de D6 va a decir «trescientos pedidos» y «dieciocho alertas» en el minuto 1:
lleva los mismos marcadores HTML que el README, porque es el mismo problema. Los diagramas no
deberían llevar cifras del corpus, y conviene decirlo como regla: un diagrama con «300» dibujado
adentro se regenera a mano.

### 8. D3 y D4, media. El «recorrido de un pedido» de ocho pasos no coincide con el flujo de nueve del Blueprint §3, que además sigue sin el paso de la corrida ni el de la explicación; y el ciclo de vida de la explicación no vale su espacio

Pregunta 3.

- **El flujo del §3** (`Salvo-Blueprint.md:105-115`) tiene nueve pasos, y el paso 2 —«Salvo procesa
  los pedidos en orden temporal»— sugiere que importar procesa. La decisión 39 lo cambió: la corrida
  es una acción explícita desde `/import` y «importar no procesa» (`Salvo-Blueprint.md:222-224`,
  `import/page.tsx:44-48`). Tampoco hay paso de explicación. El diagrama del README va a ser la
  primera representación correcta del recorrido, y el §3 tiene que corregirse en la misma pasada,
  por el coordinador. Los ocho pasos que sostiene el código: cargar o importar → correr el scoring
  (baseline solo con historia anterior) → evaluación append-only y corrida vigente → alerta (una
  `OPEN` por pedido, escalada por banda) → explicación verificada → opinión externa (reserva,
  callback, reconciliación) → veredicto terminal con auditoría → dashboard sin etiquetas.
- **Las tres fuentes de verdad.** Que el diagrama incluya `ScoringRun` y `run_evaluations` como lo
  que define «vigente»: es la pieza menos intuitiva del modelo, la que produce la divergencia, y la
  que ningún texto explica bien en una lista. Y que muestre `AlertReview.explanationId`, que es el
  hilo entre la tercera fuente y la explicación.
- **La máquina de estados externa.** El §4.5 ya tiene la tabla de transiciones
  (`Salvo-Blueprint.md:249-258`). El diagrama vale si muestra lo que la tabla no: las dos rutas de
  correlación y que `TIMEOUT`, `PROVIDER_ERROR` e `INVALID_RESPONSE` dejan la fila `PENDING` mientras
  `UNREACHABLE` y `PROVIDER_REJECTED` la cierran (`ExternalEvaluationErrorCode.cs:8-12`).
- **El ciclo de vida de la explicación** son tres estados y una flecha de vuelta
  (`ExplanationStatus.cs:7-11`). Cabe en una oración y el criterio del diseño —«un diagrama que se
  puede leer en una lista no vale su espacio»— lo descarta. Lo que sí vale un dibujo es **cómo se
  verifica una explicación**: hechos construidos en Domain a partir de la evaluación y el pedido →
  tokenizador declarado → igualdad por redondeo → el texto rechazado no se persiste ni se registra
  (`ExplanationFacts`, `NumberTokenizer`, `ExplanationGrounding`). Es el mecanismo más distintivo del
  proyecto y el más difícil de creer sin verlo.
- **Mermaid** se renderiza en GitHub y en Obsidian, y un error de sintaxis se renderiza como bloque
  de código sin aviso. La tabla de verificación necesita una fila por diagrama con «renderizado en
  la vista previa de GitHub», porque nada del repositorio lo comprueba.

### 9. D7, media. La tabla de verificación es exigible solo si una parte de ella se ejecuta

Pregunta 5. Una tabla que se llena a mano se llena sin abrir los archivos: ya pasó tres veces. Lo
que la vuelve exigible es separar las afirmaciones por cómo se verifican:

- **Rutas, comandos y nombres de tests: por script.** Un `scripts/check-docs.sh` en bash y Node —sin
  dependencias, como el smoke— que extraiga cada ruta entre acentos graves del README y afirme que
  existe, y que para cada nombre de test citado haga `grep` en `backend/tests` y `frontend/src`. Es
  el mismo patrón que `OpenApiDriftTests` y `check-openapi-types.mjs`: la deriva se detecta, no se
  promete. Puede correr en `check.sh` sin costo apreciable.
- **Afirmaciones de comportamiento: por test nombrado.** La tabla lleva una columna «test que lo
  afirma». «Un callback duplicado no repite efectos» sin test al lado no entra al README.
- **Cifras: por comando con su salida pegada**, no por `grep`. Los conteos de tests cambian en cada
  tarea —239 .NET y 197 de frontend al cerrar `E7B`, 243 y 209 en el handoff de `E7D`— y un conteo
  estático no coincide con la corrida: `grep` da 213 `[Fact]` y 6 `[Theory]` con 30 `[InlineData]`,
  y el número real sale solo de `dotnet test`. Recomendación más simple: **el README no cita
  cantidades de tests.** Cita los comandos. Una cantidad es cierta el día que se escribe y falsa la
  semana siguiente.
- La tabla tiene el formato que el proyecto ya usa para las falsaciones: «afirmación → archivo:línea
  o comando → resultado». Va en el handoff de `E8A`, no en un documento aparte.

### 10. D9, media. La partición no dice quién corrige el estado canónico desfasado, y casi todo lo desfasado es del coordinador

`E8A` incluye «la actualización de `Salvo-Overview.md` y `Salvo-MOC.md` si quedaron desfasados»
(`E8-DISENO.md:129-131`). Quedaron, y no son los únicos: la segunda mitad de este informe lista
falsedades en el Blueprint (cabecera, §3, §9), en el Progress (tablero maestro, checklist de E6,
decisiones abiertas), en el Workboard (notas rotuladas «Etapa 8» que son de la 7), en `AGENTS.md`
(«No iniciar la Etapa 7», dos veces), en Getting-Started, Portability y Project-Instructions. Todos
son estado canónico: los modifica el coordinador (`Coordination/README.md:13-20`) y `CLAUDE.md` le
prohíbe a Claude tocar Workboard y Progress en trabajo paralelo. En modo secuencial, Claude los
actualiza «únicamente cuando el task brief le asigna explícitamente la coordinación»
(`Claude-Workflow.md`).

Un README reescrito que enlace a un Blueprint cuya cabecera dice «Etapa 4 en ejecución» reproduce el
problema que la etapa quiere resolver. Corrección: **una pasada del coordinador sobre los documentos
canónicos, con el inventario de abajo, antes de despachar `E8A`** —en el mismo commit que apruebe el
diseño v2, o como ítem propio `E8-SYNC`—, y que el brief de `E8A` diga cuáles documentos derivados
puede tocar Claude (Overview, MOC, Getting-Started, Portability) y cuáles no.

### 11. D6, baja. Diez minutos es correcto; el reparto necesita una precondición y dos ajustes

Pregunta 4. Diez minutos es lo que dura una demo en una entrevista de ingeniería, y el criterio de
cerrar con límites es el correcto. Ajustes:

- **Minuto 0 tiene una precondición que no está escrita:** base nueva, corpus cargado y corrida
  ejecutada antes de empezar, o el minuto 1–3 se gasta en clics de carga. Si la carga se muestra en
  vivo, son dos clics y dos segundos; sobra un minuto ahí y falta en 5–7.
- **Minuto 5–7** es el bloque más difícil de explicar y el que más distingue: conviene llevar
  preparada la frase del tokenizador («un texto que diga “48 veces” se rechaza porque 48 no es un
  hecho de la evaluación») y el nombre del test que lo prueba.
- **Minuto 7–8** con la resolución del hallazgo 4: callbacks desde `/import`, divergencia en la
  alerta.
- **Minuto 9–10**: la sección «Calidad del criterio» del dashboard, con F1 1,00 y la advertencia
  debajo, es la mejor manera de decir un límite sin que parezca disculpa.
- Cada bloque debería tener una línea «si falla»: la consola dice «No se pudo contactar a la API»
  en las cuatro rutas de datos cuando la API no está (`smoke-ui.sh:483-487`), y ese estado también
  vale mostrarlo.

### 12. D5, baja. Detalles de las capturas que conviene fijar antes, no después

- Nombres fijos y numerados (`01-cola.png` … `06-divergencia.png`), ancho fijo y escala declarados
  (por ejemplo 1280 px y factor 2), para que una regeneración se pueda comparar.
- Cada captura muestra instantes («Abierta el…», «Vigente desde la corrida #1, …», «Redactada … el
  …») que cambian en cada regeneración. Se declara junto a la nota de regenerables.
- El README necesita un texto alternativo por captura; la «lista de tomas fija» debería incluirlo.
- El script no borra nada: `rm` está denegado. Sobrescribe en el mismo nombre.
- Seis PNG a ~200–500 KB cada uno son un par de MB por regeneración en la historia de Git. Aceptable
  si se regeneran cuando cambia la interfaz y no en cada tarea.

---

## Segunda mitad — Inventario: qué es falso hoy

Criterio, para que la lista sea inventario y no opinión: **falso** es lo que el código o Git
contradicen hoy; **desactualizado** es lo que fue cierto y dejó de serlo; **impreciso** es lo que
induce una lectura falsa sin ser falso; **omisión** es lo que falta en un documento que dice
contenerlo. La fecha de cabecera de cada documento se contrasta con la fecha del último commit que
lo tocó (`git log -1 --date=short -- <archivo>`).

### `README.md` (último commit `c87a6d1`, 2026-09-03)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 6 | «cinco etapas integradas y verificadas» | Siete: Etapas 1–7 | `Salvo-Progress.md:12-13`; merges `bca2c46`, `a412693`, `82f2487`, `ac11015`, `0d117dd`, `b4aac6b` en `git log` | Falso |
| 9-10 | «Las Etapas 6 a 8 —proveedor externo, explicabilidad y portfolio— están pendientes» | 6 y 7 completas; la 8 se llama «El argumento del proyecto» y existe una Etapa 9 | `Salvo-Blueprint.md:649-687` | Falso |
| 6-9 | Enumera lo que el sistema hace | Falta todo lo de E6 y E7: evaluación externa mock con reserva, callback autenticado, reconciliación, y explicación verificada sobre la salida | `ExternalCallbackEndpoints.cs`, `ExplanationEndpoints.cs` | Omisión |
| 39 | `/import`: «cargar el corpus demo o importar un archivo, y ejecutar la corrida» | También pide la evaluación externa del corpus entero y entrega los callbacks | `import/page.tsx:97-113` | Omisión |
| 47-48 | Con `DemoData:Enabled` «aparecen el botón de carga demo y la sección de calidad del criterio» | Y los dos botones del proveedor externo, que van en la misma bandera | `SystemEndpoints.cs:19-23` | Omisión |
| 66-68 | La compuerta «restaura NuGet…, verifica que los tipos generados… estén al día, compila…» | El primer paso es la verificación de tipos, antes de restaurar; el contenido es correcto, el orden no | `scripts/check.sh:14-26` | Impreciso |
| 76-79 | El smoke «comprueba las cuatro rutas en tres escenarios» | Cinco rutas (`/`, `/import`, `/alerts`, `/alerts/[id]`, `/dashboard`) en seis escenarios (1, 1b, 1c, 1d, 2, 3); 43 comprobaciones en el handoff de `E7D` | `scripts/smoke-ui.sh:11-18, 297-487`; `Coordination/Handoffs/Claude.md` (entrega `E7D`) | Falso |
| 83-88 | Arquitectura | No nombra los dos puertos que la etapa 6 y 7 agregaron (`IAntifraudProvider`, `IExplanationProvider`) | `Salvo-Blueprint.md:370-384` | Omisión |
| 100 | `AGENTS.md` = «Instrucciones para Codex» | `CLAUDE.md` lo importa: rige a los dos agentes | `CLAUDE.md:6` | Impreciso |
| 92-103 | Mapa de documentación | Faltan `ClaudeAgent/Claude-Model-Policy.md`, `DesignAgent/Salvo-Project-Instructions.md`, `Coordination/Workboard.md`, `Coordination/Tasks/` (diseños y revisiones adversariales) y `Coordination/Handoffs/` | `ls` | Omisión |
| 105-107 | «Anthropic es el proveedor de IA previsto para una etapa posterior» | La Etapa 7 ya se ejecutó con proveedor determinista; Anthropic es una decisión aparte, y `AI_PROVIDER=anthropic` hoy falla al arrancar | `Salvo-Progress.md:249`; `DependencyInjection.cs:95-106` | Desactualizado |

### `DesignAgent/Salvo-Blueprint.md` (último commit `3cbdd07`, 2026-09-06)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 4 | «Estado del proyecto: Etapa 3 integrada y verificada; Etapa 4 diseñada y aprobada, en ejecución» | Etapa 7 completa y verificada. Es la cabecera de la fuente de verdad | `Salvo-Progress.md:12-13` | Falso |
| 5 | «Última actualización: 2026-09-02» | Último commit 2026-09-06; la bitácora llega al 2026-09-06 (`:767`) | `git log` | Falso |
| 105-115 | Flujo principal de nueve pasos; el 2 dice «Salvo procesa los pedidos en orden temporal» | La corrida es una acción explícita desde `/import` (decisión 39); no hay paso de explicación | `import/page.tsx:44-48`; decisiones 39, 51 | Desactualizado |
| 72-73, 651 | «Escenarios externos simulados: `approved`, `denied` y `received`» | El código no usa `received` en ningún lado: los estados son `PENDING/APPROVED/DENIED/ERROR` y los resultados del adaptador son seis; el §5.1:319 explica la equivalencia | `ExternalEvaluationStatus.cs:9-12`; `ExternalProviderOutcome.cs:14-30`; `grep -ri received` sin resultados en `backend/src` | Impreciso |
| 543-572 | §9, variables del núcleo y de Koin | Falta `SALVO_CALLBACK_SHARED_SECRET`, la variable que el endpoint de callback lee de verdad; `KOIN_CALLBACK_SHARED_SECRET` (`:571`) no la lee nada | `ExternalCallbackEndpoints.cs:19`; `.env.example:10, 22`; `grep -rn CALLBACK_SHARED_SECRET backend/src` | Falso por omisión |
| 76 | «README de portfolio con decisiones, métricas, arquitectura y guion de demo» | D8 saca las métricas del README salvo como límite; hay que reconciliar | `E8-DISENO.md:113-121` | Tensión con el diseño |

Lo que sí sostiene el código y conviene saber que es cierto: `:121-122` (300 pedidos, 18 fraudes,
282 legítimos: la fixture tiene exactamente eso; ventana del 2026-05-01 al 2026-08-28), `:225` (feed
por score vigente: `alerts.ts:43`), `:446-449` (proveedor y estados: `CHECK` de la tabla), `:370`
(hay un Mermaid), `:786` («README… crecerá en la Etapa 8»).

### `DesignAgent/Salvo-Progress.md` (último commit `3cbdd07`, 2026-09-06)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 4 | «Última actualización: 2026-09-02» | El registro de actividad llega al 2026-09-06 y el commit también | `:362`, `git log` | Falso |
| 49 | Tablero maestro: «7 \| Explicabilidad \| **Pendiente** \| … \| Pendiente» | Completada en cuatro merges; contradice `:12-13` del mismo archivo | merges `82f2487`, `ac11015`, `0d117dd`, `b4aac6b` | Falso |
| 50 | «8 \| Calidad y portfolio \| Pendiente» | Se llama «El argumento del proyecto» y su compuerta cambió | `Salvo-Blueprint.md:666-673` | Desactualizado |
| 49-51 | El tablero no tiene fila para la Etapa 9 | Existe desde `3cbdd07` | `Salvo-Blueprint.md:675-687` | Omisión |
| 47 | Evidencia de la Etapa 5: «Merges `5f48db0`, `278e100` y el de `E5C`» | El merge de `E5C` es `8198fd5`; el marcador nunca se completó | `git log` | Omisión |
| 143-146 | «Etapa 5 permanece pendiente: requiere diseño, brief y autorización independientes» (dentro de la sección de Etapa 1) | Etapas 5, 6 y 7 completas | `:12-13` | Desactualizado |
| 218-223 | Seis ítems sin marcar de la Etapa 6 debajo de los ocho marcados («Implementar `IAntifraudProvider`», «Correlación por referencias»…) | Los seis están hechos: `IAntifraudProvider.cs`, `MockAntifraudProvider.cs`, `ExternalEvaluationLookup.cs`, `ExternalCallbackEndpoints.cs`, `ReconcileExternalEvaluationsHandler.cs`, `ExternalEvaluationErrorCode.cs` | archivos | Desactualizado (checklist reemplazado y no borrado) |
| 278 | «Anthropic real \| Diferida \| Etapa 7» | La Etapa 7 cerró sin Anthropic; la decisión es aparte | `:249`, decisión 6 | Desactualizado |
| 282 | «Primer trabajo paralelo Codex–Claude \| Diferida \| Después del scaffold» | No ocurrió en siete etapas; Codex no trabaja desde E3 | `Workboard.md:94-112` | Fila muerta |

### `Coordination/Workboard.md` (último commit `3cbdd07`, 2026-09-06)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 51-61 | «Notas para los briefs de la **Etapa 8**»: copia de `salvo.db` antes de migrar, `AlertSchemaTests` se extiende, `ArchitectureSmokeTests` suma `AlertExplanation`, recaptura de OpenAPI, reservas de `E7A`/`E7B` | Son las notas de la Etapa 7 con el rótulo cambiado. Para la Etapa 8 son falsas: no hay migración, ni contrato, ni `E7A` | `E8-DISENO.md:133-135` («ninguna toca código de producción») | Falso |
| 19-26 | «Cola próxima» contiene solo tareas `Verificada` | La cola está vacía; las cuatro filas de E7 pertenecen a «Historial integrado» | `:94-112`, que no las tiene | Desactualizado |
| 3 | «Siete etapas integradas y verificadas» | Cierto (1–7) | — | Cierto |

### `AGENTS.md` (último commit `f82dfd8`, 2026-09-04)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 26-56 | «Estado actual» termina en «Etapa 6 completada» | Falta «Etapa 7 completada» con sus cuatro ítems; faltan la Etapa 8 acotada y la 9 creada | `Salvo-Progress.md:12-14` | Desactualizado |
| 48 y 56 | «No iniciar la Etapa 7 sin petición o aprobación explícita del usuario», dos veces | La Etapa 7 está completa; la línea vigente sería «No iniciar la Etapa 8…» | merges de E7 | Falso |

### `DesignAgent/Salvo-Overview.md` (último commit `4053f8f`, 2026-08-31)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 56-64 | Tabla «Estado»: Etapa 1 integrada, «Etapa 2 integrada en `main` mediante `4b7bf54`», y nada más | Etapas 3–7 integradas | `Salvo-Progress.md:44-50` | Falso por omisión: la tabla se llama «Estado» |
| 66-69 | «Próximo paso: definir y aprobar el brief detallado de Etapa 3… la integración de E2 no autoriza automáticamente la siguiente etapa» | La próxima etapa es la 8 y su diseño está en revisión | `Salvo-Progress.md:14`; este informe | Falso |
| 35 | «`IAntifraudProvider` con mock local para `approved`, `denied` y `received`» | Bandas del mock: aprobado, denegado, pendiente, error (dos clases) | `MockAntifraudProvider.cs:30-40` | Impreciso |
| 4 | 2026-08-31 | Coincide con el commit; el problema es el contenido | — | — |

### `DesignAgent/Salvo-MOC.md` (último commit `df26f00`, 2026-09-03)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 4 | «Última actualización: 2026-08-31» | Modificado el 2026-09-03 sin subir la fecha | `git log` | Falso |
| 44-55 | Roadmap MVP: «7. Explicabilidad determinista y, si se aprueba, Anthropic. 8. Calidad y portfolio.» | Etapa 8 = El argumento del proyecto; Etapa 9 = Corpus, idiomas y cierre | `Salvo-Blueprint.md:666-687` | Desactualizado |
| 9-24 | «Índice principal de la documentación» | No lista `ClaudeAgent/Claude-Workflow.md`, `Claude-Handoff-Template.md`, `Coordination/Workboard.md`, `Coordination/Handoffs/` ni `Coordination/Tasks/` | `ls` | Omisión |
| 28-42 | Secciones del Blueprint | Coinciden con los encabezados 1–15 | `Salvo-Blueprint.md` | Cierto |

### `DesignAgent/Salvo-Getting-Started.md` (último commit `4053f8f`, 2026-08-31)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 10-13 | «Etapa 2 está integrada… No hay tareas activas; Etapa 3 no comienza sin autorización explícita» | Etapa 7 completa | `Salvo-Progress.md:12-14` | Falso |
| 50-59 | «Orden de construcción», ocho ítems; el 8 es «Métricas finales y README» | Nueve etapas; la 8 es el argumento y la 9 el corpus | `Salvo-Blueprint.md:666-687` | Desactualizado |
| 83-95 | Tabla de comandos | Faltan `npm run api:types:check`, `npm run api:capture` y `./scripts/smoke-ui.sh`; «`npm run check` \| Typecheck, ESLint y tests UI» omite la verificación de tipos generados, que es su primer paso | `frontend/package.json:14-21`; `scripts/smoke-ui.sh` | Omisión |
| 11 | «cerrada mediante PR #2 y verificada sobre `main` en `66f0949`» | `66f0949` es el merge del PR #2 | `git log --all` | Cierto |
| 22 | «Git 2.32.0» | `git --version` = 2.32.0 | — | Cierto |
| 97-105 | Rewrite de `/api/health`, API sin migración automática, `POST /api/order-imports` con `file` y `format` | Cierto | `next.config.ts:9-16`; `Program.cs`; `OrderEndpoints.cs:70, 87` | Cierto |

### `DesignAgent/Salvo-Portability.md` (último commit `8951fa2`, 2026-09-05)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 4 | «Última actualización: 2026-08-31» | Modificado el 2026-09-05 (el párrafo de grounding, `:48-52`) sin subir la fecha | `git log` | Falso |
| 14 | «Agente que construye \| Codex \| Claude Code, Cursor u otro» | Rotulado «elección inicial», pero desde E4 todo lo construyó Claude; Codex hizo E0–E3 | `Workboard.md:94-112` | Impreciso |
| 33-39 | Contrato conceptual `IExplanationProvider.ExplainAsync(...) → ExplanationResult` | El puerto real devuelve `ExplanationDraft` y declara `Provider` y `TemplateVersion` | `IExplanationProvider.cs:19-45` | Impreciso (dice «conceptual») |
| 67-73 | `AI_PROVIDER="anthropic"` como configuración «posterior» | Hoy ese valor se niega a arrancar; el documento no lo dice | `DependencyInjection.cs:95-106` | Omisión |
| 75-84 | Variables de Koin | Falta `KOIN_CALLBACK_SHARED_SECRET` (`.env.example:22`) y no menciona `SALVO_CALLBACK_SHARED_SECRET`, la que el código lee | `.env.example:10`; `ExternalCallbackEndpoints.cs:19` | Omisión |

### `DesignAgent/Salvo-Project-Instructions.md` (último commit `df26f00`, 2026-09-03)

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| 4 | «Última actualización: 2026-09-01» | Modificado el 2026-09-03 | `git log` | Falso |
| 36-37 | En el bloque para pegar: «Anthropic está previsto para una etapa posterior» | Misma imprecisión que el README `:105-107` | `DependencyInjection.cs:95-106` | Desactualizado |

### `ClaudeAgent/README.md` (último commit `6afa4c4`, 2026-09-02) y `Claude-Model-Policy.md`

| Línea | Dice | Realidad | Fuente | Tipo |
| --- | --- | --- | --- | --- |
| `README.md:3, 51` | «Claude Code 2.1.257 instalado y verificado» | `claude --version` = 2.1.263. Un documento que fija una versión que se autoactualiza queda falso solo | terminal | Desactualizado |
| `README.md:4` | 2026-09-01 | Commit 2026-09-02 | `git log` | Impreciso |
| `Claude-Model-Policy.md:4` | 2026-09-05 | Su último commit es del 2026-09-04: fecha posterior al commit que la escribió | `git log` | Inconsistente |
| `README.md:52-54, 58-72` | Cinco imports, tres comandos, permisos denegados | Cierto | `CLAUDE.md`, `.claude/commands/`, `.claude/settings.json` | Cierto |

### Código con afirmaciones sobre el roadmap

| Archivo | Dice | Realidad | Tipo |
| --- | --- | --- | --- |
| `frontend/src/app/dashboard/quality-section.tsx:26` | «Enriching the fixture … is scheduled work for stage 8» | Es la Etapa 9 | Desactualizado |
| `backend/src/Salvo.Domain/Explanations/SignalFacts.cs:22` | «the stage 8 candidate where the engine emits these fields» | Es la Etapa 9 | Desactualizado |
| `frontend/src/components/console-header.tsx:10-12` | «`/import` and `/dashboard` are linked before they exist» | Existen desde E5C | Desactualizado (comentario) |

### El propio `E8-DISENO.md`

| Línea | Dice | Realidad | Tipo |
| --- | --- | --- | --- |
| 27-29 | El README «dice “cinco etapas” y da las 6 a 8 por pendientes» | Cierto (`README.md:6-10`) | Cierto |
| 37 | «No un catálogo de las 58» | 58 entradas hoy (`Salvo-Blueprint.md:710-767`); D2 agrega la 59 | Cierto |
| 52 | «Copiados del artículo para revisores» | No existe (hallazgo 6) | Falso |
| 60 | «El Blueprint ya usa uno» (Mermaid) | `Salvo-Blueprint.md:370` | Cierto |
| 71 | «Los ocho pasos» del recorrido | El §3 tiene nueve y ninguno es la corrida (hallazgo 8) | Impreciso |
| 78-80 | `/alerts/[id]` «pedido, snapshot, evaluación vigente, evaluación externa, explicación y panel de veredicto» | Ese es exactamente el orden de `page.tsx:73-96` | Cierto |
| 85-88 | Chrome conectado | No hay Chrome (hallazgo 1) | Falso |
| 116-117 | «hoy F1 vale 1,00 porque la fixture recupera sus propias etiquetas» | Sobre la corrida 7, etiqueta y flag coinciden 300 de 300 | Cierto |
| 138-141 | «`Salvo-Progress.md` y `Coordination/Workboard.md`: ya aplicados» | El bloque «Estado general» sí; el tablero maestro (`:49-51`) y las notas del Workboard (`:51-61`) no | Parcialmente falso |

Pregunta 6, en una frase: además de «cinco etapas» y «6 a 8 pendientes», el README es falso en el
smoke (`:76-79`), incompleto en `/import` y en la bandera de demo, y desactualizado en Anthropic; y
el documento más grave no es el README sino la cabecera del Blueprint, que declara la Etapa 4 en
ejecución en la fuente de verdad.

---

## Tercera parte — Playwright, o cómo sacar las capturas sin mentir

### Qué prohíbe el proyecto, exactamente

No hay una regla permanente contra dependencias nuevas. `AGENTS.md` exige versiones exactas y
lockfile versionado; la plantilla de brief tiene un campo «Instalación o actualización de
dependencias» (`Task-Brief-Template.md:50`) que cada tarea completa, y el silencio significa «no».
El precedente es E5: el diseño aprobó `openapi-typescript` con nombre y motivo («Es la única
dependencia nueva de la etapa y se aprueba aquí», `E5-DISENO.md:213`) y los briefs lo heredaron.
Playwright en particular fue **diferido, no prohibido**: «quedaría como dependencia nueva y no se
aprueba en esta etapa» (`E5-DISENO.md:355-356`), y `E5C` cerró con «Si algo pareciera exigirla,
detenerse y consultar» (`E5C-IMPORT-DASHBOARD.md:146`). Esta es la consulta.

### Qué necesita interacción de verdad

Fui toma por toma a ver qué estado hay detrás de cada pantalla:

| Toma | Cómo se produce el estado | Necesita navegador que interactúe |
| --- | --- | --- |
| 1 `/alerts` | seed + corrida por API | No: la página lo lee de la base |
| 2 `/alerts/[id]` completa | ídem; el id sale de `GET /api/alerts?status=OPEN` | No |
| 3 bloque de explicación | `POST /api/alerts/{id}/explanation` persiste la fila `READY`; la página la muestra igual que tras el clic | No. Solo el aviso posterior al clic es efímero, y no está en la lista |
| 4 `/dashboard` | ídem | No |
| 5 `/import` con filas rechazadas | El resultado vive en `useActionState` (`import-form.tsx:18`, `action-state.ts:14-32`). **No hay tabla de importaciones**: `.tables` de la base no tiene ninguna, y la página de `/import` no lee ningún resultado previo | **Sí**: hay que elegir un archivo y enviar el formulario en un navegador |
| 6 divergencia de criterio | `POST /api/orders/{orderId}/external-evaluations` persiste; la página muestra el aviso | No |

Cinco de seis salen con `curl` y una captura de página estática. La sexta no tiene atajo honesto: la
única forma de fotografiar «Registros rechazados (5)» es enviar el formulario. Componer la pantalla
de otro modo sería fabricar una captura.

### Las opciones sin dependencia, en esta máquina

- **Captura estática de las cinco tomas:** Brave 145 en modo headless
  (`--headless --screenshot=… --window-size=…`) contra la consola levantada como en el smoke. Sin
  npm, sin nada. Sirve para 1, 2, 3, 4 y 6.
- **La toma 5 con Chrome DevTools Protocol y Node puro.** Brave con `--remote-debugging-port`, y un
  script de ~80 líneas con el `WebSocket` global de Node —verificado: Node 24.20.0 expone `WebSocket`
  y `fetch`— que haga `Page.navigate`, `DOM.setFileInputFiles` sobre `input[name=file]`,
  `Runtime.evaluate` para enviar el formulario, espere el `role="alert"` y `Page.captureScreenshot`.
  Funciona y no agrega nada a `package.json`. Sus costos: el navegador **no está fijado** —Brave se
  autoactualiza y hoy es 145.1.87.190—, no existe en otra máquina, y el script de CDP es código
  propio que hay que mantener, más frágil que lo que Playwright ya resuelve (esperas, selectores,
  escala). Es coherente con el precedente del smoke —bash y `node:sqlite` en vez de una
  dependencia—, pero el smoke lee HTML con `curl`; esto conduce un navegador.
- **`safaridriver`** viene con macOS (`/System/Cryptexes/App/usr/bin/safaridriver`) y habla W3C
  WebDriver por HTTP, alcanzable con `fetch`. Requiere `safaridriver --enable` una vez, y la subida
  de archivos por WebDriver en Safari no la verifiqué: no conviene apoyar la toma 5 en algo no
  comprobado.
- **Capturas a mano** para la toma 5 y script para las otras cinco. Es honesto, y rompe el criterio
  de D5 en una sola toma. Vale como plan de contingencia, no como diseño.

### Playwright, y en qué términos

Se justifica, con tres condiciones que salen del propio proyecto:

1. **En un paquete propio, no en `frontend/`.** Meterlo en `frontend/package.json` lo pone en
   `package-lock.json` —archivo serializado por las reglas de coordinación— y en cada `npm ci`, que
   es el paso previo a la compuerta (`README.md:62`). Además, el paquete `playwright` puede
   descargar navegadores al instalarse —a diferencia de `playwright-core`—; no lo verifiqué sin red
   y el brief tiene que hacerlo. Un `tools/capturas/package.json` con su propio lockfile deja la
   compuerta, el frontend y su lockfile exactamente como están. El script `scripts/capturas.sh`
   hace `npm ci --prefix tools/capturas` y `npx playwright install chromium` de forma explícita,
   igual que `check.sh` hace `dotnet tool restore`.
2. **Versión exacta**, como toda dependencia del repositorio, y Chromium el que esa versión fija:
   es lo que la vuelve reproducible en otra máquina, que es el argumento entero a favor.
3. **Fuera de `check.sh` y de los tests.** «Tests sin red» sigue siendo cierto: las capturas no son
   tests y no corren en la compuerta. La descarga de Chromium es del orden de 150 MB, una vez por
   máquina; hay que decirlo en el README junto al comando.

El script reutiliza el diseño del smoke: base temporal migrada con `--connection`, API y consola en
puertos propios, seed y corrida por `curl`, y el estado de cada toma preparado por la API
(explicación, evaluación externa) antes de fotografiar. Playwright interviene en la toma 5 y en las
capturas; nada más. La muestra con filas rechazadas tiene que estar versionada (hallazgo 3), y el
script no borra nada: sobrescribe.

**Recomendación:** aprobar Playwright en el diseño v2 en esos términos, con nombre y motivo, como
E5 hizo con `openapi-typescript`. La alternativa sin dependencia existe y la describí completa para
que la decisión sea informada; cubre cinco tomas gratis y la sexta con un navegador sin fijar y
código propio, que es lo que Playwright evita. Si el usuario prefiere no agregar la dependencia, la
opción coherente es «cinco por script con Brave headless, la quinta a mano y declarada así», no el
script de CDP.

---

## Las seis preguntas, en corto

1. **Estructura.** «Cómo se verifica» se queda y se vuelve concreta con nombres de tests (hallazgo
   5). Falta la sección o subtítulo «simulación, sandbox y producción», que es criterio de aceptación
   del Blueprint. El pitch corrige «motor» por «proveedor». El mapa de documentación suma `Tasks/`,
   `Handoffs/`, Workboard, Model-Policy y Project-Instructions.
2. **Bloque marcado.** Alcanza para el README y para nada más. Las cifras del corpus viven en
   pantalla, tests, smoke, comentarios y documentos canónicos (hallazgo 7); `E8A` entrega el
   inventario y el guion lleva los mismos marcadores.
3. **Diagramas.** Tres de cuatro son los correctos; el ciclo de vida de la explicación se cambia por
   «cómo se verifica una explicación». El recorrido tiene que corregir al §3 del Blueprint y no
   copiarlo (hallazgo 8).
4. **Guion.** Diez minutos sí; precondición de base nueva, callbacks desde `/import`, divergencia en
   la alerta, auditoría con `curl` o declarada incompleta, y un `scripts/demo.sh` que hoy no existe
   (hallazgos 4 y 11).
5. **Tabla de verificación.** Exigible si rutas, comandos y nombres de tests se comprueban por script
   y las cifras vienen con su salida; y si el README no cita cantidades de tests (hallazgo 9).
6. **Falso hoy.** La segunda mitad completa. Lo más grave está fuera del README: la cabecera del
   Blueprint, el tablero del Progress, las notas del Workboard y el «No iniciar la Etapa 7» de
   `AGENTS.md`.

## Lo que la v2 debería cambiar

1. D5: quitar la opción de Chrome; capturas por `scripts/capturas.sh` desde una base nueva; muestras
   versionadas en `docs/muestras/`; decidir cuál divergencia es la toma 6 y cómo se provoca; pies de
   captura por referencia de pedido, nunca por posición ni URL; texto alternativo en la lista de
   tomas.
2. D6: precondición de reset; `scripts/demo.sh` dentro de `E8B`; minuto 7–8 con callbacks desde
   `/import`; minuto 8–9 con la auditoría mostrada por API o declarada parcial; nombrar pedidos, no
   filas.
3. D1: agregar «simulación, sandbox y producción»; corregir «motor» por «proveedor»; nombrar las
   cinco decisiones; anclar «Cómo se verifica» a tests con nombre; ampliar el mapa de documentación.
4. D1 §6: redactar los límites desde las fuentes que existen; borrar la referencia al artículo.
5. D2: mantener la regla para el README, sumar el inventario de cifras fuera del README a la entrega
   de `E8A`, y marcar el guion igual que el README.
6. D3/D4: sustituir el ciclo de vida de la explicación por el flujo de verificación; corregir §3 del
   Blueprint; una fila de verificación por diagrama.
7. D7: tabla con columna «test que lo afirma», `scripts/check-docs.sh` para rutas y nombres, cifras
   solo con salida de comando, y el README sin cantidades de tests.
8. D9: pasada del coordinador sobre el estado canónico —con el inventario de arriba— antes de
   despachar `E8A`, y un brief que diga qué documentos derivados puede tocar Claude.
9. Dependencias: aprobar Playwright en `tools/capturas/` con versión exacta y fuera de la compuerta,
   o declarar la toma 5 manual. No hay tercera opción honesta.
10. Registrar en la bitácora, además de D2, que un documento con capturas y guion declara cómo se
    regeneran, y que las cantidades de tests no se escriben en documentos públicos.
