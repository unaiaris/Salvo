# Salvo — Task brief `E11B-ADAPTADOR-ANTHROPIC`

## Identificación

- Work ID: `E11B-ADAPTADOR-ANTHROPIC`
- Etapa: 11 — la IA de verdad
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-10-01
- Rama/worktree: `claude/e11b-anthropic`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. El coordinador acepta que este campo no lleve SHA, por la lección de
  `E8B`: el commit que lo escribiera en `main` movería la punta y lo volvería falso.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5.5 · `high`**, la fila «Implementación con ambigüedad real
  dentro del alcance» de `ClaudeAgent/Claude-Model-Policy.md`: el diseño fija las decisiones, pero
  la forma del desenlace del puerto, la regla de selección de la consola y la taxonomía de errores
  piden criterio dentro del alcance. El `brief-check`, con un modelo distinto.
- Dependencias: `E11A-INTEGRACION-CONTINUA`, verificada. **`E11C-CORRIDA-REAL` depende de ésta.**

## Resultado esperado

**Un modelo de Anthropic puede redactar la explicación de una alerta, gobernado por el mismo
verificador que gobierna a la plantilla**, y nada de lo que hoy funciona sin clave cambia de
comportamiento. Al terminar:

- `AI_PROVIDER=anthropic`, con `ANTHROPIC_API_KEY` y `ANTHROPIC_MODEL`, registra un adaptador real.
  Sin ellos, el arranque falla nombrando la variable y nunca su valor.
- **Ningún test, ni la compuerta, ni el smoke, ni la CI tocan la red de Anthropic.** El adaptador se
  prueba contra un transporte simulado que reproduce las formas documentadas de la API.
- La consola dice quién escribió cada párrafo y nunca esconde un texto aceptado detrás de un
  intento fallido.
- Queda un script para la corrida real, **que esta tarea no corre**: lo corre el coordinador en
  `E11C`, con su clave.

**Lo que esta tarea no puede verificar es que la API real se comporte como el simulador.** Eso es
`E11C`. Por eso cada forma que el simulador reproduce sale de una página oficial citada, leída el día
que se ejecute.

## Contexto obligatorio

- `Coordination/Tasks/E11-DISENO.md`, **entero**. Es la especificación: las decisiones D1 a D15 son
  de esta tarea, y D16 —qué se publica— es de `E11C`, aunque el script del punto 11 ya escribe en su
  forma. Este brief no las repite: las ordena, las corrige donde el código las desmiente, y las vuelve
  verificables.
- `Coordination/Tasks/E11-revision-adversarial.md`: por qué cada decisión es la que es.
- `DesignAgent/Salvo-Blueprint.md`, §4.7 «Explicabilidad», §7 «AlertExplanation», §9 «IA
  posterior», y las **decisiones 73 a 81** de la bitácora, que registran este diseño.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 11 — La IA de verdad»: el que esta tarea cierra
  es el de `E11B`.
- **La decisión 56** —el registro de revisión guarda qué explicación tenía delante la analista—, que
  D11 no puede romper.
- `AGENTS.md`, «Decisiones invariantes», en especial: un texto rechazado no se persiste, no se
  registra y no llega al diagnóstico; y si el modelo no pasa el verificador, se cambia el prompt,
  nunca el verificador.
- El código que la tarea toca, **leído antes de cambiarlo**:
  - `backend/src/Salvo.Application/Explanations/` entero, y `Providers/ProviderCall.cs`, que **no se
    toca** pero cuyo comentario explica por qué un adaptador no puede avisar lanzando.
  - `backend/src/Salvo.Domain/Explanations/AlertExplanation.cs`, `NumberTokenizer.cs`,
    `ExplanationProvider.cs` y `ExplanationGrounding.cs`.
  - `backend/src/Salvo.Infrastructure/Explanations/` entero, y `AddExplanationProvider` en
    `DependencyInjection.cs`.
  - `EfAlertStore.cs`: `ComposeAsync`, `GetExplanationsAsync` —que hoy elige la fila más
    recientemente pedida— y `CurrentExplanationOf`.
  - `ReviewAlertHandler.cs`, que valida la explicación citada contra lo que la alerta muestra, y
    `frontend/src/app/alerts/[id]/review-panel.tsx`, que la envía.
  - `scripts/smoke-ui.sh`, las anclas del bloque 1d sobre la explicación de otra plantilla.
  - `frontend/src/app/alerts/[id]/explanation-*.tsx` y las claves de explicación de los dos
    diccionarios.
- La documentación oficial de Anthropic que el diseño cita en «Datos verificados». **Se vuelve a abrir
  el día que se ejecute** y la tabla se rehace en el handoff.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Correcciones al diseño, hechas por este brief

**Regla de precedencia: donde este brief y `E11-DISENO.md` difieren, manda este brief.** El diseño lo
dice en su encabezado. Ésta es la lista completa de diferencias; cualquier otra que el agente
encuentre es hallazgo y consulta, no una elección:

| Punto del diseño | Lo que dice el diseño | Lo que manda este brief |
| --- | --- | --- |
| D6 | `FailureDetail` se ve en la consola | No se ve hoy; se agrega a la vista (abajo, 1) |
| D14 | No se pide razonamiento | Se apaga con `between_tools` y esfuerzo `medium` (abajo, 2; decisión 81) |
| D3 | `enum` de las reglas que dispararon | `enum` constante de todas las reglas (abajo, 3; decisión 81) |
| D6 | Tipo y `request-id` a `FailureDetail` | Solo valores de una lista cerrada, o con forma de `request-id` (abajo, 4) |
| D10 | El test de propiedad cubre la hoja | Propiedad **y** un test explícito de las tres prohibiciones (punto 7) |
| D11 | Tres pasos | Cinco casos, la revisión registra la mostrada, y qué pide el botón en cada caso (puntos 9 y 10) |
| D12 | El botón dice «escritor vigente» | Con la plantilla conserva su rótulo exacto, por el smoke (punto 10) |
| D15 | Cuatro scripts; `. ./.env` | Cinco scripts y la fábrica de tests; `SALVO_ENV_FILE` sin valor por omisión (puntos 2 y 11) |

El código y la documentación del día desmienten cuatro cosas del diseño. Se resuelven acá, y la
segunda y la tercera quedaron registradas como **decisión 81**.

1. **`FailureDetail` no se ve hoy en la consola**, aunque D6 lo afirma: la columna existe y se
   guarda, pero `AlertExplanationView` no la lleva y el frontend no la conoce. Esta tarea la agrega a
   la vista y la muestra junto al estado de un intento fallido.
2. **Sonnet 5.5 razona por defecto, y no se puede apagar con `disabled`.** Leído el 2026-10-01 en
   [What's new in Sonnet 5.5](https://platform.claude.com/docs/en/models/sonnet-5-5/whats-new-sonnet-5-5),
   [Thinking](https://platform.claude.com/docs/en/build-with-claude/thinking) y
   [Adaptive thinking](https://platform.claude.com/docs/en/build-with-claude/adaptive-thinking):
   - omitir `thinking` activa el razonamiento adaptativo, con esfuerzo `high` por defecto;
   - `thinking: {"type": "disabled"}` devuelve 400;
   - el mínimo es `thinking: {"type": "between_tools"}`, aceptado con esfuerzo `high` o menor, y
     «without tools, the response contains only text»;
   - el razonamiento cuenta contra `max_tokens` y se cobra como salida.

   **Decisión**: el adaptador envía `thinking: {"type": "between_tools"}` y
   `output_config.effort: "medium"`. Así `max_tokens` = 1.024 y el costo de D14 siguen valiendo, y
   un test sobre el cuerpo de la petición lo afirma. El adaptador lee
   `usage.output_tokens_details.thinking_tokens` cuando viene, y un valor mayor que cero se registra
   como log `Warning`, con el `request-id` y la cifra: es la señal de que la API cambió. No va a
   `FailureDetail`, porque en un texto aceptado `Complete` lo vacía. Un test afirma el log.
   **No se envían `fallbacks` ni parámetros de muestreo** —`temperature`, `top_p`, `top_k`—. El
   respaldo del servidor, en beta, reintenta ciertos rechazos con otro modelo, y un rechazo acá es un
   desenlace que se registra, no algo que se esconde; el muestreo queda en el valor del modelo. Si la
   documentación del día exige alguno de los dos, es parada.
3. **Un esquema por conjunto de reglas compilaría una gramática por conjunto.** D3 pedía el `enum` de
   las reglas que dispararon en cada evaluación; cada esquema distinto compila su gramática la primera
   vez, y la primera es la lenta, contra un plazo de 15 s. **Decisión**: el esquema es **constante**,
   con el `enum` de **todas** las reglas del motor. Que la regla citada haya disparado lo sigue
   comprobando el verificador, como `NotGroundedRule`, que es donde ya vive esa garantía. El
   adaptador comprueba además que cada valor pertenezca al `enum`, sin distinguir mayúsculas; uno que
   no pertenezca vuelve el borrador `Malformed`, **sin copiar el valor**.
4. **Lo que escribe el proveedor no entra crudo a la base ni a la pantalla.** `error.type`,
   `error.details.error_code`, el `stop_reason` y la categoría de un rechazo llegan a `FailureDetail`
   **solo** si están en una **lista cerrada** que el adaptador lleva, compilada de la documentación
   del día; si no, se escribe `unrecognized`. El `request-id` entra solo si tiene la forma que muestra
   la página de errores —`req_` seguido de letras y dígitos, con un tope de largo—; si no, se omite.
   Así lo que cruza la frontera de infraestructura es un vocabulario cerrado que el adaptador
   valida, no un tipo ni un texto del proveedor, que es lo que `AGENTS.md` y el comentario de
   `IExplanationProvider` prohíben.

## Alcance

### Dentro, en este orden de commits

El orden es obligatorio: pone las protecciones **antes** que lo que protegen.

1. **El campo «Commit base»** del brief.
2. **D15 — nada que corra sin pedirlo hereda el proveedor.** Va primero porque, desde el commit del
   adaptador, una terminal con la clave exportada haría gastar al smoke y hornearía texto de un modelo
   en la imagen pública.
   - `check.sh`, `smoke-ui.sh`, `demo.sh`, `capturas.sh` y `hornear-base.sh` exportan
     `AI_PROVIDER=mock`, una línea cada uno, con un comentario que diga por qué. El de
     `hornear-base.sh` dice el motivo verdadero: dentro de `docker build` el entorno de la terminal
     no entra, así que la línea es defensa para quien lo corra fuera de Docker.
   - **La fábrica de los tests de integración fija `AI_PROVIDER=mock`** en su propia configuración,
     por encima del entorno, salvo en los tests que piden otro proveedor a propósito. El
     `brief-check` lo midió: con `AI_PROVIDER=anthropic` exportado, `SystemCapabilitiesTests` cae 4
     de 4, porque el host de tests hereda el entorno. Un `dotnet test` corrido a mano, fuera de
     `check.sh`, tiene que estar protegido igual.
3. **D7 — los tokens en todo desenlace.** `Fail` recibe tokens, `Retake` deja de borrarlos y la fila
   acumula entre intentos. Sin migración.
4. **D1 — el desenlace cerrado del puerto.** `ExplanationDraft` se reemplaza por un resultado con
   desenlace `Drafted | Refused | Malformed | Unavailable`, más el borrador cuando lo hay, los tokens
   en todo desenlace, `ProviderVersion` tomado de la respuesta y un diagnóstico corto sin texto del
   modelo. `ExplanationExchange` asigna el código; el adaptador no puede nombrar códigos del
   verificador, del ciclo de vida ni del sistema. **`ProviderCall` no cambia un byte.** Cambian con
   el puerto `DeterministicExplanationProvider` y los proveedores de prueba de
   `ExplanationTestCorpus`. `MalformedOutput` amplía su significado a «cortado o ilegible», con su
   documentación y su rótulo en los dos diccionarios.
5. **D10, primera parte — `e3-v2` entra en `NumberTokenizer.VersionStrings`.** Un defecto latente desde
   `E9B`, con su test. Es **el único** cambio que esta tarea hace a una pieza del verificador, y lo
   autoriza la decisión 78. `anthropic-p1` **no** entra: la hoja de hechos no lleva versiones, y
   ampliar lo que el tokenizador ignora es ampliar lo que el verificador acepta.
6. **D13 — el agujero semántico, fijado como clase.** Tests de caracterización que **afirman que
   pasan** el verificador: inversión, atribución cruzada y cifras en palabras, nombrados por la clase.
   Antes del adaptador, porque describen el verificador que el adaptador va a enfrentar.
7. **El adaptador**, con D2, D3, D4, D5, D6 y la segunda parte de D10:
   - `HttpClient` directo, de vida larga, en un servicio singleton, **sin** `IHttpClientFactory` ni
     tubería de registro HTTP. Ninguna dependencia nueva.
   - Cabecera `anthropic-version`; salida estructurada con `summary` y `referencedRules`,
     `additionalProperties: false`, y `referencedRules` como `enum` **constante** de todas las reglas
     del motor —ver «Correcciones», punto 3—; normalización a minúsculas y pertenencia comprobada;
     `stop_reason` leído **antes** de deserializar.
   - `thinking: {"type": "between_tools"}` y `output_config.effort: "medium"` —ver «Correcciones»,
     punto 2—, afirmados por un test sobre el cuerpo de la petición.
   - Modelo desde `ANTHROPIC_MODEL`; `TemplateVersion` = **`anthropic-p1`**; `ProviderVersion` desde
     el campo `model` de la **respuesta**. El modelo **nunca** entra en la identidad.
   - `max_tokens` = 1.024, con el margen que pide D6.
   - **La hoja de hechos**, construida con `ExplanationFigures` y el vocabulario de la plantilla, sin
     versiones, sin centavos y sin instantes ISO, y un prompt que exige cifras en dígitos. **En el
     idioma del despliegue**: la hoja y el prompt existen en castellano y en portugués, y los tests
     cubren los dos.
   - **Dos tests sobre la hoja, porque protegen cosas distintas.** El de **propiedad** afirma que todo
     número de la hoja está fundamentado por `ExplanationFacts`. No puede proteger las tres
     prohibiciones: el monto en centavos **es** un hecho, las versiones se tachan antes de tokenizar,
     y de un instante ISO solo los segundos quedan sin fundamentar. Por eso hay un segundo test,
     **explícito**, que mira la hoja renderizada y afirma que no contiene el monto en centavos, ni un
     instante con forma ISO, ni ninguna cadena de versión.
   - La taxonomía de D6, entera, a `FailureDetail`. Se leen `error.type`, `error.details.error_code` y
     la presencia de `retry-after`; **nunca `message`**. Un log `Warning` para toda respuesta que no
     sea `2xx`, sin cuerpo y sin cabeceras.
   - **Lo que el proveedor escribe** entra a `FailureDetail` según «Correcciones», punto 4, y solo
     así: lista cerrada para los tipos, códigos, `stop_reason` y categorías; forma `req_` para el
     `request-id`. Es la **única** regla; no hay otra en este brief.
8. **D8 y D9 — el secreto y la instancia pública.** Un solo lector de `ANTHROPIC_API_KEY`, en
   `AddExplanationProvider`. Falla al arrancar sin clave o sin modelo. `SharedInstance:Enabled=true`
   con `AI_PROVIDER=anthropic` detiene el arranque **aunque la clave esté**. La documentación de
   `ExplanationProvider.Anthropic` y el mensaje de error que hoy dicen que el adaptador no existe se
   reescriben.
9. **D11 — la selección de la consola, y `FailureDetail` en la vista.** El **escritor** es el par
   proveedor y versión de plantilla; el vigente es el que registra el despliegue. Para cada
   evaluación que la alerta muestra —la del snapshot y la vigente, como hoy—:
   - **La explicación mostrada** es la `READY` del escritor vigente, si existe; si no, la `READY` más
     recientemente asentada de cualquier otro escritor, rotulada con quién la escribió; si no hay
     ninguna `READY`, la fila del escritor vigente; y si tampoco hay fila del vigente, la más
     recientemente pedida de cualquier escritor, como hoy.
   - **El intento vigente** se muestra además, con su estado, su código y su `FailureDetail`, cuando
     la explicación mostrada es de otro escritor y existe una fila del vigente que no está `READY`.
   - Los cinco casos tienen su test: `READY` del vigente; `READY` de otro y `FAILED` del vigente;
     `READY` de otro y **ninguna** fila del vigente —el caso 1d del smoke, que no cambia—; ninguna
     `READY` y una fila del vigente; y ninguna `READY`, ninguna fila del vigente y una `FAILED` o
     `PENDING` de otro.
   - **El intento vigente viaja anidado en `AlertExplanationView`.** Es la forma que la reserva de
     paths permite: no hace falta tocar `AlertViews.cs` ni `format.ts`.
   - **La revisión registra la explicación mostrada**, nunca el intento: es lo que la analista tenía
     delante, que es lo que guarda la decisión 56. `ReviewAlertHandler` acepta citar la mostrada y
     rechaza citar el intento, con su test.
   - Dónde vive la regla —el almacén o la proyección— lo decide la tarea, y cómo llega el escritor
     vigente a la lectura también. La forma de la respuesta cambia, así que el contrato se recaptura:
     `frontend/openapi/salvo-openapi.json`, `schema.d.ts`, `guards.ts`, y `OpenApiDriftTests` en
     verde.
10. **D12 — la consola dice quién escribió qué.** Una oración por proveedor en los dos idiomas. La de
    la plantilla **no cambia una letra**: el smoke la comprueba. La del modelo nombra el modelo
    —desde `ProviderVersion`— y la versión del prompt. **El rótulo del botón depende del escritor
    vigente**: cuando es la plantilla, sigue diciendo exactamente «Redactar con la plantilla vigente»
    —las anclas del bloque 1d del smoke, `expect_text` y `expect_no_text` sobre ese rótulo, lo exigen
    y lo prohíben donde corresponde, y no se tocan—; cuando
    es el modelo, dice «modelo de Anthropic» **sin el nombre del modelo**: el nombre sale de
    `ProviderVersion`, que solo existe después de una respuesta, y el botón puede aparecer antes. El
    intento vigente se muestra junto al texto aceptado de otro escritor cuando D11 lo pide, con su
    código y su `FailureDetail`.
    - **Todo texto de la consola que nombra a la plantilla como escritora** pasa a depender del
      escritor —el pie, el botón, y el título que aparece después de pulsarlo, «Redactada de nuevo con
      la plantilla vigente» en `es.ts` y su par en `pt.ts`—. Con la plantilla, cada uno queda **igual,
      letra por letra**. Con el modelo: **el pie y el título** nombran al modelo, porque los dos
      aparecen después de una respuesta y `ProviderVersion` ya existe; **el botón** dice «modelo de
      Anthropic» sin el nombre, porque puede aparecer antes de cualquier respuesta.
    - **Qué pide el botón en cada caso de D11**, porque hoy el caso 2 queda sin efecto: con una `READY`
      de otro escritor y una `FAILED` del vigente, la consola pide sin `regenerate`, el caso de uso
      encuentra la fila `FAILED` del vigente, no la retoma y contesta que ya tenía su explicación. Es
      el defecto de `E7D` en el escenario que D11 crea. La regla:
      - **Caso 2**, `FAILED` del vigente con intentos disponibles: el botón **reintenta esa fila**, con
        el mismo pedido que hoy hace «Volver a intentar», y el rótulo lo dice. Con los intentos
        agotados, no hay botón y se muestra `AttemptLimitReached`.
      - **Caso 3 y caso 5**, sin fila del vigente: el botón **crea** la fila del vigente.
      - **Con una fila `PENDING` en curso no hay botón**, sea del vigente —junto a una `READY` de
        otro— o de otro escritor **del mismo proveedor** en el caso 5. El índice parcial
        `ux_alert_explanations_pending_evaluation` es único por evaluación, proveedor e idioma mientras
        la fila esté pendiente, así que crear la del vigente chocaría. Se muestra el estado en curso,
        como hoy; con una `PENDING` de otro proveedor, el botón sí crea la fila.
      - **La afirmación tiene dos mitades, y cada una vive en su capa**, unidas por el pedido que viaja:
        - **Frontend**: un test por caso afirma **qué pedido** arma el botón —con o sin
          `regenerate`— o que no hay botón, a partir de la vista del caso. No puede afirmar `applied`:
          la respuesta la escribiría el propio test.
        - **Backend**: un test de integración por caso arma en la base el estado del caso, manda
          **exactamente ese pedido**, y afirma `applied: true` y un intento más —o el conflicto, si el
          caso no tiene botón—. Es el que ve el defecto de hoy, que está en lo que contesta la API.
11. **El script de la corrida real**, `scripts/explicar-con-anthropic.sh`, para `E11C`:
    - Es el **único** script que carga un archivo de entorno: `set -a; . "$archivo"; set +a`, con el
      archivo en `SALVO_ENV_FILE` y **sin valor por omisión**: si la variable falta, se niega. Así
      nadie —ni un agente— carga `.env` por correrlo sin argumentos. El coordinador lo corre con
      `SALVO_ENV_FILE=.env`. No imprime ni exporta más de lo que la API necesita, y **nunca imprime
      el valor de la clave**.
    - Se niega a arrancar, nombrando la variable, si falta la clave o el modelo.
    - Levanta la API sobre una base nueva y sembrada, como `demo.sh`, pide la explicación de cada
      alerta, y reintenta las `FAILED` dentro del tope de tres intentos de la fila: nunca más.
    - Escribe en `docs/explicaciones-modelo/AAAA-MM-DD/` lo que D16 permite publicar y nada más: los
      textos `READY` con modelo, versión de prompt y fecha; de cada `FAILED`, el código y
      `FailureDetail`; los tokens de cada fila, leídos de la base con `node:sqlite` como hace el
      smoke; y el costo calculado con los precios que imprime **junto a la fecha en que se leyeron**.
    - **El agente no lo corre contra Anthropic.** Lo corre sin `SALVO_ENV_FILE` y con un archivo de
      entorno vacío que él mismo crea, para ver que se niega las dos veces, y con `bash -n`.
12. **Los documentos que esta tarea vuelve falsos**, y solo ésos: `.env.example`
    (`ANTHROPIC_MODEL=claude-sonnet-5-5`), las afirmaciones del `README.md` sobre el redactor, sobre
    `AI_PROVIDER=anthropic` y sus «Límites declarados» —con el agujero de D13 dicho como clase—, y las
    menciones a Anthropic de `Salvo-Getting-Started.md` y `Salvo-Portability.md`, **y la descripción
    del puerto en `Salvo-Portability.md`** —`Task<ExplanationDraft>` con un resumen nulo para el
    rechazo—, que D1 reemplaza. El Blueprint, el
    Overview, el Progress, el Workboard, `AGENTS.md`, `Salvo-MOC.md` y
    `Salvo-Project-Instructions.md` los cierra el coordinador al integrar.

### Las falsaciones que la entrega tiene que mostrar

Un test que nunca se vio fallar no está probado. Cada una se hace revirtiendo **solo** la línea que
protege, viendo el rojo con el nombre del test, y restaurando:

| Qué se rompe a propósito | Qué tiene que ponerse rojo |
| --- | --- |
| El adaptador reintenta una vez ante un 529 | El test que cuenta exactamente una petición por intento (D5) |
| Un `ILogger` registra `HttpRequestMessage.ToString()` | El espía de logs a nivel `Trace`, en el camino que funciona (D8) |
| Se quita la guarda de `SharedInstance` | El test que arranca con la bandera **y** con una clave presente (D9) |
| La hoja de hechos lleva el monto en centavos | El test explícito de las tres prohibiciones (D10) |
| La hoja de hechos lleva un número que ningún hecho respalda. El test **elige** ese número y antes **afirma** que no está entre los `ExplanationFacts` de esa entrada: los segundos del corpus son siempre 0, y el 0 puede estar respaldado por un campo en cero, que es la lección de `E9B` sobre los minutos enteros | El test de propiedad: todo número de la hoja está fundamentado (D10) |
| La regla de selección vuelve a elegir la fila más recientemente pedida, donde sea que viva | El test con una `READY` de la plantilla y una `FAILED` del modelo (D11) |
| La fábrica de tests deja de fijar `AI_PROVIDER=mock` | Los tests de integración corridos con **solo** `AI_PROVIDER=anthropic` exportado, sin clave ni modelo: el arranque se niega y los tests caen, sin una sola petición (D15) |
| `Retake` vuelve a borrar los tokens | El test de acumulación entre intentos (D7) |

### Fuera

- **Cambiar el verificador** —`ExplanationGrounding`, `ExplanationFacts`, `NumberTokenizer`, sus
  tolerancias— para que un texto pase. Es la regla de la etapa. La única excepción es `e3-v2` en
  `VersionStrings`, del punto 5.
- **Tocar `ProviderCall`** o cualquier pieza del proveedor antifraude.
- **Llamar a la API real** desde un test, desde la compuerta, desde el smoke o desde cualquier
  comando que corra el agente. La única corrida que **podría** llegar a la red es la del entorno
  contaminado de la tabla de verificación, y existe para probar que no llega: si llegara, lo haría
  con una clave ficticia, recibiría un 401 sin costo ni secreto, y el smoke caería. Se corre **solo**
  después del commit 2, con las protecciones puestas.
- Streaming, herramientas, varias llamadas por intento, caché de prompt.
- El SDK oficial de C#: es candidata registrada, no esta tarea.
- La verificación por señal: es candidata registrada.
- Migraciones. Ninguna decisión del diseño las necesita.
- La regla de dispositivo y todo lo demás de «Mejoras para Salvo».

### Paths autorizados

- `backend/src/Salvo.Application/Explanations/**`.
- `backend/src/Salvo.Application/Alerts/AlertProjection.cs`, `AlertContext.cs`, `GetAlertHandler.cs` y
  `ReviewAlertHandler.cs`, **solo** para D11.
- `backend/src/Salvo.Domain/Explanations/**`, **excepto** `ExplanationGrounding.cs` y
  `ExplanationFacts.cs`, que solo se leen.
- `backend/src/Salvo.Infrastructure/Explanations/**`, incluidos los archivos nuevos del adaptador.
- `backend/src/Salvo.Infrastructure/DependencyInjection.cs`, **solo** `AddExplanationProvider` y lo
  que registre.
- `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs`, **solo** la lectura de explicaciones
  —`ComposeAsync`, `GetExplanationsAsync`, `CurrentExplanationOf` y el constructor si el escritor
  vigente tiene que llegar ahí—, y `EfExplanationStore.cs` si D11 lo pide.
- `backend/src/Salvo.Api/**`, **solo** si la forma de la respuesta o el registro lo exigen.
- `backend/tests/**`.
- `frontend/openapi/salvo-openapi.json`, `frontend/src/lib/api/**`, `frontend/src/test/**`.
- `frontend/src/app/alerts/[id]/explanation-*`, y `review-panel.tsx` con su test **solo** si la forma
  de la respuesta lo exige.
- `frontend/src/lib/i18n/es.ts`, `pt.ts` y `glosario-pt.md`, **solo** las claves de la explicación y el
  rótulo de `MalformedOutput`.
- `scripts/check.sh`, `scripts/smoke-ui.sh`, `scripts/demo.sh`, `scripts/capturas.sh` y
  `scripts/hornear-base.sh`, **solo** la línea de D15 y su comentario. `scripts/explicar-con-anthropic.sh`,
  nuevo.
- `.env.example`, `README.md`, `DesignAgent/Salvo-Getting-Started.md` y
  `DesignAgent/Salvo-Portability.md`, **solo** lo del punto 12.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11B-ADAPTADOR-ANTHROPIC.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno en uso. `E11C` no tiene brief y empieza cuando ésta esté integrada.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no**. Ni el SDK, ni `Microsoft.Extensions.Http`, ni ningún paquete de npm.
- Red: **solo** leer la documentación oficial de Anthropic y de las herramientas del proyecto.
- **Secretos: ninguno, nunca.** El agente no lee `.env` —la configuración lo deniega a propósito—, no
  pide la clave, no la escribe en ningún archivo, test, fixture, log ni handoff. Los tests usan un
  valor ficticio evidente, construido en el propio test.
- Escrituras externas: **no**. El agente no hace push.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] Los doce commits del orden de «Dentro», en ese orden, cada uno con la compuerta verde.
- [ ] D15: los cinco scripts exportan `AI_PROVIDER=mock` y la fábrica de tests lo fija, en un commit
      anterior al adaptador; con su falsación y con la corrida del entorno contaminado.
- [ ] D1: el puerto devuelve el desenlace cerrado; `ProviderCall` y el antifraude no cambian un byte
      (`git diff` vacío sobre esos archivos); `AProviderThatThrowsIsRecordedRatherThanRaised` sigue
      verde.
- [ ] D7: tokens en `Fail`, `Retake` no los borra, la fila acumula; con su falsación.
- [ ] D10: `e3-v2` en `VersionStrings`, con su test; la propiedad de la hoja y el test explícito de
      sus tres prohibiciones, cada uno con su falsación, en los dos idiomas; el espía extendido al
      **cuerpo HTTP** que sale.
- [ ] D13: los tres tests de caracterización, nombrados por la clase, en verde.
- [ ] D5: una petición por intento, afirmada contando peticiones, con su falsación.
- [ ] D6: **un test por fila** de la taxonomía, contra el transporte simulado, con las formas de la
      documentación del día —con `request-id` reales de la forma documentada—; uno que afirma que
      `FailureDetail` nunca lleva texto del modelo; uno con un `error.type` fuera de la lista
      cerrada, que termina como `unrecognized`; y uno con `thinking_tokens` mayor que cero, que deja
      su log.
- [ ] El cuerpo de la petición lleva `thinking: {"type": "between_tools"}`, `output_config.effort:
      "medium"`, el esquema constante y `max_tokens` = 1.024, afirmado por un test.
- [ ] D8: el espía a nivel `Trace`, camino feliz y camino que falla, buscando la clave en cada
      mensaje y en el `ToString()` de cada excepción; con su falsación. El arranque sin clave o sin
      modelo falla nombrando la variable.
- [ ] D9: el arranque con `SharedInstance:Enabled=true`, `AI_PROVIDER=anthropic` **y** clave
      falla; con su falsación.
- [ ] D11: los cinco casos de la regla, cada uno con su test, y la revisión que cita la mostrada y
      no el intento; con su falsación.
- [ ] D12: el test del bloque de explicación cubre la oración de la plantilla y la del modelo, en los
      dos idiomas; la de la plantilla, sin una letra cambiada; y lo mismo para el botón y para el
      título posterior.
- [ ] El botón de D11, en sus dos mitades: en el frontend, el pedido que arma cada caso —o la
      ausencia de botón con una `PENDING` en curso—; en el backend, ese mismo pedido sobre el estado de
      cada caso, con `applied: true` y un intento más donde hay botón.
- [ ] El contrato recapturado y `OpenApiDriftTests` verde.
- [ ] El script se niega sin clave y pasa `bash -n`; **no se corrió contra Anthropic**.
- [ ] `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes, **sin clave en el entorno**.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` | Verde |
| `./scripts/smoke-ui.sh` | Verde, con la frase de la plantilla intacta |
| Las ocho falsaciones | Rojo con el nombre del test, y verde al restaurar |
| `AI_PROVIDER=anthropic`, `ANTHROPIC_MODEL=claude-sonnet-5-5` y una clave **ficticia** exportados, y después `./scripts/check.sh` y `./scripts/smoke-ui.sh` | Los dos verdes. Si algo llamara a la API, la clave ficticia daría 401 y el smoke caería: el verde prueba que nada la llamó (D15) |
| `git diff <base>..HEAD -- backend/src/Salvo.Application/Providers backend/src/Salvo.Application/External` | Vacío |
| `git diff <base>..HEAD -- backend/src/Salvo.Domain/Explanations/ExplanationGrounding.cs backend/src/Salvo.Domain/Explanations/ExplanationFacts.cs` | Vacío |
| `git grep` del valor ficticio de la clave fuera de los tests | Sin resultados |
| `scripts/explicar-con-anthropic.sh` con un archivo de entorno vacío | Se niega, nombra la variable, no imprime ningún valor |
| **Al integrar** —lo observa el coordinador, que sube la rama después del handoff—: la corrida de GitHub Actions | Verde: la CI no tiene clave, y nada debe pedirla. El agente no espera esta corrida |

## Decisiones delegadas

- La forma exacta del desenlace del puerto, mientras sea cerrada y por valor.
- Dónde vive la regla de D11.
- La redacción del prompt `anthropic-p1`, de la hoja de hechos y de las oraciones de D12.
- El nombre y la forma del archivo que escribe el script, dentro de la carpeta fechada.
- El plazo del puerto: los 15 s actuales se conservan salvo que la documentación justifique otro. Con
  el esquema constante, la gramática se compila una sola vez, y `E11C` lee un `ProviderTimeout` en la
  primera petición sabiéndolo.

## Detenerse y consultar si

- **la documentación del día contradice el diseño**: el ID del modelo, el parámetro de la salida
  estructurada, la forma de los errores, los `stop_reason`, la cabecera de versión;
- algo exige **cambiar el verificador** o **tocar `ProviderCall`**;
- algo exige una **migración** o una **dependencia**;
- un paso pide la **clave**, leer `.env` o llamar a la API real;
- la frase de la plantilla o las anclas del smoke tienen que cambiar;
- la documentación del día dice algo distinto de lo que «Correcciones» cita sobre `between_tools`,
  el esfuerzo o el cobro del razonamiento;
- un test existente cae y arreglarlo exige tocar algo fuera de los paths autorizados.

## Entrega requerida

- Resumen, archivos tocados, y los doce commits.
- **La tabla de «Datos verificados» rehecha**, con la fecha de lectura y lo que cambió.
- Las ocho falsaciones, con su salida, y la corrida de D15 con el entorno contaminado.
- Una muestra de la hoja de hechos para una alerta del corpus, y el prompt `anthropic-p1` entero: son
  lo único que viaja a la API, junto con el esquema.
- El costo estimado por explicación y el techo de la corrida, recalculados con `max_tokens` y los
  precios del día.
- Supuestos, riesgos y pendientes, y lo que `E11C` tiene que mirar primero.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
