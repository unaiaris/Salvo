# Salvo — Workboard Codex–Claude

> Estado: Etapa 9 en ejecución, la última. `E9A-FIXTURE` y `E9B-SENALES-TIPADAS` verificadas
> (merges `41343c1` y `4f7daf9`); `E9C1-IDIOMA` despachada
> Última actualización: 2026-09-07
> Responsable: coordinador de la etapa

## Estados

`Propuesta` → `Asignada` → `En curso` → `Lista para integrar` → `Integrada` → `Verificada`

Estados alternativos: `Bloqueada`, `Cancelada`.

Una tarea no cambia a `Integrada` o `Verificada` por decisión del agente que la implementa.

## Trabajo activo

| Work ID | Estado | Propietario | Modelo y esfuerzo | Paths reservados |
| --- | --- | --- | --- | --- |
| `E9A-FIXTURE` | `Verificada` (merge `41343c1`) | `Claude` | Opus 5 · `high` | `backend/src/Salvo.Infrastructure/Seed/**`, `Salvo.Application/Orders/Seed/**`, el endpoint del seed, el panel del dashboard en backend y frontend, `frontend/src/app/alerts/[id]/divergence.ts`, `messages.ts`, `backend/tests/**` y las fixtures del frontend. **La reserva completa vive en el brief; esta fila la resume.** |
| `E9B-SENALES-TIPADAS` | `Verificada` (merge `4f7daf9`) | `Claude` | Opus 5 · `high` | `Salvo.Domain/Risk/**` y `Explanations/**`, `Application/Explanations/**`, `AlertViews.cs` y `AlertProjection.cs`, `Salvo.Infrastructure/Explanations/**`, `backend/tests/**`, el borde del frontend —`guards.ts`, `contract.ts`, `fixtures.ts`, `boundary.test.ts`, `format.ts`— y la recaptura del contrato. **La reserva completa vive en el brief; esta fila la resume.** |
| `E9C1-IDIOMA` | `Asignada` | `Claude` | Opus 5 · `high` | `SALVO_LANGUAGE`, el idioma en la identidad de la explicación con su migración, dos diccionarios en el frontend, la plantilla del backend, `messages.ts`, `describeRecordError`, `format.ts`, `<html lang>`, las anclas del smoke, **y el décimo `ExplanationFailureCode`** en la misma migración. **La reserva completa vive en el brief; esta fila la resume.** |
| `E9C2-ACCESIBILIDAD` | `Propuesta` | `Claude` | por acordar | Linter y comprobaciones automáticas de accesibilidad, la lista de hallazgos con severidad **antes** de corregir, y las correcciones que el coordinador elija tras el recorrido con VoiceOver |
| `E9D-CIERRE` | `Propuesta` | `Claude` | por acordar | Repaso final de documentos, capturas y artículo |

**El orden es obligatorio y está argumentado en el diseño v2**: la fixture primero y el motor
después, porque el corpus actual dispara tres de las seis reglas y el extractor de `SignalFacts` es
el único oráculo capaz de certificar los campos tipados de las otras tres.

Los briefs de `E9B`, `E9C` y `E9D` se escriben cuando llega su turno, con el handoff de la tarea
anterior a la vista: el de `E9B` depende de cómo quede el corpus, y el de `E9D` de lo que las tres
anteriores rompan.

## Cola próxima

| Work ID | Estado | Modelo y esfuerzo | Depende de |
| --- | --- | --- | --- |
| `E7A-EXPLICACIONES` | `Verificada` (merge `82f2487`) | Opus 5 · `high` | — |
| `E7B-EXPLICACIONES-UI` | `Verificada` (merge `ac11015`) | Opus 5 · `high` | `E7A` |
| `E7C-PULIDO-EXPLICACION` | `Verificada` (merge `0d117dd`) | Opus 5 · `high` | `E7B` |
| `E7D-PLANTILLA-VIGENTE` | `Verificada` (merge `b4aac6b`) | Opus 5 · `high` | `E7C` |

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
  que ya existen (decisión 58). Y no alcanza con que el camino de escritura lo permita: `E7D` hizo
  falta porque la lectura y la escritura no coincidían en cuál era la fila vigente, así que la
  consola nunca ofrecía pedirla.
- **El tablero de estado se lee antes de tocarlo.** El bloque «Estado general» del Progress quedó
  desfasado durante toda la Etapa 7 porque los cierres actualizaron el registro de actividad y los
  checklists pero no el tablero.

Notas para los briefs de la Etapa 8:

- **Ninguna tarea de esta etapa toca código de producción.** No hay migración, ni contrato, ni
  recaptura de OpenAPI. Si algo parece necesitarlo, es hallazgo y no tarea.
- Las capturas y el guion salen de una **base nueva**, no de `salvo.db`: esa base tiene 328 pedidos,
  siete corridas y una banda `ALTA` que solo existe por importaciones manuales.
- Ningún pie de captura ni paso del guion cita una alerta por su URL ni por su posición en la cola:
  el id es un GUID y el desempate del orden también. Se nombran pedidos.
- Verificar el estado canónico antes de declararlo pendiente, y **abrir cada archivo antes de
  afirmar algo sobre él**.
- **«Renderiza» no es «se lee».** Los cuatro diagramas del README pasaron la sintaxis al primer
  intento y hicieron falta tres renderizados: acentos que se habían quitado por precaución, un
  dibujo ilegible por cruces, y una superposición que introdujo la corrección. Un criterio de
  aceptación sobre un artefacto visual dice «visto, y por quién».
- En `stateDiagram-v2`, **un bucle sobre un estado no puede llevar rótulo** si ese estado tiene otras
  aristas salientes: el rótulo cae en la misma columna que la contigua y las dos cajas se pisan.
  Verificado en tres disposiciones.
- **Un modelo distinto para validar que para ejecutar da una verificación de verdad independiente.**
  El `brief-check` de `E9A` corrió con Fable · `xhigh` y encontró un hueco de reserva de paths que
  seis checks anteriores dejaron pasar en briefs con la misma forma — el mismo que rompió `E7A` a
  mitad de ejecución. Hasta acá el check corría con lo que la sesión tuviera puesto, y a veces eso
  significaba que el mismo modelo escribía el plan y lo validaba.
- **Congelar una captura antes de mirarla es lo que deja ver lo que está mal en ella.** El
  extractor de `E9B` ponía `scope = merchant` en `new_buyer_high_value` **siempre**, y esa regla
  dispara justamente cuando el comprador no tiene historia: era una constante disfrazada de
  medición, y estaba por entrar al fingerprint de cada señal de esa regla para siempre. Apareció al
  leer la primera captura antes de congelarla, no al escribir la tabla del brief.
- **Un criterio de aceptación imposible de cumplir es tan malo como uno que se cumple solo.** La
  tabla de verificación de `E9B` pedía cero resultados de `GeneratedRegex` en todo
  `Salvo.Domain/Explanations/`, donde vive también la de `NumberTokenizer`, que no tiene nada que
  ver con cómo se guarda una señal. Un criterio se escribe contra el árbol, no contra la idea.
- **Cerrar una tarea toca el checklist y la tabla de actividad, y se olvida de las cabeceras.**
  Tres veces seguidas —`E8A`, `E8B` y `E9A`— el estado canónico quedó a medias del mismo modo: el
  detalle se actualiza y los tres bloques que resumen dónde está el proyecto no. Son cuatro lugares
  fijos y conviene recorrerlos por lista: la cabecera del Workboard, el bloque «Estado general» del
  Progress, su tablero maestro por etapa, y el «Próximo paso» del Overview.
- **El commit que declara la base de una tarea va en la rama que describe, nunca en `main`.** Si va
  en `main` y después se rebasa, ese mismo commit pasa a ser la base y el campo vuelve a ser falso.
  Agregar commits a `main` no mueve el `merge-base`; rebasar sí. Le pasó a `E8B`.
- **Una fila del tablero resume; el brief manda.** Cuando las dos declaraciones de paths no
  coinciden, la corrección es alinear el resumen y decir cuál gobierna.

La **Etapa 8** quedó acotada al argumento del proyecto: README, diagramas, capturas y guion de
demo. Todo lo demás pasó a la **Etapa 9**, que además hace el repaso final de documentos con las
cifras del corpus nuevo. La partición es deliberada: la fixture enriquecida invalida las cifras de
todo lo que se escriba antes, así que el README concentra los números volátiles en un bloque
marcado y la Etapa 9 los actualiza de una sola pasada.

Candidatas registradas para la Etapa 9, acordadas con el usuario:

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
  tres comercios está en 16,7 % —cuatro de veinticuatro, con la ventana de treinta días y el mínimo
  de veinte que usa el motor—. Y hay un segundo motivo: veinte de los cien pedidos de cada comercio
  no llegan a ese mínimo y la regla ni siquiera evalúa. Una fixture enriquecida debería cubrir las seis reglas y las tres
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
