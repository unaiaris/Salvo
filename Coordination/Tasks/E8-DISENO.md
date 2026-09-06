# Salvo — Diseño de la Etapa 8: el argumento del proyecto

> Estado: propuesta **v2**, corregida tras la revisión adversarial
> Fecha: 2026-09-06
> Base: `main` tras el cierre de la Etapa 7 y la pasada de sincronización canónica
> Revisión adversarial: `Coordination/Tasks/E8-revision-adversarial.md` (12 hallazgos, 5 altos, más
> un inventario completo de lo que era falso en los documentos)

## Qué cambió respecto de v1

| Hallazgo | Qué estaba mal | Qué dice v2 |
| --- | --- | --- |
| 1, alta | Recomendaba automatizar las capturas con Chrome, que **no está instalado** en la máquina; y aunque lo estuviera, una captura que solo regenera una sesión de Claude no la regenera quien clona el repositorio | Playwright en `tools/capturas/`, con `scripts/capturas.sh` (D5) |
| 2, alta | Capturas y guion citaban alertas por URL o por posición; el id es un GUID y el desempate del orden también | Se nombran **pedidos**, nunca posiciones ni URLs (D5, D6) |
| 3, alta | «Del recorrido real con el corpus demo» describía `salvo.db`, que tiene 328 pedidos, siete corridas y una banda que el corpus no produce | Las capturas salen de una **base nueva**; muestras versionadas en `docs/muestras/` (D5) |
| 4, alta | El guion prometía callback y divergencia sobre la misma alerta —imposible— y era destructivo sin reset | Callbacks desde `/import`, divergencia en la alerta, y `scripts/demo.sh` (D6) |
| 5, media-alta | Faltaba «simulación, sandbox y producción», criterio global del Blueprint; y el pitch decía «no es un motor», contradiciendo el §1 | Sección propia, y «no es un **proveedor**» (D1) |
| 6, media | Citaba «el artículo para revisores», que no existe en el repositorio | Los límites se redactan desde fuentes del repositorio (D1) |
| 7, media | El bloque marcado servía para el README y para nada más | `E8A` entrega el **inventario** de cifras fuera del README; el guion lleva los mismos marcadores (D2) |
| 8, media | Cuatro diagramas, uno de ellos sin valor; y el recorrido copiaba un §3 equivocado | El ciclo de vida de la explicación se cambia por **cómo se verifica**; el §3 ya se corrigió (D3, D4) |
| 9, media | La tabla de verificación se llenaba a mano, que es como se llenó mal tres veces | Rutas y nombres **por script**; el README no cita cantidades de tests (D7) |
| 10, media | No decía quién corrige el estado canónico desfasado, y casi todo era del coordinador | Pasada ya hecha; el brief dice qué documentos derivados puede tocar Claude (D9) |
| 11 y 12, bajas | Guion sin precondición; capturas sin nombres, escala ni texto alternativo | Fijados (D5, D6) |

---

## D1 — El README argumenta

El README actual no solo está incompleto: **es falso**. Dice «cinco etapas» cuando son siete, dice
que el smoke comprueba cuatro rutas en tres escenarios cuando son cinco en seis, y presenta a
Anthropic como «previsto para una etapa posterior» cuando hoy nombrarlo hace fallar el arranque.

Estructura, en orden:

1. **Qué es y qué no.** Salvo es la consola de **un** comercio, no un **proveedor** antifraude. La
   corrección importa: Salvo sí tiene un motor de reglas deterministas —el §1 del Blueprint lo dice—;
   lo que no tiene es la posición de un proveedor, que evalúa para muchos comercios y ve fraude a
   través de toda su base.
2. **Simulación, sandbox y producción.** Sección propia, porque es criterio global de aceptación del
   Blueprint, regla permanente de `AGENTS.md` y «regla de portfolio» de `Salvo-Portability.md`. El
   mejor argumento no es una frase sino un hecho: `KOIN_MODE=sandbox` y `AI_PROVIDER=anthropic`
   **se niegan a arrancar**. Nada de esto habló nunca con Koin.
3. **El recorrido en una imagen.**
4. **Las tres fuentes de verdad que nunca se mezclan.**
5. **Cinco decisiones con su porqué**, nombradas: **29 y 31** (evaluación append-only con identidad
   de contenido, y la corrida vigente como lo único que define «vigente»); **33** (la alerta congela
   su premisa y expone la divergencia en vez de actualizarse); **37 y 38** (el dashboard no lee la
   etiqueta, y se prueba invirtiéndola); **45 y 47** (reservar antes de llamar, y que un fallo
   posterior al envío no cierre); **52 y 54** (grounding verificado sobre la salida, y ningún texto
   ajeno al motor entra al modelo).
6. **Cómo se verifica.** Se queda —el lector es una entrevista de ingeniería, y lo que distingue al
   proyecto no es qué hace sino cómo se sabe que lo hace— y se vuelve concreta citando tests por
   nombre: `DashboardEndpointTests.TheDashboardIsIndependentOfGroundTruth`,
   `ExplanationIsolationTests.ExplainingEveryAlertChangesNoDecisionSurface`,
   `.InvertingEveryLabelChangesNoSummary`, `.NoTextTheEngineDidNotWriteReachesTheProvider`,
   `ExplanationGroundingTests.AnInventedFigureIsRefusedAndNoTextIsStored`,
   `ExternalCallbackRaceTests.ACallbackThatArrivesBeforeTheIdentifierIsWrittenDownStillSettlesTheEvaluation`,
   `AlertReviewTests.TwoConcurrentReviewsLeaveOneVerdictAndExactlyOneAudit`,
   `TemporalRiskEngineTests.AddingFutureOrdersCannotChangeEarlierAssessments`,
   `DemoSeedTests.SeedIsFullyIdempotentAndContainsOnlySyntheticPseudonymousData`, `OpenApiDriftTests`,
   `boundary.test.ts` y `scripts/smoke-ui.sh`.
7. **Límites declarados**, redactados **desde el repositorio**: `FIXTURE_CAVEAT` de
   `quality-section.tsx`, las notas del Workboard sobre el arquetipo único y `unusual_hour`
   inalcanzable, §5.3 del Blueprint sobre qué falta para un sandbox, y `console-header.tsx`.
8. **Cómo correrlo.** Corto.
9. **Mapa de documentación**, ampliado con `Coordination/Tasks/` —los diseños y las revisiones
   adversariales—, `Coordination/Handoffs/`, `Coordination/Workboard.md`,
   `ClaudeAgent/Claude-Model-Policy.md` y `DesignAgent/Salvo-Project-Instructions.md`. Las tablas de
   falsación de cada handoff son el artefacto más fuerte del proyecto y hoy no se enlazan desde
   ningún lado público.

Lo que **no** va: catálogo de features, badges, ni «tecnologías usadas».

## D2 — Las cifras del corpus, marcadas, y el inventario de las que viven fuera

En el README, todas las cifras del corpus van entre `<!-- corpus:inicio -->` y `<!-- corpus:fin -->`.
Las estructurales —seis reglas, sus pesos, el umbral 60, las tres bandas— no son del corpus y van en
el cuerpo. **El guion de demo lleva los mismos marcadores**, porque cita las mismas cifras. **Los
diagramas no llevan ninguna**: un diagrama con «300» dibujado adentro se regenera a mano.

Y como la regla es ciega fuera de esos dos archivos, `E8A` entrega además el **inventario** de
cifras del corpus que viven en otro lado y que la Etapa 9 va a invalidar: el texto de `/import`, las
bandas del mock y su test, los dos textos dorados, los tests del dashboard, una línea del smoke, el
§4.1 y la decisión 19 del Blueprint, las notas del Workboard y las evidencias del Progress. La
Etapa 9 empieza con esa lista, no con un `grep`.

## D3 y D4 — Cuatro diagramas Mermaid

Mermaid versionado, porque GitHub lo renderiza y porque una imagen se desactualiza en silencio
mientras que un diagrama en texto entra en el diff.

| Diagrama | Qué tiene que dejar claro |
| --- | --- |
| **Las tres fuentes de verdad** | Tablas y ciclos de vida distintos; **`ScoringRun` y `run_evaluations`** como lo único que define «vigente», que es la pieza menos intuitiva del modelo; y `AlertReview.explanationId`, el hilo entre el veredicto y lo que la analista tenía delante |
| **El recorrido de un pedido** | Los ocho pasos que el código sostiene, con la corrida como paso explícito. El §3 del Blueprint ya se corrigió en la pasada de sincronización |
| **La máquina de estados externa** | Lo que la tabla del §4.5 no muestra: las dos rutas de correlación, y que `TIMEOUT`, `PROVIDER_ERROR` e `INVALID_RESPONSE` dejan la fila `PENDING` mientras `UNREACHABLE` y `PROVIDER_REJECTED` la cierran |
| **Cómo se verifica una explicación** | Hechos construidos en Domain → tokenizador declarado → igualdad por redondeo → el texto rechazado no se persiste. Reemplaza al ciclo de vida de la explicación de la v1, que son tres estados y una flecha: cabe en una oración |

Un error de sintaxis en Mermaid se renderiza como bloque de código sin aviso y nada del repositorio
lo detecta: la tabla de verificación lleva una fila por diagrama, «renderizado en la vista previa de
GitHub».

## D5 — Las capturas: script, base nueva, y pedidos por nombre

**Playwright en `tools/capturas/`**, aprobado acá con nombre y motivo, como la Etapa 5 aprobó
`openapi-typescript`. Tres condiciones:

1. **Paquete propio con su lockfile**, fuera de `frontend/`. Así `package-lock.json`, `npm ci` y la
   compuerta quedan exactamente como están.
2. **Versión exacta**, y el Chromium que esa versión fija. Es lo que vuelve la captura reproducible
   en otra máquina, que es el argumento entero. Ese Chromium no es el navegador de nadie: no tiene
   perfil, historial ni extensiones.
3. **Fuera de `check.sh` y de los tests.** «Tests sin red» sigue siendo cierto. La descarga inicial
   es del orden de 150 MB, una vez por máquina, y el README lo dice junto al comando.

`scripts/capturas.sh` reutiliza el diseño del smoke: base temporal migrada con `--connection`, API y
consola en puertos propios, seed y corrida por `curl`, y el estado de cada toma preparado por la API
antes de fotografiar. Cinco de las seis tomas no necesitan interacción; la de importación sí, porque
su resultado vive en memoria del cliente y no hay tabla de importaciones. **El script no borra
nada**: sobrescribe.

Lista de tomas, con nombre fijo, ancho 1280 y factor de escala 2, cada una con su texto alternativo:

| Archivo | Toma |
| --- | --- |
| `01-cola.png` | `/alerts` con la cola |
| `02-detalle.png` | `/alerts/[id]` completo, sobre **`ORD_000011`** |
| `03-explicacion.png` | El bloque de explicación en detalle |
| `04-dashboard.png` | `/dashboard`, incluida «Calidad del criterio» con su advertencia |
| `05-import.png` | `/import` tras enviar un archivo con filas rechazadas |
| `06-divergencia.png` | La divergencia **de criterio** entre motor local y proveedor |

Dos precisiones que la v1 no tenía. **La toma 6 es la divergencia de criterio, no la de banda**: la
primera es determinista y abundante —el mock aprueba todo pedido cuyo sufijo módulo 100 sea menor
que 75, y catorce de las dieciocho alertas del corpus caen ahí—, mientras que la de banda no existe
en ninguna alerta y solo se provoca con una importación retroactiva. Y **ningún pie de captura ni
paso del guion cita una alerta por su URL ni por su posición**: el id es un `Guid.NewGuid()`, todas
las alertas de una corrida comparten `createdAt`, y con scores de solo 60 y 90 el orden dentro de
cada banda lo decide un GUID. Se nombran pedidos. La nota de regenerables declara que el orden
dentro de cada banda cambia entre regeneraciones, y que los instantes en pantalla también.

**Muestras versionadas en `docs/muestras/`**, decidido con el usuario: son CSV y JSON cien por
ciento sintéticos, y versionarlos permite que cualquiera que clone el repositorio pruebe la
importación válida, la rechazada y el caso de hora inusual, que es el que demuestra la regla que el
corpus no alcanza. Lo que queda en `_local/` es material de entrevista.

## D6 — El guion de demo

Diez minutos, con **precondición escrita**: base nueva, corpus cargado y corrida ejecutada antes de
empezar. Eso exige `scripts/demo.sh`, que hoy no existe y que D9 anticipó como hallazgo: levanta API
y consola sobre una base nueva con fecha, con el molde de `smoke-ui.sh`, **sin borrar nada** —`rm`
está denegado— y dejando la base vieja donde estaba. El guion es destructivo por diseño: el
veredicto es terminal, la explicación escrita no se regenera y el seed es idempotente. Cada ensayo
consume el estado del anterior.

| Minuto | Bloque | Precisión |
| --- | --- | --- |
| 0–1 | Qué es y qué no | La consola de un comercio, no un proveedor |
| 1–2 | Importar y correr el scoring | Dos clics; importar no procesa |
| 2–5 | Una alerta: las señales con sus números | Se nombra el pedido, nunca «la primera fila» |
| 5–7 | La explicación y por qué está verificada | Llevar preparada la frase del tokenizador y el nombre del test que lo prueba |
| 7–8 | La segunda opinión y la divergencia | Los callbacks se piden y entregan **desde `/import`** —pulsarlo dos veces muestra el replay—, y la divergencia se ve en la alerta. Sobre una misma alerta no se pueden mostrar las dos cosas |
| 8–9 | El veredicto terminal y su auditoría | La pantalla muestra transición, instante y nota, **no** `explanationId`: se enseña con un `curl` o se declara parcial |
| 9–10 | Límites declarados | «Calidad del criterio» con F1 = 1,00 y su advertencia debajo |

Cada bloque lleva la frase que hay que decir, el clic que hay que dar, y **una línea «si falla»**:
con la API caída, las rutas de datos dicen «No se pudo contactar a la API», y ese estado también
vale mostrarlo.

## D7 — La verificación se ejecuta, no se promete

Una tabla que se llena a mano se llena sin abrir los archivos: ya pasó tres veces en dos días, y una
de esas veces fue en la v1 de este mismo diseño. Las afirmaciones se separan por **cómo** se
verifican:

- **Rutas, comandos y nombres de tests: por script.** `scripts/check-docs.sh`, en bash y Node sin
  dependencias, como el smoke: extrae cada ruta entre acentos graves del README y afirma que existe,
  y para cada nombre de test citado hace `grep` en `backend/tests` y `frontend/src`. Es el patrón de
  `OpenApiDriftTests`: la deriva se detecta, no se promete. Puede entrar en `check.sh`.
- **Afirmaciones de comportamiento: por test nombrado.** La tabla lleva columna «test que lo
  afirma». Una afirmación sin test al lado no entra al README.
- **Cifras: por comando con su salida.** Y una regla nueva: **el README no cita cantidades de
  tests.** Cita los comandos. Una cantidad es cierta el día que se escribe y falsa la semana
  siguiente — en este proyecto cambió cuatro veces en dos días.

La tabla va en el handoff de `E8A`, con el formato que el proyecto ya usa para las falsaciones.

## D8 — Lo que la Etapa 9 va a invalidar no se detalla ahora

Las métricas entran como límite declarado, con la advertencia que ya está escrita en
`quality-section.tsx`, y el bloque marcado trae la tabla. El README dice en una línea qué trae la
Etapa 9: un proyecto que declara lo que le falta se lee mejor que uno que finge estar terminado.

## D9 — Partición

**E8A-README-DIAGRAMAS.** README reescrito, los cuatro diagramas, `scripts/check-docs.sh`, la tabla
de verificación y el inventario de cifras. Puede tocar los documentos **derivados** —`Salvo-Overview`,
`Salvo-MOC`, `Salvo-Getting-Started`, `Salvo-Portability`— con el inventario de la revisión
adversarial como lista de trabajo. **No** toca Blueprint, Progress ni Workboard: son del coordinador
y ya se sincronizaron. Corrección de arrastre autorizada: tres comentarios de código que nombran a
la «Etapa 8» para lo que ahora es la 9.

**E8B-DEMO-CAPTURAS.** `tools/capturas/`, `scripts/capturas.sh`, `scripts/demo.sh`,
`docs/muestras/`, las seis capturas y el guion de demo. Depende de `E8A` integrada.

## Cambios de estado canónico que exige este diseño

1. **Ya aplicados**: la pasada de sincronización sobre once documentos.
2. **Bitácora**, tres entradas: un documento que cita datos declara dónde los cita y cómo se
   regeneran; las cantidades de tests no se escriben en documentos públicos; y una dependencia nueva
   se aprueba por nombre y motivo en el diseño de su etapa.
3. **Blueprint §2**: ya reconciliado con D8.
