# Salvo — Revisión adversarial del diseño propuesto de Etapa 10

> Estado: revisión, sin cambios sobre el estado canónico
> Fecha: 2026-09-08
> Revisor: Claude (Fable 5.1)
> Objeto: `Coordination/Tasks/E10-DISENO.md` v1 (commit `1d3b398`, base `main` en `c285536`)
> Método: lectura del diseño, del Blueprint (§1, §2, §3, §4.4, §6, §9, §10, §11, §12 y la bitácora
> completa), Overview, Progress, Workboard con sus lecciones, Getting-Started, Portability, el
> README entero, las revisiones adversariales de E8 y E9 y el handoff de `E9D`; del código real en
> `frontend/next.config.ts`, `frontend/src/lib/api/**`, `frontend/src/app/layout.tsx`,
> `frontend/src/components/console-header.tsx`, los dos diccionarios, `Salvo.Api/*Endpoints.cs`,
> `Program.cs`, `appsettings*.json`, `Salvo.Infrastructure/DependencyInjection.cs`,
> `EfScoringRunStore.cs`, `EfRiskOrderReader.cs`, `RunScoringHandler.cs`,
> `SeedDemoOrdersHandler.cs`, `Order.cs`, `RuleConfig.cs`, las migraciones, `.env.example`,
> `.gitignore`, `scripts/*.sh`, los tres `csproj` y `Directory.Packages.props`; de la documentación
> de Next.js 16.3.3 que viene dentro de `frontend/node_modules/next/dist/docs/`; y de **tres
> páginas públicas de plataformas**, leídas el 2026-09-08 y citadas al final con su URL, porque
> D8 exige que toda cifra de plataforma lleve fecha y fuente. Se comprobó qué hay instalado en la
> máquina (`docker`, `podman`, `colima`, `nerdctl`). No se ejecutó ninguna compuerta ni ningún test,
> no se hizo push y no se editó ningún archivo salvo este informe.

El diseño acierta en lo grande: **el reinicio como reemplazo del aislamiento** es la lectura
correcta de la decisión 8, y decir que sin autenticación no se despliega nada que reciba datos de
una persona real es la mitad que importa. Lo que sigue son trece hallazgos, cinco de severidad
alta, y casi todos comparten un patrón: **el diseño da por resuelto por topología lo que la
topología de este repositorio hace al revés**, y da por medible en esta máquina lo que esta máquina
no puede medir.

Una cosa antes de empezar, porque reordena D2, D5 y D8 a la vez: **los tres «hechos verificados» de
la sección inicial son ciertos, y de los tres se deduce lo contrario de lo que el diseño deduce.**
El rewrite existe, y es lo que publica la API entera. `DemoData:Enabled` es `false` por defecto, y
encenderlo publica las rutas de sembrado y de disparo a cualquiera que hable HTTP. Y los límites de
cuerpo existen en dos endpoints de doce.

---

## Hallazgos, por severidad

### 1. D2, alta. El rewrite no esconde la API: la publica entera, con OpenAPI incluido, y la consola no lo usa para nada

D2 dice: «la API no es alcanzable desde afuera. Ni el seed, ni el scoring, ni los callbacks, ni las
métricas. Todo lo que un visitante puede tocar pasa por el servidor de Next». El primer hecho
verificado del diseño es que «el frontend ya proxea `/api/*` al backend».

Los dos enunciados no pueden ser ciertos a la vez, y el segundo es el que lo es.
`frontend/next.config.ts:12-13` reescribe `source: "/api/:path*"` a `destination:
"${apiBaseUrl}/:path*"`. Es un proxy transparente, sin lista de rutas, sin método, sin cabecera:
`GET https://<instancia>/api/api/orders` llega a `GET /api/orders` de Kestrel, y lo mismo
`POST /api/api/demo-data/seed`, `POST /api/api/risk-evaluations:run`,
`POST /api/api/external-callbacks/external-mock`, `GET /api/api/evaluation-metrics` y
`GET /api/openapi/v1.json`, que `Program.cs:38` mapea sin condición de entorno. Con
`DemoData:Enabled=true` (D4), las dos rutas de demostración de `ExternalDemoEndpoints.cs:36-61`
también. **Todo lo que D2 dice que no es alcanzable es alcanzable por la URL pública, con un prefijo
`/api` de más.** Buena parte del modelo de amenaza no se resuelve por topología: se abre por ella.

Lo que hace esto peor de lo que parece es que **la consola no usa el rewrite**. Lo dice el propio
cliente: `frontend/src/lib/api/server-client.ts:12-14` —«the `/api/:path*` rewrite in
`next.config.ts` only exists for requests that reach the Next server from a browser»—. Y ninguna
petición llega desde un navegador: no hay un solo `fetch` en los ocho componentes cliente, y las
cuatro mutaciones son Server Actions (`review-action.ts`, `explanation-action.ts`,
`external-action.ts`, `import/actions.ts`) que llaman al cliente `server-only` con URL absoluta.
El rewrite es un resto de la Etapa 1, cuando `/api/health` lo atravesaba (`Salvo-Progress.md:133`);
el archivo que lo usaba se borró en `E5C` (`Coordination/Handoffs/Claude.md:997`). Hoy su única
función es exponer la API.

Consecuencias concretas para el resto del diseño:

- **D5 pierde su premisa.** Un limitador de tasa en las Server Actions no ve el tráfico que entra
  por el rewrite; un limitador en la API no distingue visitantes (hallazgo 5). El bucle de
  «pedir scoring 300 veces» del diseño no necesita la consola: es un `curl` a
  `/api/api/risk-evaluations:run`.
- **La condición de D1 «no puede contener otros datos»** se debilita más: `POST
  /api/api/order-imports` acepta cualquier archivo de 5 MiB sin pasar por el formulario, y `GET
  /api/api/orders` devuelve todo lo que cualquiera haya importado (`ListOrdersResult.cs:14-26`).
- **El endpoint de callback está expuesto y responde `401` a todo** porque el secreto está vacío
  (`ExternalCallbackEndpoints.cs:154-159`). Es la única ruta que la topología no protege y el
  código sí. Está bien, pero hay que decirlo así y no al revés.

Lo que la v2 tiene que decidir, y es una decisión de una línea: **quitar el rewrite en la
instancia pública, o acotarlo a exactamente lo que se quiere publicar.** La única cosa para la que
un rewrite sirve hoy es una sonda de salud que refleje a la API (hallazgo 7): `source:
"/api/health"` → `/health`, y nada más. Todo lo demás de D2 —un origen, sin CORS, la API en
`127.0.0.1`— es correcto y se conserva. Y el argumento de D2 se reescribe: la API no se publica
**porque nadie la reescribe hacia afuera**, no porque Next esté delante.

Una nota sobre cuándo se fija el destino: `demo.sh` y `smoke-ui.sh` pasan `SALVO_API_BASE_URL`
tanto al `build` como al `start`, y con razón: la documentación de Next no lo dice en la página de
`rewrites`, y conviene que `E10A` lo verifique en vez de asumirlo. Si el rewrite se quita, la
pregunta desaparece.

### 2. D8 y D3, alta. Las tres candidatas duermen; el «reinicio gratis» es por inactividad y no por horario; y el arranque en frío es lo que espera el primer visitante

D8 presenta a Koyeb, Render y SnapDeploy como tres candidatas y a Render como la que «duerme a los
15 minutos», con la implicación de que las otras dos no. D3 propone como alternativa a programar un
reloj que «si el contenedor de la plataforma elegida se duerme y arranca de cero, el reinicio ya
existe». Y D6 promete un cartel que dice **cuándo** es el próximo reinicio.

Leídas las páginas oficiales el 2026-09-08:

| Plataforma | Qué dice su documentación | Fuente |
| --- | --- | --- |
| Koyeb | «Each Free Instance provides 512MB of RAM, 0.1 vCPU, and 2GB of SSD.» «They scale down to zero when they don't receive any traffic for 1 hour.» «Each organization is limited to one Free Instance.» No dice que el scale-to-zero se pueda apagar en la gratuita | `koyeb.com/docs/reference/instances` |
| Render | «Render spins down a Free web service that goes 15 minutes without receiving any inbound traffic.» «This process takes about one minute.» Sistema de archivos efímero. «750 Free instance hours» por workspace y mes | `render.com/docs/free` |
| SnapDeploy | «Auto-sleep when idle», «Auto-wake on traffic (~60s)», sin cifras de RAM ni CPU en la página. Producto de AAR Labs | `snapdeploy.dev` |

Tres cosas se siguen de la tabla, y las tres cambian el diseño:

- **La diferencia que D8 dibuja no existe.** Las tres duermen; cambia solo el umbral —una hora,
  quince minutos, «idle»— y la duración del despertar. Elegir plataforma por «cuál no duerme» no es
  una elección disponible en el tier gratuito. Lo que sí se puede elegir es **cuánto tarda en
  despertar**, y eso no lo publica ninguna: se mide (hallazgo 3).
- **El reinicio por sueño no es un reinicio por horario.** Ocurre cuando nadie usa la instancia, y
  no ocurre mientras alguien la use. Una instancia con un visitante cada cuarenta minutos en Koyeb
  no se reinicia nunca sola. Así que la «alternativa» de D3 no reemplaza al reloj: **se suma**. Y
  el «cuándo» de D6 no puede leerse del sueño, porque el sueño no tiene hora.
- **El arranque en frío es la experiencia del primer visitante después de cada silencio.** Y en
  este diseño el arranque no es solo levantar dos runtimes con 0,1 vCPU: es migrar, sembrar 300
  pedidos y correr el scoring (D3). El frontend le da al scoring 120 s y al sembrado 60 s
  (`console.ts:41-42`) cuando los pide la consola; en el arranque no hay nadie que espere con
  tiempo límite, pero sí hay una plataforma que hace *health checks* y un visitante con la pestaña
  abierta. Ese número es el que decide si el link de entrevista sirve, y el diseño lo lista en
  `E10A` sin conectarlo con que **lo paga cada visitante que llegue después de una hora de
  quietud**.

Hay además una inconsistencia interna: D5 usa «0,1 vCPU» como dato para dimensionar el
limitador, y D8 dice que ninguna cifra de plataforma se usa sin verificar. El 0,1 vCPU es de Koyeb
—verificado arriba— y el diseño no lo atribuye. Si la plataforma termina siendo otra, D5 está
dimensionado contra la equivocada.

La recomendación es una sola primitiva, coherente con las tres restricciones: **el reinicio es un
arranque del proceso**. Al arrancar, la API recrea la base, siembra y puntúa, siempre. Un
temporizador dentro del proceso lo termina a la hora, y el supervisor del contenedor (hallazgo 7)
lo vuelve a levantar. El sueño de la plataforma es entonces un arranque más, ni mejor ni peor. Y
el «cuándo» del cartel se vuelve determinista: instante de arranque más período, publicado por la
API (hallazgo 8). Lo que **no** se puede prometer con ese diseño, y el cartel tiene que decir con
esas palabras, es que el reinicio ocurra **solo** a la hora: puede ocurrir antes si la plataforma
durmió la instancia.

### 3. D7 y partición, alta. No hay ningún runtime de contenedores en esta máquina, así que `E10A` no puede medir «todo local» como está escrito; y falta decir qué se mide contra qué

`E10A` es «la tarea que decide si el resto es posible», y D7 dice que se mide «localmente antes de
elegir plataforma, con el contenedor limitado a la mano». En la máquina no está instalado `docker`,
ni `podman`, ni `colima`, ni `nerdctl`: los cuatro devuelven *not found*. Es el mismo hallazgo que
la revisión de E8 hizo con Chrome: **la precondición de la tarea no se cumple donde la tarea va a
correr**, y el diseño no la lista.

Tres salidas, y la v2 elige una con motivo:

- Instalar un runtime (Docker Desktop, o Colima con Lima, que es más liviano). Es una dependencia
  nueva y por la decisión 61 se aprueba por nombre y motivo en el diseño, aunque no entre en el
  árbol. Con Docker en macOS el límite `--memory=512m --cpus=0.1` se aplica dentro de una VM Linux,
  que es justamente lo que se quiere medir.
- Medir **sin contenedor**: los dos procesos a mano con un `ulimit -v` y un `cpulimit`, que es una
  aproximación y hay que declararla como tal.
- Medir **en la plataforma**, en una instancia gratuita de prueba, con el `Dockerfile` recién
  hecho. Es la medición más real y contradice el orden de la partición, que pone la plataforma en
  `E10C`. Puede ser lo correcto: el alta gratuita no compromete nada, y una medición en 0,1 vCPU
  compartidos de verdad vale más que una en una fracción de M-series.

Y falta decir contra qué se mide. El diseño pide «RAM en reposo, RAM bajo la corrida, tiempo del
primer render, arranque en frío». Los umbrales ya existen en el código y son los que deberían
figurar como criterio de aceptación de `E10A`:

| Medición | Umbral que ya existe | Dónde |
| --- | --- | --- |
| Sembrar 300 pedidos | 60 s | `console.ts:41` (`IMPORT_TIMEOUT_MS`) |
| Correr el scoring del corpus | 120 s | `console.ts:42` (`SCORING_TIMEOUT_MS`) |
| Cualquier lectura de página | 5 s | `server-client.ts:22` (`DEFAULT_TIMEOUT_MS`) |
| Arranque hasta `/health` | lo que tolere el *health check* de la plataforma | a confirmar en `E10C` |
| RAM total | 512 MB, con los dos runtimes y la corrida en curso | Koyeb, verificado |

Dos perillas que la medición tiene que probar encendidas y apagadas, porque cambian el número más
que cualquier otra cosa: el proyecto web de .NET usa **GC de servidor** por defecto
(`Microsoft.NET.Sdk.Web`), que reserva más memoria; `DOTNET_gcServer=0` o
`<ServerGarbageCollection>false</ServerGarbageCollection>` lo apagan. Y el `next start` sin
`output: 'standalone'` arranca sobre `node_modules` entero; la documentación de Next 16 que viene
en el paquete (`.../next-config-js/output.md:24-47`) documenta `standalone` como «a folder that
copies only the necessary files for a production deployment» con un `server.js` mínimo. Reduce la
imagen, y hay que medir si reduce la memoria.

### 4. D1, alta. «Contiene únicamente datos sintéticos y no puede contener otros» no es una propiedad que el software pueda garantizar, y la instancia pública es una superficie de escritura anónima

La primera de las tres condiciones de la decisión 70 dice que la instancia «contiene únicamente
datos sintéticos **y no puede contener otros**». El código dice otra cosa. `POST /api/order-imports`
acepta de cualquier visitante un CSV o JSON de hasta 5 MiB y 10.000 registros
(`OrderEndpoints.cs:10`, `ImportOrdersHandler.cs:13`), con identificadores de hasta 64 caracteres y
ciudad de hasta 80 (`Order.cs:10-11`), en tantos archivos como quiera. `POST /api/alerts/{id}/review`
acepta una nota de 2.000 caracteres (`AlertEndpoints.cs:9`). Ninguno de los dos tiene autor, y
todo lo que entra lo ve el siguiente visitante en `/alerts`, en el detalle, y por `GET /api/orders`
si el rewrite sigue (hallazgo 1).

Es decir: **la instancia puede contener lo que cualquiera pegue**, un nombre real en
`buyerReferenceId`, una nota con un insulto, una URL. Que se borre a la hora acota el daño; no
vuelve cierta la frase. Y la frase importa porque es una **condición** de la decisión que supersede
a la 8: si una de las tres no se cumple, la decisión no autoriza el despliegue.

Lo que conviene hacer, en dos partes:

- **Reescribir la condición para que sea verificable.** Algo como: «el producto no pide, no
  requiere y no espera ningún dato de una persona real; todo lo que un visitante escribe es
  anónimo, visible para los demás, y desaparece en el próximo reinicio». Eso sí es cierto, y es lo
  que un visitante necesita saber. La retención —una hora como máximo— pasa a ser una afirmación
  del README y del cartel, no una consecuencia implícita.
- **Decidir si la importación y la nota de revisión quedan abiertas en la instancia pública.** Es
  una decisión de producto, no técnica. A favor de dejarlas: la importación es una de las seis
  capturas y la mitad del guion, y sin nota la revisión pierde su registro. En contra: son las dos
  únicas entradas de texto libre, y ambas son anónimas. Si se dejan, D5 tiene que acotarlas
  (hallazgo 5) y el cartel tiene que nombrarlas. Si se cierran, hace falta una bandera nueva de la
  API, publicada en `capabilities` como `DemoDataEnabled`, para que la consola no ofrezca el
  formulario. Cualquiera de las dos es defendible; lo que no es defendible es no elegir.

Y una precisión de seguridad que no está en el diseño y conviene escribir en la decisión 70 porque
es la razón por la que esto es aceptable: **nada de lo que un visitante escribe entra jamás a la
entrada de un modelo** (decisión 54). Si algún día Anthropic redacta las explicaciones de la
instancia pública, la nota anónima de un visitante no llega al prompt. Hoy eso es una propiedad
del código; la instancia pública es la primera vez que importa contra un adversario.

### 5. D5, alta. El limitador que .NET trae no puede ver visitantes en esta topología, y el costo que hay que acotar es función del estado, no de la tasa

D5 dice que «.NET trae limitación de tasa en el framework» y que los números «salen de medir».
Cierto lo primero (`Microsoft.AspNetCore.RateLimiting`, sin dependencia). Pero el diseño no
pregunta **qué ve** ese limitador, y en la topología de D2 ve una sola cosa: `127.0.0.1`.

- Toda petición que la consola hace a la API sale del proceso de Next. `server-client.ts:82-87`
  manda `Content-Type` y nada más: **ni `X-Forwarded-For`, ni ningún identificador del
  visitante.** Para la API, todos los visitantes son el mismo cliente. Un limitador por IP en la API
  limita a la consola entera como un solo usuario, que es lo mismo que un limitador global.
- Un limitador global **es** honesto y puede ser suficiente para lo que D5 quiere —que un bucle no
  tire la instancia—, pero hay que decidirlo así y no llamarlo «por visitante».
- Si se quiere por visitante, hay dos lugares: la API, si `server-client.ts` empieza a propagar una
  cabecera con la IP que la plataforma pone en `X-Forwarded-For` (y las Server Actions la leen de
  `headers()`), o **el borde de Next**, que en la versión 16 se llama `proxy.ts` (la documentación
  del paquete: «Starting with Next.js 16, Middleware is now called Proxy»). El segundo ve la IP
  real y protege también las páginas, que son la mayoría del costo con 0,1 vCPU: cada render llama
  a la API dos o tres veces (`layout.tsx` pide `capabilities`, cada página pide lo suyo).
- Y si el rewrite sigue (hallazgo 1), ningún limitador de Server Actions lo cubre: el `curl`
  directo entra por otra puerta.

El segundo problema es de forma, no de lugar. **El costo de una corrida no depende de cuántas
corridas se pidan sino de cuántos pedidos haya.** `RunScoringHandler.cs:26-28` carga **todos** los
pedidos en memoria (`EfRiskOrderReader.cs:12-17`, `ToListAsync` sin paginar) y el motor recorre
listas por comercio y por comprador para cada uno (`TemporalRiskEngine.cs:297-298`). Con 300
pedidos es nada; con las cien importaciones de 10.000 registros que un visitante puede hacer en una
hora sin que ningún limitador de tasa razonable lo frene, son un millón de pedidos en memoria y una
corrida que no termina en 120 s ni en 512 MB. Un límite de tasa no acota eso; **lo acota un tope
de pedidos por instancia**, que hoy no existe y que la instancia pública necesita: un
`MaximumOrders` por encima del cual la importación responde `409` con un código propio en
`messages.ts` (decisión 57) y una frase en los dos diccionarios.

Tres cosas más que D5 debería nombrar porque son las que un visitante concurrente **va** a ver:

- Dos corridas simultáneas: la segunda muere en `SaveRunAsync` por violación de unicidad y sale como
  `ScoringRunConflictException` (`EfScoringRunStore.cs:93-116`, `RiskEvaluationEndpoints.cs:26`).
  Correcto, y en una instancia compartida es un mensaje que alguien lee sin haber hecho nada mal.
- Dos revisiones de la misma alerta: `409` por el token de concurrencia (decisión 34). Ídem.
- El sembrado sobre una base ya sembrada: `200` sin escribir nada, o `409` si alguien importó
  pedidos que colisionan (`OrderEndpoints.cs:167-175`). En una instancia pública, el «alguien» es
  otro visitante.

Nada de eso es un defecto; es lo que «compartida» significa en la práctica, y el cartel de D6 tiene
que cubrirlo con una frase, no solo el veredicto.

### 6. D3, media. Recrear la base no es «sembrar de nuevo», borrar el archivo bajo conexiones agrupadas no es gratis, y el arranque necesita el huso horario en la imagen

Tres cosas del reinicio que el diseño deja en «hay que medir» y que se pueden resolver ahora
leyendo el código:

**Sembrar no reinicia.** El sembrado es idempotente y **se niega** ante una base con pedidos que
colisionan (`SeedDemoOrdersHandler.cs:23-25`, `OrderEndpoints.cs:167`); `demo.sh:6-8` lo explica:
«volver a sembrar no devuelve nada a cero». El veredicto es terminal, la explicación escrita no se
regenera. Así que el reinicio de D3 tiene que **recrear la base**, y el diseño lo dice bien; lo que
no dice es que la única forma de recrearla desde el proceso es borrar el archivo o vaciar las
tablas, y el proyecto tiene una postura fuerte sobre borrar: «la aplicación nunca la borra»
(`Salvo-Getting-Started.md:135`), `demo.sh` y `capturas.sh` se imponen «no borra nada». La
instancia pública es la primera vez que **el producto** borra una base. Es correcto que lo haga
—es su razón de ser— y por eso la decisión 70 tiene que decirlo con esas palabras, detrás de una
bandera que **solo** la instancia pública enciende y que `capabilities` publica, para que ningún
despliegue local pueda encenderla sin saberlo.

**El archivo bajo conexiones agrupadas.** `Microsoft.Data.Sqlite` 10.0.11
(`Directory.Packages.props`) agrupa conexiones por defecto desde la versión 6. Si el reinicio borra
`salvo.db` mientras el proceso vive, en Linux el `unlink` ocurre pero las conexiones del grupo
siguen apuntando al inodo viejo: las peticiones siguientes escriben en un archivo que ya no tiene
nombre, y la base «nueva» que `Migrate()` crea queda vacía al lado. Hace falta
`SqliteConnection.ClearAllPools()` antes de borrar, y una compuerta que responda `503` con
`Retry-After` mientras dura. **O evitar todo eso reiniciando el proceso** (hallazgo 2), que es más
simple y no tiene esta esquina: el proceso nuevo abre un archivo nuevo. Hay que verificar el
comportamiento del agrupamiento con la versión exacta; conviene que `E10B` lo haga con un test de
integración antes de elegir.

**Migrar al arrancar.** Las dos opciones de D3 son válidas y **ninguna necesita el SDK en la
imagen**: la redacción actual —«un script SQL idempotente… que no necesita el SDK»— sugiere que
`Database.Migrate()` sí lo necesita, y no: las migraciones están compiladas en
`Salvo.Infrastructure` (siete, de `20260831151027` a `20260907150822`). Hay una tercera,
`dotnet ef migrations bundle`, que produce un ejecutable autónomo; con SQLite en el mismo
contenedor no aporta nada sobre `Migrate()`. La elección con motivo: `Migrate()` detrás de la misma
bandera del reinicio, porque **es el único camino que la compuerta ya vigila**
(`check.sh:24`, `has-pending-model-changes`), y un script SQL generado sería un segundo artefacto
que puede divergir del modelo sin que nada lo detecte. Y una nota que la v2 debe recoger: el
`Migrate()` al arrancar en desarrollo local **no** se enciende, porque el proyecto dice que «la API
no aplica migraciones» y `Getting-Started` lo repite; es una conducta de la instancia pública.

**El huso horario en la imagen.** `RuleConfig.cs:28` resuelve `America/Montevideo` con
`TimeZoneInfo.FindSystemTimeZoneById` en la inicialización estática, y `Program.cs:22-25` valida
`RuleConfig.Known` antes de construir el host. En una imagen sin `tzdata` —las variantes Alpine de
las imágenes de .NET no lo traen— **la API no arranca**, con `TimeZoneNotFoundException`. Es un
fallo cerrado y por eso está bien; pero hay que saberlo antes de elegir imagen base, y el
`Dockerfile` de `E10A` tiene que dejarlo escrito. La globalización invariante, en cambio, no
afecta: todo el formato numérico del proyecto es `CultureInfo.InvariantCulture` (`grep` sobre
`backend/src`, sin una sola cultura regional).

### 7. D2, media. Dos procesos en un contenedor necesitan un supervisor, y la sonda de salud de la plataforma va a ver sana una consola con la API muerta

D2 pone los dos procesos en un contenedor y no dice quién los cuida. Sin un supervisor, si la API
muere el contenedor sigue vivo: Next atiende, `layout.tsx:31` cae al idioma por defecto cuando
`capabilities` falla —a propósito, `console.ts:83-89`—, y cada página renderiza con el aviso de «no
se pudo contactar a la API». La plataforma, que sondea el puerto público, ve `200`. **El único
`200` que refleja a la API es `/api/health` a través del rewrite**, que hoy va a `/health`. Es la
única razón para conservar un rewrite, acotado a esa ruta (hallazgo 1), o para escribir un Route
Handler de salud que llame al cliente `server-only`.

El patrón ya existe en el repositorio: `demo.sh:238-242` sondea los dos PIDs y **sale si cualquiera
muere**, con el argumento escrito —«la consola sin API no es una demo, es la pantalla de “No se pudo
contactar a la API”»—. El `Dockerfile` de `E10A` necesita exactamente eso como `ENTRYPOINT`: un
script que levante la API, espere `/health`, levante Next, y termine el contenedor si cualquiera de
los dos termina. Con el hallazgo 2, ese mismo script es el que hace del reinicio un arranque.

Cuatro detalles de la misma tarea que conviene fijar en el brief y no descubrir:

- La plataforma inyecta `PORT`; `next start` lo respeta y Kestrel no lo necesita porque escucha en
  `127.0.0.1:5100` fijo (`.env.example:1`).
- `ASPNETCORE_ENVIRONMENT` no estará puesto, así que `appsettings.Development.json` —que es donde
  `DemoData:Enabled` vale `true`— **no se carga**. `DemoData__Enabled=true` va por variable, como
  hace `demo.sh:175`. El diseño lo tiene en D4; el brief debe decir el mecanismo.
- Las Server Actions comparan `Origin` con `Host` o `X-Forwarded-Host` (documentación del paquete,
  `guides/server-actions.md:82`). Si el proxy de la plataforma reescribe `Host`, hace falta
  `serverActions.allowedOrigins` con el dominio público. Es una comprobación de `E10C`, y si falla
  lo que se rompe es **toda mutación de la consola**, con la lectura intacta.
- `AllowedHosts: "*"` en `appsettings.json` está bien para una API que solo escucha en loopback.

### 8. D6, media. El «cuándo» del cartel es un cambio de contrato con todo lo que eso arrastra, y la cabecera ya dice «uso local» en los dos idiomas

D6 promete que el cartel «dice cuándo es el próximo reinicio». Por la decisión 66 hay un solo
lector de la configuración y es la API, así que el instante lo publica la API y la consola lo toma
de `GET /api/system/capabilities`, como hace con el idioma. Eso es un **campo nuevo en
`CapabilitiesResponse`** (`SystemEndpoints.cs:66-69`), y el Workboard tiene escrita la lección
tres veces: «si una tarea de backend cambia el contrato, cambia también la guarda que lo consume»
(`Workboard.md:45`). La reserva de paths de `E10B` tiene que incluir, además del cartel: la
recaptura de OpenAPI (`frontend/openapi/salvo-openapi.json` y `schema.d.ts`), `contract.ts:90`
—donde `Capabilities` ya es un `Omit` sobre el tipo generado—, `projectCapabilities` en
`guards.ts:787`, `fixtures.ts`, `boundary.test.ts` y el test que afirma que `capabilities` se pide
exactamente una vez por render (`console.ts:62-65` lo cita). El diseño no reserva ninguno.

Tres decisiones del cartel que la v2 debe tomar:

- **Qué instante y en qué forma.** Un instante absoluto necesita un huso para leerse, y el del
  visitante no se conoce en el servidor. Lo honesto es publicar `nextResetAt` en UTC y que la
  consola lo escriba **relativo** («en 41 minutos») en el render, que es dinámico por
  `force-dynamic`. Con el hallazgo 2, además, el cartel dice «o antes, si nadie usa la instancia».
- **Dónde va.** `ConsoleHeader` (`console-header.tsx:42`) ya renderiza un aviso permanente:
  `t.nav.disclaimer`, que hoy dice **«Datos sintéticos · sin autenticación · uso local»** en
  `es.ts:35` y «uso local» también en `pt.ts:36`. En la instancia pública ese texto es falso en su
  tercera palabra. El diseño no lo menciona; `E10B` tiene que reemplazarlo o condicionarlo, porque
  un cartel nuevo al lado de un aviso viejo que dice lo contrario es peor que ninguno. Y un cartel
  con un dato que cambia no puede ser una clave estática del diccionario: es una **función** del
  instante, como las frases con cifra que `dictionary.ts:19-24` ya describe.
- **Si el cartel es también un `role="status"` o `aria-live`.** Un reinicio inminente es
  información que un lector de pantalla debería anunciar; `E9C2` ya introdujo una región viva para
  el veredicto. Alcance de `E10B`, y hay que decidirlo antes para que la lista de reglas de
  accesibilidad de la compuerta lo cubra.

### 9. Estado canónico, media. La decisión 8 vive en seis lugares y el diseño lista dos; y el orden de las etapas dice otra cosa

La lista de «Cambios de estado canónico» tiene cinco entradas: dos del Blueprint, dos del README,
y «Workboard y Progress». La afirmación que la etapa deroga está escrita, con distintas palabras,
en estos lugares, y `check-docs.sh` no puede detectar ninguno porque es prosa:

| Dónde | Dice | Quién lo corrige |
| --- | --- | --- |
| `Salvo-Blueprint.md:783` (decisión 8) | «Sin auth, la aplicación permanece local» | Listado |
| `Salvo-Blueprint.md:650` (§10) | «Sin autenticación, la app es solo local y no se publica con rutas mutables abiertas» | **No listado** |
| `Salvo-Blueprint.md:86, 89` (§2 Diferido) | «Callback público en Internet», «PostgreSQL, despliegue y CI remoto» | **No listado**: el despliegue deja de ser diferido; el callback público sigue siéndolo y hay que separarlos |
| `Salvo-Blueprint.md:755-758` (§11) | «Post-MVP — Koin sandbox, auth, observabilidad y deploy» | **No listado**: la Etapa 10 necesita su sección, con el orden obligatorio y la verificación, como las nueve anteriores |
| `AGENTS.md:114` | «Sin autenticación, la aplicación es solo local y no se despliega con rutas mutables públicas» | **No listado**, y es una **decisión invariante** de `AGENTS.md`: el agente que ejecute `E10B` está obligado a detenerse ante la contradicción |
| `AGENTS.md:57` | «Etapa 9 en ejecución, la última» | **No listado**; ya está desfasado hoy |
| `README.md:425` | «No hay autenticación, y por eso la aplicación es local y no se despliega con rutas mutables» | Listado (punto 3) |
| `README.md:44-66` | La tabla «Simulación, sandbox y producción» no tiene fila para una instancia pública con mock | **No listado**; el §12 exige que el README distinga los tres modos, y la instancia pública es un cuarto que hay que nombrar |
| `Salvo-Overview.md:50, 74-83` | «Fuera del MVP: publicación con rutas mutables»; «Próximo paso: … despliegue» | **No listado**; el Workboard (`:126`) dice que el Overview es uno de los cuatro lugares fijos que siempre se olvidan |
| `Salvo-Getting-Started.md:139` | «La app sin autenticación permanece local» | **No listado** |
| `frontend/src/lib/i18n/es.ts:35`, `pt.ts:36` | «uso local» en la cabecera de todas las pantallas | **No listado**; hallazgo 8 |

Dos observaciones más sobre esta sección:

- **«La 8 se marca como superada, no se borra»** no tiene precedente en la bitácora. El precedente
  es la decisión 65, que dice de la 19: «sigue vigente en lo que fija —300 pedidos, 300 etiquetas,
  fixture explícita— y deja de fijar cuántos fraudes». La forma del proyecto es que la decisión
  nueva diga **qué conserva** de la vieja —el motivo— y **qué deja de fijar** —«local»—, sin editar
  la fila de la 8. La numeración es correcta: la última es la 69.
- El brief de `E10B` va a heredar la contradicción con `AGENTS.md:114` y por regla del propio
  `AGENTS.md` («si el task brief las contradice, detenerse y reportar») **no puede empezar** hasta
  que el coordinador corrija esa línea. Es del coordinador, antes del despacho, no de la tarea.

### 10. D4, baja. Resistió, y la pregunta abierta tiene respuesta: el disparo de demostración no pasa por el endpoint de callback, así que el secreto queda vacío

D4 deja para la tarea «verificar qué camino usa el disparo de demostración». Verificado:
`ExternalDemoEndpoints.cs:64-77` inyecta `DeliverPendingCallbacksHandler` y llama al caso de uso
directamente; el comentario del propio archivo (`:13-18`) explica por qué no pasa por HTTP: «calling
the authenticated callback endpoint through the Next rewrite would put the shared secret in the
browser». Con `SALVO_CALLBACK_SHARED_SECRET` vacío, `POST /api/external-callbacks/{provider}`
responde `401` a todo (`ExternalCallbackEndpoints.cs:154-159`) y el disparo de la demo funciona
igual. **Se deja vacío y se explica**, como el diseño anticipa. La v2 puede cerrar la pregunta.

### 11. Hechos iniciales, baja. «Los límites de cuerpo ya existen» vale para dos endpoints de doce

El tercer hecho verificado dice que los límites de cuerpo existen: 5 MiB en la importación
(`OrderEndpoints.cs:10`; el diseño escribe «5 MB» y son 5 MiB, `5 * 1024 * 1024`) y 8 KiB en el
callback (`ExternalCallbackEndpoints.cs:25`). Los otros diez endpoints con cuerpo JSON —revisión,
explicación, evaluación externa, reconciliación, los dos de demostración— no fijan nada y heredan
el tope por defecto de Kestrel, 30.000.000 bytes. La nota de revisión se limita a 2.000 caracteres
**después** de deserializar un cuerpo que puede pesar 30 MB. Es un tope bajo global que `E10B`
puede fijar en una línea (`KestrelServerLimits.MaxRequestBodySize`) sin tocar ningún endpoint, y
conviene que el diseño no lo dé por existente.

### 12. Partición, baja. `E10C` promete dejar el link «en el artículo para revisores», que no está en el repositorio

`E10C` dice que el link va «en el README, en el artículo para revisores y en
`Salvo-Getting-Started.md`». El artículo **vive fuera del repositorio**: el brief de `E9D`
(`E9D-CIERRE.md:148-153`) lo dice —«no está en el árbol, ni en los handoffs, ni en `_local/`»— y el
handoff lo escribió de cero para que el coordinador lo publique
(`Coordination/Handoffs/Claude.md:4123-4126`). Es el hallazgo 6 de la revisión de E8, otra vez. La
tarea no puede editar el artículo; puede entregar el párrafo que lo agregue, y el coordinador lo
publica. El brief lo tiene que decir así, o `check-docs.sh` no lo va a detectar y el criterio de
aceptación quedará como cumplido sin que nadie lo haya hecho.

### 13. D3, baja. «Una hora parece razonable» se justifica con lo que ya se sabe

D3 pide justificar la cadencia en vez de asumirla, y hay tres datos que la fijan sin medir nada:
el guion de demo dura diez minutos (`docs/guion-demo.md`); Koyeb duerme la instancia gratuita a
la hora de quietud, así que un período mayor a una hora **no existiría en la práctica** en esa
plataforma; y un período menor a media hora corta una entrevista. Una hora es el techo de lo
posible y holgado para el guion. Lo que falta no es la cifra sino la frase del cartel del hallazgo
2: «cada hora, o antes si nadie la usa».

---

## Decisiones que resistieron

- **D1, el reinicio reemplaza al aislamiento.** Es la lectura correcta del motivo de la decisión 8,
  y la mitad prohibida —nada que reciba datos de una persona real— está bien dicha. Solo la primera
  condición necesita reescribirse (hallazgo 4).
- **D2, un origen y la API en loopback.** Correcto. Cae únicamente la frase de que el rewrite
  protege: hay que quitarlo o acotarlo (hallazgo 1).
- **D4, la demostración encendida y dicha, y su dependencia con D3.** Correcto, con la pregunta
  abierta ya respondida (hallazgo 10).
- **D7, medir antes de elegir y la salida en dos servicios escrita de antemano.** Correcto; falta
  dónde medir y contra qué (hallazgo 3).
- **D8, ninguna cifra sin verificar y fechada.** Correcto y **este informe lo aplicó**: las tres
  filas de la tabla del hallazgo 2 llevan fuente y fecha. Lo que cae es la comparación entre
  plataformas, no la regla.
- **La partición en tres y su orden.** Correcto. Cambia el contenido de `E10A` —el supervisor, el
  arranque como reinicio, la medición con umbrales— y `E10B` gana el contrato y el tope de
  pedidos.

## Respuestas a lo que el diseño deja abierto

| Pregunta del diseño | Respuesta |
| --- | --- |
| ¿Script SQL o `Migrate()`? | `Migrate()` detrás de la bandera de instancia pública: es el camino que `check.sh:24` ya vigila. Ninguno necesita el SDK |
| ¿Cada cuánto? | Una hora, por el hallazgo 13, y el cartel dice «o antes» |
| ¿El sueño de la plataforma reemplaza al reloj? | No: se suma. Las tres candidatas duermen por inactividad, sin hora. El reinicio es un arranque del proceso, y el sueño es un arranque más |
| ¿El reinicio rompe una petición en curso? | Si es un arranque, la petición en curso recibe el corte del proceso y la siguiente espera el arranque. Si es en caliente, hace falta `ClearAllPools()` y una compuerta `503`. El arranque es más simple |
| ¿El secreto de callback? | Vacío. El disparo de demo no pasa por HTTP |
| ¿Qué se limita y con qué números? | Un limitador global o en `proxy.ts`, nunca «por visitante» en la API sin propagar la IP; y un tope de pedidos por instancia, que no es un límite de tasa |

## Contradicciones con el Blueprint, la bitácora y `AGENTS.md`

Están en la tabla del hallazgo 9. La que bloquea el despacho es `AGENTS.md:114`.

## Resumen para los briefs

**`E10A-CONTENEDOR`.**

- Decidir e instalar el runtime de contenedores, por nombre y motivo (decisión 61), o declarar la
  medición en plataforma. El `Dockerfile` con imagen base que traiga `tzdata`, `output:
  'standalone'` si la medición lo justifica, y un `ENTRYPOINT` que supervise los dos procesos y
  termine si uno muere. `Migrate()` + recrear + sembrar + puntuar al arrancar, detrás de una
  bandera que solo la instancia pública enciende.
- Medición contra los umbrales que ya existen: 60 s sembrado, 120 s scoring, 5 s por página, 512
  MB con los dos runtimes; GC de servidor encendido y apagado; arranque en frío completo. El número
  va al handoff con la fecha.
- El rewrite: quitarlo, o acotarlo a `/api/health`. Decidido en la v2, ejecutado acá.

**`E10B-INSTANCIA-COMPARTIDA`.**

- La bandera de instancia pública publicada en `capabilities` junto con `nextResetAt`; recaptura
  de OpenAPI, `contract.ts`, `guards.ts`, `fixtures.ts`, `boundary.test.ts` y el test de una sola
  lectura, todos en la reserva de paths.
- El cartel como función del instante en los dos diccionarios; `nav.disclaimer` reemplazado o
  condicionado; decidir si es región viva.
- Tope de pedidos por instancia con código en `messages.ts`; tope global de cuerpo en Kestrel;
  limitador global o en `proxy.ts`, con la decisión de qué ve escrita.
- Decidir importación y nota abiertas o cerradas, y si cerradas, la bandera que la consola lee.
- Test de integración del reinicio: la base nueva no ve nada de la vieja, y ninguna conexión
  agrupada sobrevive.
- La decisión 70 en la forma de la 65, con «el producto borra su propia base» dicho.

**`E10C-PUBLICACION`.**

- Plataforma elegida con los números de `E10A` y las cifras vigentes leídas ese día, con fecha.
- `DemoData__Enabled`, la bandera pública, `PORT`, `serverActions.allowedOrigins` si hace falta.
- El link en README —sección «Cómo correrlo» y fila nueva en «Simulación, sandbox y producción»—,
  en `Getting-Started`, y **el párrafo para el artículo entregado en el handoff**, no editado.

**Coordinador, antes de despachar.** `AGENTS.md:114` y `:57`, Blueprint §2, §10 y §11, Overview
«Próximo paso» y «Fuera del MVP», `Getting-Started:139`, y la sección de la Etapa 10 en el Blueprint
con su orden y su verificación.

## Cómo se verificó

- Código: lectura completa de los archivos citados; `grep` de `fetch(` y de `"/api` en
  `frontend/src` fuera de tests (ningún componente cliente llama a la API); de `"use client"` y
  `"use server"` (ocho y cuatro archivos); de `RateLimit` en `backend/src` y `frontend/src` (sin
  resultados); de `Migrate|EnsureCreated|EnsureDeleted` en `backend/src` (sin resultados); de
  `BeginTransaction|IsolationLevel` en `backend/src` (sin resultados: toda escritura es un
  `SaveChangesAsync`); de `CultureInfo` en `backend/src` (solo `InvariantCulture`); de
  `FindSystemTimeZoneById` (una llamada, en la inicialización estática de `RuleConfig`); de
  `disclaimer` y `uso local` en `frontend/src` y `scripts/` (dos diccionarios, sin ancla en el smoke).
- Máquina: `which docker podman colima nerdctl` — los cuatro ausentes; `docker --version` — *command
  not found*.
- Documentación de Next 16.3.3 dentro de `frontend/node_modules/next/dist/docs/`: `output.md`
  (standalone), `16-proxy.md` (Middleware renombrado a Proxy), `server-actions.md:82` y
  `serverActions.md:11-14` (comparación de `Origin` con `Host`/`X-Forwarded-Host`,
  `allowedOrigins`), `rewrites.md` (no fija cuándo se evalúa el destino).
- Documentos: `grep -rn` de «solo local», «permanece local», «rutas mutables» y «no se despliega»
  sobre todos los Markdown fuera de `node_modules` y `_local/` — seis lugares, listados en el
  hallazgo 9.
- Plataformas, leídas el 2026-09-08: `https://www.koyeb.com/docs/reference/instances`,
  `https://render.com/docs/free`, `https://snapdeploy.dev/`. Se citan las frases textuales; nada se
  midió.
- Ningún test ni compuerta ejecutados; ninguna escritura fuera de este archivo; ningún push.
