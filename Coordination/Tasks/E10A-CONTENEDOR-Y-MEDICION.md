# Salvo — Task brief `E10A-CONTENEDOR-Y-MEDICION`

## Identificación

- Work ID: `E10A-CONTENEDOR-Y-MEDICION`
- Etapa: 10
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-08
- Rama/worktree: `claude/e10a-contenedor`
- Commit base: `e1233ba`, el `merge-base` real de `claude/e10a-contenedor` con `main`. **Esta línea
  se commitea en la rama, no en `main`**: es la lección de `E8B`.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: ninguna. `E10B` y `E10C` dependen de esta integrada, en ese orden.
- **Requisito previo, ya cumplido**: el coordinador instaló **Docker Desktop** en macOS 15.6.1,
  Apple Silicon, y `docker run --rm alpine echo hola` responde. Cuando se escribió el diseño no
  había ningún runtime de contenedores en la máquina, y esta tarea no instala software: si `docker`
  no responde, **parar y consultar** en vez de instalar nada.

## Resultado esperado

**Esta tarea decide si la Etapa 10 es posible, y puede terminar diciendo que no.**

Entrega un contenedor que corre Salvo entero —consola y API— y, sobre todo, **la medición del camino
frío**: cuánto tarda arrancar los dos procesos, migrar, sembrar 300 pedidos y correr el scoring, con
la memoria y la CPU de un tier gratuito. Ese número es lo que espera un visitante cada vez que la
instancia despierta, y es el riesgo central de la etapa.

No despliega nada. No hay instancia pública al terminar esta tarea.

## Contexto obligatorio

- `Coordination/Tasks/E10-DISENO.md` (**v2**), decisiones **D2, D3, D7 y D8**, y la condición de
  parada del final de la partición.
- `Coordination/Tasks/E10-revision-adversarial.md`, los hallazgos **1, 2 y 3**.
- `AGENTS.md`, línea 114: «sin autenticación, la aplicación es solo local y no se despliega con
  rutas mutables públicas». **Esta tarea no la contradice y no la toca**: no despliega nada, no
  abre ninguna ruta al público y no cambia quién puede llamar a qué. Su revisión es requisito de
  `E10B`, no de ésta. Si algo de esta tarea pareciera contradecirla, **parar y consultar**.
- Código, abierto antes de escribir nada:
  - `frontend/next.config.ts`: el rewrite que se borra, y `experimental.taint`, que no se toca.
  - `frontend/src/lib/api/server-client.ts`, el comentario de cabecera: explica por qué la consola
    usa URL absoluta y por qué el rewrite no le sirve.
  - `backend/src/Salvo.Api/Program.cs`: el arranque, y la ausencia de cualquier migración.
  - `backend/src/Salvo.Domain/Risk/RuleConfig.cs`:
    `TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo")`.
  - `scripts/demo.sh` y `scripts/smoke-ui.sh`: de dónde salen los umbrales.
  - `global.json` y `frontend/package.json`: las versiones que la imagen tiene que respetar.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

#### 1. Borrar el rewrite, que es lo primero y es una línea

`frontend/next.config.ts` reescribe `/api/:path*` a la API **sin filtro**. Publicado, eso pone el
sembrado, el scoring, los callbacks, las métricas y el documento OpenAPI a un `/api` de distancia de
la URL pública.

**Y no lo usa nadie.** La consola llama siempre con URL absoluta desde el servidor de Node, y el
comentario de cabecera de `server-client.ts` lo explica: una URL relativa dentro de un componente de
servidor es un `TypeError`. Es superficie expuesta sin un beneficio a cambio.

Se borra, y **se demuestra que estaba de más**: con la consola corriendo, `GET /api/dashboard`
contra el puerto de Next tiene que dar 404, y las cuatro pantallas tienen que seguir funcionando
enteras, incluida una importación y una corrida.

#### 2. La imagen

Un contenedor con los dos procesos: `next start` en el puerto público, y la API en `127.0.0.1`.

Restricciones duras, todas verificadas contra el árbol:

- **`global.json` clava el SDK en `10.0.400` con `rollForward: disable`.** La etapa de compilación
  necesita exactamente ese SDK, no uno mayor.
- **`package.json` clava Node en `24.20.0` y npm en `11.19.0`.**
- **`tzdata` tiene que estar en la imagen final.** `RuleConfig` resuelve `America/Montevideo` al
  construirse, así que una imagen slim sin husos horarios **no arranca**, y falla en el constructor
  de la configuración de reglas, que es un lugar donde el error no se va a leer como lo que es.
- **Si cualquiera de los dos procesos muere, el contenedor muere.** Un supervisor que sobreviva a la
  API deja una consola que responde 200 con todo roto detrás, y una sonda de salud que mira solo al
  puerto público la vería sana. Es el modo de falla más caro de un contenedor con dos procesos.

Decisiones delegadas con su costo escrito: si la API se publica autocontenida y recortada —menos
RAM y menos imagen, a cambio de una compilación más larga—, y si Next usa `output: "standalone"`,
que reduce mucho lo que el runtime de Node necesita.

#### 3. La base, al arrancar

Hoy no hay nada que migre: `scripts/demo.sh` corre `dotnet ef database update` antes de levantar la
API, y **la imagen de ejecución no lleva el SDK**.

Dos caminos, y se elige con el motivo escrito, no por comodidad:

- Un **script SQL idempotente** generado en tiempo de compilación, que se aplica al arrancar. No
  necesita el SDK y queda auditable en el repositorio.
- **`Database.Migrate()` al arrancar**, detrás de una bandera explícita. Más simple, menos visible.

La bandera importa en los dos casos: migrar al arrancar es correcto para una instancia efímera y
sería discutible para una con datos, y esa diferencia se declara en vez de heredarse.

#### 4. La medición, que es el entregable

Se cronometra **el camino frío completo**, por partes, con el contenedor limitado a mano a la forma
de un tier gratuito —**512 MB de RAM y una fracción de vCPU**—, y los números van al handoff:

| Qué se mide | Umbral, y de dónde sale |
| --- | --- |
| Arranque de los dos procesos hasta que `/health` responde | La sonda del smoke espera **5 s** |
| Migración de una base vacía | Sin umbral previo: se establece acá |
| Sembrado de los 300 pedidos | `scripts/smoke-ui.sh` usa `--max-time` **60 s** |
| Corrida de scoring sobre los 300 | `scripts/smoke-ui.sh` usa `--max-time` **120 s** |
| Primer render de cada una de las cuatro pantallas | El smoke usa `--max-time` **30 s** |
| RAM en reposo, y RAM durante la corrida | 512 MB es el techo |

**Los umbrales no se inventan: ya están en el código**, y son los que la compuerta usa hoy sobre una
máquina de escritorio. Si el camino frío en un contenedor limitado los pasa, **la instancia no es
usable y esta tarea lo dice con esos números en la mano**.

**Y hay que ser preciso sobre qué prueba esta medición, porque es menos de lo que parece.** Limitar
un contenedor a 512 MB en un Mac con chip M, **dentro de la máquina virtual de Docker Desktop**, no
reproduce una fracción de vCPU en hardware compartido: la memoria sí se acota, pero cada núcleo de esta máquina es mucho más rápido que el
que da un tier gratuito. Entonces:

- **Si no entra acá, no entra en ningún lado**, y la etapa cambia de forma en esta tarea. Ése es el
  valor real de la medición: es un **filtro**, y filtra barato.
- **Si entra acá, no está probado que entre allá.** El número definitivo lo mide `E10C` sobre la
  plataforma elegida, y hasta entonces ninguna afirmación del proyecto dice que la instancia
  funciona.

El handoff tiene que decir las dos cosas con esas palabras, para que nadie lea el veredicto como más
de lo que es.

Se mide **dos veces**: la primera con la base vacía —que es lo que pasa cuando la plataforma
despierta un contenedor nuevo— y la segunda con la base ya sembrada, que es lo que pasa cuando el
contenedor sigue vivo.

#### 5. Un guion para levantarlo

`scripts/contenedor.sh` o el nombre que la tarea elija: construir, correr con los límites puestos, y
esperar a que responda. Es lo que hace repetible la medición y lo que va a usar `E10C`.

### Fuera

- **Desplegar en cualquier lado.** Ninguna cuenta, ningún proveedor, ninguna URL pública.
- **El reinicio, el cartel, el límite de tasa y el tope de pedidos**: son `E10B`.
- **La decisión 8 y sus seis lugares**: los corrige el coordinador antes de `E10B`.
- Elegir plataforma. `E10C` la elige con estos números.
- Tocar el motor, el corpus, las reglas, el contrato o los diccionarios.
- Instalar software en la máquina del coordinador.

### Paths autorizados

- `Dockerfile` y `.dockerignore` (nuevos), en la raíz
- `scripts/**`
- `frontend/next.config.ts`
- `backend/src/Salvo.Api/Program.cs` y `backend/src/Salvo.Api/appsettings*.json`
- `backend/src/Salvo.Infrastructure/Persistence/**`, **solo** si el camino elegido en el punto 3 lo
  exige
- `backend/tests/**` y `frontend/src/test/**`
- `DesignAgent/Salvo-Getting-Started.md`
- `Coordination/Handoffs/Claude.md`

**No** entran: `README.md` y `docs/**` —el link no existe todavía y `E10C` los toca—, ni
`AGENTS.md`, ni el Blueprint, ni el Workboard, ni el Progress.

### Paths reservados por otros trabajos

- `AGENTS.md` y `DesignAgent/Salvo-Blueprint.md`: el coordinador, antes de `E10B`.
- `README.md`, `docs/**` y el artículo: `E10C`.

**Cuidado con la compuerta**: `scripts/check-docs.sh` verifica que cada ruta y cada nombre de test
que el README cita exista. Renombrar un test que el README nombra la rompe, y el README está
reservado.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas de aplicación: **no**. Imágenes base y paquetes del sistema dentro del
  `Dockerfile`: sí, y cada uno con su motivo en el handoff.
- Migraciones de EF: **no**. Aplicar las que existen, sí.
- Construir y correr contenedores: autorizado.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR, **y ningún alta en ningún proveedor**.
- **Acciones destructivas: ninguna.** `rm` está denegado y es regla del usuario; las imágenes y los
  contenedores que queden se listan para que los borre el coordinador.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E10A-CONTENEDOR-Y-MEDICION.md` sin faltantes antes de empezar.
- [ ] El rewrite ya no existe, `GET /api/dashboard` contra el puerto de Next da **404**, y las cuatro
      pantallas funcionan enteras, con una importación y una corrida hechas.
- [ ] La imagen construye con el SDK `10.0.400` y Node `24.20.0`.
- [ ] **El handoff registra con qué se midió**: la versión exacta de `docker`, la de macOS, el chip,
      y los recursos que la máquina virtual de Docker Desktop tenía asignados. Sin eso el número no
      se puede reproducir ni discutir.
- [ ] **El contenedor arranca con `tzdata`**, y hay una comprobación que lo demuestra: sin husos
      horarios, la API no arranca.
- [ ] **Si se mata la API dentro del contenedor, el contenedor termina.** Demostrado, no afirmado.
- [ ] La base se crea al arrancar sobre un volumen vacío, con el camino elegido y su motivo escrito.
- [ ] **La tabla de mediciones completa**, con la base vacía y con la base sembrada, contra los seis
      umbrales.
- [ ] **Un veredicto explícito**: `no entra` —y la etapa cambia de forma— o `no queda descartado`,
      con el número que lo sostiene. **No existe el veredicto `entra`**: esta máquina no reproduce el
      hardware de destino, y decirlo es parte del entregable.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes **fuera** del contenedor, como hasta ahora.
- [ ] Handoff con la tabla, el veredicto, y la lista de imágenes y contenedores que quedaron.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check` del brief | Válido |
| `curl` a `/api/dashboard` en el puerto de Next | `404` |
| Recorrido de las cuatro pantallas | Completo, con importación y corrida |
| Construcción de la imagen | Con las versiones clavadas |
| Arranque sin `tzdata` | La API no arranca, y el error se anota |
| Matar la API dentro del contenedor | El contenedor termina |
| Camino frío, base vacía | Cronometrado por partes, contra los umbrales |
| El handoff sobre el alcance de la medición | Dice que filtra, no que prueba |
| Camino tibio, base sembrada | Lo mismo |
| RAM en reposo y bajo la corrida | Por debajo de 512 MB, o el número real |
| `docker --version`, `sw_vers`, recursos de la VM | Registrados en el handoff |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E10A-CONTENEDOR-Y-MEDICION` | Con la tabla y el veredicto |

**Falsaciones exigidas.** Cada una se rompe a propósito, se corre, se anota el error exacto, y se
deshace:

1. Dejar el rewrite → `GET /api/demo-data/seed` contra el puerto público **siembra la base**. Es el
   hallazgo 1 reproducido: la demostración de que la v1 del diseño afirmaba lo contrario de lo que
   hacía el archivo.
2. Quitar `tzdata` de la imagen final → la API no arranca, y el error nombra la zona horaria.
3. Matar la API con el contenedor vivo → sin la regla del punto 2, la consola sigue respondiendo
   200 con todo roto detrás. **Anotar exactamente eso**, que es el modo de falla que la regla existe
   para impedir.

## Decisiones delegadas

- API autocontenida y recortada, o sobre el runtime de .NET, con el costo de cada una medido.
- `output: "standalone"` en Next, o no.
- Qué supervisa los dos procesos, mientras se cumpla que la muerte de uno mata al contenedor.
- El camino de creación de la base, entre los dos del punto 3.
- El nombre y la forma del guion del punto 5.
- Cómo se limita el contenedor para medir, mientras sean 512 MB y una fracción de vCPU.

## Detenerse y consultar si

- no hay runtime de contenedores instalado;
- **el camino frío no entra en los umbrales**: ahí la etapa cambia de forma y la decide el
  coordinador, no la tarea;
- borrar el rewrite rompe algo que la consola sí usaba;
- hace falta tocar el motor, el contrato o los diccionarios;
- algo de esta tarea parece contradecir la línea 114 de `AGENTS.md`;
- la imagen no puede fijar las versiones que `global.json` y `package.json` clavan.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **La tabla de mediciones**, con la base vacía y con la base sembrada.
- **El veredicto**, en los términos del criterio: `no entra` o `no queda descartado`.
- Las tres falsaciones, con el error exacto de cada una.
- **Con qué se midió**: versión de `docker`, de macOS, el chip, y los recursos de la máquina
  virtual.
- La lista de imágenes y contenedores que quedaron, para que los borre el coordinador.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
