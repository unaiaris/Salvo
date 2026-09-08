# Salvo — Task brief `E10B-INSTANCIA-COMPARTIDA`

## Identificación

- Work ID: `E10B-INSTANCIA-COMPARTIDA`
- Etapa: 10
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-08
- Rama/worktree: `claude/e10b-instancia`
- Commit base: `dc85350`, el `merge-base` real de `claude/e10b-instancia` con `main`. **Esta línea se
  commitea en la rama, no en `main`**: es la lección de `E8B`.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E10A` integrada (merge `29677f6`). `E10C` depende de ésta.
- **Desbloqueo**: la decisión **70** ya reemplazó a la 8 en sus seis lugares, así que la invariante
  de `AGENTS.md` sobre despliegue **ya no bloquea esta tarea**. Léela igual: dice qué sigue
  prohibido.

## Resultado esperado

La imagen de `E10A` se vuelve **una instancia pública defendible**: arranca rápido con los datos
puestos, se reinicia sola, avisa en pantalla lo que es, y no se puede tumbar desde afuera.

Sigue sin desplegarse nada. `E10C` publica.

## Contexto obligatorio

- `Coordination/Tasks/E10-DISENO.md` (**v2**), decisiones **D1, D3, D4, D5 y D6**, y la sección
  **«Lo que la medición de `E10A` cambió»**, que reescribe D3 y D4 con números.
- `Coordination/Tasks/E10-revision-adversarial.md`, hallazgos **2, 4 y 5**, y los medios sobre la
  mecánica del reinicio, el supervisor y la sonda de salud.
- `Coordination/Handoffs/Claude.md`, entrada de **`E10A`**: la tabla de mediciones, y el defecto del
  código de salida que encontró su tercera falsación.
- `DesignAgent/Salvo-Blueprint.md`, **decisión 70** y la invariante de §5.3 que reemplazó a la
  anterior.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 10»: el tercer ítem es el que esta tarea cierra.
- Código y configuración, abiertos antes de escribir nada:
  - `Dockerfile`, sobre todo el bloque `ENV` de la etapa final y los comentarios que explican
    `Database__MigrateOnStartup` y `DemoData__Enabled`.
  - `scripts/contenedor-entrypoint.sh` y `scripts/contenedor.sh`.
  - `backend/src/Salvo.Api/OrderEndpoints.cs`, `EvaluationMetricsEndpoints.cs`,
    `ExternalDemoEndpoints.cs` y `SystemEndpoints.cs`: **los cuatro lugares que consultan
    `DemoData:Enabled`**.
  - `frontend/src/lib/i18n/es.ts` y `pt.ts`.
  - `Coordination/Handoffs/Claude.md`, entrada de la **fase 1 de `E9C2`**: ahí está la receta exacta
    del estado que hace falta hornear —explicación escrita, evaluaciones externas entregadas—.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

#### 1. La base sembrada se hornea en la imagen

Sembrar cuesta **24,68 s** y puntuar **17,50 s** a 0,1 vCPU. Hacerlo al arrancar pondría al primer
visitante a esperar 119 segundos. Se hace **durante la construcción** y el archivo resultante se
copia a la imagen; el arranque solo lo copia a su lugar de trabajo.

**Qué tiene que contener la base horneada**, y no es solo el corpus:

- Los 300 pedidos sembrados y **la corrida de scoring hecha**, con sus 23 alertas.
- Las **evaluaciones externas pedidas y sus callbacks entregados**, porque sin eso el panel de
  denegados sin alerta local aparece vacío y es la superficie que sostiene el argumento de la
  Etapa 9.
- **Al menos una explicación escrita**, para que un visitante vea una sin tener que pedirla y
  esperar.

La receta está escrita: es la misma que la fase 1 de `E9C2` dejó en su handoff para el recorrido con
lector de pantalla. **Se reutiliza, no se reinventa.**

#### 2. El modelo de EF Core, precompilado

**23 de los 77 segundos de arranque** se van construyendo el modelo. `dotnet ef dbcontext optimize`
lo precompila y el arranque lo lee en vez de construirlo. Es una función estándar de EF Core.

Se mide antes y después, con el mismo método que `E10A`, y el número va al handoff. Si el modelo
compilado no se puede generar sin el SDK en la imagen final, se genera en la etapa de compilación,
que sí lo tiene.

#### 3. El reinicio es la salida del proceso, y eso resuelve el problema de SQLite

La revisión adversarial marcó que recrear el archivo de SQLite mientras la API lo tiene abierto —con
conexiones agrupadas— no es gratis. **Con la base horneada esa cirugía desaparece**: el reinicio es
**terminar el contenedor**, y el punto de entrada copia el archivo horneado al arrancar. Una sola
primitiva, sin tocar conexiones vivas.

Dos disparadores:

- **Por inactividad**: lo da la plataforma, que duerme el contenedor. No hay que escribir nada.
- **Por antigüedad**: si la instancia recibe tráfico continuo nunca duerme y el estado se acumula
  sin techo. Un temporizador que, pasados **N** minutos desde el arranque, termina el contenedor.

**Y hay un detalle que `E10A` dejó servido**: su tercera falsación descubrió que un proceso muerto
terminaba el contenedor con código **0**, y que una plataforma puede leer eso como «terminó su
trabajo» en vez de «se cayó». El reinicio deliberado tiene el mismo problema: **tiene que salir con
un código que la plataforma reinicie**, y cuál es eso se verifica en `E10C`. Acá se elige uno, se
escribe el motivo, y se deja dicho que `E10C` lo confirma.

#### 4. `DemoData:Enabled` no es una bandera: son cuatro cosas

Y por eso apagarla no es la respuesta simple que el diseño suponía. Hoy gobierna, en cuatro
archivos:

| Qué | Dónde | Si se apaga |
| --- | --- | --- |
| Sembrado y su ensayo previo | `OrderEndpoints.cs:32` | Desaparece la ruta que un visitante podría usar para reventar la base |
| **Métricas de calidad** | `EvaluationMetricsEndpoints.cs:21` | **Desaparece el panel de calidad**: F1, la matriz, el barrido |
| Disparadores del proveedor externo | `ExternalDemoEndpoints.cs:31` | El visitante no puede ejercitar el callback |
| Lo que la consola declara poder hacer | `SystemEndpoints.cs:22` | La consola esconde los botones correspondientes |

Con la base horneada, **el sembrado ya no hace falta y las métricas sí**. La tarea decide entre dos
caminos, con el motivo escrito:

- **Partir la bandera**: una para el sembrado y otra para el resto. Es un cambio chico y deja la
  instancia pública sin la ruta más peligrosa, conservando el argumento del proyecto.
- **Dejarla entera y encendida**, apoyándose en el reinicio, como decía el diseño antes de la
  medición.

**Recomiendo partirla**, y la razón es de fondo: el reinicio acota el daño en el tiempo, pero no
evita que un visitante encuentre la consola inutilizable durante los minutos que faltan para el
próximo. Quitar la ruta es más barato que confiar en que el reloj llegue a tiempo.

#### 5. El cartel

Visible en todas las pantallas, **en los dos idiomas**, y con una redacción que no promete una hora
exacta porque el reinicio principal es por inactividad:

> Instancia de demostración compartida. Los datos son sintéticos. **Lo que escribas acá lo ve todo
> el mundo**, y todo se reinicia cuando la instancia queda un rato sin visitas, y como máximo cada
> [N] minutos.

«Lo que escribas lo ve todo el mundo» **no es opcional**: la nota de revisión es texto libre,
anónima y pública, y la decisión 70 exige que eso se anuncie en pantalla y no en un README.

#### 6. Lo que falta del modelo de amenaza

**El límite de tasa va en la capa de Next, no en la API.** Después de que `E10A` borró el rewrite,
toda petición le llega a la API desde `127.0.0.1` sin ninguna cabecera de origen: limitar ahí sería
limitar a la consola contra sí misma. El visitante existe en el servidor de Next.

**Y un tope de pedidos por instancia**, que ningún limitador de tasa reemplaza: el costo de una
corrida de scoring depende de **cuántos pedidos hay**, no de cuántas veces se pida, y la importación
admite 5 MB por archivo tantas veces como uno quiera. Pasado el tope, la importación se rechaza con
un código propio, que va a `messages.ts` con su test de exactitud (decisión 57) y a los dos
diccionarios.

Los números salen de la medición de `E10A` y de una nueva, no de elegir algo redondo.

#### 7. La sonda de salud tiene que ver los dos procesos

Hoy una sonda contra el puerto público vería sana una consola con la API muerta. La regla del
supervisor de `E10A` mata el contenedor si un proceso cae, así que el caso está acotado — pero una
sonda que solo mira a Next sigue siendo una sonda que miente. Que compruebe los dos.

### Fuera

- **Desplegar.** Ninguna cuenta, ningún proveedor, ninguna URL pública: es `E10C`.
- Elegir plataforma, y confirmar sus límites.
- El README, el artículo y la guía con el link: `E10C`.
- Tocar el motor, el corpus, las reglas, el contrato, o la fixture.
- Autenticación, o cualquier forma de aislar visitantes entre sí.

### Paths autorizados

- `Dockerfile` y `.dockerignore`
- `scripts/**`
- `backend/src/Salvo.Api/**` — los cuatro lugares de la bandera, la sonda, y el límite si algo cae
  del lado de la API
- `backend/src/Salvo.Infrastructure/Persistence/**` y `backend/src/Salvo.Api/Salvo.Api.csproj`, por
  el modelo compilado de EF
- `frontend/src/**` — el cartel, el límite en la capa de Next, y los dos diccionarios
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, **solo recaptura** si
  el código nuevo del tope cambia el contrato
- `backend/tests/**`
- `DesignAgent/Salvo-Getting-Started.md`
- `Coordination/Handoffs/Claude.md`

**No** entran: `README.md`, `docs/**`, `AGENTS.md`, el Blueprint, el Workboard ni el Progress.

### Paths reservados por otros trabajos

- `README.md`, `docs/**` y el artículo: `E10C`.

**Cuidado con la compuerta**: `scripts/check-docs.sh` verifica que cada ruta y cada nombre de test
que el README cita exista, y el README está reservado. Si hay que renombrar un test que nombra,
**parar y consultar**.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**. El limitador de tasa se escribe con lo que Next ya trae.
- Migraciones de EF: **no**.
- Construir y correr contenedores: autorizado. `docker` está en `~/.docker/bin/docker`.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR, **ningún alta en ningún proveedor**.
- **Acciones destructivas: ninguna.** Las imágenes y contenedores que queden se listan para el
  coordinador.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E10B-INSTANCIA-COMPARTIDA.md` sin faltantes antes de empezar.
- [ ] **El arranque en frío se mide de nuevo a 0,1 vCPU**, con la base horneada y el modelo
      compilado, y el número se compara con los 77,5 s de `E10A`.
- [ ] Un contenedor recién arrancado **muestra las 23 alertas, el panel de denegados y una
      explicación escrita**, sin que nadie pida nada.
- [ ] Terminar el contenedor y volver a arrancarlo **devuelve exactamente ese estado**, aunque antes
      se hayan emitido veredictos e importado pedidos.
- [ ] El reinicio por antigüedad ocurre, y **sale con un código que una plataforma reinicia**.
- [ ] El cartel aparece en todas las pantallas y **en los dos idiomas**.
- [ ] El límite de tasa está en la capa de Next y se demuestra que actúa.
- [ ] El tope de pedidos rechaza la importación con su código, presente en los dos diccionarios y en
      su test de exactitud.
- [ ] La sonda de salud **falla cuando la API está muerta**.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.
- [ ] Handoff con las mediciones nuevas y la lista de imágenes y contenedores.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check` del brief | Válido |
| Arranque en frío a 0,1 vCPU, con las dos optimizaciones | Menor que los 77,5 s de `E10A`, con el número |
| Primer render de un contenedor nuevo | 23 alertas, denegados y una explicación, sin pedir nada |
| Emitir veredictos, importar, reiniciar | El estado vuelve al horneado |
| Reinicio por antigüedad | Ocurre, y con el código de salida elegido |
| Cartel | En las cuatro pantallas y en `es` y `pt` |
| Límite de tasa | Actúa, demostrado |
| Importar por encima del tope | Rechazo con código, en los dos idiomas |
| Sonda con la API muerta | Falla |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |

**Falsaciones exigidas.** Cada una se rompe, se corre, se anota el error, y se deshace:

1. No copiar la base horneada al arrancar → el contenedor arranca **vacío**, y la consola dice que
   no hay pedidos. Es la demostración de que el estado viene de la imagen y no de la suerte.
2. Quitar el límite de tasa → un bucle de peticiones tumba la instancia, o la degrada de forma
   medible. **Anotar el número**, que es lo que justifica el límite elegido.
3. Quitar el tope de pedidos → importar varias veces hace que la corrida siguiente cueste
   proporcionalmente más. **Anotar los dos tiempos**: es lo que demuestra que el costo depende de
   cuántos pedidos hay y no de cuántas veces se pida, que es la razón de que un limitador de tasa no
   alcance.
4. Hacer que la sonda mire solo a Next → con la API muerta, la sonda dice que todo está bien.

## Decisiones delegadas

- Cómo se hornea la base en la construcción, mientras el resultado sea reproducible.
- Si la bandera se parte o no, con el motivo escrito. **Recomendación: partirla.**
- El valor de **N** para el reinicio por antigüedad, y el código de salida.
- Dónde vive el límite de tasa dentro del servidor de Next, y sus números.
- El valor del tope de pedidos y el nombre de su código.
- La forma del cartel, mientras esté en todas las pantallas y diga las tres cosas.

## Detenerse y consultar si

- el arranque con las dos optimizaciones **no baja** de los 77,5 s;
- hornear la base exige tocar el motor, la fixture o el contrato;
- partir la bandera resulta ser más que un cambio chico;
- el límite de tasa necesita una dependencia;
- hace falta tocar el README, el Blueprint o los registros canónicos.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **La medición nueva del arranque en frío**, comparada con los 77,5 s de `E10A`, y el aporte de
  cada optimización por separado.
- Los números de las falsaciones 2 y 3.
- Qué se decidió sobre la bandera, y por qué.
- La lista de imágenes y contenedores que quedaron.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
