# Salvo — Diseño de la Etapa 9: corpus, idiomas y cierre

> Estado: propuesta **v2**, corregida tras la revisión adversarial
> Fecha: 2026-09-07
> Base: `main` tras el cierre de la Etapa 8
> Revisión adversarial: `Coordination/Tasks/E9-revision-adversarial.md` (11 hallazgos, 5 altos, más
> una primera parte entera sobre criterio de dominio que corrigió el arquetipo central)

## Qué cambió respecto de v1

| Hallazgo | Qué estaba mal | Qué dice v2 |
| --- | --- | --- |
| Dominio, alta | El falso negativo era una cuenta tomada que **no monetiza nada**: compra lo de siempre y se envía a la víctima. Un revisor preguntaría por qué está etiquetado como fraude | **Fraude amigo**, cuya etiqueta la explica el contracargo (D3) |
| 2, alta | El argumento central no se puede ver: un pedido sin alerta no tiene pantalla, y D8 excluía la superficie que lo mostraría | Panel en el dashboard, decidido con el usuario (D8) |
| 3, alta | El seed falla **por forma antes de mirar la base**, con un `500`; y renombrar un comercio anula el guardián | Validador versionado, mismos `merchantId`, y un ensayo `dryRun` (D2) |
| 4, alta | «El extractor se borra y todo lo demás queda intacto» es falso en cuatro lugares; `detail` es contrato, no prosa | `detail` heredado, precisión canónica declarada, recaptura del contrato (D5) |
| 5, alta | **El orden estaba al revés**: el corpus actual dispara tres de las seis reglas, así que `e3-v2` se habría verificado con frases escritas a mano | Fixture primero; el extractor es el **oráculo** del cambio de motor (D1) |
| 6, media | Los números objetivo eran aritmética; el barrido puede elegir 70 | Matriz por arquetipo; discrepancia aceptada y explicada (D3) |
| 7, media | El portugués no es «un diccionario más» en la explicación ni en la API | `SALVO_LANGUAGE`, idioma en la identidad de la fila (D6) |
| 8, media | `e7-v3` no estaba justificado: la plantilla produce los mismos bytes | Sube **solo** si escribe la mediana (D5) |
| 9, media | El `13,8 %` que tres documentos repiten **no existe**: el mínimo real es `16,7 %` | Corregido, y con el segundo motivo de inalcanzabilidad (D3) |
| 10 y 11, bajas | El aviso de divergencia miente tras un cambio de versión; la accesibilidad no decía quién aprueba | Los dos, adentro (D2, D7) |

---

## Lo que la revisión enseñó sobre el dominio, y que ordena la etapa

Mi arquetipo de falso negativo era el que un ingeniero imagina. **Una cuenta tomada existe para
monetizar**: mercadería a una dirección que el atacante controla, bienes digitales, saldo. Un pedido
de monto normal enviado a la dirección de siempre lo recibe la víctima y no monetiza nada. La forma
que buscaba —un pedido que ninguna de las seis reglas puede tocar— era correcta; la historia, no.

La historia correcta es el **fraude amigo**: el comprador compra, recibe, y desconoce el cargo ante
su banco. En el momento de la compra es indistinguible de una compra legítima **porque lo es**, y la
etiqueta llega meses después con el contracargo — que es exactamente lo que la decisión 37 dice que
un comercio sí conoce. Es la categoría de la que más habla el sector en Brasil. Y el argumento del
efecto de red se sostiene mejor que con la cuenta tomada: lo que un proveedor ve de ese comprador es
su historial de disputas **en otros comercios**.

La cuenta tomada entra igual, pero como lo que es: un dispositivo nuevo para una cuenta con
historia. Y ahí aparece lo que más enseña de todo el corpus: **el motor no lee `deviceSessionId`,
que está en el archivo desde la Etapa 2**. «Falta una regla» deja de ser una opinión y pasa a ser
una fila que se puede señalar.

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | **La fixture primero; `e3-v2` después, con el extractor como oráculo** | Hallazgo 5 |
| D2 | El corpus v2 exige base nueva, el sistema lo dice **antes del clic**, y el guardián no se esquiva | Hallazgo 3 |
| D3 | Siete arquetipos, cada uno invisible o marcado **por una razón distinta** | Dominio, hallazgos 1 y 6 |
| D4 | `isFraudLabel` y `countryCode` se definen; la tasa base se declara de construcción | Dominio |
| D5 | Campos tipados con `detail` heredado, precisión canónica declarada y contrato recapturado | Hallazgo 4 |
| D6 | El idioma es del despliegue, entra en la identidad de la explicación, y la consola entera se traduce | Hallazgo 7 |
| D7 | La accesibilidad produce una lista con severidad **antes** de corregir | Hallazgo 11 |
| D8 | Un panel en el dashboard muestra lo que el motor no ve y el proveedor sí | Hallazgo 2 |
| D9 | El corpus vive en el corredor UTC−3, y `BusinessTimeZone` es del despliegue | Dominio |
| D10 | El repaso final usa el inventario de `E8A` más el del hallazgo 9 | Etapa 8 |
| D11 | Partición en cuatro, en orden obligatorio | — |

---

## D1 — La fixture primero, y el extractor certifica a su reemplazo

Mi v1 argumentaba que `e3-v2` iba primero porque se verifica contra un corpus conocido. El corpus
conocido **dispara tres de las seis reglas**: sobre la base nueva de `E8B`, `amount_anomaly` aparece
en 18 evaluaciones, `foreign_country` en 34, `new_buyer_high_value` en 5, y `velocity`,
`cross_border_velocity` y `unusual_hour` en **cero**. Los campos tipados de esas tres se habrían
verificado contra tres frases escritas a mano en un test, y el extractor se habría borrado sin haber
leído nunca una señal real de ellas.

Invertido, la verificación es materialmente mejor. Con la fixture v2 sembrada y el motor quieto en
`e3-v1`, la base tiene evaluaciones reales de las seis reglas escritas en prosa, y **`SignalFacts`
—que existe exactamente para leer esa prosa— es el oráculo del cambio de motor**: para cada pedido
del corpus, lo que el extractor lee de la señal `e3-v1` tiene que coincidir campo por campo con lo
que `e3-v2` emite, y el conjunto de hechos construido desde una y otra tiene que ser el mismo.
Trescientas evaluaciones, seis reglas, ninguna frase inventada.

Es la misma clase de falsación que el proyecto ya usó: los 328 fingerprints recalculados desde el
material original en `E6A`, o invertir todas las etiquetas en `E5A`. **El extractor se borra en el
último commit de esa tarea, después de certificar a su reemplazo.**

Lo que el orden invertido cuesta: durante una tarea, las señales se ven en inglés en la consola. Las
capturas se regeneran en `E9D` de todos modos.

## D2 — La base nueva, dicha antes del clic

Sembrar el corpus v2 sobre una base con el v1 falla, pero **no por donde v1 decía**. `ValidateDocumentShape`
exige `version == "1"`, exactamente 300 pedidos y exactamente 18 etiquetas de fraude, y corre
**antes** de leer la base; el endpoint solo atrapa `DemoSeedConflictException`, así que la respuesta
es un `500` incluso sobre una base vacía. Y el mensaje que v1 citaba no existe en ninguna capa.

Peor: **el guardián se esquiva.** La referencia es `merchantId:merchantReferenceId`, así que si la
«geografía plausible» renombra un comercio, el v2 no colisiona con nada y la base queda con 600
pedidos donde la mitad contradice a la otra — exactamente lo que v1 decía querer evitar. Por eso
**el corpus v2 conserva los mismos tres `merchantId` y las mismas 300 referencias**, y eso es parte
de la decisión, no un detalle de implementación.

La etapa entrega:

- El validador de forma **versionado**, que acepte el v2 con su cantidad de fraudes.
- Un **ensayo**: `POST /api/demo-data/seed?dryRun=true` corre las mismas comparaciones sin escribir
  y devuelve qué insertaría, qué duplicaría y si hay conflicto, distinguiendo **la versión anterior
  del corpus** de una importación manual. La consola lo consulta al abrir `/import` y rotula el
  botón en consecuencia, en vez de dejar que la analista descubra el conflicto pulsándolo.
- El código `DEMO_DATA_PREVIOUS_CORPUS`, que por la decisión 57 va a `messages.ts` y a su test de
  exactitud.
- **El aviso de divergencia corregido.** Hoy compara identificadores de evaluación, así que un
  cambio de versión del motor hace que toda alerta existente muestre «la evaluación cambió (60 → 60)
  sin cambiar de banda», que es falso. Tiene que comparar score y señales.

La base local del usuario no se borra ni se migra: deja de ser la base de demostración, y eso se
dice con esas palabras.

## D3 y D4 — Los siete arquetipos, y qué significa la etiqueta

**Tres falsos negativos, cada uno invisible por una razón distinta.** Esa es la propiedad que los
vuelve didácticos: en el primero no hay señal transaccional; en el segundo la señal está en los
datos y ninguna regla la lee; en el tercero la regla existe y está atada a la entidad equivocada.

| # | Arquetipo | Por qué las seis reglas no lo ven | Qué lo vería |
| --- | --- | --- | --- |
| FN1 | **Fraude amigo.** Comprador con historia compra lo de siempre y desconoce el cargo | No hay nada que ver en la transacción: es una compra legítima hasta el contracargo | El historial de disputas de ese comprador en la red del proveedor |
| FN2 | **Cuenta tomada, vista en el dispositivo.** Comprador con historia, `deviceSessionId` nuevo, 2× su mediana, tres pedidos a 20 minutos | Las reglas por comprador piden 3× y cuatro en diez minutos; país y hora miran al comercio; **el dispositivo no es una señal del motor** | Un dispositivo desconocido para la cuenta, y el mismo dispositivo en otros comercios |
| FN3 | **Prueba de tarjetas.** Cinco compradores nuevos, el mismo `deviceSessionId`, montos mínimos, minutos entre uno y otro | `velocity` cuenta por comercio y comprador: cada comprador tiene cero previos. `new_buyer_high_value` pide monto alto | Velocidad por dispositivo, por tarjeta o por IP |

**Cuatro falsos positivos**, y dos de ellos existen para que `velocity`, `unusual_hour` y
`amount_anomaly` en clave comprador **decidan algo por primera vez**:

| # | Arquetipo | Qué dispara | Score | Por qué una analista lo descarta |
| --- | --- | --- | --- | --- |
| FP1 | **El salto de país en dos horas** | `cross_border_velocity` + `foreign_country` | 60 | Roaming, VPN o proxy corporativo: dos pedidos, dos redes. **No es «un cliente que viaja»** |
| FP2 | **La primera compra grande** | `new_buyer_high_value` + `amount_anomaly` | 70 | Un cliente nuevo comprando un electrodoméstico: el costo declarado de toda regla «nuevo y caro» |
| FP3 | **El revendedor de madrugada** | `velocity` + `foreign_country` + `unusual_hour` | 60 | Un mayorista que arma su pedido en varias compras |
| FP4 | **El regalo desde afuera** | `amount_anomaly` en clave **comprador** + `foreign_country` | 60 | Un cliente habitual de viaje compra un regalo caro |

Una corrección de v1 que conviene escribir con todas las letras: **«un cliente legítimo que viaja»
no es un falso positivo de este motor.** `foreign_country` vale 20 y sola no llega al umbral; el
corpus actual ya tiene 16 pedidos legítimos desde el exterior y los 16 quedan en 20, sin alerta.
`cross_border_velocity` exige un pedido previo desde otro país en **dos horas**, que no es viajar
sino cambiar de red entre dos compras.

**Qué significa la etiqueta.** Hoy el §7 dice solo «ground truth exclusivo de demo». Para que FN1
sea fraude y FP2 sea legítimo, la definición tiene que ser **«el pedido terminó en contracargo por
fraude, o fue confirmado como fraude»**, que además es lo que un comercio real sabe. Sin esa
definición, «fraude amigo» y «compra disputada por error» son la misma fila con dos etiquetas
posibles.

**Qué país es `countryCode`.** El Blueprint dice solo «ISO 3166-1 alpha-2». Los siete arquetipos
asumen que es **el país de la sesión**, no el de facturación: si fuera el de facturación, un cambio
de país entre dos pedidos en dos horas sería imposible y FP1 no existiría. Se escribe en §4.1.

**Los números.** No se fijan objetivos de F1: se fija la **celda de la matriz de cada arquetipo**,
en el brief, antes de correr el motor. El techo lo pone el propio corpus —con tres falsos negativos
y tres falsos positivos en el holdout, F1 no puede pasar de ~0,75— así que no hay cifra
sospechosamente buena alcanzable. Lo sospechoso sería que fuera **predecible**, y lo es: con los
errores puestos a mano, F1 es un parámetro del diseño. Por eso el README publica **la matriz con sus
conteos y su `n`**, no una razón con dos decimales: con cien pedidos de holdout, cada falso negativo
mueve el recall doce puntos.

La división temporal manda: son los primeros dos tercios de los instantes distintos. Con tres falsos
negativos en el holdout, hacen falta **al menos siete fraudes en el último tercio por tiempo** para
que el recall pase de 0,5. La fixture se construye mirando esa división.

**El umbral.** Decidido con el usuario: **se acepta la discrepancia y se explica.** El barrido
maximiza F1 y puede elegir 70 mientras el producto sigue alertando en 60. No es un defecto: es la
calibración diciendo algo. El README y el guion lo dicen así — *la calibración sugiere 70; el
producto mantiene 60 porque el umbral es una política de negocio y no un ajuste estadístico*. Es la
lección más fuerte que el proyecto puede dar sobre métricas, y lo que no puede pasar es descubrirlo
en la captura del dashboard.

## D5 — Campos tipados, sin romper lo que ya está escrito

El motor deja de escribir una frase inglesa por señal y pasa a escribir sus campos. Sube a `e3-v2` e
invalida los fingerprints a propósito. Cuatro cosas que v1 daba por gratis y no lo son:

**`detail` es contrato, no prosa.** Viaja en `RiskSignal`, en la serialización canónica, en
`AlertSignalView`, lo exige la guarda del frontend, lo pinta la consola y lo afirman seis tests de
backend y las fixtures del frontend. `e3-v2` es **un cambio de contrato con recaptura de OpenAPI**,
`schema.d.ts`, `guards.ts`, `fixtures.ts` y `boundary.test.ts`: la lección de `E7A`, otra vez. La
forma en el cable es **plana y nullable por regla**, como `SignalFacts` hoy, no un polimorfismo.

**`detail` se conserva como campo heredado y opcional.** Las evaluaciones `e3-v1` que ya existen son
el snapshot de toda alerta abierta, y ese snapshot **no se reescribe nunca** por la decisión 33. Con
un lector de una sola forma, las 21 alertas de la base del usuario quedarían con su bloque de
señales vacío. La consola compone desde los campos cuando están y muestra `detail` cuando no, y un
test lee una fila `e3-v1` real por el camino nuevo.

**La precisión canónica de cada campo se declara en el diseño de la tarea.** Hoy la prosa fija el
redondeo con `{ratio:0.0}` y compañía. En `e3-v2` los valores viajan como números y un `decimal`
arrastra su escala: `201111 / 8685` no es `23.2` hasta que alguien lo redondea. El fingerprint
hashea la cadena serializada tal cual, así que **esa precisión es parte de la identidad de toda
evaluación para siempre**. Se escribe antes, no se descubre en el test dorado.

**El conjunto de hechos se enumera a mano, y hay un test que lo vigila.** Hoy `ExplanationFacts`
mete todo número que el motor escribió, y la prosa aporta cifras que `SignalFacts` no tiene como
campo: la mediana, el monto repetido, las constantes de configuración, las horas de la franja. Si la
mediana se olvida, un modelo real que escriba «la mediana fue 86,85 BRL» es rechazado por
`NOT_GROUNDED_NUMBER` **y ningún test dorado lo nota**, porque la plantilla no la escribe. Criterio:
un test que afirme que el valor de cada campo tipado de cada regla está en el conjunto, y otro que
afirme que el conjunto construido desde campos es **igual** al construido desde la prosa para las
mismas evaluaciones. Ese segundo test es el oráculo de D1.

**`e7-v3` solo si la plantilla cambia el texto.** Alimentar la plantilla con los mismos campos
produce los mismos bytes, y la decisión 58 en su contrapositiva lo prohíbe: sin cambio de texto no
hay versión nueva, porque dos versiones con texto idéntico hacen que la consola ofrezca «redactar
con la plantilla vigente» para producir el mismo párrafo. Hay una razón legítima y distinta: **usar
la mediana**, que pasa a ser un campo y que hoy la plantilla no puede escribir. «Es 23,2 veces la
mediana del comercio, que fue 86,85 BRL» es mejor texto y una cifra más que el grounding verifica.
Si la tarea quiere eso, `e7-v3` está justificado; si no, la versión no se toca.

Y una corrección de arrastre: `RequestExplanationHandler` elige `RuleConfig.E3V1` fijo en vez de la
versión de la fila que explica. Hoy es inocuo porque los umbrales no cambian; con dos versiones
vivas deja de serlo.

## D6 — El idioma es del despliegue

Koin opera en Brasil y México. El castellano cubre México; el portugués es el que falta, y es la
señal más directa que este proyecto le puede dar a un empleador brasileño. Con campos tipados,
traducir deja de ser cosmético: la frase se compone en la consola.

Pero **no es «un diccionario más»** en dos lugares:

- **La explicación la escribe el backend y se persiste.** Una consola en portugués mostraría un
  párrafo en castellano, salvo que el idioma llegue al backend **y a la identidad de la fila**. Si
  no, vuelve el problema de `E7D`: un despliegue que cambia de idioma encuentra la fila en
  castellano y nunca escribe la portuguesa. El idioma entra en el índice único, con migración — que
  en esta etapa no está prohibida como lo estaba en la 8.
- **La superficie es la mayor del proyecto.** Hay literales en castellano en decenas de archivos del
  frontend, en sus tests, en las 43 comprobaciones del smoke, en `<html lang="es">` y en las anclas
  del script de capturas. «La consola entera» es una frase corta que describe la tarea de frontend
  más grande de todas.

**Un solo origen.** `SALVO_LANGUAGE=es|pt` la lee la API, que falla al arrancar ante un valor
desconocido —el molde de `AI_PROVIDER`— y la declara en `/api/system/capabilities`, que la consola
ya lee del lado del servidor. Nada de `NEXT_PUBLIC_`. `Accept-Language` queda descartado: haría la
identidad de la explicación dependiente de la petición y duplicaría filas por idioma.

**Dos personas del mismo comercio con idiomas distintos no se pueden atender sin identidad.** Es el
límite correcto para una instancia por comercio y va escrito al Blueprint: idioma por despliegue;
por persona, después de la autenticación, post-MVP.

Se conservan sin tocar el tokenizador y el formato numérico: el portugués de Brasil usa los mismos
separadores. No se conservan los nombres de mes ni las palabras de severidad.

## D7 — La accesibilidad produce una lista antes de producir código

Recorrido con VoiceOver, no con un linter. Lo que se verifica es el recorrido: que la cola se navegue,
que la severidad se anuncie con palabras y no solo con color, que el formulario de veredicto diga
qué se está por hacer, y que los avisos que **aparecen** —divergencia, explicación desactualizada,
errores de importación— lleguen al lector cuando aparecen.

Lo que salga es código de producto. Por eso la tarea **lista los hallazgos con su severidad antes de
corregir**, y el coordinador decide cuáles entran. La etapa no promete cumplir un nivel de WCAG:
promete que alguien recorrió la consola sin ver la pantalla y anotó qué no funcionó.

## D8 — Un panel en el dashboard, decidido con el usuario

Un falso negativo no abre alerta, y un pedido sin alerta no tiene ninguna pantalla: la opinión del
proveedor se muestra únicamente en el detalle de una alerta. Sin superficie, los tres falsos
negativos son filas que no existen y **la afirmación más importante de la etapa solo se puede hacer
con un `curl`**.

El dashboard gana una sección: **«Denegados por el proveedor sin alerta local»**, con el conteo y
esos pedidos. Es una lectura más, sin acciones y sin estado. El corpus la alimenta a propósito: el
proveedor simulado decide por los dígitos finales de la referencia, así que los tres falsos
negativos llevan referencias en la banda que el mock deniega.

Sigue fuera: `/orders`, un proveedor que se porte mal para que `NO_OP`, `SUPERSEDED` y `CONFLICTING`
se vean —es la candidata más tentadora de las que quedan—, y Anthropic, que sigue siendo decisión
aparte porque cambia quién redacta, no si el texto se verifica.

## D9 — El corredor UTC−3, y una variable muerta

El corpus v2 se queda en **Uruguay, Brasil y Argentina**. São Paulo y Buenos Aires están en UTC−3
como Montevideo y sin horario de verano: las franjas coinciden. Ciudad de México está en UTC−6, y
una jornada de 09 a 18 caería de 12 a 21 hora de Montevideo, cruzando dos franjas: la explicación
diría «franja de 18:00 a 24:00, hora del comercio» sobre un comercio que cerró a las 18. Una zona
por comercio es una columna que el contrato de importación no tiene y una migración que la etapa no
necesita. **`BusinessTimeZone` es del despliegue, no del comercio**, y se escribe.

Y una corrección de arrastre: `.env.example` declara `BUSINESS_TIMEZONE` y **nada la lee**. La única
aparición del huso en el backend es una constante. Es una variable muerta que promete una
configuración que no existe: se saca.

Sobre `unusual_hour`: la cifra que tres documentos repiten —«la franja más rara está en 13,8 %»— es
falsa. El mínimo real con la aritmética del motor es **16,7 %**. La conclusión se sostiene: sigue
siendo inalcanzable. Y hay un segundo motivo que v1 no nombraba: **veinte de los cien pedidos de
cada comercio no llegan al mínimo de veinte en treinta días** y la regla ni siquiera evalúa. Un
comercio «de horario comercial» necesita 21 pedidos en los 30 días previos, con no más de 2 en su
franja rara.

## D10 — El repaso final

El inventario que `E8A` entregó, más el del hallazgo 9: los tests fijados al corpus —distribución de
scores, histograma, fingerprint dorado, matrices, conteos del mock, los del dashboard—, el pedido de
la demo, y las cifras de los documentos.

**El pedido de la demo se elige al construir la fixture, no después.** Tiene que cumplir tres
condiciones a la vez: identificar exactamente una alerta, ser **crítico**, estar **aprobado por el
mock** para que la captura de divergencia exista, y tener un ratio con decimal para uno de los dos
textos dorados. Aparece en el script de capturas, en el guion, en las muestras de importación y en
los dorados.

Entra además: los códigos de fila que hoy pegan el mensaje inglés de la API, los dos que no tienen
rótulo, «Se importaron 1 pedidos», las seis capturas regeneradas, el artículo para revisores, y la
lista de bases `.db` para que el usuario borre lo que quiera —`rm` está denegado y es regla suya.

Los dos hallazgos que la Etapa 8 dejó: el desempate de la cola por un identificador aleatorio, y que
`explanationId` no se pinta en el panel de revisión. **Adentro o afuera, pero dicho.** Recomiendo
afuera los dos: el primero es código de producto que ninguna afirmación de la etapa necesita, y el
segundo tiene su registro en la base, que es donde importa.

## D11 — Partición

En este orden, y el orden es obligatorio:

1. **`E9A-FIXTURE`** — el corpus v2 con los siete arquetipos, el seed versionado con su ensayo, el
   panel del dashboard, el aviso de divergencia corregido, y todos los tests fijados al corpus.
   Motor quieto en `e3-v1`.
2. **`E9B-SENALES-TIPADAS`** — `e3-v2`, campos por regla, `detail` heredado, contrato recapturado, y
   el diferencial con el extractor como oráculo. **El borrado del extractor es el último commit.**
3. **`E9C-PORTUGUES-ACCESIBILIDAD`** — `SALVO_LANGUAGE`, el idioma en la identidad de la
   explicación, los dos diccionarios, y el recorrido con lector de pantalla.
   **Partida en dos el 2026-09-07, después de medir la superficie**: `E9C1-IDIOMA` y
   `E9C2-ACCESIBILIDAD`. Son 52 archivos de frontend con literales, una migración, la plantilla del
   backend y 42 anclas del smoke por un lado; y por el otro un trabajo de criterio que produce una
   lista antes que código y que necesita un paso manual del coordinador en el medio. Juntas, el
   riesgo concreto era que la accesibilidad se hiciera apurada al final de una tarea agotada.
   `E9C1` va primero: la accesibilidad crea texto nuevo, y con el diccionario ya puesto ese texto
   nace en los dos idiomas en vez de nacer en castellano y traducirse después.
4. **`E9D-CIERRE`** — el repaso final, las capturas y el guion con el pedido nuevo, el artículo, y
   las decisiones nuevas en la bitácora.

## Cambios de estado canónico que exige este diseño

1. **Blueprint §4.1**: qué país es `countryCode`; `BusinessTimeZone` es del despliegue.
2. **Blueprint §7**: qué significa `isFraudLabel`.
3. **Blueprint §11**: la Etapa 9 con su partición.
4. **Bitácora**: el idioma es del despliegue y por persona es post-MVP; el umbral es una política y
   no un ajuste; una versión de plantilla no sube si el texto no cambia (la contrapositiva de la 58).
5. **`.env.example`**: sin `BUSINESS_TIMEZONE`.
6. **Workboard, README y `docs/muestras/README.md`**: `16,7 %`, no `13,8 %`.
