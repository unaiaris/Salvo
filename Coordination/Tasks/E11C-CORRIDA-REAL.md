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
pasó**: los textos que el verificador aceptó, y de los que rechazó, el código y el detalle. El README
deja de decir que el adaptador nunca se llamó y dice qué se midió, con su fecha y su costo.

**La corrida la hace el coordinador, con su clave, y en tres tiempos que cuidan el saldo.** El
coordinador cargó US$ 5 de crédito prepago, sin recarga automática, y quiere que le queden para otras
pruebas: la clave vive en un espacio de trabajo dedicado con un tope mensual de US$ 1.

| Tiempo | Quién | Qué | Costo |
| --- | --- | --- | --- |
| 0. Ensayo en seco | El agente | El script entero contra la plantilla, sin clave y sin red | US$ 0 |
| 1. Canario | El coordinador | 2 alertas contra la API real | Unos US$ 0,01; a lo sumo US$ 0,08 |
| 2. Corrida completa | El coordinador | Las 23 alertas | Unos US$ 0,14; a lo sumo US$ 0,91 |

El ensayo en seco existe porque **el cuerpo del script nunca se ejecutó**: `E11B` probó que se niega
sin clave, antes de compilar nada. Un error en la parte que escribe los archivos, descubierto después
de pagar las llamadas, es el gasto que este orden evita.

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

### Dentro, en tres tiempos, y cada uno espera al anterior

**Tiempo 0 — el agente prepara el script y lo ensaya en seco. Después se detiene.**

1. El campo «Commit base» del brief.
2. **`EXPLICAR_MAX_ALERTAS`**, opcional: un entero positivo que limita la corrida a las primeras N
   alertas, en el orden en que la API las devuelve. Sin la variable, corre todas, como hoy. Un valor
   que no sea un entero positivo detiene el script antes de compilar.
3. **Una corrida limitada no publica.** Con `EXPLICAR_MAX_ALERTAS`, los dos archivos van a un
   directorio temporal fuera del repositorio, y el script dice dónde. `docs/explicaciones-modelo/`
   queda solo para la corrida completa: así el canario no ocupa la carpeta del día, que el script se
   niega a pisar.
4. **`EXPLICAR_ENSAYO=1`**, el ensayo en seco: levanta la API con `AI_PROVIDER=mock`, **no lee ningún
   archivo de entorno**, no exige `SALVO_ENV_FILE`, y escribe en un directorio temporal. Recorre el
   mismo código que la corrida real —la siembra, el scoring, los pedidos, la lectura de la base, los
   dos archivos— con la plantilla como escritor. El costo que informa es cero, y el resumen dice que
   fue un ensayo.
5. **El agente corre el ensayo dos veces**, completo y con `EXPLICAR_MAX_ALERTAS=2`, y comprueba la
   salida: 23 y 2 filas, todas `READY` por la plantilla, los dos archivos bien formados, y nada
   escrito en `docs/explicaciones-modelo/`. Si el ensayo descubre un defecto del script, lo corrige.
6. **El agente se detiene** y entrega al coordinador los dos comandos exactos, el del canario y el de
   la corrida completa.

**Tiempo 1 — el coordinador corre el canario y pega la salida.**

`SALVO_ENV_FILE=.env EXPLICAR_MAX_ALERTAS=2 ./scripts/explicar-con-anthropic.sh`. El agente lee lo
que el coordinador pega —la salida de la terminal y el `explicaciones.md` del directorio temporal— y
contesta **una de tres cosas**, sin tocar código:

- **Seguir**: al menos una fila `READY`, con `ProviderVersion` igual a `claude-sonnet-5-5`.
- **Seguir, con aviso**: las filas fallan por el verificador —`NOT_GROUNDED_NUMBER`,
  `NOT_GROUNDED_RULE`, `TOO_LONG`—. La API funciona y el modelo escribe; la corrida completa dirá
  cuántas, y la decisión sobre el prompt es posterior y del coordinador.
- **Parar**: `PROVIDER_UNAVAILABLE` con `HTTP 400` o `credentials`, `MALFORMED_OUTPUT`, o cualquier
  forma que el simulador no reproduce. La API real no se comporta como el simulador, y la etapa manda
  **corregir el simulador primero**: eso es otra tarea, con su brief.

Un `PROVIDER_TIMEOUT` en la **primera** fila se lee sabiendo que la primera petición compila la
gramática: el reintento de la fila lo resuelve o no, y eso es el dato.

**Tiempo 2 — el coordinador corre la completa, y el agente publica.**

7. El coordinador corre `SALVO_ENV_FILE=.env ./scripts/explicar-con-anthropic.sh` y avisa. Los dos
   archivos quedan en `docs/explicaciones-modelo/AAAA-MM-DD/`.
8. **El agente no edita esos dos archivos**: se commitean tal como el script los escribió. Antes de
   commitearlos comprueba que cumplen D16 —ninguna fila `FAILED` trae `summary`— y que no contienen
   la clave ni nada con forma de clave.
9. **El README dice lo que se midió**, con la fecha de la corrida: cuántas alertas, cuántas aceptadas
   y cuántas rechazadas, por qué códigos, los tokens, el costo con la fecha del precio, y el modelo
   que respondió. Enlaza la carpeta. La fila del redactor y «Límites declarados» dejan de decir que el
   adaptador nunca se llamó. **Las cifras se copian de `explicaciones.json`**, no de la terminal ni de
   memoria.
10. `Salvo-Getting-Started.md`: cómo se corre el script, con el ensayo y el canario.
11. El handoff, con la lectura de la corrida: las cinco cosas que `E11B` dejó para mirar primero, una
    por una, con lo que se vio.

### Fuera

- **Que el agente corra el script contra Anthropic**, lea `.env`, pida la clave o la vea. El agente
  corre **solo** el ensayo en seco.
- **Cambiar el verificador, el prompt o el adaptador.** Si la corrida pide un `anthropic-p2`, o
  corregir el simulador, es otra tarea.
- **Repetir la corrida para que salga mejor.** Es una tirada: se publica la que salió.
- **Editar a mano los archivos de la corrida**, o publicar cualquier texto rechazado.
- Correr contra la instancia pública, o tocar `render.yaml`, el `Dockerfile` o el flujo de CI.

### Paths autorizados

- `scripts/explicar-con-anthropic.sh`.
- `docs/explicaciones-modelo/**`, **solo** para agregar los archivos que el script escribió.
- `README.md` y `DesignAgent/Salvo-Getting-Started.md`, **solo** lo de los puntos 9 y 10.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11C-CORRIDA-REAL.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno. No hay otra tarea abierta.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no**.
- Red: **no**. El ensayo en seco no la usa.
- **Secretos: ninguno, nunca.** El agente no lee `.env`, no corre el script sin `EXPLICAR_ENSAYO=1`,
  y no pide que le peguen la clave. Lo que el coordinador pega es salida de terminal y archivos que
  el script escribió, que no la contienen.
- Escrituras externas: **no**. El agente no hace push.
- Acciones destructivas: **no**. El script no borra nada, y las bases de las corridas quedan donde
  están.

## Criterios de aceptación

- [ ] `EXPLICAR_MAX_ALERTAS` limita la corrida, y un valor inválido detiene el script antes de
      compilar.
- [ ] Una corrida limitada escribe fuera del repositorio; `docs/explicaciones-modelo/` no cambia.
- [ ] `EXPLICAR_ENSAYO=1` corre sin `SALVO_ENV_FILE`, sin leer ningún archivo de entorno y con
      `AI_PROVIDER=mock`; el agente lo corrió completo y con 2 alertas, y pega las dos salidas.
- [ ] Sin `EXPLICAR_ENSAYO`, el script se sigue negando en los cinco casos que `E11B` probó.
- [ ] El canario corrió, y el agente contestó seguir, seguir con aviso o parar, con el motivo.
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
| `./scripts/explicar-con-anthropic.sh`, sin ninguna variable | Se niega, nombrando `SALVO_ENV_FILE` |
| El canario, **que corre el coordinador** | Su salida, pegada en la entrega tal como salió |
| La corrida completa, **que corre el coordinador** | Los dos archivos, commiteados sin editar |
| `git grep -n "sk-ant" -- docs README.md` | Sin resultados |
| Las cifras del README contra `explicaciones.json` | Iguales |
| `./scripts/check.sh` y `./scripts/smoke-ui.sh` | Verdes |

## Decisiones delegadas

- Cómo se llama y dónde vive el directorio temporal de una corrida limitada o de un ensayo.
- La redacción del README y de la guía, mientras cada cifra salga del archivo.
- Si el ensayo necesita algún otro ajuste menor del script para poder correr sin clave.

## Detenerse y consultar si

- el ensayo en seco exige tocar algo fuera de `scripts/explicar-con-anthropic.sh`;
- el canario da cualquiera de las señales de **parar**;
- la corrida completa termina con más rechazadas que aceptadas: qué se publica y si hay un
  `anthropic-p2` lo decide el coordinador;
- la corrida completa no termina, o el tope del espacio de trabajo la corta a la mitad;
- los archivos de la corrida traen un texto rechazado o algo con forma de clave;
- cualquier paso pide la clave, leer `.env` o correr el script real desde el agente.

## Entrega requerida

- Resumen, archivos tocados y commits.
- Las dos salidas del ensayo en seco.
- La salida del canario y la respuesta que se le dio.
- La lectura de la corrida completa: aceptadas y rechazadas por código, tokens, costo, y las cinco
  comprobaciones que dejó `E11B`.
- Qué dijo el modelo que el verificador no pudo ver: si alguna explicación aceptada es falsa por
  inversión, atribución cruzada o cifras en palabras, se dice, con la referencia de la alerta. Es
  observación, no compuerta.
- Supuestos, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
