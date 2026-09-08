# Salvo — Diseño de la Etapa 10: la instancia pública

> Estado: propuesta **v1**, pendiente de revisión adversarial
> Fecha: 2026-09-08
> Base: `main` tras el cierre del MVP (`c285536`)
> Decidido con el coordinador antes de escribir: **sandbox compartida que se reinicia**, no solo
> lectura ni base por visitante.

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
| D2 | Un solo servicio y un solo origen; la API no se publica | El rewrite que ya existe |
| D3 | La base se crea al arrancar y se reinicia sola, y el reinicio es la superficie que reemplaza al aislamiento | D1 |
| D4 | La instancia pública enciende la demostración a propósito, y eso se dice | D3 |
| D5 | Límite de tasa, porque es lo único que falta del modelo de amenaza | Superficie pública |
| D6 | Un cartel visible, en los dos idiomas | D1 |
| D7 | 512 MB es una restricción de diseño y se mide antes de elegir plataforma | Gratuito |
| D8 | Ninguna cifra de plataforma se publica sin haberla verificado | Regla del proyecto |

---

## D1 — La decisión 8 no se deroga: se reemplaza con su motivo intacto

La decisión 8 dice «sin auth, la aplicación permanece local», y su motivo es **«evitar rutas mutables
públicas sin aislamiento»**. El motivo es correcto y sigue siéndolo. Lo que esta etapa discute es si
«local» era la única forma de honrarlo.

La decisión nueva, que la supersede y **cita su motivo**:

> Una instancia pública de demostración es admisible sin autenticación cuando se cumplen las tres
> cosas a la vez: **contiene únicamente datos sintéticos y no puede contener otros**, **anuncia en
> pantalla que es compartida y efímera**, y **se reinicia sola**. El reinicio es lo que reemplaza al
> aislamiento: nadie queda con el estado que otro dejó, porque el estado no sobrevive.

Lo que **no** cambia: sin autenticación no se despliega nada que reciba datos de una persona real.
Eso sigue prohibido y hay que escribirlo con esas palabras, porque es la mitad que importa.

Y una consecuencia que conviene decir en voz alta: **un visitante puede emitir un veredicto que otro
visitante va a ver.** Eso no es un defecto de la instancia, es lo que significa «compartida», y por
eso el cartel de D6 no es decoración.

## D2 — Un solo servicio, un solo origen, la API sin publicar

El contenedor corre los dos procesos: el servidor de Next en el puerto público, y la API de .NET
escuchando en `127.0.0.1`. El rewrite de Next hace de puente.

Lo que eso compra, y es más de lo que parece: **la API no es alcanzable desde afuera**. Ni el seed,
ni el scoring, ni los callbacks, ni las métricas. Todo lo que un visitante puede tocar pasa por el
servidor de Next, que es el único que escucha. Buena parte del modelo de amenaza se resuelve por
topología y no por código.

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

**Y una alternativa que la tarea tiene que considerar antes de programar un reloj**: si el
contenedor de la plataforma elegida se duerme y arranca de cero, el reinicio **ya existe** y es la
propia plataforma. Puede que no haya que escribir nada.

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

Es lo único del modelo de amenaza que no está cubierto ni por los topes de cuerpo ni por la
topología de D2. Un visitante puede pedir un scoring sobre 300 pedidos en un bucle, y con 0,1 vCPU
eso tira la instancia. .NET trae limitación de tasa en el framework: no hace falta una dependencia.

Lo que se limita y con qué números **sale de medir**, no de elegir un número redondo.

## D6 — El cartel, en los dos idiomas

Visible en todas las pantallas, no escondido en un pie de página:

> Instancia de demostración compartida. Los datos son sintéticos. Lo que hagas acá lo ve todo el
> mundo, y todo se reinicia [cuándo].

Va a los dos diccionarios, como todo literal desde `E9C1`. **Y no es decoración**: es lo que
convierte «compartida» en algo que el visitante sabe antes de emitir un veredicto, en vez de algo
que descubre cuando ve el veredicto de otro.

## D7 — 512 MB es una restricción de diseño

.NET más Next SSR en el mismo contenedor, con 512 MB de RAM y una fracción de CPU. **Se mide
localmente antes de elegir plataforma**, con el contenedor limitado a la mano, y el número va al
handoff: cuánta RAM en reposo, cuánta bajo la corrida de scoring, y cuánto tarda el primer render.

Si no entra, la salida está decidida de antemano y no se improvisa: **se parte en dos servicios**
—la consola en un tier gratuito de Next, la API en otro— y se paga el costo de tener dos orígenes y
CORS. Escribirlo ahora evita decidirlo cansado.

## D8 — Ninguna cifra de plataforma se publica sin verificarla

Los tiers gratuitos cambian seguido y este proyecto tiene la regla de no afirmar lo que no midió.
De la búsqueda del 2026-09-08 hay tres candidatas sin tarjeta y sin prueba que vence —Koyeb, Render
y SnapDeploy—, con una diferencia que importa: **Render duerme a los 15 minutos y tarda entre 30 y
50 segundos en despertar**, y un link de entrevista que tarda cuarenta segundos es un link que nadie
espera.

Pero eso es una búsqueda, no una medición. **La tarea confirma los límites vigentes en la
documentación de la plataforma el día que despliega**, y lo que se escriba en el README lleva esa
fecha, igual que el bloque de cifras del corpus.

---

## Partición

En este orden, y el orden es obligatorio:

1. **`E10A-CONTENEDOR`** — el `Dockerfile` con los dos procesos, la creación de la base al arrancar,
   y **la medición**: RAM en reposo y bajo carga, tiempo de arranque en frío, tiempo del primer
   render. Todo local. Es la tarea que decide si el resto es posible.
2. **`E10B-INSTANCIA-COMPARTIDA`** — el reinicio, el cartel en los dos idiomas, el límite de tasa, y
   la decisión 8 reemplazada en el Blueprint con su motivo.
3. **`E10C-PUBLICACION`** — elegir plataforma con los números de `E10A` en la mano, desplegar, y
   dejar el link en el README, en el artículo para revisores y en `Salvo-Getting-Started.md`. El
   alta en la plataforma la hace el coordinador; el agente prepara y verifica.

## Cambios de estado canónico que exige este diseño

1. **Blueprint, bitácora**: la decisión **70** reemplaza a la 8, citando su motivo y las tres
   condiciones de D1. La 8 se marca como superada, no se borra.
2. **Blueprint, límites**: «sin autenticación no se despliega nada que reciba datos de una persona
   real» queda escrito con esas palabras.
3. **README, «Límites declarados»**: la instancia pública es compartida y efímera, con el enlace.
4. **README, «Cómo correrlo»**: el link, antes de las instrucciones de correrlo local.
5. **Workboard y Progress**: la Etapa 10 abierta, con sus tres tareas.
