# Salvo — Etapa 11, la IA de verdad — diseño v2

> Estado: **v2, después de la revisión adversarial.** Lista para escribir el brief de `E11B`.
> Fecha: 2026-10-01
> Coordinador: Unai Arismendes
> Revisión: `Coordination/Tasks/E11-revision-adversarial.md` — quince hallazgos, cinco altos. La v1
> queda en la historia de Git, que es donde una versión equivocada sirve de evidencia.
> Base: la Etapa 7 dejó el puerto, la verificación, las columnas de costo y los códigos de fallo
> preparados para este adaptador. Esta etapa escribe lo que falta y **no afloja nada de lo que ya
> existe**.

## El resultado de la etapa, en una frase

Un modelo de Anthropic puede redactar la explicación de una alerta, **gobernado por el mismo
verificador que hoy gobierna a la plantilla**, sin que la integración continua ni la instancia
pública tengan jamás la clave, y sin que la consola diga una sola cosa falsa sobre quién escribió qué.

## Qué cambió de la v1, y por qué

Tres de los cinco hallazgos altos son afirmaciones de la v1 que el repositorio ya desmentía, escritas
sin abrir el archivo que las sostenía. Es la lección más vieja del proyecto, aplicada al diseño que
la cita.

| Hallazgo | Lo que la v1 decía | Lo que la v2 decide |
| --- | --- | --- |
| 1, alta | Una excepción propia, clasificada «antes» del clasificador genérico | `ProviderCall` descarta la excepción antes de ese punto. El puerto devuelve un **desenlace cerrado, por valor**, como ya hace el puerto antifraude (D1) |
| 2, alta | El modelo entra en la identidad, «sin contradecir» ninguna decisión | Contradecía la decisión de la Etapa 7 en tres lugares. **Se conserva la de la Etapa 7**: el modelo nunca es parte de la identidad (D4). Y la consola deja de esconder un texto correcto detrás de un intento fallido (D11) |
| 3, alta | Publicar los textos rechazados | `AGENTS.md` lo prohíbe. Se publican los aceptados y, de los rechazados, el código y la cifra que los hizo fallar (D16) |
| 4, alta | Ninguna línea de frontend | La consola diría «Redactada por una Anthropic, no por un modelo». Una oración por proveedor, en los dos idiomas (D12) |
| 5, alta | `ExplanationInput` serializado tal cual | El modelo copia lo que ve, y ve cosas que el verificador no puede fundamentar. Una hoja de hechos renderizada como la plantilla las escribe (D10) |
| 6, media | «El costo real sale de los tokens que la fila guarda» | La fila no guarda los tokens de un intento fallido. Se guardan en todo desenlace, acumulados (D7) |
| 7, media | Todo 429 y todo 4xx a `ProviderUnavailable`, con un log | El tope de gasto del tier no es transitorio, y el lugar visible es `FailureDetail`, no un log (D6) |
| 8, media | «Cero dependencias nuevas»; el SDK descartado sin compararlo | Existe un SDK oficial. **El coordinador elige `HttpClient` directo** y deja el SDK anotado como candidata (D2) |
| 9, media | `claude-haiku-4-5` | Su compromiso de disponibilidad vence el 2026-10-15. **`claude-sonnet-5-5`**, con compromiso hasta el 2027-09-28 (D4) |
| 10, media | Un test que fija una oración invertida | Se fija la **clase**: inversión, atribución cruzada y cifras en palabras. La lectura humana es observación, no compuerta (D13) |
| 11, media | Cuatro cambios de estado canónico | Son más de veinte lugares. La lista completa está al final |
| 12, baja | Detalles de la API sin escribir | `anthropic-version`, el esquema sin `maxLength`, `referencedRules` como `enum`, `stop_reason` antes de deserializar (D3) |
| 13, baja | — | Los scripts heredan `AI_PROVIDER` de la terminal: `hornear-base.sh` podría **hornear texto de un modelo en la imagen pública** (D15) |
| 14, baja | Techo de costo con 300 tokens de salida | Se calcula con `max_tokens`. Y el espacio de trabajo por defecto **no admite** topes (D14) |
| 15, baja | Un espía de logs sobre un camino que falla | Nivel `Trace`, camino feliz incluido, y `HttpRequestMessage.ToString()`, que vuelca cabeceras (D8) |

## Lo que no cambia

- La IA **redacta** la explicación de una decisión ya tomada. **Nunca decide** fraude, severidad ni
  bloqueo. El interceptor de SQL y los tres diferenciales de la Etapa 7 lo siguen probando, sin tocarlos.
- Al modelo le entra **solo texto que escribe el motor**. El proveedor espía lo sigue probando.
- Todo texto pasa por `ExplanationGrounding` **antes** de persistirse, en el caso de uso.
- **Un texto rechazado no se persiste, no se registra y no llega al diagnóstico** (`AGENTS.md`). Se
  registra el token ofensor, nunca la frase. Esta etapa no abre una excepción.
- **Si el modelo no pasa el verificador, se cambia el prompt, nunca el verificador.**

## Datos verificados

Leídos en la documentación oficial el 2026-09-30 y el 2026-10-01. `E11B` vuelve a abrir estas
páginas el día que ejecute y rehace la tabla si algo cambió.

| Qué | Valor | Fuente |
| --- | --- | --- |
| La suscripción de Claude no incluye la API | Se pagan por separado | [support.claude.com](https://support.claude.com/en/articles/9876003-i-have-a-paid-claude-subscription-pro-max-team-or-enterprise-plans-why-do-i-have-to-pay-separately-to-use-the-claude-api-and-console) |
| Modelo elegido | `claude-sonnet-5-5`, activo, **compromiso hasta el 2027-09-28**. US$ 2 / 10 por millón de tokens de entrada / salida | [Deprecations](https://platform.claude.com/docs/en/about-claude/model-deprecations), [Pricing](https://platform.claude.com/docs/en/about-claude/pricing) |
| Modelo descartado | `claude-haiku-4-5-20251001`, compromiso **hasta el 2026-10-15** | Deprecations |
| El ID sin fecha es un snapshot fijo | Desde la generación 4.6, «Anthropic does not update the weights or configuration of an existing model ID» | [Model IDs](https://platform.claude.com/docs/en/about-claude/models/model-ids-and-versions) |
| Salida con esquema garantizado | `output_config.format`, `type: "json_schema"`; Sonnet 5.5 la soporta | [Structured outputs](https://platform.claude.com/docs/en/build-with-claude/structured-outputs) |
| Cuándo **no** se garantiza el esquema | `stop_reason: "refusal"` —con HTTP 200, y puede venir sin JSON— y `"max_tokens"` | Structured outputs, [Stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons) |
| Lo que el esquema no admite | `minLength`, `maxLength`. Sí admite `enum`, sin garantizar mayúsculas | Structured outputs |
| Cabecera obligatoria | `anthropic-version` | [Versioning](https://platform.claude.com/docs/en/api/versioning) |
| Errores transitorios | 429 con `retry-after`, 500, 504, 529 | [Errors](https://platform.claude.com/docs/en/api/errors) |
| Tope de gasto propio alcanzado | 400 `invalid_request_error` — **mismo tipo** que un pedido mal formado | Errors, [Rate limits](https://platform.claude.com/docs/en/api/rate-limits) |
| Tope de gasto del tier alcanzado | 429 `rate_limit_error`, **sin `retry-after`**, con `error.details.error_code = "enforced_spend_limit_reached"` | Rate limits |
| Topes por espacio de trabajo | Sí, mensuales. **El espacio por defecto no admite topes** | [Workspaces](https://platform.claude.com/docs/en/manage-claude/workspaces) |
| SDK oficial para C# | Existe: paquete `Anthropic`, con reintentos configurables y `HttpClient` inyectable | [SDK C#](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp) |
| Trazabilidad | Cabecera `request-id` en toda respuesta | Errors |
| Primera petición con un esquema | Compila una gramática: es más lenta, y se cachea 24 horas | Structured outputs |

---

## D1 — El puerto devuelve un desenlace cerrado, por valor

Hoy un adaptador solo puede avisar un problema lanzando una excepción, y `ProviderCall` —compartido con
el proveedor antifraude— la convierte en `Faulted` sin dejar cruzar qué se lanzó: lo dice su propio
comentario. Un texto cortado terminaría como «el proveedor no estaba disponible», que es falso.

La v2 hace lo que el puerto antifraude ya hace por construcción. El puerto devuelve un **resultado con
un desenlace cerrado**:

| Desenlace | Cuándo | Código que asigna `ExplanationExchange` |
| --- | --- | --- |
| `Drafted` | Hay borrador | Ninguno: va al verificador, que decide |
| `Refused` | El proveedor rehusó escribir | `ProviderRefused` |
| `Malformed` | Texto cortado, ilegible, o un `stop_reason` inesperado | `MalformedOutput` |
| `Unavailable` | El proveedor no pudo responder | `ProviderUnavailable` |

Más el borrador cuando lo hay, **los tokens en todo desenlace** (D7), `ProviderVersion` tomado de la
respuesta, y un diagnóstico corto **sin texto del modelo** (D6).

- **El código lo fija el intercambio, no el adaptador.** El adaptador no puede nombrar
  `NotGroundedNumber`, `NotGroundedRule`, `TooLong`, `Cancelled`, `AttemptLimitReached` ni
  `LegacySignalFormat`: son del verificador, del ciclo de vida o del sistema.
- **`ProviderCall` no se toca.** `Faulted` sigue existiendo para lo que nadie esperaba y sigue siendo
  `ProviderUnavailable`. El antifraude no cambia un byte.
- Los productores del borrador cambian con él: `DeterministicExplanationProvider` y los proveedores de
  prueba de `ExplanationTestCorpus`. El test `AProviderThatThrowsIsRecordedRatherThanRaised` sigue
  valiendo para la excepción imprevista.
- `MalformedOutput` amplía su significado a «cortado o ilegible»: su documentación y su rótulo en los
  dos diccionarios cambian con él.

## D2 — `HttpClient` directo, y el SDK anotado como candidata

**El coordinador elige `HttpClient` directo** contra la API documentada, con estos fundamentos:

- **Cero dependencias nuevas** en un proyecto que fija y bloquea cada paquete.
- **Ningún canal de log ajeno que auditar** por la clave.
- **La clasificación fina de los errores de gasto necesita la respuesta cruda igual** —la presencia de
  `retry-after` y `error.details.error_code`—, así que el SDK no ahorra esa parte.

**El costo, dicho**: los detalles del protocolo son nuestros. La cabecera `anthropic-version`, la forma
de la salida estructurada, los `stop_reason`. Se cubren con tests sobre las formas documentadas.

Cómo se arma:

- **Un `HttpClient` de vida larga**, en un servicio singleton. Crear uno por pedido agota los sockets,
  y el proveedor se registra hoy como `Scoped`.
- **Sin tubería de registro HTTP.** Sin `IHttpClientFactory` ni su registro de cabeceras, no hay nada
  que redactar, y el test de D8 afirma la ausencia. Eso evita además sumar `Microsoft.Extensions.Http`.
- **Una petición por intento, como test**: el transporte simulado cuenta las peticiones y afirma
  exactamente una. D5 deja de ser promesa.

**El SDK oficial queda anotado como candidata, por pedido del coordinador.** El paquete `Anthropic`
existe, es oficial, admite `MaxRetries = 0` y un `HttpClient` inyectado, y tiene excepciones tipadas.
Cuesta tres dependencias y un canal de log de depuración propio. Si algún día el protocolo crece
—streaming, herramientas, varias llamadas—, el balance cambia, y conviene reabrir la comparación.

## D3 — Salida estructurada, con lo que el esquema puede y no puede pedir

- El esquema es el espejo del borrador: `summary` y `referencedRules`, con `additionalProperties:
  false`.
- **`referencedRules` es un `enum` de las reglas que dispararon en esa evaluación.** Citar una regla
  que no disparó se vuelve imposible por construcción. Como la API no garantiza mayúsculas en un
  `enum`, el adaptador normaliza a minúsculas antes de entregar el borrador.
- **El esquema no admite `maxLength`**, así que el tope de 1.200 caracteres sigue siendo del
  verificador, como `TooLong`. Está bien que sea así: el verificador ya lo hace.
- **`stop_reason` se lee antes de deserializar.** Un `refusal` puede venir sin JSON.

## D4 — El modelo, y por qué no entra en la identidad

**Modelo: `claude-sonnet-5-5`.** Haiku 4.5 era más barato, pero su compromiso de disponibilidad vence
el 2026-10-15, dos semanas después de este diseño. Sonnet 5.5 cuesta el doble y su compromiso llega al
2027-09-28. Para algo que se muestra en entrevistas durante meses, la disponibilidad pesa más que un
dólar. Y desde la generación 4.6 el ID sin fecha **es** el snapshot fijo: no hay alias que se mueva.

**La identidad: se conserva la decisión de la Etapa 7.** La versión de plantilla del adaptador es
**`anthropic-p1`** —la versión del prompt—, y el modelo va a `ProviderVersion`, tomado de la
**respuesta** y no de la configuración. El Blueprint, el diseño de la Etapa 7 y la entidad lo dicen
con las mismas palabras: el modelo concreto **nunca** es parte de la identidad.

- **Con un modelo, «el texto cambió» no sirve como criterio de versión**, y por eso la decisión 64 no
  aplica: un modelo escribe distinto cada vez que se lo llama, con el mismo prompt y el mismo modelo.
  Lo único que el sistema controla es el prompt, y eso es lo que identifica.
- **Cambiar de modelo es cambiar una variable**, no rehacer todas las explicaciones.
- **No hay migración**: `anthropic-p1` entra en los 32 caracteres que declara la columna.

## D5 — Ningún reintento invisible

Una llamada HTTP por intento, y ninguna más, **afirmada por un test** (D2). Los reintentos existen,
pero son los de la fila: tres intentos (`AlertExplanation.MaximumAttempts`), cada uno visible, contado
y con su código. El costo por evaluación queda acotado a tres llamadas.

## D6 — La taxonomía de fallos

| Lo que pasa | Desenlace | Lo que queda en `FailureDetail` |
| --- | --- | --- |
| HTTP 200, `end_turn`, esquema válido | `Drafted` | — el verificador decide |
| `stop_reason: "refusal"` | `Refused` | La categoría de política, si viene |
| `"max_tokens"`, `"model_context_window_exceeded"`, cualquier otro `stop_reason` | `Malformed` | El `stop_reason` |
| Cuerpo ilegible | `Malformed` | `unreadable` |
| 429 **con** `retry-after`, 500, 504, 529, error de red | `Unavailable` | Tipo de error y `request-id` |
| 429 **sin** `retry-after`, o con `enforced_spend_limit_reached` | `Unavailable` | **`spend cap`**, tipo y `request-id` |
| 400 | `Unavailable` | Tipo y `request-id` |
| 401, 403 | `Unavailable` | **`credentials`**, tipo y `request-id` |
| 402, 404, 409, 413, otros 4xx | `Unavailable` | Tipo y `request-id` |
| Vence el plazo del puerto | — | `ProviderTimeout`, como hoy |

Las reglas detrás:

- **Se lee `error.type`, `error.details.error_code` y la presencia de `retry-after`. Nunca `message`**,
  que es prosa del proveedor y puede cambiar sin aviso.
- **Un 400 de tope propio es indistinguible de un 400 de formato** sin leer `message`. Se acepta y se
  dice: la consola de Anthropic muestra el gasto, y el `request-id` permite cruzarlo.
- **La clasificación va a `FailureDetail`**, que ya existe, mide 200 caracteres, nunca lleva texto del
  modelo y se ve en la consola. Un log en una demo no lo mira nadie. El log queda además, a nivel
  `Warning`, para toda respuesta que no sea `2xx`.
- **`max_tokens` se fija con margen** por encima de lo que piden 1.200 caracteres, para que un texto
  largo lo detecte el verificador como `TooLong` y no se corte como `MalformedOutput`.

## D7 — Los tokens se guardan en todo desenlace, acumulados

Hoy la fila guarda tokens solo cuando el texto se acepta: un `refusal`, un texto cortado o un
`NotGroundedNumber` no dejan rastro de lo que costaron, y un reintento borra los del intento anterior.
Lo que se mide así es el costo del subconjunto barato.

- **`Fail` recibe tokens**, y `Retake` **deja de borrarlos**.
- **La fila acumula entre intentos.** El costo de una explicación es lo que se pagó por ella, no lo
  que costó el último intento.
- No hay migración: la restricción de la columna ya admite tokens en cualquier estado.

## D8 — El secreto

- **Un solo lector**: el registro de dependencias de `Infrastructure` lee `ANTHROPIC_API_KEY`.
- **Falla al arrancar** si `AI_PROVIDER=anthropic` llega sin clave o sin modelo. El mensaje **nombra la
  variable y nunca su valor**.
- **Ningún objeto con cabeceras llega a un log.** `HttpRequestMessage.ToString()` vuelca las cabeceras,
  y con ellas la clave.
- **El test del espía de logs corre a nivel `Trace`**, sobre el camino que funciona **y** sobre el que
  falla, y busca el valor de la clave en cada mensaje de log y en el `ToString()` de cada excepción.
- **Lo que sí llega al navegador, y no es secreto**: `ProviderVersion` —el nombre del modelo— y la
  versión del prompt. Se dice para que nadie lo tome por una fuga.

## D9 — La instancia pública no puede arrancar con una clave paga

`SharedInstance:Enabled=true` junto con `AI_PROVIDER=anthropic` detiene el arranque. La guarda vive al
lado de la que ya lee `AI_PROVIDER`. **El test arranca con la bandera y con una clave presente**: un
test sin clave probaría otra cosa.

## D10 — Una hoja de hechos, no los datos crudos

La plantilla nunca copia su entrada: la **renderiza**. Un modelo al que se le dice «usá solo los
hechos dados» copia lo que ve, y en `ExplanationInput` ve cosas que el verificador no puede
fundamentar: centavos, instantes en formato ISO con segundos, nombres de versión.

- **El adaptador construye una hoja de hechos** con las mismas funciones que usa la plantilla
  —`ExplanationFigures` para montos, razones, minutos, horas y años—, con el nombre y el peso de cada
  regla, **sin versiones, sin centavos y sin instantes ISO**.
- **El prompt exige cifras en dígitos, nunca en palabras.** El tokenizador no valida cifras escritas
  en palabras, y un modelo las va a usar en la primera frase que diga «cuatro reglas».
- **Un test lo vuelve propiedad**: todo número que aparece en la hoja que viaja al modelo está
  fundamentado por `ExplanationFacts`. Es el espejo del espía: el espía afirma qué no entra; éste
  afirma que lo que entra se puede verificar.
- **El espía se extiende al cuerpo HTTP** que sale por la red.
- **`e3-v2` entra en `NumberTokenizer.VersionStrings`.** Falta desde `E9B` y hoy no cuesta nada porque
  la plantilla no escribe versiones; es un defecto latente que esta etapa cierra igual.

## D11 — Qué muestra la consola cuando el escritor falla

Hoy la consola muestra la fila **más recientemente pedida**, sin mirar quién la escribió. Con la
plantilla no importaba, porque la plantilla no falla. Con un modelo, un intento fallido **esconde un
párrafo correcto** que sigue en la base. Y volver a la plantilla deja un botón que no hace nada
visible.

La regla de selección para mostrar:

1. Si hay una fila `READY` del escritor vigente —mismo proveedor, versión e idioma—, se muestra ésa.
2. Si el intento vigente está pendiente o falló **y existe una `READY` de otro escritor**, se muestra
   la `READY`, rotulada con quién la escribió, **y además** el estado del intento vigente con su
   código.
3. Si no hay ninguna `READY`, se muestra el estado del intento vigente, como hoy.

**Nunca se esconde un texto aceptado detrás de un intento fallido.** Dónde se implementa —la lectura
del almacén o la proyección— lo decide `E11B` dentro de los paths que el brief reserve.

## D12 — La consola dice quién escribió qué

El pie de la explicación dice hoy *«Redactada por una plantilla determinista, no por un modelo»*. Con
el adaptador diría *«Redactada por una Anthropic, no por un modelo»*: una afirmación falsa dentro del
producto, el mismo defecto que la Etapa 9 encontró mirando las capturas.

- **Una oración por proveedor**, en los dos diccionarios. La de la plantilla conserva «no por un
  modelo», que es lo que el smoke comprueba en la instalación por defecto. La del modelo dice qué
  modelo —desde `ProviderVersion`— y qué versión de prompt.
- **«Redactar con la plantilla vigente»** se vuelve «redactar con el escritor vigente», o su
  equivalente: lo vigente puede ser un prompt.
- **`ProviderVersion` se muestra.** Hoy viaja y nadie lo ve.
- **Las anclas del smoke siguen comprobando la frase de la plantilla**, porque el smoke corre sin
  clave. El test del bloque de explicación gana el caso del modelo.

## D13 — El agujero semántico se fija como clase

El verificador comprueba que cada cifra **exista** entre los hechos, no que la oración sea verdadera.
Esta etapa no lo cierra, y lo fija con tests de caracterización que **afirman que pasan**, nombrados
por la clase y no por el ejemplo:

- **Inversión**: «3,4 veces **menor** que la mediana».
- **Atribución cruzada**: una cifra verdadera pegada al hecho equivocado —«la mediana fue 1.250»,
  usando el monto—.
- **Cifras en palabras**: «se dispararon cuatro reglas», «el triple de la mediana».

Si alguien cierra alguno, su test se pone rojo y obliga a corregir lo que el README dice.

**La lectura humana es observación, no compuerta.** Una fila `READY` es inmutable: si el coordinador lee
un texto falso, no puede rechazar **ese** texto. Su palanca es subir el prompt a `p2`, que rehace
todas. Se dice así para que nadie crea que la lectura protege algo que no protege.

Se consideró y se deja como candidata una verificación **por señal**: cada oración atada a una regla y
sus cifras verificadas solo contra los hechos de esa regla. Cerraría la atribución cruzada, pero cambia
la forma del puerto y de la plantilla, y eso es otra etapa.

## D14 — Costo: estimado, acotado y medido

- **Estimación**: una explicación ronda 1.500 tokens de entrada y 300 de salida. Con Sonnet 5.5 son
  US$ 0,003 + US$ 0,003, cerca de **US$ 0,006**. El número real sale de los tokens de la fila (D7).
- **Techo de la corrida real, calculado con `max_tokens` y no con la estimación**: un texto cortado
  cobra todo `max_tokens`. Con 1.024, el peor caso son 69 llamadas a US$ 0,013, cerca de **US$ 0,91**.
- **El espacio de trabajo dedicado es obligatorio**: el espacio por defecto no admite topes. Tope
  sugerido: US$ 5 mensuales.
- **El script imprime el precio que usó y la fecha en que lo leyó.** Un costo sin fecha es lo que la
  decisión 69 prohíbe.
- **El adaptador no pide razonamiento extendido.** Si Sonnet 5.5 lo aplica por defecto, `E11B` lo
  verifica en la documentación del día y lo dice, porque cambia el costo.

## D15 — Los scripts no heredan el proveedor de la terminal

`smoke-ui.sh`, `demo.sh`, `capturas.sh` y `hornear-base.sh` lanzan la API con el entorno que heredan, y
ninguno fija `AI_PROVIDER`. Después de esta etapa, una terminal con la clave exportada haría que el
smoke gaste dinero, y que `hornear-base.sh` **hornee texto de un modelo dentro de la imagen pública**.
Los cuatro exportan `AI_PROVIDER=mock`. Una línea por script.

El script de la corrida real es el **único** que carga `.env`, con `set -a; . ./.env; set +a`, y no
imprime ni exporta más de lo que la API necesita.

## D16 — La evidencia que se publica

- **Los textos aceptados**, con la fecha, el modelo —tomado de la respuesta— y la versión del prompt.
- **De cada rechazado, lo que la fila ya guarda**: el código y el token ofensor. Nunca la frase.
- En **una carpeta propia y fechada**. `docs/muestras/` es de las muestras de importación.
- **Es una tirada, no un dorado.** Un modelo no es determinista: lo publicado no se compara contra nada
  en un test.

---

## Partición

1. **`E11A-INTEGRACION-CONTINUA`** — brief escrito, en corrección tras su `brief-check`. Va primero.
2. **`E11B-ADAPTADOR-ANTHROPIC`** — D1 a D15. Backend, frontend y scripts. **Opus 5.5 · `high`**.
3. **`E11C-CORRIDA-REAL`** — el coordinador crea la cuenta, el espacio de trabajo dedicado con tope y la
   clave, y corre el script. El agente lee la salida, publica la evidencia (D16) y actualiza los
   documentos. **La primera petición puede ser la lenta** —la gramática se compila— y un
   `ProviderTimeout` ahí se lee sabiendo eso.

## Condiciones de parada de la etapa

- Si en la corrida real la mayoría de los textos no pasa el verificador, **se cambia el prompt, no el
  verificador**. Si ni así pasan, la decisión es del coordinador.
- Si la API real se comporta distinto de lo que el transporte simulado supone, **se corrige el
  simulador primero** y la corrida se repite.
- Si algún paso pide la clave dentro de un agente, se detiene.

## Candidatas registradas

- **El SDK oficial de Anthropic para C#**, por pedido del coordinador (D2).
- **Verificación por señal** (D13).

## Cambios de estado canónico que exige este diseño

La v1 listaba cuatro. Abriendo los archivos, lo que afirma «no hay adaptador» o «no por un modelo» está
en todos estos lugares, y la lección de cerrar el estado canónico **por lista** pide recorrerlos todos:

1. **Blueprint, bitácora**, desde la 73 —la 71 es la apertura de la etapa y la 72 pone `E11A0` antes de `E11A`—: el desenlace cerrado del puerto (D1), el modelo y la identidad
   conservada (D4), ningún reintento invisible (D5), los tokens en todo desenlace (D7), la instancia
   pública sin clave (D9), la hoja de hechos (D10), la selección de la consola (D11) y el agujero
   fijado como clase (D13).
2. **Blueprint** §9 —`ANTHROPIC_MODEL` con su fecha— y las menciones a un adaptador inexistente.
3. **`.env.example`**: `ANTHROPIC_MODEL=claude-sonnet-5-5`.
4. **README**: la fila «Redacción de explicaciones», la frase sobre `AI_PROVIDER=anthropic`, el pie de
   la captura de la explicación, y «Límites declarados».
5. **Overview**: la tabla de estado y el «Próximo paso».
6. **Getting-Started** y **Portability**: cada mención a Anthropic.
7. **Progress**: el checklist abierto «Decidir aparte si se activa Anthropic».
8. **Código**: la documentación de `ExplanationProvider.Anthropic` y el mensaje de error de
   `AddExplanationProvider`, que hoy afirman que el adaptador no existe.
9. **Workboard**: la fila de `E11B`, con Opus 5.5 · `high`.
10. **`AGENTS.md`**: la regla de que si el modelo no pasa, se cambia el prompt y no el verificador.
