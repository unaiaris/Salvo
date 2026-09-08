# Salvo — Diseño de la Etapa 10: la instancia pública

> Estado: propuesta **v2**, corregida tras la revisión adversarial
> Fecha: 2026-09-08
> Base: `main` tras el cierre del MVP (`c285536`)
> Revisión adversarial: `Coordination/Tasks/E10-revision-adversarial.md` (13 hallazgos, 5 altos)
> Decidido con el coordinador antes de escribir: **sandbox compartida que se reinicia**, no solo
> lectura ni base por visitante.

## Qué cambió respecto de v1

| Hallazgo | Qué estaba mal | Qué dice v2 |
| --- | --- | --- |
| 1, alta | **D2 afirmaba lo contrario de lo que hace el archivo**: dije que el rewrite esconde la API y la publica. `/api/:path*` reescribe sin filtro, y la consola **no lo usa** —siempre URL absoluta desde el servidor de Node, como documenta `server-client.ts`—. Hoy es superficie expuesta sin beneficio | El rewrite **se borra**. Es una línea, y recién entonces la topología dice lo que D2 afirmaba (D2) |
| 2, alta | «Koyeb no duerme» salió de un blog, no de la fuente. **Las tres candidatas duermen**: Koyeb a la hora, Render a los quince minutos, SnapDeploy también | El arranque en frío es **el riesgo central de la etapa**, no la RAM. Y el reinicio deja de ser un reloj (D3, D8) |
| 3, alta | `E10A` medía «todo local» y **no había runtime de contenedores en la máquina**. Es el hallazgo de Chrome en `E8`, otra vez | Lo instala el coordinador antes de despachar, y los umbrales salen del código (D7) |
| 4, alta | «No puede contener otros datos» **no es verificable**: importación y nota de revisión son escritura anónima de texto libre, visible para todos hasta el reinicio | La condición se reescribe, y se decide qué queda abierto (D1) |
| 5, alta | El limitador **no ve visitantes**: todo llega desde `127.0.0.1` sin cabecera de origen, y el costo de una corrida depende de cuántos pedidos hay, no de cuántas veces se pida | El límite sube a la capa de Next, y aparece un tope de pedidos por instancia (D5) |
| Medias | La mecánica del reinicio con conexiones agrupadas, `tzdata` en la imagen, el supervisor de dos procesos, una sonda que vería sana una consola con la API muerta, y la decisión 8 escrita en **seis** lugares y no dos | Todo adentro (D3, D9) |

## Qué decide esta etapa

Que cualquiera pueda abrir un link y usar Salvo desde el navegador, gratis, sin instalar nada.

Y decide algo más grande que eso, que es lo que la vuelve una etapa y no una tarea de despliegue:
**la decisión 8 del Blueprint dice que sin autenticación la aplicación permanece local, para evitar
rutas mutables públicas sin aislamiento.** Esta etapa no la deroga: la satisface de otra manera, y
escribe cuál.

## Lo que ya está resuelto y conviene saber antes de diseñar nada

Tres hechos del repositorio que hacen esto más chico de lo que parece, los tres verificados:

- **El frontend ya proxea `/api/*` al backend** con un rewrite de Next (`frontend/next.config.ts`).
  El navegador nunca habla con la API. Eso da un solo origen, sin CORS y sin publicar la API, sin
  escribir una línea.
- **`DemoData:Enabled` es `false` por defecto** en `appsettings.json`. Las rutas de demostración no
  se encienden solas: encenderlas es una decisión explícita de esta etapa.
- **Los límites de cuerpo ya existen**: 5 MB por archivo importado
  (`OrderEndpoints.MaximumFileSizeBytes`) y un tope propio en el endpoint de callbacks.

Y un hecho que va en contra, también verificado: **nada migra la base al arrancar.**
`scripts/demo.sh` corre `dotnet ef database update` antes de levantar la API, y un contenedor de
producción no lleva el SDK.

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | La decisión 8 se reemplaza por una más precisa, con su motivo intacto | Blueprint |
| D2 | **El rewrite se borra**, y recién entonces la API queda sin publicar | Hallazgo 1 |
| D3 | El reinicio **es el arranque del proceso**, más un tope de antigüedad. El arranque en frío es el riesgo central | Hallazgo 2 |
| D4 | La instancia pública enciende la demostración a propósito, y eso se dice | D3 |
| D5 | El límite sube a la capa de Next, y aparece un **tope de pedidos por instancia** | Hallazgo 5 |
| D6 | Un cartel visible, en los dos idiomas | D1 |
| D7 | El runtime de contenedores **lo instala el coordinador** antes de despachar; los umbrales salen del smoke | Hallazgo 3 |
| D8 | Ninguna cifra de plataforma se publica sin verificarla. **La v1 rompió esta regla en su propio texto** | Regla del proyecto |
| D9 | La decisión 8 está en **seis** lugares, uno de ellos bloquea la ejecución | Hallazgo medio |

---

## D1 — La decisión 8 no se deroga: se reemplaza con su motivo intacto

La decisión 8 dice «sin auth, la aplicación permanece local», y su motivo es **«evitar rutas mutables
públicas sin aislamiento»**. El motivo es correcto y sigue siéndolo. Lo que esta etapa discute es si
«local» era la única forma de honrarlo.

La decisión nueva, que la supersede y **cita su motivo**:

> Una instancia pública de demostración es admisible sin autenticación cuando se cumplen las tres
> cosas a la vez: **arranca con datos sintéticos y vuelve a ellos en cada reinicio**, **anuncia en
> pantalla que es compartida y efímera**, y **el reinicio no depende de que nadie se acuerde**. El
> reinicio es lo que reemplaza al aislamiento: nadie queda con el estado que otro dejó, porque el
> estado no sobrevive.

**La v1 decía «no puede contener otros datos», y eso es falso.** La importación acepta un archivo de
cualquiera y la nota de revisión es texto libre; las dos son **escritura anónima visible para todos
los visitantes hasta el próximo reinicio**. Una instancia pública tiene un muro donde se puede
escribir, y prometer lo contrario sería exactamente la clase de afirmación que este proyecto pasó
nueve etapas eliminando.

Así que hay que decidirlo y no descubrirlo. Tres piezas, cada una con su respuesta en la tarea:

- **La importación**: queda abierta, porque sin ella no se puede mostrar el recorrido completo, y ya
  tiene tope de 5 MB y validación estricta por registro. Lo que falta es el tope de D5.
- **La nota de revisión**: es el campo más expuesto —texto libre, sin longitud acotada por el
  producto, visible a todos—. **Recomiendo dejarla y decirlo**, porque quitarla amputa el momento que
  la instancia existe para mostrar; pero es decisión del coordinador y va escrita.
- **El reinicio**: es lo que acota el daño de las dos, y por eso D3 dejó de ser un detalle de
  infraestructura.

Lo que **no** cambia: sin autenticación no se despliega nada que reciba datos de una persona real.
Eso sigue prohibido y hay que escribirlo con esas palabras, porque es la mitad que importa.

Y una consecuencia que conviene decir en voz alta: **un visitante puede emitir un veredicto que otro
visitante va a ver.** Eso no es un defecto de la instancia, es lo que significa «compartida», y por
eso el cartel de D6 no es decoración.

## D2 — El rewrite se borra, y recién entonces la API queda sin publicar

**La v1 de este diseño decía lo contrario de lo que hace el archivo, y conviene dejarlo escrito.**
Afirmé que el rewrite de `frontend/next.config.ts` esconde la API detrás del servidor de Next.
Reescribe `/api/:path*` a la API **sin filtro**, así que publicaría el sembrado, el scoring, los
callbacks, las métricas y el documento OpenAPI a un `/api` de distancia de la URL pública.

Y lo peor del hallazgo es que **ese rewrite no lo usa nadie**. La consola llama siempre con URL
absoluta desde el servidor de Node —`server-client.ts` lo documenta en su comentario de cabecera, y
explica que una URL relativa dentro de un componente de servidor es un `TypeError`—. Es superficie
expuesta sin un solo beneficio a cambio.

**Se borra.** Es una línea. Y recién entonces es cierto lo que la v1 daba por hecho: el contenedor
corre los dos procesos, la API escucha en `127.0.0.1`, **nada de afuera la alcanza**, y buena parte
del modelo de amenaza se resuelve por topología en vez de por código.

Si algo llegara a necesitar el puente —una sonda externa, por ejemplo— se acota a esa ruta y a
ninguna más, con el motivo escrito.

La contra, que hay que medir y no suponer: dos runtimes en el mismo contenedor, con 512 MB.

## D3 — La base se crea al arrancar, y se reinicia sola

**Al arrancar.** Hoy no hay nada que migre. Dos caminos, y la tarea elige con el motivo escrito:
un script SQL idempotente generado en tiempo de compilación —que no necesita el SDK en la imagen—,
o `Database.Migrate()` al arrancar detrás de una bandera explícita. El segundo es más simple y el
primero es más auditable; **no se elige por comodidad sino por argumento**.

**El reinicio.** Recrear la base, sembrar el corpus y correr el scoring, en una cadencia fija.
Tres cosas que decidir, y las tres tienen filo:

- **Cada cuánto.** Muy seguido y a alguien se le borra la alerta que estaba mirando; muy espaciado y
  la instancia acumula el ruido de todos. Una hora parece razonable y **hay que justificarlo, no
  asumirlo**.
- **Que se pueda ver cuándo.** El cartel de D6 dice cuándo es el próximo reinicio. Un reinicio
  sorpresa en el medio de una revisión es exactamente la clase de cosa que hace que alguien cierre
  la pestaña.
- **Que el reinicio no rompa una petición en curso.** Recrear el archivo de SQLite mientras la API
  lo tiene abierto no es gratis.

**Y acá está la corrección más útil de la revisión: las tres candidatas duermen.** Koyeb escala a
cero tras una hora sin tráfico, Render a los quince minutos, SnapDeploy también. Leído en las
páginas oficiales, no en un blog — que es de donde saqué la afirmación falsa de la v1.

Eso cambia la forma del reinicio. **No hace falta programar un reloj: el reinicio es el arranque del
proceso**, y ocurre solo cada vez que la instancia queda un rato sin visitas, que para un proyecto
de portafolio va a ser casi siempre. Una sola primitiva en vez de dos.

Lo que hay que agregarle es lo que esa primitiva no cubre: **si la instancia recibe tráfico continuo
nunca duerme, y el estado se acumula sin techo**. Un tope de antigüedad dentro del proceso —al
pasar N minutos desde el último reinicio, se rehace— cierra ese caso por poco dinero. Y el cartel no
puede prometer una hora exacta: dice **«se reinicia cuando queda un rato sin visitas, y como máximo
cada [N]»**, que es verdad en los dos casos.

**Y el costo real de esa primitiva es lo que hay que mirar de frente**: arrancar dos runtimes,
migrar, sembrar 300 pedidos y correr el scoring, con una fracción de vCPU. Eso es lo que espera el
primer visitante después de cada silencio, y **es el riesgo central de la etapa, no la RAM**.

## D4 — La demostración se enciende a propósito, y se dice

`DemoData:Enabled` en `true`. Sin eso desaparecen el sembrado, las métricas de calidad y el disparo
de callbacks — es decir, la mitad del producto y todo el argumento de la Etapa 9.

Encenderlo expone la ruta de sembrado a cualquiera. **Es aceptable justamente porque la base se
reinicia**, y esa dependencia entre D3 y D4 es una sola decisión partida en dos: encender la
demostración sin el reinicio sería otra cosa.

El secreto de callback queda como esté **después de verificar qué camino usa el disparo de
demostración**. Hoy vacío significa «todo callback entrante se rechaza», que es el default seguro; si
el disparo interno no pasa por ahí, se deja vacío y se explica.

## D5 — Límite de tasa

Es lo único del modelo de amenaza que no cubren ni los topes de cuerpo ni la topología de D2. Pero
la v1 lo puso en el lugar equivocado, y la revisión lo mostró con precisión:

**Un limitador en la API no ve visitantes.** Después de borrar el rewrite, *toda* petición le llega
desde `127.0.0.1`, sin ninguna cabecera de origen, porque el único cliente es el servidor de Next.
Limitar ahí limita a la consola contra sí misma.

Entonces el límite sube a **la capa donde el visitante existe**, que es el servidor de Next.

**Y hay un segundo problema que ningún limitador de tasa resuelve.** El costo de una corrida de
scoring no depende de cuántas veces se pida sino de **cuántos pedidos hay en la base**, y la
importación deja subir 5 MB por archivo, tantas veces como uno quiera. Diez importaciones y la
corrida siguiente cuesta diez veces más. Hace falta **un tope de pedidos por instancia**, que hoy no
existe: pasado ese número, la importación se rechaza con un código propio y la consola lo dice.

Los números **salen de medir**, no de elegir algo redondo.

## D6 — El cartel, en los dos idiomas

Visible en todas las pantallas, no escondido en un pie de página:

> Instancia de demostración compartida. Los datos son sintéticos. **Lo que escribas acá lo ve todo
> el mundo**, y todo se reinicia cuando la instancia queda un rato sin visitas, y como máximo cada
> [N] minutos.

Es una redacción y no un hueco a completar: **no puede prometer una hora exacta**, porque el
reinicio principal es por inactividad y no tiene reloj. Y «lo que escribas lo ve todo el mundo» es la
frase que D1 vuelve obligatoria: la nota de revisión es texto libre y pública.

Va a los dos diccionarios, como todo literal desde `E9C1`. **Y no es decoración**: es lo que
convierte «compartida» en algo que el visitante sabe antes de emitir un veredicto, en vez de algo
que descubre cuando ve el veredicto de otro.

## D7 — 512 MB es una restricción de diseño

**Primero hay que poder medir: cuando se escribió este diseño no había runtime de contenedores en
la máquina.** Ni `docker`, ni `podman`, ni `colima`, ni `nerdctl`. Fue el hallazgo de Chrome en la
Etapa 8, otra vez. **Lo instaló el coordinador** —Docker Desktop 29.7.2 sobre macOS 15.6.1— antes
de despachar `E10A`: instalar software en la máquina del usuario no es trabajo de una tarea.

.NET más Next SSR en el mismo contenedor, con 512 MB de RAM y una fracción de CPU. Se mide con el
contenedor limitado a mano, y los números van al handoff: RAM en reposo, RAM bajo la corrida de
scoring, **y sobre todo el camino frío completo**: arranque de los dos procesos, migración, sembrado
y scoring, cronometrado por partes.

**Los umbrales no se inventan: ya están en el código.** El smoke espera 60 s para el sembrado, 120 s para el
scoring, 30 s por página y 90 s hasta que un proceso responda. Los 5 s que uno recuerda son otra
cosa: el tiempo máximo de una petición de la consola a la API, en `server-client.ts`. Si el camino frío en un contenedor limitado los pasa, la
instancia no es usable y hay que saberlo en la primera tarea.

Si no entra, la salida está decidida de antemano y no se improvisa: **se parte en dos servicios**
—la consola en un tier gratuito de Next, la API en otro— y se paga el costo de tener dos orígenes y
CORS. Escribirlo ahora evita decidirlo cansado.

## D8 — Ninguna cifra de plataforma se publica sin verificarla

Los tiers gratuitos cambian seguido y este proyecto tiene la regla de no afirmar lo que no verificó.
**La v1 de este mismo documento la rompió**: escribí que Koyeb no duerme, tomándolo de un blog
comparativo, y la revisión leyó las páginas oficiales y encontró que escala a cero tras una hora sin
tráfico. Una afirmación falsa sobre la elección de plataforma, dentro de la decisión que dice que no
hay que hacer afirmaciones sin verificar.

Lo que queda en pie de la búsqueda del 2026-09-08: tres candidatas sin tarjeta y sin prueba que
vence —Koyeb, Render y SnapDeploy—. **Las tres duermen**, con umbrales distintos, y por eso la
diferencia que la v1 usaba para elegir no existe.

**`E10C` confirma los límites vigentes en la documentación oficial el día que despliega**, y lo que
se escriba en el README lleva esa fecha, igual que el bloque de cifras del corpus.

---

## D9 — La decisión 8 está escrita en seis lugares, no en dos

La v1 listaba dos. La revisión encontró seis, y uno de ellos **bloquea la ejecución**:
`AGENTS.md`, línea 114, la tiene como invariante —«sin autenticación, la aplicación es solo local y
no se despliega con rutas mutables públicas»—, y un agente que la lea se detiene con razón. Es
exactamente lo que pasó en `E9B` con la regla del `detail`.

**La corrige el coordinador antes de despachar `E10B`**, en los seis lugares y de una vez, con la
decisión 70 escrita y la 8 marcada como superada. Ninguna tarea la toca desde su rama.

## Partición

En este orden, y el orden es obligatorio:

1. **`E10A-CONTENEDOR-Y-MEDICION`** — borrar el rewrite,
   escribir el `Dockerfile` con los dos procesos y su supervisor, resolver la creación de la base al
   arrancar, y **medir el camino frío completo** contra los umbrales que ya existen en el smoke. Es
   la tarea que decide si el resto es posible, y **puede terminar diciendo que no lo es**.
2. **`E10B-INSTANCIA-COMPARTIDA`** — el reinicio por antigüedad, el cartel en los dos idiomas, el
   límite en la capa de Next, el tope de pedidos por instancia, y la sonda de salud que hoy vería
   sana una consola con la API muerta. **Se despacha después de que el coordinador corrija los seis
   lugares de la decisión 8** (D9).
3. **`E10C-PUBLICACION`** — elegir plataforma con los números de `E10A` en la mano y **los límites
   confirmados en la documentación oficial ese día**, desplegar, y dejar el link en el README, en el
   artículo para revisores y en `Salvo-Getting-Started.md`. El alta la hace el coordinador; el agente
   prepara y verifica.

**Y una condición de parada que vale para toda la etapa**: si `E10A` mide que el camino frío no entra
en los umbrales, la etapa **cambia de forma antes de seguir** —dos servicios, o instancia de solo
lectura, o pagar cinco dólares por mes— y esa decisión es del coordinador, no de la tarea.

## Cambios de estado canónico que exige este diseño

1. **Blueprint, bitácora**: la decisión **70** reemplaza a la 8, citando su motivo y las tres
   condiciones de D1. La 8 se marca como superada, no se borra.
2. **Blueprint, límites**: «sin autenticación no se despliega nada que reciba datos de una persona
   real» queda escrito con esas palabras.
3. **README, «Límites declarados»**: la instancia pública es compartida y efímera, con el enlace.
4. **README, «Cómo correrlo»**: el link, antes de las instrucciones de correrlo local.
5. **Workboard y Progress**: la Etapa 10 abierta, con sus tres tareas.
