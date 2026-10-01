# Salvo — Revisión adversarial del diseño propuesto de Etapa 11

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-30
> Revisor: Claude (Fable 5.1)
> Objeto: `Coordination/Tasks/E11-DISENO.md` v1 (commit `ec6b3b7`, punta de `main`)
> Método: lectura del diseño, del Blueprint (§4 «AlertExplanation», §7, §9 y la bitácora completa,
> decisiones 51 a 70), Overview, Progress, Workboard con sus lecciones, Getting-Started,
> Portability, el README entero, el diseño de E7 y el brief de `E11A`; del código real en
> `Salvo.Application/Providers/ProviderCall.cs`, `Explanations/ExplanationExchange.cs`,
> `External/ExternalProviderExchange.cs`, `Explanations/IExplanationProvider.cs`,
> `RequestExplanationHandler.cs`, `ExplanationInputFactory.cs`, `ExplanationViews.cs`,
> `ExplanationOptions.cs`, `Salvo.Domain/Explanations/*` (entidad, códigos, grounding, hechos,
> tokenizador, `SignalFacts`, `ExplanationInput`), `Salvo.Domain/Risk/RuleConfig.cs` y `RiskSignal.cs`,
> `Salvo.Infrastructure/DependencyInjection.cs`, `Explanations/DeterministicExplanationProvider.cs`,
> `ExplanationFigures.cs`, `ExplanationVocabulary.cs`, `Persistence/AlertExplanationConfiguration.cs`,
> `EfExplanationStore.cs`, `EfAlertStore.cs`, la migración `20260905013514_Explanations`,
> `Salvo.Api/Program.cs`, `SystemEndpoints.cs`, `ExplanationEndpoints.cs`, `appsettings*.json`, los
> tres `csproj`, `Directory.Packages.props` y los `packages.lock.json`; los tests
> `ExplanationIsolationTests`, `ExplanationGroundingTests`, `ExplanationFactsTests`,
> `ExplanationTemplateVersionTests` y `ExplanationTestCorpus`; el frontend en
> `frontend/src/lib/i18n/es.ts` y `pt.ts`, `app/alerts/[id]/explanation-block.tsx` y su test;
> `scripts/smoke-ui.sh`, `demo.sh`, `capturas.sh`, `hornear-base.sh`, `Dockerfile`, `.dockerignore`,
> `render.yaml`, `.env.example` y `.claude/settings.json`; y de **catorce páginas oficiales de
> Anthropic, NuGet y GitHub**, abiertas el 2026-09-30 y citadas al final con su URL, porque la tabla
> «Datos verificados» del diseño exige que cada afirmación sobre la API lleve fuente. No se ejecutó
> ninguna compuerta ni ningún test, no se hizo push y no se editó ningún archivo salvo este informe.

El diseño acierta en lo que más importa: **el verificador no se toca y el modelo no decide**, la
IA redacta sobre una decisión ya tomada, los tests no hacen red y la clave no entra ni a la
integración continua ni a la instancia pública. Y las nueve filas de la tabla «Datos verificados» son
ciertas, con un matiz cada una en el 429 y en el tope de gasto (hallazgo 7).

Lo que sigue son quince hallazgos, cinco de severidad alta, y casi todos comparten un patrón: **el
diseño describe el adaptador como si lo único que cambiara fuera quién escribe**, y lo que cambia es
que por primera vez el que escribe puede fallar, cobrar, copiar lo que se le da y equivocarse con
cifras verdaderas. La plantilla nunca ejercitó ninguno de esos cuatro caminos, así que el código que
los rodea —el canal de fallo del puerto, la columna de tokens, la lectura de «cuál explicación se
muestra», el pie de la consola, la forma de los hechos que viajan— está escrito para un escritor que
no falla, y el diseño lo hereda sin mirarlo.

Una cosa antes de empezar, porque reordena D4 y D6 a la vez: **dos de las afirmaciones centrales
del diseño están desmentidas por comentarios del propio código que el diseño cita.** D6 propone que
`ExplanationExchange` clasifique una excepción que `ProviderCall` ya descartó una línea antes. Y D4
mete el modelo en la identidad de la fila cuando la entidad, el Blueprint y el diseño de E7 dicen,
con las mismas palabras, que el modelo **nunca** es parte de la identidad.

---

## Hallazgos, por severidad

### 1. D6, alta. El canal propuesto para `MalformedOutput` no puede funcionar como está escrito, y hay un camino mejor que el puerto ya usa del otro lado

D6 propone «una excepción propia de `Application` —`ExplanationOutputMalformedException`— que
`ExplanationExchange` clasifica como `MalformedOutput` **antes** del clasificador genérico».

No hay un «antes». `ProviderCall.InvokeAsync` es quien recibe la excepción, y la descarta:

```csharp
// Salvo.Application/Providers/ProviderCall.cs
internal sealed record ProviderCallResult<T>(ProviderCallStatus Status, T? Value);
...
catch (Exception) when (!cancellationToken.IsCancellationRequested)
{
    return new(ProviderCallStatus.Faulted, default);
}
```

El resultado lleva un estado y un valor, y ningún campo para la excepción; el propio enum lo declara:
«`Faulted` — The provider threw. **What it threw does not cross this boundary.**» Cuando
`ExplanationExchange` ve el resultado, lo único que sabe es `Faulted`, y `Faulted` termina en
`ProviderUnavailable` (`ExplanationExchange.cs`, rama `_ =>`). Para que la excepción llegue al
clasificador de la explicación hay dos caminos, y los dos son peores que el tercero:

- **Cambiar `ProviderCall`** para que el resultado cargue la excepción o un estado nuevo. Es
  exactamente lo que el diseño prohíbe: «su comportamiento para ese proveedor no puede cambiar ni un
  byte». Se podría hacer sin cambiar el comportamiento del antifraude, pero es tocar la clase
  compartida para un solo consumidor.
- **Atrapar la excepción dentro de la lambda** que se le pasa a `ProviderCall`, antes de que la vea.
  Funciona y no toca nada compartido, pero es control de flujo por excepción para un resultado que
  el proveedor **conoce** en el momento de producirlo.

El tercer camino está escrito dos veces en el repositorio. El puerto de explicaciones ya declara que
un fallo del proveedor es un valor y no una excepción: «`null` means the provider declined to write
anything, which a model may legitimately do and **which is a failure with a code rather than an
exception**» (`IExplanationProvider.cs`, doc de `Summary`). Y el puerto antifraude lo hace por
construcción: `ExternalEvaluationResult(Outcome, ErrorCode, ...)` y
`ExternalProviderExchange.Apply`, cuyo comentario dice por qué: «The error code is fixed by the
outcome rather than taken from the result, **so an adapter cannot report "never sent" while naming a
timeout**».

La corrección: el puerto devuelve un **resultado con un desenlace cerrado del lado del proveedor**
—`Drafted`, `Refused`, `Malformed`, `Unavailable`— más el borrador cuando hay, los tokens **siempre**
(hallazgo 6), `ProviderVersion`, y un diagnóstico corto sin texto del modelo (tipo de error y
`request-id`, hallazgo 7). `ExplanationExchange` mapea desenlace a código y **el adaptador no puede
nombrar** `NotGroundedNumber`, `NotGroundedRule`, `TooLong`, `Cancelled`, `AttemptLimitReached` ni
`LegacySignalFormat`, que son del verificador, del ciclo de vida o del sistema. `ProviderCall` queda
intacto; `Faulted` sigue existiendo para lo imprevisto y sigue siendo `ProviderUnavailable`, que
para «tiró una excepción que nadie esperaba» es el nombre correcto. El antifraude no cambia un byte.

Lo que esto toca, y el brief de `E11B` tiene que reservar: `ExplanationDraft` y sus tres productores
—`DeterministicExplanationProvider`, `SwitchableProvider` y `CallerCancellingProvider` en
`ExplanationTestCorpus`, que hoy devuelven `new ExplanationDraft(null, [])` para «rehusó»—,
`ExplanationExchange`, y el test `AProviderThatThrowsIsRecordedRatherThanRaised`, que sigue valiendo
para la excepción imprevista. Y el doc de `ExplanationFailureCode.MalformedOutput` —«empty, or
carrying markup or links»— y el rótulo de `es.ts:229` —«vino vacío o con marcado»— pasan a incluir
«cortado» e «ilegible», porque D6 los reutiliza para `max_tokens` y para un cuerpo que no se puede
leer.

### 2. D4, alta. El modelo dentro de la versión de plantilla contradice una decisión escrita de la Etapa 7 en tres lugares, el diseño dice que no la contradice, y las consecuencias sobre lo ya guardado no están dichas

D4 afirma: «El modelo entra en la identidad de la explicación, a través de la versión de plantilla
(`anthropic-p1/claude-haiku-4-5`)», y que «no contradice la decisión 68». Es cierto sobre la 68 —que
habla del idioma— y falso sobre la decisión que sí existe para este caso y que el diseño no cita:

| Dónde | Texto |
| --- | --- |
| `Salvo-Blueprint.md`, §4 «AlertExplanation» | «`providerVersion` opcional: el modelo concreto. **Nunca parte de la identidad**» |
| `Coordination/Tasks/E7-DISENO.md`, D1 | `provider_version` — «Modelo concreto cuando lo haya. Nunca parte de la identidad»; y más abajo: «el mismo prompt con otro modelo es otra explicación, **pero no otra identidad**» |
| `AlertExplanation.cs`, doc de `ProviderVersion` | «The concrete model, when there is one. **Never part of the identity**: the same prompt answered by another model is another explanation, but it replaces this one rather than coexisting» |

La Etapa 7 decidió lo contrario de D4, con motivo, y dejó la columna para eso. D4 puede revocarlo
—como la 70 revocó la 8— pero entonces tiene que **decirlo**, escribir la entrada de la bitácora
como revocación, y corregir los tres lugares. Lo que no puede es afirmar que no hay contradicción.

Hay además dos hechos que hacen preferible **conservar** la decisión de E7:

- **El alias es un puntero, no una versión.** La documentación oficial: «On the Claude API, these
  models also have shorter aliases (for example, `claude-sonnet-4-5`) that point to the most recent
  dated snapshot for that minor version» y «This guarantee [de que el modelo no cambia] covers
  model IDs, **not the convenience aliases**». El ID de Haiku 4.5 es `claude-haiku-4-5-20251001`;
  `claude-haiku-4-5` es su alias. Poner el alias en la identidad es escribir una cadena que no
  cambia para nombrar un modelo que puede cambiar. Y si se pone el snapshot, la versión mide 38
  caracteres contra los 32 que declara la columna (`AlertExplanationConfiguration.cs`,
  `HasMaxLength(32)`; migración `Explanations`, `maxLength: 32`). SQLite no lo impone, así que el
  «no necesita migración» es cierto para la base y falso para el modelo declarado: subir el tope
  dispara `has-pending-model-changes` en `check.sh`, que es una migración de anotación.
- **Con un modelo, «el texto cambió» no sirve como criterio de versión.** La decisión 64 —«una
  versión no sube si el texto no cambia»— está pensada para una plantilla, que escribe siempre lo
  mismo. Un modelo escribe un texto distinto **cada vez que se lo llama**, con el mismo prompt y el
  mismo modelo. Lo único que este sistema controla, y por lo tanto lo único que puede identificar, es
  el prompt. La versión del adaptador es `anthropic-p1`; el modelo que respondió va a
  `ProviderVersion`, que existe para eso y se llena desde la **respuesta** y no desde la
  configuración.

Y la pregunta del encargo, «¿qué pasa con las explicaciones ya guardadas?», tiene una respuesta
precisa que el diseño no da, y que vale **con o sin** D4, porque `provider` ya está en la identidad:

1. Las filas `MOCK`/`e7-v2` quedan intactas en la base. El detalle de la alerta las sigue
   encontrando, porque `EfAlertStore.GetExplanationsAsync` filtra por evaluación, política e idioma
   y **no por proveedor ni por versión**, y elige «la más recientemente pedida»
   (`OrderByDescending(RequestedAt)`). Con `AI_PROVIDER=anthropic`, la consola muestra el párrafo de
   la plantilla con `WrittenByAnotherTemplate = true` y ofrece «Redactar con la plantilla vigente».
2. Al aceptar, `RequestExplanationHandler.FindAsync` busca por `(evaluación, ANTHROPIC, versión,
   política, idioma)`, no encuentra nada, reserva una fila nueva y llama al modelo. Si el modelo
   **falla** —rehúsa, o el verificador rechaza su texto—, esa fila `FAILED` es ahora la más reciente,
   y **el párrafo correcto de la plantilla desaparece de la pantalla** aunque siga en la base. Con la
   plantilla esto no pasaba porque la plantilla no falla; con un modelo es el caso esperado. El
   diseño tiene que decidir qué muestra la consola cuando la fila más reciente es un fallo y hay una
   `READY` de otro proveedor detrás.
3. Si el despliegue vuelve a `mock`, la consola muestra la fila del modelo con
   `WrittenByAnotherTemplate = true` y ofrece «Redactar con la plantilla vigente»; el botón manda
   `regenerate: false` (E7D), el handler encuentra la fila `MOCK` **ya `READY`** y devuelve sin cambiar
   nada. El botón no hace nada visible y la pantalla no lo explica.

Los puntos 2 y 3 son herencia de E7/E7D y se vuelven visibles el día que un escritor puede fallar.
Son de esta etapa.

### 3. D13, alta. Publicar los textos rechazados contradice `AGENTS.md`, el Blueprint y el código: no existe ningún canal por el que un texto rechazado llegue a un archivo

D13: «Los textos que escriba el modelo real sobre el corpus sintético se versionan en
`docs/muestras/`, con la fecha, el modelo y el resultado del verificador para cada uno, **incluidos
los rechazados**.»

`AGENTS.md`: «Un texto rechazado por la validación de grounding **no se persiste, no se registra y
no llega al diagnóstico**: se registra el token ofensor, nunca la frase.» El Blueprint lo repite en
§7, y el README lo dibuja en el diagrama de verificación. Y el código lo cumple:
`RequestExplanationHandler.GenerateAsync` llama a `SettleAsync(explanation, verdict.FailureCode,
verdict.Offender)` y el borrador se pierde ahí; el esquema lo remacha con
`ck_alert_explanations_ready`. **No hay ninguna ruta por la que el script de la corrida real pueda
obtener un texto rechazado**, salvo dos que rompen algo: agregar un registro del borrador antes de
verificarlo —que es exactamente lo que la regla prohíbe— o llamar a la API desde el script sin pasar
por Salvo, que vuelve imposible responder «qué dejó pasar el verificador».

Como `CLAUDE.md` ordena detenerse ante una contradicción con `AGENTS.md`, `E11B` no puede empezar con
D13 así. Dos salidas, y la v2 elige una:

- **La consistente**: se publican los textos `READY` y, para cada rechazado, lo que la fila ya
  guarda —el código y el token ofensor—. Eso ya cuenta la historia que D13 quiere contar: cuántos
  pasaron, cuántos no y por qué cifra.
- **La excepción explícita**: un observador de borradores que **solo** el script de la corrida real
  registra, fuera de la aplicación, con una entrada en la bitácora que amplíe la regla para ese
  caso. Es una decisión del coordinador, no del brief.

Y dos detalles: `docs/muestras/` hoy es «Muestras de importación» —cuatro archivos para probar
`/import`—; los textos de un modelo van en una carpeta propia con fecha. Y como el modelo no es
determinista, lo publicado es **una tirada**, no un dorado: no se compara contra nada en un test.

### 4. Partición y D9, alta. La consola afirma «no por un modelo» para cualquier proveedor, y el diseño no tiene una sola línea de frontend

El pie del bloque de explicación está escrito para un mundo con un solo escritor:

```ts
// frontend/src/lib/i18n/es.ts:392
explanationWrittenBy: (provider: string, version: string, settledAt: string) =>
  `Redactada por una ${provider} (${version}), no por un modelo${settledAt}.`,
// frontend/src/lib/i18n/pt.ts:345
  `Redigida por um ${provider} (${version}), não por um modelo de linguagem${settledAt}.`,
```

`explanation-block.tsx:140-146` lo llama con `explanationProviderLabel(explanation.provider)` y
`explanation.templateVersion`. El rótulo `ANTHROPIC: "Anthropic"` existe en los dos diccionarios.
El día que el adaptador escriba, la pantalla dirá: **«Redactada por una Anthropic
(anthropic-p1/claude-haiku-4-5), no por un modelo.»** Es la afirmación falsa dentro del producto,
a veinte píxeles del texto, que la lección de `E9D` en el Workboard ya describió una vez: «el
producto también afirma cosas».

Alrededor hay más que un rótulo: `explanationAskCurrentTemplate: "Redactar con la plantilla vigente"`
cuando lo vigente es un prompt; `ProviderVersion` viaja en `AlertExplanationView` y **ningún
componente lo muestra**; y las anclas que lo comprueban —`smoke-ui.sh:435` (`"no por un modelo"`),
`:581` (portugués), `:502` (`"plantilla determinista (e7-v1)"`), y
`explanation-block.test.tsx:104`— están escritas contra la frase de la plantilla. El pie de la
captura `03-explicacion.png` en el README (línea 98) también dice «no por un modelo».

Ni la partición ni la lista de cambios canónicos nombran `frontend/src/**`. La corrección: una
oración por proveedor —la de la plantilla conserva «no por un modelo», que es lo que el smoke afirma
en la instalación por defecto; la del modelo dice qué modelo (desde `providerVersion`) y qué versión
de prompt—, en los dos diccionarios, con el test del bloque y **las anclas del smoke que sigan
comprobando la frase de la plantilla**, porque el smoke corre sin clave. Y el brief reserva esos
paths.

### 5. D9, alta. Serializar `ExplanationInput` tal cual es la forma equivocada de darle los hechos al modelo: el verificador no puede fundamentar lo que el modelo copie de su propia entrada

D9: «Los hechos, como datos: `ExplanationInput` serializado como JSON en el turno del usuario».
Es lo que hoy recibe la plantilla, así que parece neutro. No lo es, porque **la plantilla nunca
copia su entrada: la renderiza** con `ExplanationFigures` —monto en unidades con dos decimales,
razón recortada, reloj en hora de negocio, año sin agrupar—. Un modelo al que se le dice «usá solo
los hechos dados» copia lo que ve, y lo que ve en `ExplanationInput` son representaciones que el
verificador no puede fundamentar:

| Campo de `ExplanationInput` | Lo que el modelo puede copiar | Lo que el verificador lee |
| --- | --- | --- |
| `RuleConfigVersion = "e3-v2"`, `AlertPolicyVersion = "e4-v1"` | «según la configuración e3-v2» | `NumberTokenizer.VersionStrings` es `["e3-v1", "e4-v1", "e7-v1", "e7-v2"]`: **`e3-v2` no está**, aunque `RuleConfig.E3V2` existe desde `E9B`. «e3-v2» se tokeniza en `3` y `2`, fundamentados solo por coincidencia |
| `OccurredAt` como instante ISO | «2026-03-14T06:15:00+00:00» | `ExplanationFacts.AddInstant` agrega año, mes, día, hora y minuto, **no segundos**: los dos `00` del final se leen como `0`, que es un hecho solo si alguna señal tiene un conteo en cero |
| `AmountCents = 125000` | «125.000 centavos» o «1.250,00» | Los dos están en los hechos; pero «125000 pesos» también pasa y es falso: el agujero semántico de D10 con una cifra que **el propio sistema le dio** |
| `Signals[*].Ratio` con un decimal | «3,4 veces» | Bien. Es el único que ya viene como la plantilla lo escribe |

La corrección respeta la regla de la etapa —se cambia lo que entra, nunca el verificador—: el
adaptador construye **un payload propio**, una hoja de hechos renderizada con las mismas funciones que
usa la plantilla (`ExplanationFigures.Money`, `Ratio`, `Minutes`, `Clock`, `Year`), con los nombres de
regla y sus pesos, sin versiones, sin centavos y sin instantes ISO; y el prompt exige **cifras en
dígitos, nunca en palabras**, porque el tokenizador declara que «numbers written as words are not
extracted and therefore not validated» —una limitación que la plantilla jamás ejercitó y que un
modelo va a ejercitar en la primera frase que diga «cuatro reglas»—.

Y un test que vuelve propiedad lo que hoy es intención: **todo número que aparece en el payload que
viaja al modelo está fundamentado por `ExplanationFacts`**. Es el espejo del test del espía —el
espía afirma qué **no** entra; éste afirma que lo que entra **se puede verificar**—, y hoy fallaría
por los segundos del instante y por las versiones. El espía que D9 quiere extender al cuerpo HTTP
corre sobre ese mismo payload, que es donde debe correr.

Aparte, y sea cual sea el resultado de esta etapa: `e3-v2` falta en `VersionStrings`. Hoy no cuesta
nada porque la plantilla no escribe versiones; es un defecto latente de `E9B`.

### 6. D11, media. «Medido» es falso para los intentos que más cuestan: la fila no guarda los tokens de un intento fallido, y borra los del anterior al reintentar

D11: «el número real sale de `input_tokens` y `output_tokens`, que la fila ya guarda desde la Etapa
7». La fila los guarda **solo en `Complete`**. `AlertExplanation.Fail(code, detail, settledAt)` no
recibe tokens; `Retake()` pone `InputTokens = null` y `OutputTokens = null`; y
`ExplanationExchange` descarta el borrador entero cuando `Summary is null`, con sus tokens adentro.
Sobre `RequestExplanationHandler.GenerateAsync`: un texto que el verificador rechaza llega con
tokens y se asienta con `SettleAsync(code, offender)`, sin ellos.

Así que **ninguno** de estos intentos deja rastro de lo que costó: una refusal, un texto cortado por
`max_tokens` —que por construcción consumió **todo** el presupuesto de salida—, un
`NotGroundedNumber`, un `TooLong`. Y una explicación que sale bien al tercer intento registra los
tokens del tercero. Lo que la fila mide es el costo de los textos que se guardaron, que es
justamente el subconjunto barato.

La corrección: el resultado del puerto lleva tokens en **todo** desenlace (hallazgo 1); `Fail`
los recibe; y se decide si la fila acumula entre intentos o guarda el último, con la decisión
escrita. `ck_alert_explanations_tokens` ya admite tokens en cualquier estado, así que no hay
migración. Y el script de la corrida real suma desde su propia salida y **imprime el precio y la
fecha en que lo leyó**, porque un número de costo sin fecha es lo que la decisión 69 prohíbe.

### 7. D6, media. El 429 del tope de tier no es transitorio, el 400 del límite propio es indistinguible de un 400 de formato con «tipo + request-id», y la fila ya tiene la columna donde guardar la diferencia

Leído el 2026-09-30 en las páginas de errores y de límites:

| Qué pasó | HTTP | `error.type` | Lo que lo distingue |
| --- | --- | --- | --- |
| Límite de gasto **propio**, de organización o de espacio de trabajo | **400** | `invalid_request_error` | El `message` empieza con «You have reached your specified API usage limits» o «…workspace API usage limits» |
| Tope de gasto **del tier** (Start: US$ 500) | **429** | `rate_limit_error` | **Sin cabecera `retry-after`**, y `error.details.error_code = "enforced_spend_limit_reached"` |
| Límite de tasa | 429 | `rate_limit_error` | Con `retry-after` |
| Petición mal formada | 400 | `invalid_request_error` | — |

D6 manda el 429 a `ProviderUnavailable` sin log, y el 400 a `ProviderUnavailable` con «un log de
nivel error con el tipo y el `request-id`». El tipo del 400 de gasto y del 400 de formato es el
mismo: `invalid_request_error`. Y el 429 de tope de tier, que «keeps failing until access resumes»,
se registra como si el proveedor estuviera saturado. En este proyecto el tope de gasto es **la única
defensa contra una clave filtrada** (D11): el momento en que dispara es exactamente el momento en
que alguien tiene que enterarse.

La corrección: el adaptador lee `error.type`, `error.details.error_code` y la presencia de
`retry-after` —nunca el `message`, que es prosa del proveedor—; un 429 sin `retry-after` o con
`enforced_spend_limit_reached` no es transitorio; y la clasificación va a **`FailureDetail`**, que
existe, mide 200 caracteres, nunca lleva texto del modelo y se ve en la base y en la consola, en vez
de a un log que nadie mira en una demo. El log queda además, a nivel `Warning`, para todo lo que no
sea `2xx`.

### 8. D2, media. «No hay SDK» está desactualizado: hay uno oficial que hace las tres cosas por las que el diseño lo descarta, y «cero dependencias nuevas» tampoco se sostiene para el camino sin SDK

Verificado el 2026-09-30:

- El paquete NuGet **`Anthropic`**, versión **12.52.0** publicada ese mismo día, MIT, es «the
  official Anthropic SDK for C#» desde la versión 10 (`platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp`).
  Exige .NET Standard 2.0 o posterior.
- **Reintentos**: «The SDK automatically retries 2 times by default … configure the client using
  the `MaxRetries` property». Se pone en cero.
- **Transporte inyectable**: `ClientOptions.HttpClient` es una propiedad pública con setter
  (`src/Anthropic/Core/ClientOptions.cs`), y `HttpClientPassthroughHandler` enruta cada petición por
  el `HttpClient` que se le dé. Un `HttpMessageHandler` simulado funciona igual que en el diseño.
  Hay además una lista `Handlers` de `DelegatingHandler`.
- **Timeout** configurable; `WithRawResponse` expone cabeceras y `request-id`; excepciones tipadas
  por estado (`AnthropicBadRequestException`, `AnthropicUnauthorizedException`,
  `AnthropicRateLimitException`, `Anthropic5xxException`); `OutputConfig.Format` para la salida
  estructurada.
- **Costo**: tres dependencias en net8+ —`Microsoft.Extensions.AI.Abstractions ≥ 10.5.1`,
  `System.Net.ServerSentEvents ≥ 10.0.1`, `System.Text.Json ≥ 10.0.6`— y la decisión 61 pide
  nombre y motivo. Y un canal de registro propio, `ANTHROPIC_LOG=debug`, cuyo contenido la
  documentación declara «intended for debugging only»: un camino más que el test de D7 tiene que
  cubrir si se adopta.

Del otro lado, la premisa de «cero dependencias nuevas» tampoco cierra sola. D7 promete que la
cabecera «se redacta explícitamente en el registro del `HttpClient`». Esa API
—`RedactLoggedHeaders` / `HttpClientFactoryOptions.ShouldRedactHeaderValue`— vive en
**`Microsoft.Extensions.Http`**, y ningún proyecto lo referencia: cero apariciones en los tres
`packages.lock.json`, e `Infrastructure` trae solo `CsvHelper`, `EntityFrameworkCore.Sqlite` y
`.Design`. `Salvo.Api` lo tiene por el framework compartido, pero D7 pone el único lector en
`Infrastructure`. Las opciones reales son: **un** paquete de primera parte en la misma línea 10.0.11
que EF, o un `HttpClient` desnudo sin ninguna tubería de registro —donde no hay nada que redactar y
el test afirma la ausencia—. Y con `IExplanationProvider` registrado como `Scoped`
(`DependencyInjection.cs`), un `new HttpClient` por ámbito agota sockets: el cliente es singleton o
sale de una fábrica.

El diseño tiene que elegir con los dos hechos sobre la mesa. Las dos son defendibles; lo que no es
defendible es el argumento tal como está. Y en cualquiera de las dos, **«una llamada por intento» se
vuelve un test**: el transporte simulado cuenta peticiones y afirma exactamente una por intento, con
`MaxRetries = 0` o sin SDK. D5 pasa de promesa a propiedad.

### 9. D4 y D11, media. Haiku 4.5 es la elección correcta hoy, pero su compromiso de retiro vence el 2026-10-15 y el diseño no lo dice

En la tabla oficial de modelos, leída el 2026-09-30: Claude Haiku 4.5, ID `claude-haiku-4-5-20251001`,
alias `claude-haiku-4-5`, «Fastest», US$ 1 / 5 por millón, y «Retirement: **Not sooner than October
15, 2026**». Quince días después de la fecha del diseño. Los otros tres modelos de la tabla comparativa tienen
compromisos hasta 2027. La tabla de deprecaciones lo marca «Active», la política es «at least 60
days' notice», y Haiku 3.5 y Haiku 3 ya están retirados —así que «el más barato» es cierto entre los
vigentes de primera parte—.

No bloquea nada, pero «el valor elegido se escribe con su fecha en el Blueprint» tiene que decir
también que el compromiso de este modelo es el más corto de la gama y qué se hace cuando llegue el
aviso: cambiar `ANTHROPIC_MODEL` es, con D4 tal como está, **rehacer las veintitrés explicaciones**.
Con la decisión de E7 conservada (hallazgo 2), es cambiar una variable y una fila de
`ProviderVersion` por explicación nueva.

Dos notas de la misma página: Haiku 4.5 **no admite `effort`** («Default effort: Not supported») y
usa el tokenizador anterior a 4.7, así que el adaptador no manda `output_config.effort` ni
`thinking`, y las estimaciones de D11 valen para ese tokenizador y no para uno de la familia 5.

### 10. D10, media. El test del agujero está bien planteado como test de caracterización, pero fija una oración y no la clase, y la lectura humana no tiene palanca

Que un test afirme que una oración invertida **pasa** es la forma correcta de fijar una limitación
declarada: cuando alguien la cierre, el test se pone rojo y obliga a corregir lo que el README dice.
Tres cosas le faltan:

- **Fija una oración, no la clase.** El agujero es «el verificador comprueba existencia, no verdad»,
  y tiene al menos tres formas distintas de aparecer: la inversión («3,4 veces **menor**»), la
  atribución cruzada («la mediana fue 1.250» usando el monto, que también es un hecho), y la regla
  bien citada con semántica equivocada («`velocity` indica que el comprador es nuevo»). El test
  debería fijar al menos las dos primeras, con el nombre de la clase y no del ejemplo.
- **Las cifras en palabras no se validan**, y eso es un segundo agujero que el diseño no menciona
  porque la plantilla nunca lo alcanzó. «Se dispararon cuatro reglas» y «el triple de la mediana»
  pasan el verificador enteros. Va al prompt (hallazgo 5) y va al test, para que el README lo diga.
- **La lectura humana no tiene palanca.** Una fila `READY` es inmutable: `Retake` lanza sobre
  `Ready`, y `regenerate: true` sobre una `READY` devuelve el conflicto `AlreadyReady`. Si el
  coordinador lee un texto semánticamente falso, no hay forma de rechazar **ese** texto; la única
  palanca es subir el prompt a `p2`, que rehace las veintitrés. El diseño tiene que decirlo así: la
  lectura es una observación, no una compuerta, y su palanca es la versión del prompt.

### 11. Cambios de estado canónico, media. La lista está incompleta, y es la lección de `E8A`/`E8B`/`E9A` otra vez

El diseño lista la bitácora, el §9 del Blueprint, «Límites declarados» del README y `AGENTS.md`.
Abriendo los archivos, lo que afirma «no hay adaptador» o «no por un modelo» está además en:

| Archivo | Dónde |
| --- | --- |
| `README.md` | línea 42 («No habló nunca … ni con Anthropic»), 52 (fila de la tabla «Redacción de explicaciones»), 55 («`AI_PROVIDER=anthropic` hacen fallar el arranque»), 98–99 (pie de captura, «no por un modelo»), 430 |
| `Salvo-Overview.md` | línea 73 (tabla de estado) y 88 («Próximo paso») |
| `Salvo-Getting-Started.md` | líneas 40–41, 67 y 235 |
| `Salvo-Portability.md` | líneas 15, 62 y 93–98 |
| `Salvo-Blueprint.md` | línea 322, y el §4 en la línea 507 si D4 se mantiene |
| `Salvo-Progress.md` | línea 245, el checklist abierto «Decidir aparte si se activa Anthropic» |
| `ExplanationProvider.cs` | doc de `Anthropic`: «Reserved. No adapter exists yet, and asking for one refuses to start» |
| `DependencyInjection.cs` | remarks de `AddExplanationProvider`, y el mensaje de error que afirma «this build has no Anthropic adapter» |
| `Workboard.md` | fila de `E11B`: «Modelo y esfuerzo: por acordar en el diseño». El diseño no lo acuerda; `E11A` fijó Opus 5.5 · `high` |

`check-docs.sh` va a atrapar rutas y nombres de test, no prosa. Los cuatro lugares que la lección
del Workboard manda recorrer por lista —cabecera del Workboard, «Estado general» y tablero del
Progress, «Próximo paso» del Overview— están además en la lista de arriba.

### 12. D6 y D3, baja. Detalles de la API que el adaptador tiene que saber y el diseño no escribe

- **`anthropic-version` es obligatoria**: «When making API requests, you must send an
  `anthropic-version` request header. For example, `anthropic-version: 2023-06-01`». Sin SDK, la
  manda el adaptador; el diseño no la nombra.
- **El esquema exige `additionalProperties: false`** y **no admite `maxLength`** («Not supported:
  `minLength`, `maxLength`»), así que el tope de 1.200 caracteres no se puede pedir por esquema:
  `TooLong` queda del verificador, como el diseño quiere, y conviene decir por qué.
- **`enum` sí se admite**: `referencedRules` puede restringirse a las reglas que dispararon, lo que
  vuelve imposible por construcción citar una que no disparó. Con una salvedad documentada:
  «Structured outputs don't guarantee capitalization of `enum` and `const` string values», y
  `ExplanationGrounding.VerifyRules` compara con `StringComparer.Ordinal`: una variante con mayúscula
  sería `NotGroundedRule`. El adaptador normaliza a minúsculas, o se acepta y se dice.
- **`refusal` puede venir sin JSON**: el cuerpo «may not match your schema». Se lee `stop_reason`
  **antes** de deserializar, no después.
- **Cualquier otro `stop_reason`** —`stop_sequence`, `tool_use`, `pause_turn` no pueden ocurrir sin
  herramientas— va a `MalformedOutput`; la tabla de D6 enumera cuatro y no dice qué pasa con el resto.
- **La primera petición con un esquema compila una gramática**: «First request latency … Compiled
  grammars are cached for 24 hours from last use». Con el timeout del puerto en 15 s
  (`Explanations:RequestTimeoutSeconds`), la primera alerta de la corrida real es la que puede ver un
  `ProviderTimeout` que las demás no ven; hay que saberlo antes de leer la salida.
- **409 `conflict_error`** existe y no está en la lista de 4xx de D6. Cae en «otros 4xx» si la tabla
  lo escribe así.

### 13. D12 y D5, baja. Los scripts existentes heredan el entorno de la terminal y ninguno fija `AI_PROVIDER=mock`

`smoke-ui.sh:241`, `demo.sh:176`, `capturas.sh:178` y `hornear-base.sh:68` lanzan
`dotnet … Salvo.Api.dll` con el entorno heredado, y ninguno exporta `AI_PROVIDER`. Hoy es inocuo:
`anthropic` se niega a arrancar. Después de `E11B`, una terminal con `AI_PROVIDER=anthropic` y la
clave exportadas —que es la terminal en la que el coordinador acaba de correr la corrida real— hace
que el smoke gaste dinero y falle en `"no por un modelo"` con un mensaje que no explica nada, y que
`hornear-base.sh` **hornee texto de un modelo dentro de la imagen** de la instancia pública. Una
línea por script.

Y el mecanismo de D12: ningún script del repositorio lee `.env` (búsqueda de `source .env` y
`set -a`, sin resultados); ASP.NET Core tampoco. El script de la corrida real es el primero que lo
carga, así que el brief tiene que decir cómo (`set -a; . ./.env; set +a`) y que no imprime ni
exporta más de lo que la API necesita. Lo que sí está verificado: `.claude/settings.json:27-34`
deniega `Read(./.env)` y sus siete variantes, como el diseño afirma.

### 14. D11, baja. La aritmética cierra; el techo no, y el espacio de trabajo dedicado no es una sugerencia

1.500 × US$ 1/M + 300 × US$ 5/M = US$ 0,0015 + 0,0015 = **US$ 0,003** ✓. 69 × 0,003 = US$ 0,207 ≈
US$ 0,20 ✓. Pero el peor caso no son 300 tokens de salida: un texto cortado por `max_tokens` cobra
**todo** `max_tokens`, y D6 lo fija «con margen». Con 1.024, el techo es 69 × (0,0015 + 0,00512) ≈
US$ 0,46. Sigue siendo nada contra los US$ 5, pero el techo se calcula con `max_tokens`, no con la
estimación.

Y dos hechos de la página de espacios de trabajo que cambian el tono de D11: **«You cannot set limits
on the Default Workspace»**, así que el espacio de trabajo dedicado es obligatorio para que exista el
tope; y el tope del tier Start es US$ 500, que es lo que acota el daño si alguien crea la clave en el
espacio por defecto por apuro. La página de precios dice además que «New users receive a small amount
of free credits»: la corrida real puede costar cero, y conviene que el handoff diga cuánto costó de
verdad.

### 15. D7 y D8, baja. Los caminos de la clave que el test del espía de logs no cubre como está descrito

D7 propone «un registrador espía sobre un camino de llamada que falla —401, por ejemplo—». Los
caminos que ese test no ve:

- **`HttpRequestMessage.ToString()` incluye las cabeceras.** Cualquier `{Request}` en un mensaje de
  log estructurado vuelca `x-api-key`. El espía tiene que correr a nivel `Trace`, sobre el camino
  que **funciona** además del que falla, y buscar el valor de la clave en cada mensaje y en el
  `ToString()` de cada excepción capturada.
- **`ANTHROPIC_LOG=debug`**, si se adopta el SDK (hallazgo 8): el test corre también con esa variable
  puesta.
- **La versión de plantilla llega al navegador** vía `AlertExplanationView.TemplateVersion`, y con D4
  lleva el nombre del modelo. No es un secreto; hay que decirlo para que nadie lo tome por fuga.
- **D8** está bien y el `Dockerfile` ya trae `SharedInstance__Enabled=true`; el test que lo afirma
  arranca con la bandera **y** con una clave presente, y espera el fallo. Un test que lo pruebe sin
  clave prueba otra cosa.

---

## Decisiones que resistieron

- **Lo que no cambia.** Verificador intacto, en el caso de uso; el modelo no decide; «si el modelo
  no pasa, se cambia el prompt». Correcto, y este informe lo aplicó: cada corrección propuesta al
  camino de grounding está del lado de la entrada, no del verificador.
- **D1, el puerto.** Resistió en su lugar y en su regla —ningún tipo del cliente ni error del
  proveedor lo cruza—; cambia la **forma del resultado** (hallazgo 1), que es lo que el propio D1
  anticipa con «salvo lo que D6 pide».
- **D3, salida estructurada.** Verificado: GA, `output_config.format` con `type: "json_schema"`,
  Haiku 4.5 (`claude-haiku-4-5-20251001`) en la lista, no garantizada en `refusal` ni `max_tokens`.
  Cae solo el detalle de qué puede y qué no puede pedir el esquema (hallazgo 12).
- **D5, ningún reintento invisible.** Correcto: los SDK reintentan dos veces por defecto y eso está
  verificado. Lo que gana es un test que lo afirme (hallazgo 8).
- **D7, un lector, fallar al arrancar, nombrar la variable y no el valor.** Correcto. El mecanismo de
  redacción necesita decidir su paquete (hallazgo 8) y el test necesita más caminos (hallazgo 15).
- **D8, la instancia pública no arranca con la clave.** Correcto y es la decisión más barata de la
  etapa: la guarda vive en `AddExplanationProvider`, al lado de la que ya lee `AI_PROVIDER`.
- **D12, tests sin red y la corrida real en manos del coordinador.** Correcto. `.claude/settings.json`
  deniega `.env` como el diseño afirma.
- **La partición y su orden.** `E11A` primero es correcto. `E11B` gana frontend, la forma del
  resultado del puerto, los tokens en `Fail`, y la decisión de qué muestra la consola cuando la fila
  más reciente falló; `E11C` pierde «incluidos los rechazados» o gana una excepción escrita.

## Respuestas a las seis preguntas del encargo

| Pregunta | Respuesta |
| --- | --- |
| **1. D6.** ¿La propuesta cambia algo para el antifraude? ¿Hay un camino mejor? | Como está escrita no cambia nada porque **no puede funcionar**: `ProviderCall` descarta la excepción antes de que `ExplanationExchange` la vea. El camino mejor es el que el antifraude ya usa: un desenlace cerrado por valor, con el código fijado por el intercambio y no por el adaptador. `ProviderCall` no se toca (hallazgo 1) |
| **2. D4.** ¿Contradice las decisiones 64 o 68? ¿Qué pasa con lo guardado? | No contradice la 68. Contradice la decisión de E7 escrita en el Blueprint §4, en `E7-DISENO` y en la entidad: «`providerVersion` … nunca parte de la identidad». Y con un modelo, la 64 no tiene criterio aplicable: el texto cambia siempre. Lo guardado queda; la consola muestra la fila más reciente **sin mirar el proveedor**, así que un intento fallido del modelo esconde el párrafo correcto de la plantilla, y volver a `mock` deja un botón que no hace nada (hallazgo 2) |
| **3. D2.** ¿Existe un SDK oficial para .NET? | Sí: `Anthropic` 12.52.0, GA, MIT, con `MaxRetries`, `Timeout`, `HttpClient` inyectable y `Handlers`. Cuesta tres dependencias y un canal de log propio. Y el camino sin SDK **también** necesita un paquete para redactar cabeceras. La comparación está en el hallazgo 8; cualquiera de las dos vale si D5 se vuelve test |
| **4. D7 y D8.** ¿Algún camino por el que la clave llegue a un log, una excepción, el navegador o la instancia pública? | Al navegador y a la instancia pública, ninguno encontrado: nada del proveedor sale por `capabilities`, `.dockerignore` excluye `.env*`, `render.yaml` declara solo `PORT`, y D8 cierra la puerta por configuración. A un log: `HttpRequestMessage.ToString()` y `ANTHROPIC_LOG=debug` si hay SDK; el test tiene que cubrirlos a nivel `Trace` y en el camino feliz (hallazgo 15). A una excepción: ninguno en el código actual; se afirma por test, no por lectura |
| **5. D10 y D11.** ¿Las cifras cierran? ¿El test está bien planteado? | Las cifras cierran con los precios verificados; el techo se calcula con `max_tokens` y no con 300 (hallazgo 14). El test es correcto en su forma y corto en su alcance: fija una oración y no la clase, ignora las cifras en palabras, y la lectura humana no tiene forma de rechazar un texto `READY` (hallazgo 10). Y «medido» no es cierto para los intentos fallidos (hallazgo 6) |
| **6. Lo que el diseño no vio.** | El pie de la consola que dice «no por un modelo» para cualquier proveedor (hallazgo 4); que serializar `ExplanationInput` le da al modelo cosas que el verificador no puede fundamentar (hallazgo 5); que D13 pide publicar lo que `AGENTS.md` prohíbe registrar (hallazgo 3); que el retiro de Haiku 4.5 tiene compromiso hasta el 2026-10-15 (hallazgo 9); que los scripts heredan `AI_PROVIDER` (hallazgo 13); y `e3-v2` ausente del tokenizador |

## Contradicciones con el Blueprint, la bitácora y `AGENTS.md`

| Afirmación del diseño | Contradice | Dónde |
| --- | --- | --- |
| D4: el modelo entra en la identidad vía `templateVersion` | «`providerVersion` … Nunca parte de la identidad» | `Salvo-Blueprint.md:507`, `E7-DISENO.md` D1, `AlertExplanation.cs` |
| D13: se publican los textos rechazados | «Un texto rechazado … no se persiste, no se registra y no llega al diagnóstico» | `AGENTS.md:194`, `Salvo-Blueprint.md:314` |
| D6: `ExplanationExchange` clasifica la excepción antes del clasificador genérico | «What it threw does not cross this boundary» | `ProviderCall.cs` |
| D2: cero dependencias nuevas, y D7: redacción en el registro del `HttpClient` | `Microsoft.Extensions.Http` no está en ningún lockfile | `packages.lock.json` ×3 |

La que bloquea el despacho de `E11B` es la de D13 con `AGENTS.md`, por la regla de `CLAUDE.md`.

## Resumen para los briefs

**`E11B-ADAPTADOR-ANTHROPIC`.**

- El puerto devuelve un resultado con desenlace cerrado —`Drafted | Refused | Malformed |
  Unavailable`—, tokens en todo camino, `ProviderVersion` desde la respuesta y un diagnóstico sin
  texto del modelo; `ExplanationExchange` mapea, el adaptador no elige códigos, `ProviderCall`
  intacto. `Fail` recibe tokens. Reserva: `IExplanationProvider.cs`, `ExplanationExchange.cs`,
  `AlertExplanation.cs`, `DeterministicExplanationProvider.cs`, `ExplanationTestCorpus.cs`, el doc de
  `MalformedOutput`.
- `TemplateVersion = anthropic-p1`; `ANTHROPIC_MODEL` fijado al snapshot `claude-haiku-4-5-20251001`;
  el modelo en `ProviderVersion`. Si el coordinador prefiere D4, la bitácora lo escribe como
  revocación y se corrigen los tres lugares y el `HasMaxLength(32)`.
- Qué muestra la consola cuando la fila más reciente es `FAILED` y hay una `READY` de otro
  proveedor: decisión escrita, en `EfAlertStore.GetExplanationsAsync` o en la proyección.
- Payload propio renderizado con `ExplanationFigures`, sin versiones ni instantes ISO; prompt que
  exige dígitos; test «todo número que viaja está fundamentado»; espía sobre el cuerpo HTTP;
  `e3-v2` en `VersionStrings`.
- Frontend: oración por proveedor en `es.ts` y `pt.ts`, `providerVersion` visible, test del bloque,
  anclas del smoke conservadas para la plantilla. Reserva: `frontend/src/lib/i18n/**`,
  `app/alerts/[id]/explanation-block.tsx` y su test, `scripts/smoke-ui.sh`.
- Clasificación de 400/429 por `error.type`, `details.error_code` y `retry-after`, escrita en
  `FailureDetail`; `anthropic-version`; `stop_reason` leído antes de deserializar; «cualquier otro
  `stop_reason` → `MalformedOutput`»; `enum` en `referencedRules` con normalización de mayúsculas.
- SDK o `HttpClient`, decidido con los dos hechos del hallazgo 8, y el test que cuenta una petición
  por intento. Cliente singleton o de fábrica.
- Test del espía de logs a nivel `Trace`, camino feliz incluido, `ToString()` de excepciones, y
  `ANTHROPIC_LOG=debug` si hay SDK. Test de D8 con clave presente.
- `AI_PROVIDER=mock` exportado en `smoke-ui.sh`, `demo.sh`, `capturas.sh` y `hornear-base.sh`.
- Los tests de D12 incluyen: 429 sin `retry-after`, 400 de límite propio, `refusal` sin JSON,
  `max_tokens`, cifra en palabras, oración invertida y atribución cruzada.

**`E11C-CORRIDA-REAL`.**

- Espacio de trabajo **dedicado** con tope —no el Default, que no admite límites—; snapshot del
  modelo, precio y fecha impresos por el script; tokens sumados desde la salida del script.
- Publicación de los `READY` con fecha y modelo, y de código + token ofensor para los rechazados, en
  una carpeta propia; o la excepción explícita de D13 escrita antes.
- La primera petición puede ser la lenta (gramática); la lectura humana es observación, y su
  palanca es `p2`.
- El párrafo para README, Overview, Getting-Started y Portability, con las once líneas del
  hallazgo 11.

**Coordinador, antes de despachar.** Resolver D13 contra `AGENTS.md:194`; decidir D4 en una de sus
dos formas y escribirla; fijar modelo y esfuerzo de `E11B` en el Workboard; y las entradas de la
bitácora desde la 71 con las decisiones nuevas que este informe agrega: la forma del resultado del
puerto, los tokens en el fallo, el payload propio, y qué muestra la consola cuando el escritor falla.

## Cómo se verificó

- Código: lectura completa de los archivos citados. Búsquedas: `ProviderCall` en `backend/src`
  (tres archivos); `Microsoft.Extensions.Http` en los tres `packages.lock.json` (cero); `AI_PROVIDER`
  y `ANTHROPIC` en `scripts/`, `Dockerfile`, `render.yaml` (solo un comentario en `smoke-ui.sh:613`);
  `source .env` y `set -a` en `scripts/` y Getting-Started (cero); `explanationWrittenBy` en
  `frontend/src` (un componente, dos diccionarios); `"no por un modelo"`, `"plantilla determinista"`
  y `"não por um modelo"` en `smoke-ui.sh` y en los tests del frontend; `e3-v2` en `RuleConfig.cs`
  (existe) y en `NumberTokenizer.VersionStrings` (no está); `SharedInstance` en `SystemEndpoints.cs`
  y `Dockerfile`; `\.env` en `.claude/settings.json` (ocho denegaciones de lectura).
- Documentos: `grep -n` de «Anthropic», «identidad», «provider_version», «no se registra» y las
  decisiones 51–70 sobre Blueprint, Overview, Progress, Workboard, Getting-Started, Portability,
  README, `E7-DISENO.md` y `E11A-INTEGRACION-CONTINUA.md`.
- Aritmética de D11 rehecha con los precios de la página oficial; conteo de caracteres de las dos
  formas de la versión (29 y 38) contra `HasMaxLength(32)`.
- Páginas oficiales, leídas el 2026-09-30, citadas textualmente donde se afirma algo de ellas:
  - `https://platform.claude.com/docs/en/about-claude/pricing`
  - `https://platform.claude.com/docs/en/about-claude/models/overview`
  - `https://platform.claude.com/docs/en/about-claude/models/model-ids-and-versions`
  - `https://platform.claude.com/docs/en/about-claude/model-deprecations`
  - `https://platform.claude.com/docs/en/build-with-claude/structured-outputs`
  - `https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons`
  - `https://platform.claude.com/docs/en/api/errors`
  - `https://platform.claude.com/docs/en/api/rate-limits`
  - `https://platform.claude.com/docs/en/api/versioning`
  - `https://platform.claude.com/docs/en/manage-claude/workspaces`
  - `https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp`
  - `https://www.nuget.org/packages/Anthropic`
  - `https://github.com/anthropics/anthropic-sdk-csharp` — `src/Anthropic/Core/ClientOptions.cs` y
    `src/Anthropic/Core/HttpClientPassthroughHandler.cs`
  - `https://support.claude.com/en/articles/9876003-...` (la suscripción no incluye la API)
- Lo que **no** se pudo verificar y se dice: si la respuesta de la API devuelve el alias o el
  snapshot en su campo `model` cuando la petición usa el alias. La referencia pública no lo
  especifica; fijar el snapshot en `ANTHROPIC_MODEL` vuelve irrelevante la pregunta, y la corrida
  real lo anota.
- Ningún test ni compuerta ejecutados; ninguna escritura fuera de este archivo; ningún push.
