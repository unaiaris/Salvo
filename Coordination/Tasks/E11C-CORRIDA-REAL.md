# Salvo — Task brief `E11C-CORRIDA-REAL`

## Identificación

- Work ID: `E11C-CORRIDA-REAL`
- Etapa: 11 — la IA de verdad
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-10-02
- Rama/worktree: `claude/e11c-corrida`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. El coordinador acepta que este campo no lleve SHA, por la lección de
  `E8B`: el commit que lo escribiera en `main` movería la punta y lo volvería falso.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Sonnet 5.5 · `high`**, la fila «Implementación con brief cerrado» de
  `ClaudeAgent/Claude-Model-Policy.md`: el cambio de código es chico y está dicho hasta el nombre de
  la variable, y lo demás es leer una salida y escribir lo que dice. El `brief-check`, con un modelo
  distinto.
- Dependencias: `E11B-ADAPTADOR-ANTHROPIC`, verificada (merge `e9dda5a`). Cierra la Etapa 11.

## Resultado esperado

**El adaptador de Anthropic corre una vez contra la API real, y lo que pasó queda publicado tal como
pasó**: los textos que el verificador aceptó, y de cada intento rechazado, el código y el detalle. El
README deja de decir que el adaptador nunca se llamó y dice qué se midió, con su fecha y su costo.

**La corrida la hace el coordinador, con su clave, y en cuatro tiempos que cuidan el saldo.** El
coordinador cargó US$ 5 de crédito prepago, sin recarga automática, y quiere que le queden para otras
pruebas: la clave vive en un espacio de trabajo dedicado con un tope mensual de US$ 1.

| Tiempo | Quién | Qué | Costo |
| --- | --- | --- | --- |
| 0. Ensayo en seco | El agente | El script entero contra la plantilla, sin clave y sin red | US$ 0 |
| 1. Canario de fallo | El coordinador | 2 alertas contra la API real, con una clave **ficticia** | US$ 0: un pedido que falla no se cobra |
| 2. Canario | El coordinador | 2 alertas contra la API real, con la clave verdadera | Unos US$ 0,01; a lo sumo US$ 0,08 |
| 3. Corrida completa | El coordinador | Las 23 alertas | Unos US$ 0,14; a lo sumo US$ 0,91 |

Los dos primeros existen porque **el cuerpo del script nunca se ejecutó**: `E11B` probó que se niega
sin clave, antes de compilar nada. El ensayo en seco recorre el camino en que todo sale bien, y **no
puede recorrer el de fallo, porque la plantilla nunca falla**. El canario de fallo lo recorre gratis:
con una clave ficticia, la API real contesta un error de autenticación a cada pedido, y el bucle de
reintentos, el agotamiento de la fila y el renglón de código y detalle corren por primera vez sin
pagar una llamada. De paso mide, contra la API real, si el error de autenticación tiene la forma que
el simulador supone.

## Contexto obligatorio

- `Coordination/Tasks/E11-DISENO.md`, D14 —el costo—, D15 —el script— y D16 —qué se publica—, y sus
  «Condiciones de parada de la etapa».
- `Coordination/Handoffs/Claude.md`, la entrada de `E11B`: «Lo que `E11C` tiene que mirar primero»,
  «Riesgos y pendientes» y «Costo, con los precios del día».
- `DesignAgent/Salvo-Blueprint.md`, §4.7 y las decisiones 69 —una cifra publicada lleva su fecha— y 73
  a 82.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 11 — La IA de verdad»: el que esta tarea cierra es
  el de `E11C`.
- `AGENTS.md`, «Decisiones invariantes»: un texto rechazado no se persiste, no se registra y no llega
  al diagnóstico; si el modelo no pasa el verificador, se cambia el prompt, nunca el verificador; y
  ningún agente tiene ni pide la clave.
- `scripts/explicar-con-anthropic.sh`, **entero**, y `scripts/demo.sh`, que es su molde.
- El `README.md`: la fila del redactor, lo que dice de `AI_PROVIDER=anthropic` y «Límites declarados».

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro, en cuatro tiempos, y cada uno espera al anterior

**Tiempo 0 — el agente prepara el script, lo ensaya en seco, lo commitea y se detiene.**

1. El campo «Commit base» del brief.
2. **La historia de intentos.** Hoy el script reintenta cada fila hasta agotar sus tres intentos, y lo
   que queda al final esconde lo que pasó: en el intento que agota, `AlertExplanation.Fail` guarda
   `ATTEMPT_LIMIT_REACHED` y manda el código real al detalle como `<CÓDIGO>: <detalle>`, y `Retake`
   borra el rechazo del intento anterior. **El script anota cada intento en el momento en que
   ocurre**, tomando de la respuesta de ese pedido el estado, `failureCode` y `failureDetail` —la
   vista los lleva desde `E11B`—, y publica por fila esa lista: intento, estado, código y detalle. El
   código real de un intento que agotó la fila es el prefijo de su detalle, y el script lo separa.
   Sigue siendo lo que D16 permite: código y detalle, nunca el texto.
3. **`EXPLICAR_MAX_ALERTAS`**, opcional: un entero positivo que limita la corrida a las primeras N
   alertas, en el orden en que la API las devuelve. Sin la variable, corre todas, como hoy. Un valor
   que no sea un entero positivo detiene el script antes de compilar.
4. **Una corrida limitada no publica.** Con `EXPLICAR_MAX_ALERTAS`, los dos archivos van a un
   directorio temporal fuera del repositorio, y el script dice dónde. `docs/explicaciones-modelo/`
   queda solo para la corrida completa: así un canario no ocupa la carpeta del día, que el script se
   niega a pisar.
5. **`EXPLICAR_ENSAYO=1`**, el ensayo en seco: levanta la API con `AI_PROVIDER=mock`, **no lee ningún
   archivo de entorno**, no exige `SALVO_ENV_FILE`, y escribe en un directorio temporal. Recorre el
   mismo código que la corrida real en el camino en que todo sale bien —la siembra, el scoring, los
   pedidos, la lectura de la base, los dos archivos— con la plantilla como escritor. El costo que
   informa es cero, y el resumen dice que fue un ensayo.
6. **Lo que la corrida mide de sí misma, en el resumen**, para que la evidencia no dependa de un
   registro temporal:
   - el commit del script que corrió y si el árbol estaba limpio;
   - **los tokens de cada llamada**, con el promedio y el máximo. La respuesta del pedido no los
     trae y la fila los acumula entre intentos, así que el script lee la fila en la base **después de
     cada pedido** y anota la diferencia con la lectura anterior: eso es lo que costó ese intento;
   - **cuántas veces la API registró el aviso de razonamiento**, contado por el script en el registro
     de la API antes de terminar. El texto que busca es el del mensaje de `Log.ThinkingReported` en
     `AnthropicExplanationProvider.cs` —«Anthropic reported … thinking tokens on request …»—, no el
     nombre del campo de la API. El agente no lee ese registro ni lo pega en ningún lado.
   - **El contador se tiene que ver contar.** Cero es también el resultado bueno, y sale en el
     ensayo, en el canario de fallo y en una corrida sana: un contador roto daría lo mismo. El agente
     corre la cuenta del script sobre un archivo que él arma, con dos renglones escritos con el
     mensaje real, y muestra que da 2; y sobre uno sin el mensaje, que da 0.
7. **El agente corre el ensayo dos veces**, completo y con `EXPLICAR_MAX_ALERTAS=2`, y comprueba la
   salida: 23 y 2 filas, todas `READY` por la plantilla en un intento, los dos archivos bien formados,
   y nada escrito en `docs/explicaciones-modelo/`. Si el ensayo descubre un defecto del script, lo
   corrige.
8. **Las negativas, y solo ellas, sin `EXPLICAR_ENSAYO`.** El agente comprueba que el script se sigue
   negando en los cinco casos que `E11B` probó: sin `SALVO_ENV_FILE`, con un archivo vacío, con uno
   inexistente, con clave y sin modelo, y con la clave exportada en la terminal pero no en el archivo.
   Los archivos de entorno los crea el agente en un directorio temporal, con una clave **ficticia y
   evidente**; nunca `.env`. **Son los únicos casos en que el agente corre el script sin
   `EXPLICAR_ENSAYO=1`, y todos se detienen antes de compilar.** Un archivo con clave **y** modelo
   levantaría la API: ese caso no lo corre el agente, y es el canario de fallo, del coordinador.
9. **El agente commitea el script** en `claude/e11c-corrida`, deja escrito en un directorio temporal
   el archivo de entorno del canario de fallo —clave ficticia y `claude-sonnet-5-5`—, **se detiene**,
   y entrega al coordinador los tres comandos exactos. El coordinador corre desde esa rama, con el
   árbol limpio, así que lo que se publique lo escribió un script que está en la historia. **Antes de
   detenerse escribe el handoff, con estado `Parcial`**, y en él las dos salidas del ensayo, las cinco
   negativas y la prueba del contador: si la sesión se pierde entre tiempos, nada de eso hay que
   rehacerlo. El handoff se completa en el Tiempo 3, en la misma entrada.

**Tiempo 1 — el coordinador corre el canario de fallo y pega la salida.**

`SALVO_ENV_FILE=<el archivo de la clave ficticia> EXPLICAR_MAX_ALERTAS=2
./scripts/explicar-con-anthropic.sh`. Son a lo sumo seis pedidos, todos rechazados por la API y
ninguno cobrado. Lo esperado: las dos filas `FAILED`, tres intentos cada una, cada intento con
`PROVIDER_UNAVAILABLE` y `credentials` en el detalle, con su `request-id`, y los dos archivos escritos
en el directorio temporal. El agente lee lo que el coordinador pega y contesta:

- **Seguir**: es lo esperado.
- **Corregir el script**: el bucle, la historia de intentos o los archivos tienen un defecto. Lo
  corrige, commitea, y el canario de fallo se repite. Sigue costando cero.
- **Parar**: el error de autenticación no tiene la forma que el simulador supone —el detalle dice
  `unrecognized`, falta el `request-id`, o el desenlace no es `PROVIDER_UNAVAILABLE`—. Es la
  condición de la etapa: se corrige el simulador primero, y eso es otra tarea.

**Tiempo 2 — el coordinador corre el canario y pega la salida.**

`SALVO_ENV_FILE=.env EXPLICAR_MAX_ALERTAS=2 ./scripts/explicar-con-anthropic.sh`. El agente lee la
**historia de intentos** de las dos filas y contesta una sola cosa. **Si se cumple más de una, manda
la de más arriba**:

1. **Parar: la API no es la que el simulador supone.** `MALFORMED_OUTPUT`, un detalle
   `unrecognized`, una fila `READY` cuyo modelo no es `claude-sonnet-5-5`, o un `HTTP 400` **después
   de que el coordinador descartó el tope**. El adaptador no puede distinguir el tope propio del
   espacio de trabajo de un pedido mal formado: los dos llegan como `HTTP 400;
   invalid_request_error`, y es un límite que D6 ya declara. **Lo distingue el coordinador, en la
   consola**: si el gasto del espacio llegó al tope, o el saldo a cero, es la respuesta 2; si no, es
   la forma de la petición, y se corrige el simulador primero, que es otra tarea. En el canario el
   tope no puede haberse alcanzado —son a lo sumo US$ 0,08 de US$ 1—, así que ahí un 400 es la forma.
2. **Parar: es de la cuenta, no del código.** `credentials` —la clave, o el espacio de trabajo—,
   `spend_cap` —el tope del tier—, o el `HTTP 400` que el coordinador confirmó como tope propio o
   saldo agotado. El coordinador lo resuelve en la consola. No se toca código.
3. **Repetir el canario más tarde, una vez.** Todos los intentos de una fila fallan por algo
   transitorio: `PROVIDER_TIMEOUT`, un 429 con `retry-after`, un 500 o un 529. Si al repetir sigue,
   se para y se consulta. Un `PROVIDER_TIMEOUT` **solo en el primer intento de la primera fila** no
   cuenta: la primera petición compila la gramática.
4. **Seguir, con aviso.** Intentos rechazados por el verificador —`NOT_GROUNDED_NUMBER`,
   `NOT_GROUNDED_RULE`, `TOO_LONG`— o `PROVIDER_REFUSED`. La API funciona y el modelo escribe; la
   corrida completa dirá cuántos, y qué se hace con el prompt lo decide después el coordinador. Si
   **las dos** filas terminan `FAILED`, se consulta antes de seguir.
5. **Seguir.** Las dos filas `READY`, con `claude-sonnet-5-5`.
6. **Ninguna de las anteriores: parar y consultar.** Un 402, 404, 409 o 413 —el adaptador los
   registra con su tipo, sin `credentials` ni `spend_cap`—, un estado distinto de 200 de la propia
   API de Salvo, o cualquier salida que estas reglas no nombran. No se adivina.

**Y siempre, el presupuesto**, con la entrada que el canario midió y no con la estimación del diseño,
que supuso 1.500 tokens:

- **Esperado**: 23 por el costo medio de una fila del canario.
- **Techo, como manda D14**: 69 llamadas, cada una con la **entrada más grande medida** y con
  **`max_tokens` de salida, 1.024**, porque un texto cortado cobra toda la salida. Dos filas no acotan
  la salida, así que la salida medida no entra en el techo.

Si el techo, sumado a lo ya gastado, pasa el tope de US$ 1 del espacio de trabajo, el agente lo dice
antes de la corrida completa y el coordinador decide: subir el tope, o aceptar que el tope puede
cortarla.

**Tiempo 3 — el coordinador corre la completa, y el agente publica.**

10. El coordinador corre `SALVO_ENV_FILE=.env ./scripts/explicar-con-anthropic.sh` y avisa. Los dos
    archivos quedan en `docs/explicaciones-modelo/AAAA-MM-DD/`.
11. **El agente no edita esos dos archivos**: se commitean tal como el script los escribió. Antes de
    agregarlos comprueba, **sobre los archivos en disco** —`git grep` no mira lo que todavía no está
    en el índice—, que cumplen D16 —ninguna fila ni intento `FAILED` trae texto— y que no contienen
    nada con forma de clave.
12. **El README dice lo que se midió**, con la fecha de la corrida: cuántas alertas, cuántas aceptadas
    y en qué intento, cuántos intentos rechazados y **por qué códigos, contados de la historia de
    intentos**, los tokens, el costo con la fecha del precio, y el modelo que respondió. Enlaza la
    carpeta. La fila del redactor y «Límites declarados» dejan de decir que el adaptador nunca se
    llamó. **Las cifras se copian de `explicaciones.json`**, no de la terminal ni de memoria.
13. `Salvo-Getting-Started.md`: cómo se corre el script, con el ensayo y los dos canarios.
14. **El handoff se completa**, con la salida de cada canario, la lectura de la corrida y las cinco
    comprobaciones que `E11B` dejó, cada una con su evidencia: la clave de un espacio dedicado con tope —lo afirma el
    coordinador—; la primera fila; el modelo de las filas `READY`; el contador del aviso de
    razonamiento, del resumen; y los rechazos por código, de la historia de intentos. El estado pasa
    de `Parcial` al que corresponda.

### Fuera

- **Que el agente corra el script contra Anthropic**, lea `.env`, pida la clave o la vea. El agente
  corre **solo** el ensayo en seco y las cinco negativas del punto 8, que se detienen antes de
  compilar.
- **Cambiar el verificador, el prompt o el adaptador.** Si la corrida pide un `anthropic-p2`, o
  corregir el simulador, es otra tarea.
- **Repetir la corrida para que salga mejor.** Es una tirada: se publica la que salió.
- **Editar a mano los archivos de la corrida**, o publicar cualquier texto rechazado.
- Correr contra la instancia pública, o tocar `render.yaml`, el `Dockerfile` o el flujo de CI.

### Paths autorizados

- `scripts/explicar-con-anthropic.sh`.
- `docs/explicaciones-modelo/**`, **solo** para agregar los archivos que el script escribió.
- `README.md` y `DesignAgent/Salvo-Getting-Started.md`, **solo** lo de los puntos 12 y 13.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11C-CORRIDA-REAL.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno. No hay otra tarea abierta.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no**.
- Red: **no**. El ensayo en seco no la usa.
- **Secretos: ninguno, nunca.** El agente no lee `.env` y no pide que le peguen la clave. Corre el
  script **solo** con `EXPLICAR_ENSAYO=1`, o en las cinco negativas del punto 8, con archivos de
  entorno que él mismo crea y una clave ficticia. **Nunca con un archivo que tenga clave y modelo a
  la vez**: eso levanta la API y llama a Anthropic, y es del coordinador. Lo que el coordinador pega
  es salida de terminal y archivos que el script escribió, que no contienen la clave. **El registro
  de la API de una corrida real no se lee ni se pega**: lo que hace falta de él lo cuenta el script.
- Escrituras externas: **no**. El agente no hace push.
- Acciones destructivas: **no**. El script no borra nada, y las bases de las corridas quedan donde
  están.

## Criterios de aceptación

- [ ] El script publica la historia de intentos de cada fila, con el código real de cada uno.
- [ ] `EXPLICAR_MAX_ALERTAS` limita la corrida, y un valor inválido detiene el script antes de
      compilar.
- [ ] Una corrida limitada escribe fuera del repositorio; `docs/explicaciones-modelo/` no cambia.
- [ ] `EXPLICAR_ENSAYO=1` corre sin `SALVO_ENV_FILE`, sin leer ningún archivo de entorno y con
      `AI_PROVIDER=mock`; el agente lo corrió completo y con 2 alertas, y pega las dos salidas.
- [ ] Sin `EXPLICAR_ENSAYO`, el script se sigue negando en los cinco casos del punto 8, corridos por
      el agente con archivos propios y clave ficticia.
- [ ] El resumen trae el commit del script, los tokens de cada llamada y el contador del aviso de
      razonamiento; y el contador se vio contar 2 y 0 sobre archivos armados.
- [ ] El canario de fallo corrió, sin costo, y mostró la historia de intentos de dos filas `FAILED`.
- [ ] El canario corrió, y el agente contestó **una** de las seis respuestas, con el motivo, y el
      presupuesto de la corrida completa: el esperado, y el techo con la entrada medida y `max_tokens`.
- [ ] La corrida completa está en `docs/explicaciones-modelo/AAAA-MM-DD/`, sin editar, sin un texto
      rechazado y sin nada con forma de clave.
- [ ] El README dice lo que se midió, con cifras que coinciden con `explicaciones.json`, la fecha de
      la corrida y la del precio.
- [ ] `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `EXPLICAR_ENSAYO=1 ./scripts/explicar-con-anthropic.sh` | 23 filas `READY` de la plantilla, dos archivos en un directorio temporal, costo cero |
| `EXPLICAR_ENSAYO=1 EXPLICAR_MAX_ALERTAS=2 ./scripts/explicar-con-anthropic.sh` | 2 filas |
| `git status` después de los dos ensayos | Nada nuevo en `docs/explicaciones-modelo/` |
| Las cinco negativas del punto 8, **que corre el agente** | Cada una se niega antes de compilar, nombrando la variable y nunca un valor |
| La cuenta del aviso de razonamiento sobre dos archivos armados por el agente | 2 y 0 |
| El canario de fallo, **que corre el coordinador** | Dos filas `FAILED`, tres intentos cada una, `credentials` en cada detalle; costo cero |
| El canario, **que corre el coordinador** | Su salida, pegada en la entrega tal como salió |
| La corrida completa, **que corre el coordinador** | Los dos archivos, commiteados sin editar |
| `grep -rn "sk-ant" docs/explicaciones-modelo README.md`, **antes de `git add`** y sobre el disco | Sin resultados |
| Las cifras del README contra `explicaciones.json` | Iguales |
| `./scripts/check.sh` y `./scripts/smoke-ui.sh` | Verdes |

## Decisiones delegadas

- Cómo se llama y dónde vive el directorio temporal de una corrida limitada o de un ensayo.
- La redacción del README y de la guía, mientras cada cifra salga del archivo.
- Si el ensayo necesita algún otro ajuste menor del script para poder correr sin clave.

## Detenerse y consultar si

- el ensayo en seco exige tocar algo fuera de `scripts/explicar-con-anthropic.sh`;
- cualquiera de los dos canarios da una señal de **parar**, o ninguna respuesta aplica;
- la corrida completa termina con más rechazadas que aceptadas: qué se publica y si hay un
  `anthropic-p2` lo decide el coordinador;
- la corrida completa no termina, o el tope del espacio de trabajo la corta a la mitad;
- los archivos de la corrida traen un texto rechazado o algo con forma de clave;
- cualquier paso pide la clave, leer `.env` o correr el script real desde el agente.

## Entrega requerida

- Resumen, archivos tocados y commits.
- Las dos salidas del ensayo en seco, y las cinco negativas.
- La salida de cada canario y la respuesta que se le dio, con el presupuesto recalculado.
- La lectura de la corrida completa: aceptadas y en qué intento, rechazos por código, tokens, costo,
  y las cinco comprobaciones que dejó `E11B`.
- Qué dijo el modelo que el verificador no pudo ver: si alguna explicación aceptada es falsa por
  inversión, atribución cruzada o cifras en palabras, se dice, con la referencia de la alerta. Es
  observación, no compuerta.
- Supuestos, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
