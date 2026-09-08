# Guion de demo — diez minutos

Sirve para grabar y para hablar en una entrevista. Cada bloque trae **la frase**, **el clic** y una
línea **si falla**, porque una demo se rompe siempre en el minuto peor y lo que salva el momento es
tener escrito qué decir mientras tanto.

Se nombran **pedidos**, nunca «la primera fila» ni una URL: el identificador de una alerta es un
`Guid.NewGuid()` y el desempate del orden de la cola también, así que la fila de arriba es otra en
cada corrida. Buscar `ORD_000011` en la cola siempre encuentra lo mismo.

## Antes de empezar

```bash
./scripts/demo.sh
```

Levanta la API y la consola sobre una **base nueva**, con el corpus cargado y la corrida ya
ejecutada, y deja las URLs impresas. Es una precondición, no un paso de la demo: sin ella el primer
minuto se va en clics de carga, y el feed arranca diciendo que no hay pedidos.

**El guion es destructivo y de un solo uso.** El veredicto de una alerta es terminal, una explicación
escrita no se regenera, y el seed es idempotente: repetirlo no devuelve nada a cero. Para el segundo
ensayo se vuelve a correr `scripts/demo.sh`, que estrena otra base y no toca la anterior.

Conviene tener **una terminal libre** al lado: el bloque de las 8 la usa.

---

## 0–1 · Qué es y qué no

**Se abre en** `/alerts`.

**La frase.** «Esto es la consola antifraude de un comercio, no un proveedor antifraude. El comercio
recibe pedidos, los puntúa con reglas propias, y además le pregunta a un proveedor externo. Las dos
opiniones conviven, y ninguna de las dos es la de un modelo de lenguaje: la IA acá solo redacta, y
lo que redacta se verifica antes de guardarse.»

**El clic.** Ninguno. Se deja la cola en pantalla mientras se dice eso.

**Si falla.** Si la consola dice «No se pudo contactar a la API», es que `scripts/demo.sh` no
terminó de levantar o se cayó. Vale mostrarlo un segundo: las cuatro rutas de datos degradan con un
mensaje propio en vez de romperse, y eso también es parte del diseño.

## 1–2 · Importar y correr el scoring

**Se va a** `/import`.

**La frase.** «Importar y puntuar son dos cosas distintas, y la consola no las mezcla. Importar
escribe pedidos y nada más. Hasta que no corre el scoring no hay evaluaciones ni alertas, y la
pantalla lo dice.»

**El clic.** Subir `docs/muestras/import-con-errores.csv`, formato CSV, «Importar pedidos». Se ve
**un pedido importado y cinco rechazados**, cada rechazo con su registro, su línea, su campo y qué le
pasó, en castellano: la consola traduce el código del importador y nunca muestra el identificador
crudo. Vale señalar el último, «la referencia ya existe con otros datos», sobre `ORD_000011`, porque
ese pedido ya está en el corpus. La referencia es compuesta, `(comercio, referencia)`: dos comercios
pueden usar la misma numeración sin chocar.

**La frase que remata.** «El archivo no es todo o nada. Las filas válidas se escriben todas juntas,
en una transacción; las rechazadas no se escriben nunca. Diez mil pedidos con doce filas rotas
entran nueve mil novecientos ochenta y ocho, y las doce se devuelven para corregir.»

**Si falla.** Si el archivo no se puede elegir, se cuenta el resultado con la captura
`docs/capturas/05-import.png`, que muestra exactamente esta pantalla.

## 2–5 · Una alerta, con sus números

**Se va a** `/alerts` y se busca la fila de **`ORD_000011`**.

**La frase.** «Cada alerta congela la evaluación que la abrió. Esto no es un score suelto: son reglas
deterministas, cada una con su peso y con un detalle legible que dice de dónde salió el número.»

**El clic.** Abrir el detalle de `ORD_000011`. Se leen las tres señales en voz alta: monto atípico
—3,4 veces la mediana del comercio, calculada sobre tres pedidos previos—, comprador nuevo con monto
alto, y país distinto del habitual. Suman noventa sobre un umbral de sesenta, y noventa cae en la
banda crítica.

**La frase que más distingue.** «El baseline de cada pedido usa solo historia anterior a ese pedido.
Nunca el presente ni el futuro. Y la etiqueta de fraude del dataset no es una señal: se usa para
medir el criterio, jamás para calcularlo.»

**Si falla.** Si la alerta no aparece en la cola, es que la corrida no se ejecutó: se vuelve a
`/import` y se pulsa «Ejecutar corrida de scoring».

## 5–7 · La explicación, y por qué está verificada

**Se sigue en** el detalle de `ORD_000011`, en el bloque «Explicación».

**El clic.** «Explicar esta evaluación». El texto aparece escrito.

**La frase.** «Esto lo redactó una plantilla determinista, y la pantalla lo dice: “no por un modelo”.
Pero el punto no es que hoy no haya modelo detrás. El punto es que **da igual quién lo escriba**,
porque el texto se verifica contra la evaluación **antes** de guardarse: cada cifra y cada regla que
menciona tienen que existir en la evaluación. Un texto que dijera “cuarenta y ocho veces la mediana”
se rechazaría, porque cuarenta y ocho no es un hecho de esta evaluación. Y un texto rechazado no se
guarda, no se registra y no se muestra: del ofensor queda el token, nunca la frase.»

**Lo que conviene tener preparado.** El nombre del test que lo prueba es
`ExplanationGroundingTests.AnInventedFigureIsRefusedAndNoTextIsStored`, y la mutación que lo falsa
está en el handoff de `E7A`. Y la otra mitad de la regla: al input de un modelo no entra ningún texto que no escriba el motor —ni campos
importados, ni identificadores, ni notas escritas por personas—, y hay un proveedor espía en la
suite que lo afirma.

**Si falla.** Si el botón no está, es que la explicación ya se pidió en un ensayo anterior sobre esta
misma base: el texto ya está en pantalla y se lee igual. Una explicación escrita no se regenera, que
es justamente lo que hay que contar.

## 7–8 · La segunda opinión, y la divergencia

**Se va a** `/import`, a la sección del proveedor externo.

**El clic.** «Solicitar evaluación externa del corpus», y después «Entregar los callbacks del
proveedor». **Pulsar el segundo dos veces**: la segunda vez dice que esos callbacks ya se habían
recibido.

**La frase.** «El proveedor es una fuente aparte: su propia tabla, su propio ciclo de vida, su propio
estado. Una fila se reserva y se persiste **antes** de llamarlo, así que un fallo posterior al envío
la deja pendiente con su código de error, no cerrada. Y el callback es idempotente: un duplicado es
la ausencia de una segunda fila, detectada por unicidad. Por eso pulsarlo dos veces no rompe nada.»

**El clic que cierra.** Volver al detalle de `ORD_000011`. Ahí está la divergencia: **el motor local
marcó el pedido y el proveedor lo aprobó**.

**La frase que cierra.** «No se combinan en un veredicto único, y no se comparan los scores: son
escalas distintas de sistemas distintos, y “externo 11 contra local 90” no significa nada. Lo que se
contrasta son los dos veredictos, cada uno con su procedencia. La discrepancia es información para
quien revisa, no una operación aritmética.»

**Los callbacks se piden y se entregan desde `/import`, nunca desde la alerta.** Sobre una misma
alerta no se pueden mostrar las dos cosas: el proveedor aprueba `ORD_000011` de forma síncrona, así
que ahí no hay ningún callback pendiente que entregar. Por eso el mecanismo se muestra sobre el
corpus entero y la divergencia sobre el pedido.

**Si falla.** Si no aparece la sección del proveedor, la API arrancó sin `DemoData:Enabled`; con
`scripts/demo.sh` está encendida siempre.

## 8–9 · El veredicto terminal y su auditoría

**Se sigue en** el detalle de `ORD_000011`.

**El clic.** Revisar la alerta —confirmar segura o reportar fraude— con una nota corta.

**La frase.** «El veredicto es terminal. Nada reabre una alerta revisada: si aparece información
nueva, se crea una alerta nueva enlazada a la anterior. Y la revisión y la auditoría se escriben en
una sola transacción de base.»

**Lo que la pantalla muestra, y lo que no.** Muestra la transición, el instante y la nota. **No
muestra el identificador de la explicación que la analista tenía delante**, aunque el sistema lo
guarde: es el dato que justifica conservar una explicación vieja cuando la plantilla cambia. Se
enseña en la terminal libre:

```bash
curl -s http://127.0.0.1:5100/api/alerts/<id-de-la-alerta> | grep -o '"explanationId":"[^"]*"'
```

O se declara en voz alta que en pantalla se ve la mitad. Las dos salidas son honestas; inventar que
la pantalla lo muestra, no.

**Si falla.** Si la revisión da conflicto, es que la evaluación vigente cambió de banda respecto de
la que abrió la alerta: hay que marcar la casilla que reconoce la divergencia. Es deliberado —la
alerta congela su premisa— y vale contarlo.

**Un aviso para el que maneja el mouse.** Al revisarla, la alerta **desaparece de la cola**: el feed
pide solo las abiertas. No hay pantalla que liste alertas cerradas, así que conviene dejar este
bloque para el final del recorrido por la alerta.

## 9–10 · Los límites, dichos por el proyecto

**Se va a** `/dashboard`, hasta «Calidad del criterio».

**La frase.** «Acá está la parte que más me interesa mostrar. Las métricas **no** dan perfectas, y
eso es deliberado: el corpus está construido para que las reglas se equivoquen. Hay fraude que estas
reglas no pueden ver y hay pedidos legítimos que marcan, puestos a mano. La advertencia está escrita
debajo, en el producto, no en una nota al pie.»

<!-- corpus:inicio -->

Las cifras de este bloque salen del corpus de demostración, de la corrida del 2026-09-07: **300
pedidos** de tres comercios con **28 fraudes**, **23 alertas abiertas** —11 medias, 6 altas y 6
críticas—, umbral **60**, y F1 de **0,688** en calibración y **0,632** en holdout. La matriz de
confusión, con sus conteos, está en el README.

<!-- corpus:fin -->

**La frase que remata.** «Ahora, ojo con la cifra: la tasa base es del 9,3 % y la elegí yo al
escribir la fixture, igual que elegí qué pedidos son fraude y cuáles de ellos las reglas no pueden
ver. Con los errores puestos a mano, F1 es un parámetro del diseño y no un resultado. Lo que estas
métricas sí prueban es que la evaluación es honesta: división temporal, holdout sin retuning y
aritmética que cierra. Un proyecto que declara lo que le falta se lee mejor que uno que finge estar
terminado.»

**Si falla.** Si «Calidad del criterio» no está, la API arrancó sin `DemoData:Enabled`. Esa sección
vive detrás de esa bandera a propósito: la etiqueta de fraude es verdad de campo y el dashboard
operativo no la lee por ningún camino.

---

## Si hay que cortar

Con cinco minutos, se dejan **2–5** y **5–7**: la alerta con sus números y la explicación verificada.
Es lo que distingue al proyecto de un CRUD con reglas.

Si preguntan por qué no hay un modelo decidiendo, la respuesta corta: «porque el fraude se audita.
Una decisión que no se puede reconstruir no se puede defender ante un banco ni ante un cliente. El
modelo redacta; las reglas deciden.»
