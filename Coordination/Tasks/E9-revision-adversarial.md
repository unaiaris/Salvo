# Salvo — Revisión adversarial del diseño propuesto de Etapa 9

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-06
> Revisor: Claude (Fable 5.1 · `xhigh`)
> Objeto: `Coordination/Tasks/E9-DISENO.md` v1 (base `main` en `ed1e90f`)
> Método: lectura del diseño, del Blueprint (§2, §4.1, §4.2, §4.4, §4.6, §4.7, §5, §7, §11, §12 y la
> bitácora completa), Progress, Workboard, la plantilla de brief, las revisiones adversariales de E7
> y E8 y el inventario de cifras del handoff de `E8A`; del código real en `Salvo.Domain/Risk`,
> `/Explanations`, `/Alerts`, `/Evaluation`, `Salvo.Application/Orders/Seed`, `/Risk`,
> `/Explanations`, `/Alerts`, `/Metrics`, `Salvo.Infrastructure` (seed embebido, mock, plantilla,
> configuraciones EF, composición), `Salvo.Api` (Program, endpoints), `backend/tests` completo,
> `frontend/src/lib`, `frontend/src/app/**`, `frontend/src/test`, `scripts/*.sh`,
> `tools/capturas/capturar.mjs`, `docs/**` y `.env.example`. **La fixture `demo-orders.v1.json` se
> analizó entera con Python** —instantes, comercios, compradores, montos, países, franjas horarias
> con la misma aritmética de ventana de 30 días que usa el motor— y las cifras de disparo de cada
> regla salen de consultas de solo lectura (`immutable=1`) sobre la base nueva que dejó `E8B`,
> `backend/src/Salvo.Api/salvo-demo-20260906-194152.db` (300 pedidos, una corrida, 18 alertas), y
> sobre `salvo.db` para el punto de la franja horaria. No se ejecutó ninguna compuerta ni ningún
> test, no se hizo push y no se editó ningún archivo salvo este informe.

La revisión tiene tres partes, en el orden en que pediste: el criterio de dominio primero, porque
es la pregunta que este proyecto nunca se hizo y la que decide si el corpus nuevo enseña algo o
enseña algo falso; después los hallazgos por severidad contra el código; y al final las tres
verificaciones estructurales, respondidas de forma directa. Las seis preguntas abiertas se
responden dentro de los hallazgos y se recogen al final.

Una cosa antes de empezar, porque cambia la lectura de todo lo demás: **la fixture actual no es un
corpus, es una grilla.** Los 300 pedidos están a exactamente 9,6 horas uno del otro —mínimo, mediana
y máximo de la separación son iguales—, así que en hora de Montevideo solo existen cinco horarios
(01:48, 06:36, 11:24, 16:12 y 21:00), cada uno de los 75 pares comercio–comprador tiene exactamente
cuatro pedidos, los días de la semana están repartidos 14 o 15 cada uno, y los montos legítimos
no llegan a 1,6 veces la mediana mientras los de fraude están entre 13 y 21 veces. Nada de eso lo
dice ningún documento. Importa para la Etapa 9 porque el corpus v2 no puede ser «el v1 más casos
duros»: cualquier arquetipo realista rompe la grilla, y los tests que hoy pasan lo hacen en parte
porque la grilla no tiene esquinas.

---

## Primera parte — El criterio de dominio

### Qué ven las seis reglas, exactamente

Antes de discutir arquetipos hay que fijar qué observa cada regla, porque la mitad de la discusión
es sobre la **clave** de cada una y no sobre su umbral. Todo sale de `TemporalRiskEngine.cs` y
`RuleConfig.cs`:

| Regla | Peso | Clave | Dispara cuando | Dónde |
| --- | --- | --- | --- | --- |
| `amount_anomaly` | 40 | comprador si tiene ≥3 pedidos previos en 90 días en la misma moneda; si no, comercio con ≥3 | el monto es ≥3× la mediana de esa clave | `TemporalRiskEngine.cs:61-109`, `RuleConfig.cs:12-16` |
| `velocity` | 30 | (comercio, comprador) | ≥3 pedidos previos **del mismo comprador** en 10 minutos | `:111-130`, `RuleConfig.cs:18-20` |
| `cross_border_velocity` | 40 | (comercio, comprador) | un pedido previo del mismo comprador, **desde otro país**, en las últimas 2 horas | `:132-155`, `RuleConfig.cs:22-23` |
| `unusual_hour` | 10 | comercio | ≥20 pedidos del comercio en 30 días y la franja de 6 h del pedido, **en hora de Montevideo**, tiene ≤10 % de ellos | `:157-188, 262-266`, `RuleConfig.cs:10, 25-29` |
| `new_buyer_high_value` | 30 | comprador **sin ningún pedido previo**, sin ventana | monto ≥2,5× la mediana del comercio, con ≥3 pedidos previos | `:190-224`, `RuleConfig.cs:31-34` |
| `foreign_country` | 20 | comercio | el país del pedido no es el habitual del comercio, y el habitual tiene ≥60 % de ≥3 pedidos en 90 días | `:226-260`, `RuleConfig.cs:36-39` |

Umbral 60, tope 100 (`RuleConfig.cs:8-9`); bandas 60–69 media, 70–89 alta, 90–100 crítica
(`AlertPolicy.cs:33-39`). Y tres hechos que se deducen de la tabla y que el diseño no escribe:

- **El motor no lee el dispositivo.** `Order` tiene `DeviceSessionId` (`Order.cs:72`), la
  importación lo acepta (`OrderImportFields.cs:14`), la fixture lo trae en 250 de 300 pedidos, y
  `TemporalRiskEngine.cs` no lo menciona ni una vez. Ninguna regla es por dispositivo.
- **Solo dos reglas miran al comprador** y las dos son extremas: `amount_anomaly` a 3× su propia
  mediana y `velocity` a cuatro pedidos en diez minutos. País y hora son hábitos **del comercio**,
  no del comprador: un comprador que siempre compra a las 11:00 y una noche compra a las 03:00 no
  dispara nada si el comercio vende de noche.
- **Hay combinaciones imposibles.** `new_buyer_high_value` exige cero pedidos previos
  (`TemporalRiskEngine.cs:196`), y `velocity` y `cross_border_velocity` exigen pedidos previos del
  mismo comprador: nunca conviven. Un comprador nuevo llega como máximo a 40+30+10+20 = 100; uno con
  historia, a 40+30+40+10+20 = 140, que el tope deja en 100.

La aritmética de las sumas también manda. Con pesos {40, 30, 40, 10, 30, 20} y umbral 60:

- **60, banda media**: `amount`+`foreign`; `cross_border`+`foreign`;
  `velocity`+`foreign`+`unusual_hour`; `new_buyer`+`foreign`+`unusual_hour`.
- **70–89, banda alta**: `amount`+`new_buyer`; `amount`+`velocity`; `cross_border`+`velocity`;
  `amount`+`foreign`+`unusual_hour`; `amount`+`cross_border` (80).
- **90–100, crítica**: `amount`+`new_buyer`+`foreign` (la de hoy); `amount`+`velocity`+`foreign`;
  `amount`+`cross_border`+`unusual_hour`; y cualquier cosa con cuatro reglas.
- **`unusual_hour` decide un veredicto en exactamente dos combinaciones**: cuando el resto suma 50,
  que es `velocity`+`foreign` o `new_buyer`+`foreign`. En todas las demás solo mueve la banda
  (60→70). «Alcanzable» no es «decisiva»: un corpus donde `unusual_hour` dispara pero nunca cambia
  ni el veredicto ni la banda tampoco la demuestra. El criterio de `E9B` tiene que decir cuál de las
  dos cosas exige.

### El falso negativo del diseño: la forma es correcta, la historia no

El diseño propone «una cuenta tomada de un comprador establecido que compra algo de valor
corriente: monto normal, país habitual, hora habitual, comprador con historia, sin ráfaga»
(`E9-DISENO.md:82-88`). La **forma** —un pedido que ninguna de las seis reglas puede tocar— es
exactamente lo que hace falta. La **historia** es lo que un ingeniero imagina que es una cuenta
tomada, y en un corpus que se elige «por lo que enseña» (D4) la historia es lo que se enseña.

Una cuenta tomada existe para monetizar antes de que la víctima se dé cuenta. Monetizar es sacar
valor de la cuenta: mercadería a una dirección que controla el atacante, bienes digitales o tarjetas
de regalo, saldo o puntos. Un pedido de monto normal enviado a la dirección de siempre **no
monetiza nada**: lo recibe la víctima. Por eso las tres señales que cualquier equipo antifraude
asocia a una cuenta tomada son otras: un dispositivo o sesión nueva para un comprador conocido, un
cambio de datos de envío o de contacto justo antes de comprar, y un monto o una cadencia por encima
del hábito **de esa cuenta**. La cuenta tomada que imita a su dueño en monto, país, hora y ritmo es
una minoría marginal, y la primera pregunta de alguien del rubro sería «¿y entonces cómo se etiquetó
como fraude?». El arquetipo necesita una historia para la etiqueta también, y el diseño no la da.

Con la forma que el diseño describe hay un fraude real, frecuente y con la etiqueta explicada por sí
misma: el **fraude amigo** —«autofraude», *first-party fraud*, «não reconheço a compra»—. El
comprador compra, recibe, y desconoce el cargo ante su banco. En el momento de la compra es
**indistinguible de una compra legítima porque lo es**; la etiqueta llega meses después con el
contracargo, que es precisamente lo que la decisión 37 dice que un comercio sí conoce
(`Salvo-Blueprint.md:758`). En Brasil es la categoría de la que más habla el sector. Y el argumento
del efecto de red se sostiene mejor que con la cuenta tomada: lo que un proveedor ve de ese
comprador es su **historial de contracargos en otros comercios**, que es la información que ningún
comercio tiene solo. La forma del diseño se conserva; la historia se corrige.

Y la cuenta tomada sí entra al corpus, pero como lo que es. Un atacante con una cuenta con historia
compra desde un dispositivo que esa cuenta nunca usó, en el mismo país, por 2 veces la mediana del
comprador —no 3: los atacantes tantean—, y hace tres pedidos separados por veinte minutos. Contra
las seis reglas: `amount_anomaly` en clave comprador pide 3×, `velocity` pide cuatro en diez
minutos, `cross_border` pide cambio de país en dos horas, `unusual_hour` y `foreign_country` miran
al comercio, `new_buyer` pide cero historia. Score 0. Y la señal que lo delataría —**dispositivo
nuevo para un comprador conocido**— está en el archivo y el motor no la lee. Eso enseña algo
preciso: la regla que falta es por comprador y por dispositivo, y un proveedor con efecto de red ve
ese dispositivo en otros comercios.

### Los arquetipos que recomiendo

Tres falsos negativos, cada uno invisible **por una razón distinta**, que es lo que los vuelve
didácticos: en el primero no existe señal transaccional; en el segundo la señal está en los datos y
ninguna regla la lee; en el tercero la regla existe y está clavada a la entidad equivocada.

| # | Arquetipo | Cómo se ve en el archivo | Por qué las seis reglas no lo ven | Qué lo vería | Etiqueta |
| --- | --- | --- | --- | --- | --- |
| FN1 | **Fraude amigo.** Comprador con historia compra lo de siempre y desconoce el cargo | monto ≈ mediana propia, país y franja habituales, mismo dispositivo de siempre, sin ráfaga | No hay nada que ver en la transacción: es una compra legítima hasta el contracargo | Historial de disputas del comprador en la red del proveedor | fraude, por contracargo |
| FN2 | **Cuenta tomada, vista en el dispositivo.** | comprador con ≥3 pedidos previos; `deviceSessionId` nuevo; 2× su mediana; mismo país; tres pedidos a 20 min | Las dos reglas por comprador piden 3× y 4-en-10-min; país y hora son del comercio; el dispositivo no es *feature* | Un dispositivo desconocido para la cuenta; el mismo dispositivo en otros comercios | fraude |
| FN3 | **Prueba de tarjetas desde un dispositivo.** Cinco «compradores» distintos, el mismo `deviceSessionId`, montos mínimos, minutos entre uno y otro | cinco `BUY_` nuevos, un `DEV_`, montos ≤ mediana, misma hora | `velocity` cuenta por (comercio, comprador) (`TemporalRiskEngine.cs:298, 303, 319`): cada comprador tiene cero previos; `new_buyer` pide monto alto | Velocidad por dispositivo, por tarjeta o por IP | fraude |

Los tres se **sí** ven con un proveedor externo, y el corpus puede mostrarlo: el mock decide por los
dígitos finales de la referencia (`MockAntifraudProvider.cs:17-18, 84-103`), así que basta elegir
para esos pedidos referencias con resto 75–89 para que el proveedor los deniegue. Pero ahí aparece
el hallazgo 2 de la segunda parte: **un pedido sin alerta no tiene ninguna pantalla donde mostrar
ese veredicto**.

Cuatro falsos positivos. Los dos del diseño, corregidos, y dos más que hacen decisivas a las reglas
que hoy no deciden nada:

| # | Arquetipo | Cómo se ve | Qué dispara | Score | Por qué una analista lo descarta |
| --- | --- | --- | --- | --- | --- |
| FP1 | **El salto de país en dos horas** —no «el cliente que viaja»— | comprador con historia; un pedido desde AR y otro desde UY 90 minutos después | `cross_border` 40 + `foreign` 20 | 60, media | Roaming: el tráfico móvil sale por el país de la operadora, el wifi del hotel por el país real. También VPN y proxies corporativos. Dos pedidos, dos redes |
| FP2 | **La primera compra grande** | comprador sin historia, 3× la mediana del comercio, país habitual | `new_buyer` 30 + `amount` (clave comercio) 40 | 70, alta | Un cliente nuevo que compra un electrodoméstico. Es el costo declarado de cualquier regla «nuevo y caro» |
| FP3 | **El revendedor de madrugada** | comprador con historia; cuatro pedidos en ocho minutos, desde AR, a las 02:00 de un comercio que vende de 09 a 18 | `velocity` 30 + `foreign` 20 + `unusual_hour` 10 | 60, media | Un mayorista que arma el pedido en varias compras. Es la **única** forma de que `velocity` y `unusual_hour` decidan un veredicto legítimo |
| FP4 | **El regalo desde afuera** | comprador con ≥3 pedidos previos; 3× su propia mediana; desde BR estando el comercio en UY | `amount` (clave **comprador**) 40 + `foreign` 20 | 60, media | Un cliente habitual de viaje compra un regalo caro. Pone `amount_anomaly` en clave comprador en un falso positivo, que hoy no ocurre en ninguna alerta |

Sobre FP1 hay una corrección al diseño que conviene escribir con todas las letras: **«un cliente
legítimo que viaja» no es un falso positivo de este motor.** `foreign_country` vale 20 y sola no
alcanza el umbral; el corpus actual ya tiene 16 pedidos legítimos desde el exterior y los 16 quedan
en score 20, sin alerta (consulta sobre la base nueva: 34 evaluaciones con `foreign_country`, 18 en
alertas y 16 en score 20). El diseño dice que el viajero «dispara `foreign_country` y
`cross_border_velocity`» (`E9-DISENO.md:89-90`), y la segunda exige un pedido previo **desde otro
país en las dos horas anteriores** (`TemporalRiskEngine.cs:138-142`). Eso no es viajar: es cambiar
de país entre dos pedidos en dos horas, y la explicación honesta de por qué pasa en la vida real es
la geolocalización por IP, no el pasaporte.

Y eso destapa una indefinición del Blueprint que el corpus necesita resuelta: **qué país es
`countryCode`.** El §4.1 y el §7 dicen solo «ISO 3166-1 alpha-2» (`Salvo-Blueprint.md:137, 418`).
Si es el país de facturación, un viajero no lo cambia y FP1 no existe; si es la geolocalización de
la sesión, FP1 y FN2 tienen sentido. Los seis relatos de arriba asumen que es el país de la sesión.
El diseño v2 tiene que decirlo y `E9D` llevarlo al §4.1.

### Tres cosas más que alguien del rubro pediría

- **Qué significa la etiqueta.** El §7 define `OrderEvaluationLabel` como «ground truth exclusivo
  de demo/evaluación» y nada más (`Salvo-Blueprint.md:424-432`). Para que FN1 sea fraude y FP2 sea
  legítimo la definición tiene que ser «el pedido terminó en contracargo por fraude o fue
  confirmado como fraude», que además es lo que un comercio real sabe (decisión 37). Sin esa
  definición, «fraude amigo» y «compra legítima disputada por error» son la misma fila con dos
  etiquetas posibles.
- **La tasa base está inflada, y la precisión depende de ella.** 18 de 300 es un 6 %; las tasas de
  fraude en comercio electrónico se cuentan en fracciones de punto o en un par de puntos. Un corpus
  de demostración tiene que sobre-muestrear el fraude para que haya algo que mirar, y no hay
  problema con eso, pero el bloque marcado del README tiene que decir que la tasa es de
  construcción y que **la precisión no se extrapola**: a una tasa real del 1 %, un criterio que
  marca el 6 % de los pedidos tendría una precisión de un dígito.
- **La zona horaria única.** `unusual_hour` y la plantilla escriben «hora del comercio» y las dos
  quieren decir Montevideo (`RuleConfig.cs:10`, `DeterministicExplanationProvider.cs:113-126`). D6
  argumenta Brasil y México (`E9-DISENO.md:121`) y D9 pide «mercados plausibles». São Paulo y Buenos
  Aires están en UTC−3 como Montevideo, sin horario de verano desde 2019 y 2009: las franjas
  coinciden. Ciudad de México está en UTC−6: una jornada de 09 a 18 cae de 12 a 21 hora de
  Montevideo, cruza dos franjas, y la explicación diría «franja de 18:00 a 24:00, hora del comercio»
  sobre un comercio que cerró a las 18. La salida barata y correcta es que el corpus v2 se quede en
  el corredor UTC−3 —Uruguay, Brasil, Argentina— y que el diseño diga que `BusinessTimeZone` es del
  despliegue, no del comercio. Una zona por comercio es una columna que el contrato de importación
  no tiene (`OrderImportFields.cs:5-14`) y una migración que la etapa no necesita. Y de paso:
  `.env.example:4` declara `BUSINESS_TIMEZONE=America/Montevideo` y **nada lo lee**; la única
  aparición del huso en el backend es la constante de `RuleConfig.cs:10`. Es una variable muerta que
  promete una configuración que no existe.

---

## Segunda parte — Hallazgos, por severidad

### 1. D3 y D4, alta. Los arquetipos son los de un ingeniero, y el que más enseña está mal contado

Es la primera parte entera. Lo que va al brief: los tres falsos negativos y los cuatro falsos
positivos de las tablas, con su historia, su etiqueta y la regla que los ve o no los ve; la
definición de `isFraudLabel`; la semántica de `countryCode`; la nota de tasa base; el corredor
UTC−3. Y el criterio de aceptación de `E9B` no es «F1 < 1»: es **«cada arquetipo produce la fila de
la matriz de confusión que su relato predice»**, con la lista de pedidos y su celda esperada escrita
en el brief antes de correr el motor. Si hay que retocar montos hasta que el motor coincida con la
predicción, el corpus se está ajustando a las reglas otra vez, con otro número.

### 2. D3 y D8, alta. El argumento central del corpus —«un proveedor con efecto de red sí lo vería»— no se puede mostrar en la consola, porque un pedido sin alerta no tiene pantalla

D3 dice que el falso negativo es «exactamente el argumento que el README hace sobre lo que Salvo no
es, demostrado con datos en vez de afirmado» (`E9-DISENO.md:85-88`). Contra el código:

- La opinión del proveedor se muestra en un solo lugar: el bloque externo del detalle de una alerta
  (`frontend/src/app/alerts/[id]/external-block.tsx`). El dashboard no tiene ningún panel de
  evaluación externa: `DashboardViews.cs`, `GetDashboardHandler.cs`, `panels.tsx` y
  `dashboard/page.tsx` no contienen la palabra.
- Un falso negativo, por definición, tiene score menor que 60 y **no abre alerta**. El endpoint que
  pide la evaluación externa de todo el corpus existe justamente para alcanzarlos, y su propia
  descripción lo dice: «covers the orders that never produced an alert, which the alert detail
  cannot reach, without adding an orders screen» (`schema.d.ts:263`).
- D8 excluye `/orders` porque «no cambia lo que el proyecto puede afirmar» (`E9-DISENO.md:147`).
  Es al revés: sin una superficie que liste pedidos sin alerta con su veredicto externo, la
  afirmación más importante de la etapa —«hay fraude que el motor local no ve y el proveedor sí»—
  solo se puede hacer con un `curl` o con el README. En pantalla, los tres falsos negativos son
  filas que no existen.

Opciones, de menor a mayor superficie: (a) un panel en el dashboard, «Denegados por el proveedor sin
alerta local», con el conteo y los pedidos —es una lectura más, sin acción, y cabe en la sección
externa que hoy no existe—; (b) `/orders` mínima, solo lectura, con el score vigente y el veredicto
externo, que además cierra el hallazgo 4 de la revisión de E8 sobre volver a una alerta revisada; (c)
aceptar que el argumento se hace fuera de la consola y decirlo en D8. La (a) es la que menos cuesta
y la que hace visible exactamente lo que el corpus quiere enseñar. Lo que no puede quedar es la
contradicción actual entre D3 y D8.

### 3. D2, alta. El seed no puede cargar un corpus v2: se niega antes de mirar la base, con un `500`, y el guardián de conflicto se esquiva con solo renombrar un comercio

La restricción que «ordena toda la etapa» dice que sembrar una fixture nueva sobre una base con la
vieja «lanza `DemoSeedConflictException`» (`E9-DISENO.md:22-25`). Es cierto solo en un caso que el
diseño mismo excluye. Contra el código:

- **Primero falla la forma, no el conflicto.** `ValidateDocumentShape` exige `version == "1"`,
  exactamente 300 pedidos y exactamente 18 etiquetas de fraude
  (`SeedDemoOrdersHandler.cs:87-103`), y corre **antes** de leer la base (`:16` contra `:29`). Un
  `demo-orders.v2.json` con `"version": "2"`, o con un número distinto de fraudes —que es lo que D3
  pide—, lanza `InvalidOperationException` con «The demo fixture version is not supported.» o «The
  demo fixture must contain exactly 18 fraud labels.» (`:91, :101`). El endpoint solo atrapa
  `DemoSeedConflictException` (`OrderEndpoints.cs:151-157`) y `Program.cs` no registra ningún
  manejador de excepciones (`Program.cs:31-52`): la respuesta es un **`500`**, también sobre una
  base vacía. Y el recurso embebido se llama `demo-orders.v1.json` por constante
  (`EmbeddedDemoOrderSource.cs:9-10`). «Motor quieto» en `E9B` (`E9-DISENO.md:176`) está bien,
  pero `E9B` toca código: el validador de forma, el nombre del recurso, `DatasetVersion`
  (`SeedDemoOrdersResult.cs:4`) y `DemoSeedTests.cs:54-68`.
- **El mensaje que el diseño cita no existe en ninguna capa.** «El conjunto de demostración entra en
  conflicto con una referencia existente» (`E9-DISENO.md:35-36`) no está en el handler, ni en el
  endpoint, ni en la consola. Hay tres textos distintos: el handler distingue dos causas —«conflicts
  with an existing merchant order reference» y «…with an existing evaluation label»
  (`SeedDemoOrdersHandler.cs:45-46, 67-68`)—; el endpoint **las aplasta** en una sola,
  `DEMO_DATA_CONFLICT` «The demo dataset conflicts with existing immutable order data.»
  (`OrderEndpoints.cs:153-156`); y la consola ya dice lo que D2 pide que diga a medias: «La base
  tiene pedidos con las mismas referencias que la fixture pero con datos distintos … Usá una base
  vacía para cargar la demo» (`messages.ts:84-89`). Lo que falta no es la recomendación sino la
  **causa**: «esta base tiene la versión anterior del corpus». Y para decirla, la API tiene que
  saberla.
- **Renombrar un comercio anula el guardián.** La referencia es `merchantId:merchantReferenceId`
  (`SeedDemoOrdersHandler.cs:116-119`, `Order.cs:76`) y las etiquetas se buscan por el id del
  pedido (`:53-54`). Si D9 «geografía plausible» cambia `MER_US_MARKET` por `MER_AR_TIENDA`, el v2
  no colisiona con nada: 300 inserciones, `duplicateOrders = 0`, `200 OK`, y la base queda con los
  600 pedidos donde «la mitad contradice a la otra» que D2 dice querer evitar
  (`E9-DISENO.md:32-34`). «Reusa el mismo espacio de referencias» tiene que incluir **los mismos
  tres `merchantId`**, o el guardián es decorativo.

Y la pregunta 6, si el sistema puede decirlo antes de fallar. La detección ya ocurre antes de
escribir —el `throw` de `:45` precede al `AddSeedDataAsync` de `:74`—, así que nada queda a medias.
Lo que no hay es un aviso **antes del clic**: `/import` muestra el botón de carga con solo
`demoDataEnabled` (`import/page.tsx:62-72`). La forma barata y sin duplicar el caso de uso es un
**ensayo** del mismo handler: `POST /api/demo-data/seed?dryRun=true` que ejecuta las mismas
comparaciones y devuelve `{ wouldInsert, duplicates, conflict: PREVIOUS_CORPUS | IMPORTED_ORDERS |
NONE }` sin escribir, y la consola lo consulta al cargar la página para rotular el botón: «Esta base
tiene el corpus v1; cargar el v2 exige una base nueva». El conflicto distingue las dos causas con un
criterio simple: si toda referencia en conflicto está en el rango del corpus con los tres comercios
de la fixture, es la versión anterior; si no, son importaciones. Eso da un código nuevo,
`DEMO_DATA_PREVIOUS_CORPUS`, que por la decisión 57 va a `messages.ts` y a su test de exactitud.

### 4. D5, alta. «El extractor se borra y todo lo demás queda intacto» es falso en cuatro lugares, y el diseño no dice qué pasa con las filas ya escritas

Es la pregunta 5. La Etapa 7 lo prometió (`SignalFacts.cs:19-28`, `E7-DISENO.md:398`), y contra el
código de hoy la promesa alcanza a la plantilla y a `ExplanationFacts` **como tipos**, no como
callers:

1. **El proveedor determinista parsea por su cuenta.** `DeterministicExplanationProvider.ExplainAsync`
   llama a `SignalFacts.ParseAll(input.Signals)` (`:61`), y `ExplanationInput.Signals` es
   `IReadOnlyList<RiskSignal>` (`ExplanationInput.cs:54`). Con campos tipados, el input tiene que
   cargarlos y el proveedor dejar de parsear; lo mismo `RequestExplanationHandler.cs:180`.
2. **El conjunto de hechos se alimenta de la prosa, no de los campos.** `SignalFacts.Numbers` sale
   de tokenizar `detail` (`SignalFacts.cs:100`) y `ExplanationFacts.For` mete en el conjunto **todo
   número que el motor escribió** (`ExplanationFacts.cs:70-77`). Hoy la prosa aporta cifras que
   `SignalFacts` **no tiene como campo**: la mediana (el grupo `median` de la expresión regular de
   `:233` se captura y se descarta en `:137-145`), el monto en centavos repetido dentro de la señal,
   las constantes de configuración (90 días, 10 minutos, umbral 4) y las horas de la franja. D5 lista
   `median` y `window` entre los campos nuevos, bien; pero «`ExplanationFacts` pasa a alimentarse de
   los campos, sin parsear nada» (`E9-DISENO.md:112-113`) exige que alguien enumere esos hechos a
   mano. Si la mediana se olvida, un modelo real que escriba «la mediana fue 86,85 BRL» es rechazado
   con `NOT_GROUNDED_NUMBER` y **ningún test dorado lo nota**, porque la plantilla no escribe la
   mediana. Criterio de aceptación para la tarea: un test que afirme que el valor de cada campo
   tipado de cada regla está en el conjunto de hechos, y que el conjunto construido desde campos es
   igual al construido desde la prosa para las mismas evaluaciones. Ese segundo test es la razón del
   hallazgo 5.
3. **`detail` es contrato, no solo prosa.** `RiskSignal` es `(Rule, Weight, Detail)`
   (`RiskSignal.cs:3`); la serialización canónica lo escribe (`RiskSignalSerializer.cs:36-39`);
   la API lo expone como `AlertSignalView(Rule, Weight, Detail)` (`AlertViews.cs:5`,
   `AlertProjection.cs:204-212`); la guarda **rechaza** una señal sin `detail` (`guards.ts:198-204`);
   la consola lo pinta (`evaluation-blocks.tsx:26`); y lo afirman `AlertEndpointTests.cs:129`,
   `RiskEvaluationTests.cs:58-60, 114`, `TemporalRiskEngineTests.cs:334`,
   `RiskSignalSerializerTests.cs:71-72` y la fixture de tests del frontend, que ya finge un `detail`
   en castellano que la API nunca emitió (`fixtures.ts:18`). `e3-v2` es un cambio de contrato con
   recaptura de OpenAPI, `schema.d.ts`, `guards.ts`, `fixtures.ts` y `boundary.test.ts`: la lección
   de `E7A` en el Workboard, otra vez. D5 dice «la interfaz compone el texto» y no lista nada de
   esto. Y para que la proyección de `guards.ts` siga siendo simple, la forma en el cable conviene
   que sea **plana y nullable** por regla —como `SignalFacts` hoy— y no un polimorfismo por regla.
4. **La precisión numérica del fingerprint queda sin definir.** Hoy la prosa fija el redondeo:
   `{ratio:0.0}`, `{elapsed.TotalMinutes:0.##}`, `{share:0.0}` (`TemporalRiskEngine.cs:108, 154,
   187, 259`). En `e3-v2` esos valores viajan como números JSON escritos por `Utf8JsonWriter`, y un
   `decimal` arrastra su escala: `(decimal)201111 / 8685` no es `23.2` hasta que alguien lo redondea.
   El fingerprint hashea la cadena serializada tal cual (`RiskEvaluationFingerprint.cs:11-14`), así
   que la **precisión canónica de cada campo** es parte de la identidad de toda evaluación para
   siempre y tiene que estar escrita en el diseño, no descubierta en el test dorado de
   `ScoringRunPersistenceTests.cs:95-109`.

Y lo que el diseño no pregunta: **qué pasa con las filas `e3-v1` que ya existen.**
`RiskSignalSerializer.Deserialize` lee `{rule, weight, detail}` (`:52-60`); si `RiskSignal` pierde
`Detail` y gana campos, una fila vieja deserializa con campos nulos. Eso alcanza a
`alerts.signals_snapshot_json`, que por diseño **no se reescribe nunca** (`Alert.cs:6-11, 49`,
decisión 33), y a las evaluaciones viejas que siguen siendo el snapshot de toda alerta existente.
D2 dice que la base del usuario «no se pierde» (`E9-DISENO.md:31`); con un lector de una sola forma,
sus 21 alertas quedan con el bloque «Snapshot que abrió la alerta» vacío o roto. Dos salidas, y hay
que elegir una: (i) `e3-v2` conserva `detail` como campo **opcional y heredado**, la consola compone
desde los campos cuando están y muestra `detail` cuando no, y un test lee una fila `e3-v1` real por
el camino nuevo; (ii) se declara que `e3-v2` también exige base nueva y se deja de prometer que la
vieja se puede abrir. La (i) es barata y honesta. Dos cosas menores del mismo lugar: la excepción
que el handler atrapa en `RequestExplanationHandler.cs:182-191` queda muerta, y el handler usa
`RuleConfig.E3V1` (`:174`) en vez de `target.RuleConfigVersion`, con lo que una evaluación `e3-v1`
se explicaría con la configuración `e3-v2`; hoy es inocuo porque los umbrales no cambian, pero es
el lugar donde se elige la config y debería elegirse por la versión de la fila. `E3V1` está fijo en
seis llamadas (`RunScoringHandler.cs:27`, `EvaluateLocalRiskHandler.cs:13`,
`GetDashboardHandler.cs:128`, `RequestExplanationHandler.cs:174`,
`DeterministicExplanationProvider.cs:60`, `Program.cs:20`).

### 5. D1, alta. El orden está al revés: el extractor es el único oráculo de los campos tipados de tres reglas, y el diseño lo borra antes de que ninguna evaluación guardada las haya disparado

Es la pregunta 1. D1 argumenta que `e3-v2` va primero porque «es un cambio de código verificable
contra un corpus conocido» (`E9-DISENO.md:65-69`). El corpus conocido dispara tres de las seis
reglas: sobre la base nueva de `E8B`, `amount_anomaly` aparece en 18 evaluaciones, `foreign_country`
en 34, `new_buyer_high_value` en 5, y `velocity`, `cross_border_velocity` y `unusual_hour` en
**cero**. Los campos tipados de esas tres reglas se verificarían, en el orden del diseño, solo con
tres frases escritas a mano en un test (`ExplanationFactsTests.cs:118-142`), y el extractor se
borraría en `E9A` sin haber leído jamás una señal real de ellas.

Al revés, la verificación es materialmente mejor. Si la fixture v2 va primero con el motor quieto en
`e3-v1`, la base nueva tiene evaluaciones reales de las seis reglas escritas en prosa, y el extractor
—que existe exactamente para leer esa prosa— sirve de **oráculo del cambio de motor**: para cada
pedido del corpus v2, `SignalFacts.Parse` sobre la señal `e3-v1` tiene que coincidir campo por campo
con lo que `e3-v2` emite para el mismo pedido, y el conjunto de hechos construido desde una y otra
tiene que ser el mismo (hallazgo 4.2). Son 300 evaluaciones, seis reglas y ninguna frase inventada.
Es la misma clase de falsación que este proyecto ya usó: los 328 fingerprints recalculados desde el
material original en `E6A`, o invertir las etiquetas en `E5A`. El extractor se borra al final de esa
tarea, después de certificar a su reemplazo.

Lo que el orden invertido **no** cuesta: los fingerprints se re-fijan dos veces en cualquiera de los
dos órdenes (la fixture cambia el manifiesto; la versión lo cambia otra vez), y las cifras del mock,
del dashboard, de las alertas y de las métricas se tocan una sola vez, en la tarea de la fixture,
igual que hoy. Lo que sí cuesta: durante una tarea, las señales nuevas se ven en inglés en la
consola. Las capturas se regeneran en `E9D` de todos modos.

Recomendación: `E9A` = fixture, motor quieto; `E9B` = `e3-v2` con el extractor como oráculo y su
borrado como último commit. Si el coordinador prefiere conservar el orden del diseño, entonces el
extractor **no se borra en `E9A`**: se borra en `E9B` después del diferencial, lo que equivale a
correr `e3-v2` sin haber podido verificar tres de sus seis reglas contra datos hasta la tarea
siguiente. No es una opción mejor.

### 6. D3, media. Los números objetivo son aritmética, no criterio; el propio corpus fija el techo de F1, y el barrido puede elegir un umbral distinto de 60

Es la pregunta 3. La división temporal toma los primeros dos tercios de los **instantes distintos**
como calibración y el resto como holdout (`RiskMetricsEvaluator.cs:70-99`, decisión 26). Con la
fixture actual son 200 y 100 pedidos, 12 y 6 fraudes. D3 pide «al menos tres falsos negativos y tres
falsos positivos en la cohorte de holdout» con precisión y recall «por encima de 0,5»
(`E9-DISENO.md:99-101`). Con FN = 3, recall > 0,5 exige TP ≥ 4, o sea **al menos siete fraudes
etiquetados en el último tercio por tiempo**; con seis, como hoy, las dos condiciones son
incompatibles. La fixture tiene que construirse mirando la división.

Y el techo se deduce: con FP = FN = 3, F1 = 2·TP / (2·TP + 6).

| TP en holdout | Fraudes en holdout | Precisión = recall | F1 |
| --- | --- | --- | --- |
| 4 | 7 | 0,57 | 0,57 |
| 6 | 9 | 0,67 | 0,67 |
| 8 | 11 | 0,73 | 0,73 |
| 12 | 15 | 0,80 | 0,80 |

Un F1 de 0,95 con esos mínimos necesita 57 verdaderos positivos en el holdout: sesenta fraudes en
cien pedidos. **No hay número sospechosamente bueno alcanzable**; la banda posible es 0,57–0,75. Lo
sospechoso no es el valor sino que sea **predecible**: con los errores puestos a mano, F1 es un
parámetro del diseño y no una medición, con cualquier cifra. Por eso el criterio del hallazgo 1 es
la celda de la matriz por arquetipo, y por eso el README debe publicar la matriz —conteos, con
`n`— y no una razón con dos decimales que sugiere una precisión que cien pedidos no tienen: cada
falso negativo mueve el recall doce puntos.

Hay una consecuencia más que el diseño no anticipa. `SelectBest` maximiza F1, después minimiza la
tasa de falsos positivos y después elige el umbral más alto (`RiskMetricsEvaluator.cs:54-68`), y la
API informa el umbral más alto de cada meseta (`GetEvaluationMetricsHandler.cs:82-97`). Si en la
cohorte de calibración todos los falsos positivos caen en 60 —FP1, FP3 y FP4 suman exactamente 60—
y ningún fraude cae en 60, el barrido elige **70**, la pantalla dice «Umbral 70, elegido sobre la
cohorte de calibración y aplicado acá sin retocar nada» (`quality-section.tsx:91`), y las alertas
siguen abriéndose a 60 por la decisión 23. No es un defecto: es la calibración diciendo algo. Pero
hay que decidirlo: o el corpus pone fraudes en 60 y legítimos por encima para que el barrido confirme
60, o se acepta la discrepancia y el README y el guion la explican como lo que es —«la calibración
sugiere 70; el producto mantiene 60 porque el umbral es una política y no un ajuste»—, que es la
lección más fuerte. Lo que no puede pasar es descubrirlo en la captura 4. `RiskEvaluationTests.cs:49-55`
fija hoy el umbral 60 y las dos matrices perfectas; `E9B` lo reescribe.

### 7. D6, media. El portugués es «un diccionario más» en la consola y no lo es en la explicación ni en la API, y la elección por configuración necesita un solo lugar

Es la pregunta 4.

- **La explicación la escribe el backend, en castellano, y se persiste.** La plantilla compone el
  párrafo (`DeterministicExplanationProvider.cs:84-165`) y la fila se identifica por (evaluación,
  proveedor, `templateVersion`, política) (`AlertExplanationConfiguration.cs:146-153`). Una consola
  en portugués mostraría un párrafo en castellano, salvo que el idioma llegue al backend **y a la
  identidad de la fila**; si no, vuelve el problema de `E7D`: un despliegue que cambia de idioma
  encuentra la fila en castellano y nunca escribe la portuguesa. Dos formas: codificar el idioma en
  `templateVersion` (`e7-v3-pt`) o una columna `language` en el índice único, que es una migración.
  La segunda es la limpia, y en esta etapa una migración no está prohibida como lo estaba en la 8.
  Lo que sí se conserva sin tocar: el tokenizador y `SpanishNumberFormat`, porque el portugués de
  Brasil escribe los números con los mismos separadores; los nombres de mes y `SeverityWord` no.
- **La API habla inglés a propósito** y la consola traduce por código (`messages.ts:7-12`).
  `describeRecordError` rotula el código y después **pega `error.message` en inglés**
  (`actions.ts:269-277`): ese es el ítem «códigos que caen al inglés». Y dos códigos de fila que el
  backend emite no tienen rótulo: `DUPLICATE_HEADER` y `UNKNOWN_FIELD` no están en
  `IMPORT_ERROR_LABELS` (`format.ts:140-146`).
- **La superficie es mucho más grande que un diccionario.** Hay literales en castellano en 34
  archivos fuente del frontend, 16 archivos de test que los afirman, 43 comprobaciones de texto en
  `smoke-ui.sh`, `<html lang="es">` fijo en `layout.tsx:13`, seis capturas y las anclas de texto del
  script que las saca (`capturar.mjs:95-147`). «La consola entera» (`E9-DISENO.md:132`) es la tarea
  de frontend con más archivos tocados de todo el proyecto, y el diseño la describe en una frase.
  Un `t()` con dos diccionarios y un test que afirme que los dos tienen las mismas claves; el smoke
  corre en un idioma y lo declara; y `lang` sale de la configuración —VoiceOver lee portugués con
  fonética castellana si no—, que es donde D6 y D7 se tocan.
- **Dónde vive la configuración.** Los dos procesos la necesitan: el backend para la explicación, la
  consola para todo lo demás. Un solo origen: la API la declara en `/api/system/capabilities`
  (`SystemEndpoints.cs:19-26, 41`), que la consola ya lee del lado del servidor para
  `demoDataEnabled`, y la variable —`SALVO_LANGUAGE=es|pt`— la lee solo la API, fallando al arrancar
  ante un valor desconocido, como `AI_PROVIDER` (`DependencyInjection.cs:95-105`). Nada de
  `NEXT_PUBLIC_`. `Accept-Language` haría la identidad de la explicación por petición y duplicaría
  filas por idioma: no.
- **Dos personas del mismo comercio con idiomas distintos** no se pueden atender sin identidad. Es
  el límite correcto para una instancia por comercio y hay que escribirlo en el Blueprint: idioma
  por despliegue; por persona, después de la autenticación, post-MVP.

### 8. D5, media. `e7-v3` no está justificado tal como está escrito, y la decisión 58 lo prohíbe si el texto no cambia

D5 dice que la plantilla «sube a `e7-v3`, por la decisión 58: cambia el texto que produce»
(`E9-DISENO.md:117`). No cambia. La plantilla ya escribe desde `SignalFacts` —ratio, alcance,
historia, ventana, países, franja— (`DeterministicExplanationProvider.cs:129-168`) y alimentarla con
los mismos campos desde el motor produce los mismos bytes. La decisión 58 dice «cambiar el texto sube
la versión»; su contrapositiva también vale: **sin cambio de texto no hay versión nueva**, porque dos
versiones con texto idéntico hacen que `E7D` ofrezca «redactar con la plantilla vigente» para
producir el mismo párrafo. Hay una razón legítima para subirla, y es distinta: **usar la mediana**,
que pasa a ser un campo tipado y que hoy la plantilla no puede escribir. «Es 23,2 veces la mediana
del comercio, que fue 86,85 BRL» es mejor texto y una cifra más que el grounding verifica. Si `E9A`
quiere eso, `e7-v3` está justificado y los dos dorados cambian; si no, la versión se queda.

### 9. D9 y D10, media. Cifras y tests que la etapa rompe y el diseño no lista, y una cifra del propio diseño que no está anclada

- **El 13,8 % no existe.** El diseño (`E9-DISENO.md:96`), el Workboard, `docs/muestras/README.md` y
  el README («no baja de ahí», `README.md:374-378`) dicen que la franja más rara de los tres
  comercios está en 13,8 %. Con la aritmética exacta del motor —ventana de 30 días, mínimo de 20,
  franja del pedido en hora de Montevideo— el mínimo sobre la fixture es **16,7 %** (4 de 24,
  `ORD_000073` y `ORD_000074`) para los comercios de Uruguay y Brasil, y **18,2 %** (4 de 22,
  `ORD_000069`) para el de Estados Unidos; sobre `salvo.db` da lo mismo. Sobre el archivo entero,
  cada franja está en 20 % o 40 %. La conclusión —inalcanzable— se sostiene; la cifra no, y es la
  clase de dato que el Workboard dice que se saca del archivo en el momento. Y hay un segundo
  motivo de inalcanzabilidad que el diseño no nombra: **20 de los 100 pedidos de cada comercio no
  llegan al mínimo de 20 en 30 días** y la regla ni evalúa. Un comercio «de horario comercial» en el
  corpus v2 necesita 21 pedidos en los 30 días previos al pedido raro, con no más de 2 en su franja.
- **Tests fijados al corpus que `E9B` reescribe**, además de los del inventario de `E8A`:
  `RiskEvaluationTests.cs:37-55` (distribución de scores, umbral 60, matrices perfectas),
  `ScoringRunPersistenceTests.cs:85-109` (histograma 13/5, 18 denegadas, fingerprint dorado de
  `ORD_000001` y digest del manifiesto), `AlertCreationTests.cs:28-45`,
  `DashboardEndpointTests.cs:39, 87, 211-214`, `EvaluationMetricsEndpointTests.cs:28` (304/300/4),
  `DemoSeedTests.cs:54-68`, `AlertFeedOrderTests`, `ExplanationFactsTests.cs:18-33`, y en el frontend
  `dashboard/page.test.tsx:65, 98-101`. El inventario de `E8A` decía que el primer test en caer sería
  el del mock; con la forma del seed (hallazgo 3), los primeros son `DemoSeedTests` y todo lo que
  siembra.
- **`ORD_000011` está en todos lados** y hay que reelegirlo con criterio: `capturas.sh:200-256` exige
  que identifique exactamente una alerta y que el mock la **apruebe** (resto < 75) para fotografiar
  la divergencia de criterio; `capturar.mjs:95-102`; `docs/guion-demo.md:9, 55, 68, 73, 86, 120,
  129, 138`; `docs/capturas/README.md`; `docs/muestras/import-con-errores.csv` fila 6 y su README;
  los dos dorados (`ExplanationGoldenTests.cs:33, 52`) que necesitan uno con decimal y otro sin él.
  El pedido de la demo tiene que ser crítico, aprobado por el mock, y con una explicación cuyo ratio
  tenga decimal. Son tres condiciones sobre la referencia y el monto, y se eligen al construir la
  fixture, no después.
- **El reparto del mock** depende de los dígitos de las referencias: con `ORD_000001`–`ORD_000300`
  cada resto aparece tres veces y da 225/45/21/9 (`MockAntifraudProvider.cs:20-23`,
  `ExternalEvaluationIsolationTests.cs:166-171`). Si el corpus conserva las 300 referencias, los
  cuatro números se conservan; lo que cambia es **qué pedidos** divergen. El constructor elige las
  referencias de los fraudes para que haya al menos una alerta crítica aprobada por el mock, una en
  la banda pendiente (90–96) con resto impar para que el callback la cierre en denegado, y los tres
  falsos negativos en 75–89 para que el proveedor los deniegue (hallazgo 2).
- **La revisión de E8 le dejó dos hallazgos a esta etapa y D8/D10 no los mencionan**: el desempate
  de la cola por `alert.Id` aleatorio (`EfAlertStore.cs:86-91`; `E8-revision-adversarial.md:50-73`)
  y que `explanationId` no se pinta en el panel de revisión, solo viaja como campo oculto
  (`review-panel.tsx:44`; `E8-revision-adversarial.md:118-149`). Adentro o afuera, pero dicho.
- **Las bases de ensayo** que D9 lista para el repaso (`E9-DISENO.md:166`) son cuatro archivos
  `.db` en `backend/src/Salvo.Api/` (`salvo.db`, `salvo.design.db` y dos `salvo-demo-*`), y
  `demo.sh` crea una más por ensayo a propósito (`demo.sh:56`). Borrarlas no es del agente: `rm` está
  denegado y es regla del usuario. El brief lo dice y el agente lista.
- `«Se importaron 1 pedidos»` es `actions.ts:113`. Trivial, y va.

### 10. D2, baja. Un cambio de versión del motor hace que toda alerta existente muestre un aviso falso

Sobre una base con alertas, una corrida `e3-v2` no reutiliza ninguna evaluación —la versión está en
el fingerprint (`RiskEvaluationFingerprint.cs:11, 32`)—, no abre alertas nuevas —mismos scores,
`RunScoringHandler.cs:144-155`—, y deja a cada alerta con `CurrentRiskEvaluationId` distinto del
snapshot. Consecuencias: toda explicación pasa a «desactualizada» (`RequestExplanationHandler.cs:54-55`),
que es correcto, y toda alerta muestra el aviso «La evaluación del pedido cambió (60 → 60) sin
cambiar de banda» (`divergence.ts:26-36`), que compara **identificadores** y afirma un cambio que no
hubo. Con D2 y base nueva no se ve; sobre la base del usuario se ve 21 veces. El aviso debería
comparar score y señales, o decir «la evaluación vigente es otra fila con el mismo resultado». Es
código de producto chico y cabe en `E9B`.

### 11. D7, baja. Bien planteado; falta decir que lo que salga es código y quién lo aprueba

Un recorrido con VoiceOver produce correcciones en componentes: rótulos, `aria-live` para los avisos
que aparecen (divergencia, explicación desactualizada, errores de importación), el nombre del
formulario de veredicto. Todo eso es código de producto de `E9C`, y `boundary.test.ts` va a vigilar
que ninguna corrección meta un componente cliente con objetos. El brief debe autorizar
`frontend/src/**` y decir que las correcciones se listan antes de hacerse, con la severidad del
hallazgo, para que el coordinador decida cuáles entran.

---

## Tercera parte — Las tres verificaciones estructurales, en directo

**¿Sembrar una fixture nueva sobre una base existente falla, y con qué mensaje?** Depende de qué
cambie la fixture, y en el caso que la etapa necesita no falla como el diseño dice:

- Si el v2 cambia `version`, la cantidad de pedidos o la cantidad de fraudes: `500`, sin cuerpo útil,
  por `InvalidOperationException` en `ValidateDocumentShape` (`SeedDemoOrdersHandler.cs:87-103`),
  antes de tocar la base y también sobre una base vacía. Mensajes: «The demo fixture version is not
  supported.» / «…must contain exactly 300 orders.» / «…must contain exactly 18 fraud labels.»
- Si el v2 conserva `"1"`, 300 y 18 y cambia datos con las mismas referencias: `409
  DEMO_DATA_CONFLICT`, detalle «The demo dataset conflicts with existing immutable order data.»
  (`OrderEndpoints.cs:153-156`), que la consola traduce a «El corpus de demostración choca con
  pedidos que ya existen … Usá una base vacía para cargar la demo» (`messages.ts:84-89`). El handler
  sabía si fue la referencia o la etiqueta (`:45-46, 67-68`); el endpoint lo olvida. Nada se escribe:
  el `throw` precede al `AddSeedDataAsync` (`:74`).
- Si el v2 renombra un comercio: `200`, 300 pedidos nuevos junto a los 300 viejos, sin conflicto
  (`:36-40`, referencia compuesta en `:116-119`).

**¿Qué se rompe exactamente al borrar el extractor?** En compilación: `DeterministicExplanationProvider.cs:61`,
`RequestExplanationHandler.cs:180-191`, `ExplanationTestCorpus.cs:109`,
`ExplanationFactsTests.cs:95-97, 120-128, 154-155, 289`. En comportamiento, lo que no compila no
avisa: el conjunto de hechos pierde toda cifra que hoy entra solo por la prosa (mediana, monto dentro
de la señal, constantes, horas de franja) salvo que se enumere a mano (`ExplanationFacts.cs:70-77`);
y toda fila `e3-v1` —snapshots de alertas incluidos— deserializa sin campos
(`RiskSignalSerializer.cs:52-60`). En contrato: `AlertSignalView.Detail` y todo lo que lo consume
(hallazgo 4.3). «El resto queda intacto» es cierto de `ExplanationGrounding` y del cuerpo de la
plantilla, y falso de sus llamadores, de su contrato y de sus datos.

**¿El orden `e3-v2` → fixture es el correcto?** No. Al revés hay un oráculo —el extractor sobre
señales reales de las seis reglas— y en el orden del diseño no lo hay para tres de ellas, porque el
corpus actual no las dispara ni una vez (hallazgo 5). El costo de invertir es cero en artefactos
tocados dos veces y una tarea con señales en inglés en pantalla.

---

## Respuestas a las seis preguntas abiertas

1. **Orden de D1.** Al revés: fixture con motor quieto, después `e3-v2` con el extractor como oráculo
   sobre las seis reglas, borrado al final. Lo que se verifica mejor después de tener el corpus con
   desacuerdo es justamente `e3-v2`: los campos de `velocity`, `cross_border_velocity` y
   `unusual_hour`, que hoy no existen en ninguna fila (hallazgo 5).
2. **Arquetipos de D3.** La forma del falso negativo es correcta y la historia es la de un
   ingeniero. Una cuenta tomada no se ve así: se ve en el dispositivo, el envío y el monto. La forma
   del diseño es el fraude amigo, que se etiqueta por el contracargo. Los tres falsos negativos y los
   cuatro falsos positivos están en la primera parte, con la regla que los ve o no y por qué. «El
   cliente que viaja» no es un falso positivo de este motor (hallazgo 1).
3. **F1 sospechoso.** Con los mínimos de D3 y cien pedidos de holdout, F1 no puede pasar de ~0,75;
   0,95 exigiría 57 verdaderos positivos. No hay valor sospechoso: hay predictibilidad. El criterio
   es la celda por arquetipo, el README publica conteos, y hay que decidir si el barrido debe
   confirmar 60 o no (hallazgo 6).
4. **Idioma por configuración.** Alcanza, y es lo correcto sin identidad. Pero no es solo la
   consola: la explicación se escribe en el backend y su identidad tiene que llevar el idioma; la
   configuración vive en la API y la consola la lee de `capabilities`; el límite «por persona,
   post-autenticación» se escribe en el Blueprint (hallazgo 7).
5. **Borrar el extractor.** Rompe el proveedor, el handler, el conjunto de hechos, el contrato y la
   lectura de toda fila vieja. «Intacto» es cierto de dos tipos y falso de sus bordes (hallazgo 4 y
   tercera parte).
6. **Decirlo antes de fallar.** La detección ya es previa a toda escritura. Lo que falta es el
   ensayo —el mismo handler sin escribir— consultado al cargar `/import`, y un código nuevo que
   distinga «versión anterior del corpus» de «pedidos importados» (hallazgo 3).

## Decisiones que resistieron

- **D2, base nueva.** Correcta, y más necesaria de lo que el diseño cree: la exige la fixture y, si
  no se conserva `detail` heredado, también el motor. `demo.sh` ya la crea.
- **D4, arquetipos por lo que enseñan.** Es el criterio correcto; la primera parte solo lo aplica con
  más rigor: cada falso negativo enseña una razón distinta de invisibilidad.
- **D7, lector de pantalla real.** Correcto, con la salvedad de alcance del hallazgo 11.
- **D8, Anthropic y el proveedor que se porta mal afuera.** Correcto los dos. `/orders` no, por el
  hallazgo 2, salvo que el panel del dashboard lo reemplace.
- **D9, el inventario de `E8A` como lista de trabajo.** Correcto; el hallazgo 9 lo completa.
- **D10, cuatro tareas.** Correcto; el orden de las dos primeras se invierte.
- **La restricción de inmutabilidad como eje.** Es la lectura correcta del sistema; solo que el
  primer muro no es la inmutabilidad sino el validador de forma.

## Contradicciones con el Blueprint, la bitácora y `AGENTS.md`

| Dónde | Dice | La etapa lo cambia | Quién lo corrige |
| --- | --- | --- | --- |
| `Salvo-Blueprint.md:194` (§4.2) | «Cada señal tiene `{ rule, weight, detail }` y `detail` es legible para una persona» | `e3-v2` emite campos; si se conserva `detail` heredado, es opcional | Coordinador, diseño v2 |
| `Salvo-Blueprint.md:294-296` (§4.7) | El conjunto de hechos incluye «todos los números de cada `detail`» | Pasan a ser los valores de los campos, enumerados | Coordinador, diseño v2 |
| `Salvo-Blueprint.md:129-130` (§4.1) y decisión 19 (`:740`) | 300 pedidos, 18 fraudes, 282 legítimos; «fixture fija de 300 pedidos» | El v2 cambia al menos las etiquetas; la decisión no se reescribe, se agrega otra | Coordinador |
| `Salvo-Blueprint.md:137, 418` | `countryCode` sin semántica | Los arquetipos exigen «país de la sesión» | Coordinador, §4.1 |
| `Salvo-Blueprint.md:424-432` (§7) | `isFraudLabel` sin definición | «terminó en contracargo por fraude o confirmado» | Coordinador, §7 |
| Decisión 58 (`:779`) | Cambiar el texto sube la versión | D5 sube `e7-v3` sin cambiar el texto | Diseño v2 (hallazgo 8) |
| Decisión 57 (`:778`) | Los códigos de problema van a `messages.ts` con test de exactitud | El código nuevo del seed entra ahí | Brief de la fixture |
| `.env.example:4` y `Salvo-Blueprint.md:196` | `BUSINESS_TIMEZONE` configurable / «en `America/Montevideo`» | Nada lee la variable; el huso es constante | `E9D` quita la variable o el diseño la conecta |
| `AGENTS.md`, «Ningún componente cliente recibe objetos» | — | Vale para el `t()` y para las correcciones de accesibilidad | Brief de `E9C` |

## Resumen para los briefs

En el orden recomendado:

**`E9A-FIXTURE`** (antes `E9B`), motor quieto en `e3-v1`.

- `demo-orders.v2.json` con los mismos tres `merchantId`, las referencias `ORD_000001`–`ORD_000300`,
  países en el corredor UTC−3, y los siete arquetipos de la primera parte con su celda de la matriz
  escrita en el brief antes de correr el motor. Al menos siete fraudes en el último tercio por
  tiempo; un comercio con 21+ pedidos en 30 días y ≤2 en la franja rara; un pedido de demo crítico,
  aprobado por el mock y con ratio decimal; una alerta en la banda 90–96 impar; los falsos negativos
  con resto 75–89.
- Definir `isFraudLabel` y `countryCode`; declarar la tasa base como de construcción.
- Decidir si el barrido debe confirmar 60.
- Código: `ValidateDocumentShape` versionado, nombre del recurso, `DatasetVersion`, ensayo
  `dryRun` con el código `DEMO_DATA_PREVIOUS_CORPUS` en `messages.ts`, el aviso de `divergence.ts`.
- La superficie del hallazgo 2, o su exclusión dicha en D8.
- Todos los tests del hallazgo 9 y del inventario de `E8A`; los dos dorados reelegidos.

**`E9B-SENALES-TIPADAS`** (antes `E9A`), corpus quieto.

- `RuleConfig` `e3-v2`, campos por regla, precisión canónica por campo escrita en el diseño,
  `detail` heredado opcional, forma plana y nullable en el cable, recaptura de OpenAPI, `guards.ts`,
  `fixtures.ts`, `boundary.test.ts`, la consola compone.
- Diferencial con el extractor como oráculo sobre las 300 evaluaciones del corpus v2, más igualdad
  del conjunto de hechos; el borrado del extractor es el último commit.
- `ExplanationFacts` enumera los hechos desde los campos, con un test por campo.
- `e7-v3` solo si la plantilla escribe la mediana; si no, la versión no se toca.
- `RuleConfig` elegido por la versión de la fila donde se explica.

**`E9C-PORTUGUES-ACCESIBILIDAD`.**

- `SALVO_LANGUAGE` leído por la API, declarado en `capabilities`, fallo al arrancar ante valor
  desconocido; `lang` desde ahí; `t()` con dos diccionarios y test de claves; idioma en la identidad
  de la explicación (columna, con migración); plantilla en portugués; smoke en un idioma declarado;
  rótulos de `DUPLICATE_HEADER` y `UNKNOWN_FIELD`; `describeRecordError` sin el mensaje inglés.
- Recorrido con VoiceOver: lista de hallazgos con severidad antes de corregir; `frontend/src/**`
  autorizado; el límite «idioma por persona es post-MVP» al Blueprint.

**`E9D-CIERRE`.**

- El inventario de `E8A` más el hallazgo 9; las capturas y el guion con el pedido nuevo; §4.1, §4.2,
  §4.7, §7 y decisiones nuevas en la bitácora; `.env.example` sin `BUSINESS_TIMEZONE`; el bloque
  marcado con matriz, `n` y la nota de tasa base; la lista de `.db` para que el usuario borre.

## Cómo se verificó

- Fixture: `python3` sobre `demo-orders.v1.json` —separación entre instantes, franjas de 6 h en
  UTC−3 sobre el archivo entero y con la ventana de 30 días y el mínimo de 20 del motor, pedidos por
  par comercio–comprador, países y monedas por comercio, medianas y máximos de montos legítimos y de
  fraude, elegibilidad de `velocity` y `cross_border` (cero pedidos en ambos casos), y la división
  2/3–1/3 por instantes distintos (200/100, 12/6).
- Base nueva de `E8B`: `sqlite3 "file:…salvo-demo-20260906-194152.db?immutable=1"` —conteos,
  distribución de scores (266/16/13/5) y apariciones de cada regla en `signals_json` (18/0/0/0/5/34).
- `salvo.db` con el mismo modo, solo para la franja horaria por comercio.
- Código: lectura completa de los archivos citados; `grep` de `DeviceSessionId` en el motor (sin
  resultados), de `BUSINESS_TIMEZONE` en `backend/src` (sin resultados), de `E3V1` (seis llamadas),
  de `External` en el dashboard (sin resultados), de usos de `.Detail` y `SignalFacts` en `src` y
  `tests`.
- Ningún test ni compuerta ejecutados; ninguna escritura fuera de este archivo; ningún push.
