# Salvo — Etapa 11, la IA de verdad — diseño v1

> Estado: **v1, pendiente de revisión adversarial.** Nada de esto se despacha antes de esa revisión.
> Fecha: 2026-09-30
> Coordinador: Unai Arismendes
> Base: la Etapa 7 dejó el puerto, la verificación, las columnas de costo y los códigos de fallo
> preparados para este adaptador (su diseño, decisión D11). Esta etapa escribe lo que falta, y
> **no afloja nada de lo que ya existe**.

## El resultado de la etapa, en una frase

Un modelo de Anthropic puede redactar la explicación de una alerta, **gobernado por el mismo
verificador que hoy gobierna a la plantilla**, sin que la integración continua ni la instancia
pública tengan jamás la clave.

## Lo que no cambia, y conviene decirlo primero

- La IA **redacta** la explicación de una decisión ya tomada. **Nunca decide** fraude, severidad ni
  bloqueo. El interceptor de SQL y los tres diferenciales de la Etapa 7 lo siguen probando, y el
  adaptador tiene que pasarlos sin tocarlos.
- Al modelo le entra **solo texto que escribe el motor**. El proveedor espía lo sigue probando.
- Todo texto pasa por `ExplanationGrounding` **antes** de persistirse. El verificador vive en el caso
  de uso, no en el adaptador, así que el adaptador no tiene cómo esquivarlo.
- **Si el modelo no pasa el verificador, se cambia el prompt, nunca el verificador.** Es la regla que
  gobierna toda la etapa, y la más tentadora de romper el día que el modelo real falle.

## Datos verificados el 2026-09-30

Todo lo que este diseño afirma sobre la API de Anthropic salió de la documentación oficial, abierta
ese día. `E11B` **vuelve a abrirla el día que ejecute** y rehace esta tabla si algo cambió.

| Qué | Valor | Fuente |
| --- | --- | --- |
| La suscripción de Claude no incluye la API | Se pagan por separado | [support.claude.com](https://support.claude.com/en/articles/9876003-i-have-a-paid-claude-subscription-pro-max-team-or-enterprise-plans-why-do-i-have-to-pay-separately-to-use-the-claude-api-and-console) |
| Claves atadas a un espacio de trabajo, con tope de gasto mensual | Sí | [Workspaces](https://platform.claude.com/docs/en/manage-claude/workspaces) |
| Modelo más barato y más rápido | `claude-haiku-4-5`, US$ 1 / 5 por millón de tokens de entrada / salida | [Pricing](https://platform.claude.com/docs/en/about-claude/pricing) |
| Salida con esquema garantizado | `output_config.format` con `type: "json_schema"`; Haiku 4.5 la soporta | [Structured outputs](https://platform.claude.com/docs/en/build-with-claude/structured-outputs) |
| Cuándo **no** se garantiza el esquema | `stop_reason: "refusal"` (con HTTP 200) y `stop_reason: "max_tokens"` | Structured outputs, y [Stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons) |
| Errores transitorios | 429, 500, 504, 529 | [Errors](https://platform.claude.com/docs/en/api/errors) |
| Tope de gasto alcanzado | Aparece como **400** o como **429**, no con un código propio | Errors |
| Reintentos de los SDK oficiales | **Dos por defecto**, con espera exponencial | Errors |
| Trazabilidad | Toda respuesta trae la cabecera `request-id` | Errors |

---

## D1 — El adaptador vive detrás del puerto que ya existe

`IExplanationProvider` y `ExplanationDraft` se diseñaron en la Etapa 7 para este momento: el
comentario del puerto dice textualmente que a un modelo se le va a pedir *exactamente* esa forma como
esquema JSON. El adaptador devuelve un `ExplanationDraft` con `Summary`, `ReferencedRules`,
`ProviderVersion`, `InputTokens` y `OutputTokens`. **El puerto no cambia**, salvo lo que D6 pide.

Ningún tipo del cliente HTTP ni ningún error del proveedor cruza el puerto.

## D2 — `HttpClient` contra la API documentada, sin SDK

Se llama a la Messages API con `HttpClient` y `System.Text.Json`, sin librería cliente.

- **Cero dependencias nuevas.** El proyecto fija y bloquea cada paquete; una librería cliente es una
  superficie más que auditar y actualizar.
- **Los SDK oficiales reintentan dos veces por defecto.** Cada reintento se paga y ninguno se ve en
  el contador de intentos de la fila. Ver D5.
- **El transporte simulado es exactamente un `HttpMessageHandler`.** Los tests prueban el adaptador
  real, byte por byte, sin red.

**Dónde puede estar equivocada**: un SDK sigue los cambios de la API por vos. Se cambia esa comodidad
por control, y el costo es que un cambio de la API se descubre en la corrida real o en la
documentación, no en una actualización de paquete. Si existe un SDK oficial para .NET que permita
inyectar el `HttpClient` y apagar los reintentos, la revisión adversarial lo compara.

## D3 — Salida estructurada, espejo del borrador

El pedido usa `output_config.format` con un esquema JSON que es el espejo de `ExplanationDraft`:
`summary` (texto) y `referencedRules` (lista de identificadores de regla). La API garantiza el
esquema **salvo** en dos casos, y los dos tienen código propio (D6).

## D4 — El modelo, y cómo entra en la identidad

**`claude-haiku-4-5`**, por tres motivos verificados: es el más barato, el más rápido, y soporta
salida estructurada. Un párrafo de 1.200 caracteres sobre hechos dados no necesita más. Se configura
con `ANTHROPIC_MODEL`; el valor elegido se escribe con su fecha en el Blueprint.

**El modelo entra en la identidad de la explicación, a través de la versión de plantilla.** La
versión del adaptador es la versión del prompt más el modelo: `anthropic-p1/claude-haiku-4-5`.

- Es la **decisión 64** aplicada: sin cambio de texto no hay versión nueva, y un modelo distinto
  escribe un texto distinto.
- **No contradice la decisión 68**, que puso el idioma como columna y no como versión porque *la
  misma* plantilla escribe los dos idiomas. Un modelo distinto no es la misma plantilla.
- **No necesita migración**: la versión de plantilla ya es una columna de texto.

## D5 — Ningún reintento invisible

**Una llamada HTTP por intento, y ninguna más.** Los reintentos existen, pero son los de la fila: una
explicación admite tres intentos (`AlertExplanation.MaximumAttempts`), cada uno visible, contado y con
su código de fallo. Eso acota el costo por evaluación a **tres llamadas**, sin excepciones.

Un reintento automático dentro del adaptador sería un multiplicador de costo que el sistema no
muestra, y convertiría un «falló tres veces» en «falló nueve veces» sin que nadie lo sepa.

## D6 — La taxonomía de fallos, sin colapsos engañosos

| Lo que pasa | Código |
| --- | --- |
| HTTP 200, `stop_reason: "end_turn"`, esquema válido | Ninguno: el borrador va al verificador, que decide |
| `stop_reason: "refusal"` | `ProviderRefused` — el borrador vuelve con `Summary` nulo, y ese camino ya existe |
| `stop_reason: "max_tokens"` o `"model_context_window_exceeded"` | `MalformedOutput` |
| Cuerpo que no se puede leer | `MalformedOutput` |
| 429, 500, 504, 529, error de red | `ProviderUnavailable` |
| Vence el plazo del puerto | `ProviderTimeout`, como hoy |
| 400, 401, 402, 403, 404, 413 | `ProviderUnavailable` en la fila, **y un log de nivel error con el tipo y el `request-id`** |

**Hay un hueco en el código de hoy, y esta decisión lo cierra.** Hoy un adaptador solo puede avisar
un problema lanzando una excepción, y `ProviderCall` convierte cualquier excepción en `Faulted`, que
termina como `ProviderUnavailable`. Un texto cortado por `max_tokens` quedaría registrado como «el
proveedor no estaba disponible», que es falso.

La propuesta: una excepción propia de `Application` —`ExplanationOutputMalformedException`— que
`ExplanationExchange` clasifica como `MalformedOutput` **antes** del clasificador genérico. La
restricción: **`ProviderCall` es compartido con el proveedor antifraude, y su comportamiento para ese
proveedor no puede cambiar ni un byte.** La revisión tiene que mirar esto con cuidado.

Por qué los 4xx no se reintentan como transitorios: la documentación los describe como errores de
formato, de clave o de facturación. Reintentar un 401 es pagar —o hacer esperar a alguien— por un
error que no se va a arreglar solo. Y el tope de gasto aparece como 400 o 429: por eso el log lleva
el **tipo** de error y el `request-id`, que es lo único que permite distinguirlos después.

`max_tokens` se fija **con margen por encima** de lo que necesitan los 1.200 caracteres de
`MaximumSummaryLength`. Así un texto demasiado largo lo detecta el verificador como `TooLong`, que
dice lo que pasó, en vez de cortarse y aparecer como `MalformedOutput`.

## D7 — El secreto

- **Un solo lector**: el registro de dependencias de `Infrastructure` lee `ANTHROPIC_API_KEY`. Nadie
  más.
- **Nunca en un log, nunca en una excepción, nunca en el navegador.** La cabecera que lleva la clave
  se redacta explícitamente en el registro del `HttpClient`, aunque hoy no se loguee: se redacta por
  construcción, no por suerte.
- **Un test que lo afirma**: un registrador espía sobre un camino de llamada que falla —401, por
  ejemplo—, y la afirmación de que la clave no aparece en **ningún** mensaje de log ni de excepción.
- **Falla al arrancar**, como hoy: `AI_PROVIDER=anthropic` sin `ANTHROPIC_API_KEY` o sin
  `ANTHROPIC_MODEL` detiene el proceso. El mensaje **nombra la variable y nunca su valor**.

## D8 — La instancia pública no puede arrancar con una clave paga

`SharedInstance:Enabled=true` junto con `AI_PROVIDER=anthropic` **detiene el arranque**. La decisión
de no poner una clave paga en una sandbox anónima deja de ser una promesa y pasa a ser código.
Cambiarla algún día es una decisión consciente, con su propia entrada en la bitácora.

## D9 — Qué le entra al modelo

- **Un prompt de sistema fijo y versionado** (`p1`), que declara el papel —redactar, no decidir—, el
  idioma del despliegue, el tope de largo, la instrucción de usar solo los hechos dados y citar las
  reglas que menciona.
- **Los hechos, como datos**: `ExplanationInput` serializado como JSON en el turno del usuario,
  delimitado como datos y no como instrucciones. Ya es todo texto del motor; delimitarlo cuesta nada
  y es la práctica correcta aunque hoy no haga falta.
- **El proveedor espía se extiende al cuerpo HTTP**: el JSON que sale por la red no contiene ciudad,
  identificadores, notas de revisión ni etiquetas. Hoy el espía mira lo que recibe el proveedor; ahora
  mira además lo que viaja.

## D10 — El agujero semántico se prueba, no se esconde

El verificador comprueba que cada cifra **exista** entre los hechos, no que la oración que la
contiene sea **verdadera**. Con la plantilla eso nunca pasa. Con un modelo sí: «el monto es 3,4 veces
**menor** que la mediana» pasa el verificador, porque el 3,4 está.

Esta etapa **no lo cierra**, y lo dice de dos maneras:

1. **Un test con nombre propio** que alimenta al verificador con esa oración invertida y **afirma que
   pasa**. Si algún día alguien cierra el agujero, ese test se pone rojo y obliga a actualizar lo que
   el README dice.
2. **Los textos de la corrida real los lee una persona**: el coordinador. «Renderiza» no es «se lee».

Se consideró y se deja como candidata una verificación **por señal** —cada oración atada a una regla,
y sus cifras verificadas solo contra los hechos de esa regla—. Cierra una parte del agujero, pero
cambia la forma del puerto y de la plantilla, y eso es otra etapa.

## D11 — Costo: estimado, acotado y medido

- **Estimación, con los precios verificados**: una explicación ronda 1.500 tokens de entrada y 300 de
  salida. Con Haiku 4.5, eso es cerca de **US$ 0,003 por explicación**. Es una estimación; el número
  real sale de `input_tokens` y `output_tokens`, que la fila ya guarda desde la Etapa 7.
- **Techo de la corrida real**: 23 alertas por tres intentos como máximo son 69 llamadas, del orden de
  **US$ 0,20** en el peor caso.
- **Techo externo**: el espacio de trabajo dedicado lleva un **tope de gasto mensual**, sugerido en
  US$ 5. Es lo que acota el daño si la clave se filtra.

## D12 — Tests sin red; la corrida real la hace el coordinador

**Todos los tests usan el transporte simulado**: un `HttpMessageHandler` con respuestas grabadas del
formato documentado. Como mínimo: éxito; `refusal`; `max_tokens`; 429; 529; 500; 401; 400; plazo
vencido; una cifra inventada; una regla que no disparó; marcado; y la oración invertida de D10. Cada
una termina en el código que D6 le asigna.

**La corrida real no la corre ningún agente.** `E11B` deja un script que la hace —base nueva, siembra,
scoring, `AI_PROVIDER=anthropic`, y una explicación pedida para cada alerta— y que imprime por alerta
el estado, el código de fallo, los tokens y el costo estimado, **sin imprimir jamás una variable de
entorno**. El coordinador lo corre en su terminal, con la clave en su `.env`. El agente nunca lee ese
archivo: su configuración se lo prohíbe, a propósito.

## D13 — La evidencia se publica

Los textos que escriba el modelo real sobre el corpus sintético **se versionan** en `docs/muestras/`,
con la fecha, el modelo y el resultado del verificador para cada uno, **incluidos los rechazados**.
Un revisor tiene que poder ver qué escribió el modelo, qué dejó pasar el verificador y qué no.

---

## Partición

1. **`E11A-INTEGRACION-CONTINUA`** — brief escrito. Va primero: la compuerta tiene que correr sola
   antes de tocar el camino más delicado del proyecto.
2. **`E11B-ADAPTADOR-ANTHROPIC`** — D1 a D12: el adaptador, la taxonomía, el canal de
   `MalformedOutput`, los guardianes de arranque, los tests del transporte simulado, el espía de logs
   y el script de la corrida real, **sin correrlo**.
3. **`E11C-CORRIDA-REAL`** — el coordinador crea la cuenta, el espacio de trabajo con tope, la clave,
   y corre el script. El agente lee la salida, publica la evidencia (D13) y actualiza README,
   artículo y guía.

## Condiciones de parada de la etapa

- Si en la corrida real la mayoría de los textos no pasa el verificador, **se cambia el prompt, no el
  verificador**. Si ni así pasan, es un hallazgo sobre el modelo o sobre el verificador, y la decisión
  es del coordinador.
- Si la API real se comporta distinto de lo que el transporte simulado supone, **se corrige el
  simulador primero** y la corrida se repite.
- Si algún paso pide la clave dentro de un agente, se detiene.

## Cambios de estado canónico que exige este diseño

1. **Blueprint, bitácora**: una entrada por cada decisión D4, D5, D6, D8 y D10, desde la 71.
2. **Blueprint §9** y **`.env.example`**: `ANTHROPIC_MODEL` deja de estar vacío, con su fecha.
3. **README, «Límites declarados»**: «la explicación la escribe una plantilla» pasa a decir que puede
   escribirla un modelo, que la instancia pública usa la plantilla **por decisión**, y que el agujero
   semántico sigue abierto y tiene un test que lo fija.
4. **`AGENTS.md`**: la regla de que si el modelo no pasa, se cambia el prompt y no el verificador.
