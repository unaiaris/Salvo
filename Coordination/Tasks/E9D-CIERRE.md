# Salvo — Task brief `E9D-CIERRE`

## Identificación

- Work ID: `E9D-CIERRE`
- Etapa: 9
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-07
- Rama/worktree: `claude/e9d-cierre`
- Commit base: `ad70c58`, el `merge-base` real de `claude/e9d-cierre` con `main`. **Esta línea se
  commitea en la rama, no en `main`**: es la lección de `E8B`.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E9A`, `E9B`, `E9C1` y `E9C2` integradas (merges `41343c1`, `4f7daf9`, `0d65f9a` y
  `835a76a`). Es la última tarea del MVP.

## Resultado esperado

**Es la tarea que decide qué ve alguien que abre este repositorio por primera vez.** Se escribe con
ese lector en la cabeza y no con un checklist: un revisor de Koin, o quien lo mire en una entrevista.

Hoy el repositorio miente en cifras, y no por descuido sino porque cuatro tareas seguidas lo
cambiaron. El bloque de corpus del README describe el corpus v1 entero: 18 fraudes, 18 alertas sin
banda `ALTA`, y **precisión, recall y F1 en 1,00**, que es exactamente la cifra que la Etapa 9 existe
para destruir. El guion repite lo mismo. Las seis capturas son de una consola anterior al idioma y a
la accesibilidad.

Al terminar, **toda cifra que un documento afirma sale de una corrida real y está fechada**, la
deuda que la etapa decidió no pagar está escrita con nombre, y el artículo para revisores dice lo
que el repositorio dice.

## Contexto obligatorio

- `Coordination/Tasks/E9-DISENO.md` (**v2**), decisión **D10** entera: el inventario del repaso.
- `Coordination/Handoffs/Claude.md`, las entradas de **`E8A`, `E9A`, `E9B`, `E9C1` y las dos fases de
  `E9C2`**. Ahí están las cifras reales y la deuda que cada una declaró.
- `DesignAgent/Salvo-Blueprint.md`, **§11 «Etapa 9»** (línea 729), que es la sección que gobierna el
  cierre, y la bitácora de decisiones.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 9»: el último ítem es el que esta tarea cierra.
- `Coordination/Workboard.md`, la sección de lecciones: varias son material del README.
- `README.md`, el bloque entre `<!-- corpus:inicio -->` y `<!-- corpus:fin -->` (líneas 392 a 422).
- `docs/guion-demo.md`, `docs/muestras/README.md`, `docs/capturas/README.md`.
- `scripts/capturas.sh`, `tools/capturas/capturar.mjs` y `scripts/check-docs.sh`.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

#### 1. Las cifras, de una corrida real y fechada

El bloque `corpus:inicio`/`corpus:fin` se regenera **entero**, de una base nueva: migrar, sembrar,
correr scoring, y leer `/api/dashboard`, `/api/evaluation-metrics` y `/api/alerts`. **Ninguna cifra
se copia de un handoff**; los handoffs sirven para contrastar, no para transcribir.

Lo que hoy es falso y hay que reemplazar, verificado línea por línea:

| Dice hoy | Qué pasó |
| --- | --- |
| «18 fraudes y 282 legítimos» | El corpus v2 tiene **28** fraudes |
| «18 alertas: 13 media, 0 alta, 5 crítica» | Hay **23** alertas y la banda `ALTA` existe por primera vez |
| «precisión 1,00, recall 1,00 y F1 1,00» | **F1 holdout 0,632** y 0,688 en calibración. Es el corazón de la etapa |
| «`foreign_country` en 34, `amount_anomaly` en 18, `new_buyer_high_value` en 5» | Las **seis** reglas disparan |
| «3 comercios, uno por moneda: UYU, BRL y USD» | **Esto sigue siendo cierto y no se toca.** Lo que falta es **el país de cada comercio**: el corpus vive en el corredor UTC−3 —UY, BR y AR— y `MER_US_MARKET` factura en USD con 90 de sus 100 pedidos desde Argentina |
| «del 2026-05-01 al 2026-08-28» | Verificar, no asumir |

Lo mismo en `docs/guion-demo.md`, cuyas líneas 177 y 178 repiten «18 alertas abiertas» y «F1 1,00»,
y en `docs/muestras/README.md`, donde el `16,7 %` de `unusual_hour` **se recalcula sobre el corpus
v2** en vez de arrastrarse: fue falso una vez y no se hereda de memoria.

**Y hay prosa invalidada que ningún `grep` de cifras alcanza, porque son frases.** Es la parte que
más fácil se pasa por alto y la que peor queda, porque un párrafo entero que describe otro proyecto
se lee como descuido y no como desactualización:

| Dónde | Qué afirma, y por qué ya es falso |
| --- | --- |
| `README.md` 373–379 | «El corpus alcanza tres de las seis reglas», «un solo arquetipo de fraude», «la banda alta, 70–89, tampoco aparece». Las **seis** reglas disparan, hay **siete** arquetipos y la banda `ALTA` tiene seis alertas |
| `README.md` 381–384 | «Los detalles de las señales están en inglés» y «cuando el motor los emita directamente, el extractor se borra». **El motor ya los emite y el extractor ya no existe**: se borró en `E9B` |
| `docs/guion-demo.md` 170–172 y 183–186 | «Las métricas dan perfectas» y «tres de las seis reglas nunca abren una alerta, y la banda alta no se alcanza». **Están fuera del bloque marcado**, así que regenerar el bloque no las toca |
| `docs/capturas/README.md` 24 | «con scores de solo 60 y 90». El v2 llega a 70 y 80 |

El inventario que `E8A` entregó es explícitamente **de cifras fuera del README** y no lista esta
prosa. Buscar «lo que quedó viejo» con `grep` de números no la encuentra: hay que leer las secciones
«Límites declarados» del README y las de límites del guion, enteras.

**Y la afirmación que hace honesta a toda la tabla: la tasa base es del 9,3 % y es de construcción.**
Veintiocho fraudes en trescientos pedidos no es una medición de nada; es un parámetro que se eligió
al armar la fixture. Con los errores puestos a mano, **F1 es un parámetro del diseño y no un
resultado**, y decirlo en el README vale más que la cifra misma. Va con la matriz de confusión con
sus conteos y su `n`, no como una razón con dos decimales: con cien pedidos de holdout cada falso
negativo mueve el recall doce puntos.

#### 2. Las seis capturas y el pedido de la demo

`./scripts/capturas.sh` regenera las seis sobre una base nueva. La consola cambió dos veces desde que
se sacaron —la frase de cada señal se compone desde campos y **ahora muestra la mediana**, y la
accesibilidad movió marcado—, así que **las aserciones previas a cada disparo pueden fallar**, y eso
es lo que tienen que hacer si el texto ya no está: `capturar.mjs` no saca una captura sin comprobar
antes que en pantalla esté lo que promete. Cada ancla que falle se actualiza contra el texto real.

Las capturas se sacan **en castellano**, que es el idioma por defecto y el de la demostración.

#### 3. La deuda, dicha con nombre

Ninguna se paga. Todas se escriben, en «Límites declarados» del README, y con el motivo:

- **El contraste de color no lo comprueba nada.** `jsdom` no calcula estilos, así que `axe-core`
  devuelve `color-contrast` como incompleto y está apagada por su nombre para que un incompleto no
  se lea como aprobado.
- **El recorrido con lector de pantalla fue parcial**: la portada y el encabezado de la cola, sin
  hallazgos, y se interrumpió ahí. **Ningún documento puede decir que alguien recorrió la consola
  entera sin ver la pantalla.** La pasada automática sí se hizo entera: 31 reglas y `axe-core` en la
  compuerta.
- **El portugués no lo revisó un hablante nativo**, y el glosario está listo para que alguien lo
  corrija fila por fila.
- **`MER_US_MARKET` es hoy el comercio argentino.** Los `merchantId` del v1 se conservaron a
  propósito, porque la referencia es `merchantId:merchantReferenceId` y renombrar uno anula el
  guardián de conflicto: dos corpus contradictorios convivirían en la misma base. El nombre quedó
  mintiendo y **se explica en vez de arreglarse**, porque arreglarlo cuesta el guardián.
- **Los códigos de error de fila** que pegan el mensaje inglés de la API, y los dos que no tienen
  rótulo.
- **«Se importaron 1 pedidos»**, en `importSomeTitle`.
- **`glosario.mjs` vive dentro de `frontend/src/lib/i18n/`**, que es un generador dentro del árbol de
  fuentes. Se dice, y la tarea decide si lo mueve a `tools/` o lo deja con su motivo.
- Los dos hallazgos que la Etapa 8 dejó: **el desempate de la cola por un identificador aleatorio** y
  que **`explanationId` no se pinta** en el panel de revisión. Los dos quedan **afuera y dichos**.
- **Anthropic sigue siendo una decisión aparte**, y sigue sin adaptador: `AI_PROVIDER=anthropic` no
  arranca a propósito.

#### 4. Las bases `.db`, listadas para que el coordinador borre

Hay **siete** en `backend/src/Salvo.Api/`. La tarea las lista con su fecha y qué contiene cada una, y
**no borra ninguna**: `rm` está denegado y es regla del usuario. Decide él.

Lo mismo con `demo-orders.v1.json`: la tarea **recomienda** conservarlo o retirarlo, con el motivo, y
el coordinador decide. Conservarlo documenta de dónde vino el v2; retirarlo evita que alguien lo
siembre por error.

#### 5. `Salvo-Getting-Started.md`

Gana la línea que faltó cuando el `git push` falló con `HTTP 400`: un empujón con las capturas
adentro pasa el buffer por defecto de 1 MiB, y `git config http.postBuffer 524288000` lo arregla. Es
información que le sirve a cualquiera que clone y empuje este repositorio.

#### 6. El artículo para revisores

**Se escribe de cero.** El artículo vive fuera del repositorio, publicado por el coordinador, y la
tarea **no puede leerlo**: no está en el árbol, ni en los handoffs, ni en `_local/`. Intentar
«actualizarlo» sería editar a ciegas un texto que no se tiene. Lo que hay que producir es un texto
nuevo, escrito contra el estado final del repositorio, que el coordinador publica reemplazando el
anterior.

Qué tiene que decir: las cifras nuevas, la explicación verificada, el idioma en la identidad de la
fila, la accesibilidad con su alcance real —automática entera, recorrido humano parcial—, y **la
geografía que corrigió el amigo del coordinador: Koin opera en Brasil y México como países
principales, con presencia en algunos otros de LATAM, y no en Uruguay ni en Estados Unidos.**

Va en el handoff, en un bloque que se pueda copiar entero.

#### 7. La compuerta y la bitácora

`scripts/check-docs.sh` tiene que seguir verde: cada ruta y cada nombre de test que el README cita
debe existir.

Sobre la bitácora: **las decisiones 62 a 68 ya existen** y cubren el umbral como política, la
contrapositiva de la 58 y el idioma por despliegue. **La única que puede faltar es la de las
dependencias de accesibilidad**, y hay que mirar antes si hace falta: la decisión 61 ya establece que
una dependencia se aprueba en el diseño de su etapa, y D7 lo hizo. Si con eso alcanza, **no se
agrega una entrada duplicada** y se dice por qué.

### Fuera

- **Arreglar cualquiera de las deudas del punto 3.** La tarea las escribe; pagarlas es post-MVP.
- Tocar el motor, el corpus, la fixture, el contrato o los diccionarios salvo por un literal que un
  documento cite mal.
- `/orders`, el proveedor que se porta mal, y Anthropic.
- Borrar archivos, incluidas las bases `.db`.
- Un tercer idioma, y regenerar las capturas en portugués.

### Paths autorizados

- `README.md`
- `docs/**`, incluidas las seis capturas
- `scripts/capturas.sh`, `scripts/demo.sh` y `tools/**` —no solo `tools/capturas/**`: si la tarea
  decide mover `glosario.mjs`, su destino cae ahí—
- `DesignAgent/Salvo-Getting-Started.md` y `DesignAgent/Salvo-Blueprint.md`, este último **solo** la
  bitácora de decisiones
- `Coordination/Handoffs/Claude.md`
- `frontend/src/lib/i18n/es.ts` y `pt.ts`, **solo** si un documento cita un literal que ya no existe
- `frontend/src/lib/i18n/glosario.mjs`, solo si la tarea decide moverlo, y
  `frontend/src/lib/i18n/glosario-pt.md`, cuya línea 9 **cita la ruta del script** y quedaría
  mintiendo si el script se mueve sin tocarla

**No** entran: `backend/**`, el resto de `frontend/src/**`, `Coordination/Workboard.md` y
`DesignAgent/Salvo-Progress.md` —los dos últimos los cierra el coordinador después del merge—.

### Paths reservados por otros trabajos

Ninguno: es la última tarea de la etapa.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Migraciones: **no**.
- Levantar la API y la consola, y correr `scripts/demo.sh` y `scripts/capturas.sh`: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- **Acciones destructivas: ninguna.** No se borra ni una base ni un archivo: se listan.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E9D-CIERRE.md` sin faltantes antes de empezar.
- [ ] **Toda cifra del bloque de corpus sale de una corrida real hecha en esta tarea**, y el bloque
      dice de qué fecha es. Ninguna transcrita de un handoff.
- [ ] `F1 = 1,00` no aparece en ningún documento.
- [ ] La matriz de confusión se publica con conteos y `n`.
- [ ] **La tasa base está declarada como de construcción**, y el README dice que con los errores
      puestos a mano F1 es un parámetro del diseño.
- [ ] El `16,7 %` de `unusual_hour` **recalculado** sobre el corpus v2, no arrastrado.
- [ ] **La prosa invalidada, reescrita**: las cuatro entradas de la tabla del punto 1. Un `grep` de
      cifras no las encuentra, así que se leen enteras las secciones de límites del README y del
      guion.
- [ ] Ninguna afirmación **verdadera** fue reemplazada: las monedas siguen siendo una por comercio, y
      lo que se agrega es el país.
- [ ] Las seis capturas regeneradas, con sus anclas actualizadas donde el texto cambió.
- [ ] Las nueve deudas del punto 3, escritas con su motivo.
- [ ] Las siete bases `.db` listadas con fecha y contenido. **Ninguna borrada.**
- [ ] `Salvo-Getting-Started.md` con la línea de `http.postBuffer`.
- [ ] El texto del artículo para revisores en el handoff, **escrito de cero**, en un bloque
      copiable, con la geografía corregida.
- [ ] `./scripts/check-docs.sh` verde, y `/gate` y `./scripts/smoke-ui.sh` verdes.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9D-CIERRE.md` | Brief válido |
| Corrida sobre base nueva | Las cifras del bloque salen de ahí, con fecha |
| `grep -rn "1,00" README.md docs/` | Sin F1 en 1,00 |
| `grep -rn "18 alertas\|18 fraudes" README.md docs/` | Sin resultados |
| «tres de las seis reglas» y «el extractor se borra» | Sin resultados en README ni en `docs/` |
| Lectura completa de «Límites declarados» y de los límites del guion | Ninguna frase describe el corpus v1 |
| Recálculo de `unusual_hour` sobre el v2 | La cifra publicada coincide |
| `./scripts/capturas.sh` | Seis capturas, con sus aserciones pasando |
| Lista de bases `.db` | Siete, con fecha y contenido, ninguna borrada |
| `./scripts/check-docs.sh` | Verde |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E9D-CIERRE` | Con el texto del artículo |

**Falsación exigida.** Una, y es la que corresponde a una tarea de documentación: **cambiar una cifra
del README por una falsa y comprobar que nada la detecta.** `check-docs.sh` verifica rutas y nombres
de test, no cifras. El punto no es agregar esa comprobación —no entra en esta tarea— sino
**escribirlo como límite**: el README dice qué parte de sí mismo está verificada mecánicamente y qué
parte depende de que alguien la regenere. Es la afirmación más honesta que un README puede hacer
sobre sí mismo.

## Decisiones delegadas

- La forma de la tabla de cifras y de la matriz de confusión.
- Dónde va cada deuda dentro de «Límites declarados», y en qué orden.
- Si `glosario.mjs` se mueve a `tools/` o se queda con su motivo escrito.
- La redacción del artículo para revisores.
- Qué anclas de `capturar.mjs` se reescriben y cómo.

## Detenerse y consultar si

- una cifra de la corrida no coincide con la del handoff de la tarea que la produjo;
- una captura no se puede sacar porque la consola cambió de forma y no solo de texto;
- `check-docs.sh` se pone rojo por un test que hay que renombrar;
- alguna deuda del punto 3 resulta ser un defecto que no se puede dejar dicho;
- una afirmación que el brief marca como falsa resulta ser cierta contra el árbol;
- hace falta tocar `backend/**` o el resto de `frontend/src/**`.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **La corrida de la que salen las cifras**, con su fecha y sus comandos.
- La lista de las siete bases `.db`, con fecha y contenido.
- **El texto del artículo para revisores**, listo para publicar.
- La falsación de la cifra falsa, con lo que no la detectó.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
