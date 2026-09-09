# Salvo — Task brief `E10C-PUBLICACION`

## Identificación

- Work ID: `E10C-PUBLICACION`
- Etapa: 10
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-09
- Rama/worktree: `claude/e10c-publicacion`
- Commit base: **`3d2136d`**, el `merge-base` real de `claude/e10c-publicacion` con `main`. Lo
  escribe este commit, que es el primero de la rama, con el SHA que devolvió
  `git merge-base main HEAD`. El campo se dejó vacío a propósito y el motivo es la lección de `E8B`:
  el commit que lo escribiera en `main` movería la punta y volvería falso el campo que acaba de
  declarar. **La primera versión de este brief lo intentó igual y el `brief-check` lo cazó**:
  declaraba `1ea0747`, y el commit que lo declaraba dejó la punta en `db204ac`.
  **Y la regla se acaba de ganar un tercer ejemplo**: cuando la rama se cortó, la punta de `main` no
  era `e879094` —el último commit que este brief-check vio— sino `3d2136d`, el commit de despacho
  del coordinador, que llegó después. Cualquier SHA escrito de antemano habría estado mal por
  tercera vez.
  **El coordinador aceptó que el campo no llevara SHA**, y eso desplazó la verificación: el paso del
  `brief-check` que comprueba el commit base no se pudo ejecutar sobre las tres primeras versiones
  de este brief, y quien lo comprueba es este commit y después el handoff. La regla es determinista
  mientras la integración sea por merge, que este brief exige.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E10B` integrada y verificada (merge `23a47cd`). Ninguna tarea depende de ésta.
- **Esta tarea cierra la Etapa 10.**

## Resultado esperado

Salvo tiene **una URL pública que cualquiera abre desde un navegador, gratis, sin instalar nada**, y
los tres documentos que la citan dicen la verdad sobre lo que esa URL es: compartida, efímera,
sintética, y lenta la primera vez.

Una afirmación que esta tarea **no** puede producir es «la instancia anda»: eso lo dice una medición
desde afuera, con su fecha, igual que las cifras del corpus.

## Contexto obligatorio

- `Coordination/Tasks/E10-DISENO.md` (**v2**), decisión **D8** entera —«ninguna cifra de plataforma
  se publica sin verificarla»— y el punto 3 de la sección de partición, que define esta tarea.
- `Coordination/Tasks/E10-revision-adversarial.md`, hallazgo **2**: es el que corrigió la afirmación
  falsa sobre Koyeb que la v1 del diseño había tomado de un blog.
- `Coordination/Handoffs/Claude.md`, entrada de **`E10B`**: las mediciones, las cuatro falsaciones,
  y la lista de variables de entorno que la imagen trae puestas. Y sobre todo su sección **«Riesgos y
  trabajo pendiente»**, que le deja a esta tarea un encargo con todas las letras: confirmar que la
  plataforma manda `x-forwarded-for`.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 10»: el que esta tarea cierra es el **último**
  —«Publicada, con el link en el README, en el artículo y en la guía»—. Los anteriores ya están
  marcados; los dos que hablan del reinicio y de la base horneada los cerró `E10B`.
- `DesignAgent/Salvo-Blueprint.md`, **decisión 70** y la invariante del §10 «Seguridad y privacidad».
  Dicen qué sigue prohibido después de publicar.
- `Dockerfile`, el bloque `ENV` de la etapa final **con sus comentarios**: cada variable tiene
  escrito por qué está en el valor en que está, y esta tarea no puede cambiar ninguna sin decir por
  qué.
- `scripts/contenedor-entrypoint.sh`: el supervisor, los códigos de salida, y el reinicio por
  antigüedad.
- `frontend/src/proxy.ts`, `frontend/src/lib/rate-limit.ts` y `frontend/src/app/health/route.ts`.
- `README.md`, sección **«Límites declarados»**, y `DesignAgent/Salvo-Getting-Started.md`, sección
  **«El contenedor»**.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.** Y antes de escribir
cualquier cifra de la plataforma, abrir la página oficial que la sostiene.

## La plataforma ya está elegida, y por qué

**Render**, plan **Free**. El coordinador la eligió el 2026-09-09 con un criterio explícito: *que sea
gratis, siempre*. Lo que decidió no fue el rendimiento sino el **modo de falla**.

> «If you haven't added a payment method and you would incur charges, Render instead disables your
> services for the duration of the current billing period.»
> — https://render.com/docs/faq

Sin método de pago cargado, Render **no puede cobrar: suspende**. Google Cloud Run es mejor
exactamente donde más duele —1 vCPU por defecto contra 0,1, más CPU extra durante el arranque— pero
exige cuenta de facturación con tarjeta, y ahí el modo de falla deja de ser «se apaga» y pasa a ser
«se factura el excedente». Koyeb pide tarjeta igual, da la misma CPU floja, encierra el Free en una
sola región, y **fue adquirida por Mistral AI** (changelog oficial, febrero de 2026) con el tier
gratuito ya fuera de su página de precios.

Y una advertencia que esta tarea hereda del diseño: **«SnapDeploy», que la v2 del diseño listaba como
una de tres candidatas verificadas, no es una candidata.** La opera una sociedad constituida hace
cuatro meses cuya dirección legal registrada dice «SR.NO.43 Privet Drive», no publica RAM ni vCPU en
ninguna página, y sus propias páginas se contradicen sobre cuántos despliegues permite. Es la segunda
vez que este diseño publica una candidata sin verificarla de verdad. **No se la vuelve a nombrar.**

## Alcance

### Dentro

#### 1. Reconfirmar los límites vigentes, el día del despliegue

D8 lo pide y no es ceremonia: los tiers gratuitos cambian, y este proyecto ya escribió una vez una
cifra de plataforma falsa. Lo verificado el **2026-09-09**, en documentación oficial, es esto:

| Qué | Valor | Fuente oficial |
| --- | --- | --- |
| Plan Free permite Dockerfile y también imagen de registry | Sí | https://render.com/docs/web-services |
| Horas de instancia incluidas | 750 / mes / workspace | https://render.com/docs/free |
| RAM y CPU | 512 MB · 0,1 CPU | https://render.com/docs/compute-plans |
| Duerme tras | **15 minutos** sin tráfico entrante | https://render.com/docs/free |
| Despertar tarda | «about one minute», con pantalla de carga | https://render.com/docs/free |
| Tamaño máximo de imagen | 10 GB comprimida | https://render.com/docs/deploy-an-image |
| Sin método de pago, el exceso | suspende, no cobra | https://render.com/docs/faq |

Lo que el Free **no** soporta, textual de https://render.com/docs/free: escalar más allá de una sola
instancia, discos persistentes, caché de borde, trabajos sueltos, y acceso por consola o SSH. Además
no puede escuchar en los puertos **18012, 18013 y 19099**, ni sacar tráfico por el **25, 465 y 587**.

**Nada de eso nos limita, y conviene decir por qué**: la instancia es de una sola réplica por diseño
—el tope de pedidos y el limitador de tasa suponen exactamente eso—, y **no querer disco persistente
es el diseño mismo**: la base viene horneada en la imagen y el reinicio es copiarla. Salvo no escucha
en ninguno de los tres puertos prohibidos ni manda correo.

La tarea vuelve a abrir esas páginas el día que despliega y **rehace esta tabla con la fecha de ese
día**. Si algún número cambió, lo que se corrige es la tabla, no el recuerdo.

#### 2. Lo que hay que comprobar antes de desplegar, porque no está verificado

Estas cinco cosas **no tienen respuesta en la documentación oficial** y cada una puede romper el
despliegue o degradarlo en silencio. Se comprueban **primero**, y el resultado de cada una se escribe
con su evidencia.

1. **El puerto.** La imagen trae `PORT=3000` y Next escucha ahí. Render asigna el puerto por
   variable de entorno. Hay que confirmar **en la documentación oficial** cómo lo hace y que el
   punto de entrada lo respete: si Render inyecta un `PORT` distinto y el `ENV` de la imagen lo pisa,
   el servicio nunca responde y el diagnóstico es feo. **La API interna en `127.0.0.1:5100` no se
   toca**: no es pública y no interviene acá.
2. **El código de salida 75.** El reinicio por antigüedad termina el proceso con 75 a propósito, para
   que la plataforma lo distinga de una salida limpia. Hay que confirmar qué hace Render con una
   salida distinta de cero: si lo reinicia, si lo marca fallado, y si hay un tope de reinicios
   seguidos tras el cual deja de intentarlo. Si Render no lo reinicia, **el reinicio por antigüedad
   no funciona en esta plataforma** y hay que decirlo, no taparlo.
3. **La sonda de salud contra el sueño.** Render permite configurar un *health check path* y tenemos
   `/health`. Pero si esas sondas cuentan como tráfico entrante, **la instancia no duerme nunca y
   consume las 750 horas del mes**. Hay que averiguarlo antes de configurarla. Ante la duda,
   **no se configura**: dormir es parte del diseño, no un defecto.
4. **El tiempo de construcción.** La imagen pesa 851 MB y su construcción hace `dotnet publish` con
   ReadyToRun, `next build`, y una etapa que siembra la base. Los minutos de construcción son un
   recurso facturable en Render —«If you exceed your monthly included amount of outbound bandwidth or
   build pipeline minutes, Render bills you for a supplementary amount», https://render.com/docs/faq—,
   así que hay que medir cuánto tarda una construcción allá y cuánto queda del cupo. Sin tarjeta el
   peor caso sigue siendo suspensión y no factura, pero una suspensión a mitad de mes también es un
   link roto en un CV.

5. **La cabecera de origen.** El limitador de tasa de `E10B` identifica al visitante por
   `x-forwarded-for` y, si no está, por `x-real-ip`. **Sin ninguna de las dos, todos los visitantes
   comparten un solo cubo de diez mutaciones por minuto** y dos personas mirando la consola al mismo
   tiempo se estorban. Es el caso conservador y elegido —la instancia se protege igual—, pero
   degradado y silencioso. El handoff de `E10B` le encarga a esta tarea confirmarlo. Se confirma en
   documentación oficial **y** contra la instancia publicada, que es donde se ve de verdad.

Preferir el despliegue **desde el repositorio con el Dockerfile** antes que desde una imagen ya
construida: «Services deployed from prebuilt images don't support auto-deploys»
(https://render.com/docs/web-services), y un repositorio público que se despliega solo es además
parte del argumento del proyecto.

#### 3. El alta la hace el coordinador; el agente prepara y verifica

Esto viene del diseño y no se negocia. **El agente no crea cuentas, no ingresa credenciales, no
acepta términos y no conecta el repositorio.** Prepara todo lo que se pueda preparar —el archivo de
configuración si Render lo admite, la lista exacta de variables de entorno con su valor y su motivo,
la región elegida y por qué, y los pasos numerados— y **se detiene** ahí, entregándole al coordinador
un bloque que él ejecuta.

Sobre la región: el Free ofrece Oregon, Ohio, Virginia, Frankfurt y Singapur. **Ninguna está en
Sudamérica.** Virginia es la más cercana a Montevideo y a São Paulo. Eso agrega latencia de red a un
producto que ya arranca lento, y **va dicho**, no escondido.

#### 4. Medir el arranque en frío desde afuera, y publicar el número con su fecha

`E10A` y `E10B` midieron dentro de un M1. Esta es la primera medición en hardware compartido de
verdad, y es **la única que puede decir qué espera un visitante**. Se mide:

- **El primer arranque tras el sueño**: dejar la instancia quince minutos sin tocarla y cronometrar
  desde el pedido hasta que `/alerts` muestra las 23 alertas. El número esperado es del orden de
  «un minuto de plataforma más 41 s de aplicación», pero **eso es extrapolación y no se publica como
  medición**: se publica lo que el cronómetro diga.
- **El camino tibio**: la misma ruta con la instancia ya despierta.
- **Que los datos estén**: 23 alertas abiertas, 51 denegados por el proveedor sin alerta local, y la
  explicación escrita de `ORD_000011`. Son las mismas tres cifras que imprime
  `contenedor.sh medir-instancia` —**`medir` es otro subcomando**, que arranca con base vacía y
  siembra—. Y las **imprime**, no las afirma: leer la salida es parte de la comprobación.
- **Que el reinicio ocurra y no rompa nada.** Es la medición más incómoda y conviene decir cómo se
  hace: el tope de antigüedad son 30 minutos y Render duerme a los 15, así que hay que darle tráfico
  cada menos de quince durante media hora para que el reinicio por antigüedad llegue a ocurrir en vez
  de que la instancia se duerma antes. Y **se observa por lo que se lleva puesto**: dejar una nota de
  revisión escrita, y comprobar después que ya no está y que las 23 alertas volvieron. Si la
  comprobación 2 dice que Render no reinicia tras una salida con código 75, esto **no se puede medir**
  y lo que corresponde es escribirlo, no simularlo.

#### 5. Lo que los tres documentos tienen que decir

El link va al `README.md`, al artículo para revisores y a `DesignAgent/Salvo-Getting-Started.md`. En
los tres, **junto al link y no en una nota al pie**, va lo que esa instancia es:

- Es **compartida**: lo que un visitante escribe lo ve el siguiente. Una nota de revisión es texto
  libre, anónimo y público hasta el próximo reinicio.
- Es **efímera**: se reinicia sola y se lleva puesto lo que haya.
- Es **sintética**: 300 pedidos generados, cero datos de una persona real. Es la decisión 70.
- Es **lenta la primera vez**: dormida quince minutos, la primera carga tarda lo que la medición diga.
  Render muestra una pantalla de carga mientras tanto —«Render displays a loading page to connecting
  browsers while a service is spinning up», https://render.com/docs/free—, así que el visitante ve
  que algo está pasando y no un error. **Decirlo es mejor que disimularlo**: un revisor que entiende
  por qué tarda ve una decisión de ingeniería; uno que no, ve una app rota.

El artículo para revisores vive publicado en `claude.ai` y **el agente no lo puede republicar**: la
tarea entrega el texto exacto y el coordinador lo sube.

#### 6. La deuda que este despliegue crea, escrita con nombre

Va a «Límites declarados» del README, junto a las nueve que ya están. **Son dos, no tres**: la de
autenticación **ya está escrita ahí** desde la decisión 70 —«No hay autenticación, y por eso no se
despliega nada que reciba datos de una persona real. Una instancia pública de demostración es la
única excepción…»— y lo que corresponde es **enlazarle el link**, no duplicar el párrafo.

- No hay región sudamericana en el plan gratuito. La más cercana a Montevideo es Virginia, y eso le
  agrega latencia de red a un producto que ya arranca lento.
- Mantenerla despierta con un pinger **se evaluó y se descartó**: consumiría 730 de las 750 horas y,
  sobre todo, Render no documenta esa práctica como permitida. Este proyecto no apoya lo único que
  es público en un mecanismo que no verificó.

### Fuera

- **Cambiar el comportamiento del producto.** Esta tarea publica lo que `E10B` dejó. Si algo del
  producto hay que cambiar para que Render lo acepte, es **hallazgo y consulta**, no tarea.
- Autenticación, cuentas, o cualquier cosa que reciba datos de una persona real.
- Dominio propio, TLS a mano, CDN, observabilidad, alertas de disponibilidad.
- Segunda plataforma, o comparar plataformas de nuevo: la elección está tomada arriba.
- **El estado canónico**: `Coordination/Workboard.md`, `DesignAgent/Salvo-Progress.md`,
  `DesignAgent/Salvo-Overview.md` y el Blueprint los cierra el coordinador.

### Paths autorizados

- `README.md`, sección del link y «Límites declarados».
- `DesignAgent/Salvo-Getting-Started.md`, sección «El contenedor» y la de estado.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- Un archivo de configuración de despliegue en la raíz **solo si** Render lo admite y aporta algo que
  la consola no dé; si entra, entra documentado.
- `Dockerfile` y `scripts/contenedor-entrypoint.sh`, **solo** si la comprobación **1** —el puerto—
  encuentra un defecto que impide desplegar, y solo lo que ese defecto exija.
- `frontend/src/app/health/route.ts`, **solo** si la comprobación **3** —la sonda contra el sueño—
  encuentra un defecto.
- El comentario del `Dockerfile` que hoy dice «la sonda de la plataforma contra `/health`, que
  `E10C` configura»: si la decisión delegada termina en **no** configurarla, ese comentario queda
  falso y **hay que corregirlo**. Es edición de un comentario, no de comportamiento.

### Paths reservados por otros trabajos

Ninguno. No hay tareas en vuelo.

## Acciones autorizadas

- Ediciones locales permitidas: sí, en los paths de arriba.
- Instalación o actualización de dependencias: **no**.
- Escrituras externas: **no**. Ni cuentas, ni credenciales, ni aceptar términos, ni conectar el
  repositorio, ni disparar un despliegue. Todo eso lo hace el coordinador con un bloque que la tarea
  le prepara.
- Acciones destructivas: **no**. Ni borrar imágenes, ni contenedores, ni bases.
- Red: **solo lectura de documentación oficial de la plataforma** y, tras el alta, pedidos HTTP
  contra la instancia ya publicada para medirla.

## Criterios de aceptación

- [ ] La tabla de límites de Render rehecha con la documentación oficial **del día del despliegue**,
      con URL por fila y la fecha escrita.
- [ ] Las **cinco** comprobaciones del punto 2 contestadas, cada una con su evidencia, **incluidas
      las que se contesten «no se pudo verificar»**.
- [ ] El bloque de pasos para el coordinador, ejecutable tal cual, con la lista completa de variables
      de entorno y el motivo de cada una.
- [ ] La instancia responde en una URL pública, y un visitante sin instalar nada ve **23 alertas
      abiertas, 51 denegados sin alerta local y la explicación de `ORD_000011`**.
- [ ] El arranque en frío tras el sueño **medido desde afuera**, con fecha, y publicado como medición
      y no como estimación.
- [ ] El reinicio comprobado en la plataforma: ocurre, y la instancia vuelve con los datos puestos.
- [ ] El link y las cuatro propiedades —compartida, efímera, sintética, lenta la primera vez— en el
      README, en la guía y en el texto entregado para el artículo.
- [ ] Las **dos** deudas nuevas escritas en «Límites declarados», y el párrafo de autenticación que
      ya está ahí **enlazado al link, no duplicado**.
- [ ] `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes sobre la rama.
- [ ] `./scripts/check-docs.sh` verde. **Y dicho con precisión**: ese script descarta los enlaces con
      esquema, no toca la red, y lo dice en su propia cabecera. **Ningún script del proyecto comprueba
      que la URL pública funcione.** Lo comprueba la medición del punto 4, a mano y con fecha, igual
      que las cifras del corpus.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` | Verde. El README cambia, así que `check-docs.sh` es la parte que importa |
| `./scripts/smoke-ui.sh` | Verde, las 71 comprobaciones |
| `./scripts/contenedor.sh medir-instancia` | Corre en local y **se lee la salida**: imprime las cifras, no las afirma. Es para saber que la imagen que se sube es la que se midió |
| `curl -s -o /dev/null -w '%{time_total}' <URL>/alerts` tras 15 min de silencio | El número que se publica como arranque en frío |
| `curl -s <URL>/alerts`, leyendo el HTML | La cola muestra **23 alertas abiertas** |
| `curl -s <URL>/dashboard`, leyendo el HTML | El panel muestra **51 denegados por el proveedor sin alerta local** |
| La alerta de `ORD_000011`, en el navegador | La explicación está escrita |
| `curl -s -o /dev/null -w '%{http_code}' <URL>/api/dashboard` | **404**, y es un acierto: la API no se publica |
| Agotar el limitador desde una red y probar desde otra —el wifi y el teléfono— | Si la comprobación 5 dio que sí, la segunda red no está limitada |
| El cartel en pantalla | Dice compartida, efímera, y el tope de antigüedad que la variable programa |
| El reinicio, por el método del punto 4 | La nota de revisión desaparece y las 23 alertas vuelven |

**Todo se comprueba por páginas, y hay que decir por qué**: la API escucha en `127.0.0.1:5100`, el
rewrite que la publicaba se borró en `E10A` (decisión D2) y no hay Route Handlers bajo
`frontend/src/app/api`. `contenedor.sh medir-instancia` puede leerla porque entra por `docker exec`,
y el plan Free de Render **no da consola ni SSH**. Una tabla de verificación que le pidiera datos a
`<URL>/api/...` sería imposible de ejecutar — **la primera versión de este brief la tenía así**, y
la cazó el `brief-check`.

## Decisiones delegadas

- La región dentro de las que el Free ofrece, con su motivo escrito.
- Desplegar desde el Dockerfile del repositorio o desde imagen construida, con el motivo escrito.
- La redacción exacta de las cuatro propiedades en cada uno de los tres documentos, mientras diga lo
  mismo en los tres.
- Configurar o no el *health check path*, **según lo que conteste la comprobación 3**; ante la duda,
  no configurarlo.

## Detenerse y consultar si

- una comprobación del punto 2 da un resultado que **exige cambiar el producto** para desplegar;
- Render pide tarjeta en algún paso: **eso invalida el criterio con el que se la eligió** y la
  decisión vuelve al coordinador;
- la construcción de la imagen no entra en el cupo de minutos, o lo consume de una manera que deje al
  mes sin margen;
- el arranque en frío medido desde afuera resulta tan malo que publicar el link haga más daño que
  bien: esa es una decisión del coordinador y no de la tarea;
- aparece cualquier paso que exija crear una cuenta, ingresar una credencial o aceptar términos.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados, **con las cifras medidas y su fecha**.
- La tabla de límites oficiales rehecha, con URL por fila.
- Las cinco comprobaciones del punto 2, contestadas o declaradas no verificables.
- El bloque de pasos para el coordinador.
- El texto exacto para el artículo de revisores.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
