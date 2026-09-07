# Salvo — Task brief `E9C1-IDIOMA`

## Identificación

- Work ID: `E9C1-IDIOMA`
- Etapa: 9
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-07
- Rama/worktree: `claude/e9c1-idioma`
- Commit base: `163d7ea`, el `merge-base` real de `claude/e9c1-idioma` con `main`. **Esta línea se
  commitea en la rama, no en `main`**: un commit que declara la base y va a `main` pasa a ser la
  base y vuelve falso el campo que acaba de escribir. Es la lección de `E8B`.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5 · `high`**.
- Dependencias: `E9B-SENALES-TIPADAS` integrada (merge `4f7daf9`). La consola compone la frase de
  cada señal desde campos tipados desde esa tarea; sin eso, traducir sería reescribir prosa inglesa.
- Tamaño, medido y no estimado: **52 archivos del frontend** contienen literales en castellano —20
  en `frontend/src/app/alerts/[id]`, 8 en `import`, 6 en `dashboard`, 5 en `lib/api`, y 17 de ellos
  son tests—, más **41 llamadas a `expect_text`** en `scripts/smoke-ui.sh` —la aparición 42 es la
  definición de la función, en la línea 175—, unas 13 anclas repartidas en 3 llamadas a `expectText`
  en `tools/capturas/capturar.mjs`, el `<html lang="es">` de `frontend/src/app/layout.tsx` (línea 13),
  y los nombres de mes y las palabras de severidad dentro de la plantilla del backend.

## Resultado esperado

**El idioma deja de estar escrito en el código y pasa a ser una propiedad del despliegue.** La
consola entera se compone desde un diccionario, la explicación que el backend escribe y persiste
sale en el idioma del despliegue, y ese idioma **entra en la identidad de la fila**, que es la parte
que no es cosmética y la única que enseña algo.

**El castellano es el valor por defecto y el idioma de la demostración.** Quien clone el repositorio
y corra `scripts/demo.sh` ve exactamente lo que ve hoy. El portugués existe por dos razones, y las
dos importan: **un interruptor con un solo valor no está probado** —no se puede demostrar que el
idioma es del despliegue ni que entra en la identidad si nunca hay un segundo valor—, y Koin opera
en Brasil.

## Contexto obligatorio

- `Coordination/Tasks/E9-DISENO.md` (**v2**), decisión **D6** entera, y la nota de partición de
  D11.
- `Coordination/Tasks/E9-revision-adversarial.md`, hallazgo **7**: por qué el portugués no es «un
  diccionario más» en la explicación ni en la API.
- `Coordination/Handoffs/Claude.md`, entrada de **`E7D`**. Es el defecto que esta tarea puede
  repetir con otra cara: un arreglo que no alcanza a lo ya guardado no está terminado, y acá lo ya
  guardado son las filas de explicación.
- `Coordination/Handoffs/Claude.md`, entrada de **`E9B`**, sección «Riesgos o pendientes»: el
  décimo código de fallo que esta tarea implementa, y por qué el que se usa hoy
  —`PROVIDER_UNAVAILABLE`, el **primero** de la enumeración— miente.
- `DesignAgent/Salvo-Blueprint.md`: §5.3, §6 y las decisiones **33**, **57** y **58**.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 9», el ítem «Idioma del despliegue».
- Código, abierto antes de escribir nada:
  - `backend/src/Salvo.Infrastructure/DependencyInjection.cs`, líneas 82 a 105: **el molde**. Es
    cómo `AI_PROVIDER` falla al arrancar ante un valor que este build no puede servir, con un
    mensaje que dice qué usar en su lugar.
  - `backend/src/Salvo.Api/SystemEndpoints.cs`: `GET /api/system/capabilities` y
    `CapabilitiesResponse`.
  - `backend/src/Salvo.Infrastructure/Persistence/Configurations/AlertExplanationConfiguration.cs`:
    los dos índices únicos (líneas 146 y 159) y los once `CHECK`.
  - `backend/src/Salvo.Domain/Explanations/ExplanationFailureCode.cs` y
    `backend/src/Salvo.Domain/Explanations/ExplanationWireNames.cs`.
  - `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`:
    `MonthNames` (línea 43), `SeverityWord` (línea 228) y `Version = "e7-v2"`.
  - `backend/src/Salvo.Domain/Explanations/SpanishNumberFormat.cs`.
  - `frontend/src/lib/format.ts` y `frontend/src/lib/api/messages.ts`, 343 y 344 líneas.
  - `frontend/src/app/import/actions.ts`, `describeRecordError` en la línea 269.
  - `frontend/src/app/layout.tsx`: el `<html lang="es">` de la línea 13. **No consulta nada hoy.**
  - Las tres páginas que sí llaman a `fetchCapabilities()` del lado del servidor:
    `frontend/src/app/dashboard/page.tsx`, `frontend/src/app/import/page.tsx` y
    `frontend/src/app/alerts/[id]/page.tsx`.
  - `frontend/src/app/page.tsx`, cuyo comentario declara que la raíz **se queda estática a
    propósito**.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

#### 1. `SALVO_LANGUAGE`, con un solo origen

`SALVO_LANGUAGE=es|pt` la lee **la API**, que falla al arrancar ante un valor desconocido con el
molde de `AI_PROVIDER`: un mensaje que nombra el valor recibido y dice cuáles son los válidos. Sin
la variable, `es`.

La API la declara en `GET /api/system/capabilities`, que la consola ya consulta del lado del
servidor. **Nada de `NEXT_PUBLIC_`**, y nada de que el proceso de Next lea la variable por su
cuenta: si la consola y la API leyeran la misma variable por separado podrían discrepar en un
despliegue mal configurado, y el resultado sería una consola en portugués alrededor de un párrafo en
castellano. Tomarlo de la API hace esa discrepancia imposible por construcción.

`Accept-Language` queda **descartado y dicho**: haría la identidad de la explicación dependiente de
la petición y duplicaría filas por idioma según quién mire.

`.env.example` gana `SALVO_LANGUAGE=es` y **pierde `BUSINESS_TIMEZONE`**, que declara una
configuración que no existe: la variable no la lee nadie, verificado en todo el árbol, y la única
aparición del huso en el backend es una constante de `RuleConfig`. Lo pide la decisión D9.

#### 2. El idioma en la identidad de la fila, con su migración

Esta es la parte que no es cosmética, y hay **dos** índices que tocar, no uno:

- **`ux_alert_explanations_identity`** es `(riskEvaluationId, provider, templateVersion,
  alertPolicyVersion)`. Sin el idioma adentro, un despliegue que cambia a portugués busca la fila,
  **encuentra la castellana**, la da por buena y no escribe nunca la portuguesa. Es el defecto de
  `E7D`, exacto, con otro campo.
- **`ux_alert_explanations_pending_evaluation`** es `(riskEvaluationId, provider)` filtrado por
  `status = 'PENDING'`. Sin el idioma adentro, una reserva viva en un idioma hace que la reserva del
  otro **viole el índice** en vez de escribir su fila: el `FindAsync` por identidad no la encuentra,
  el `Reserve` la choca, y lo que el analista ve es un error de base de datos. Este segundo el
  diseño no lo había visto; salió de leer la configuración.

La columna es `NOT NULL` con un `CHECK` que la limita a los idiomas conocidos, y las filas
existentes migran a `es`, que es el idioma en el que fueron escritas.

**Y el índice no alcanza: hay tres costuras de consulta que tienen que filtrar por idioma**, y son
las que convierten la columna en comportamiento en vez de en una columna:

- `FindAsync` en `backend/src/Salvo.Infrastructure/Persistence/EfExplanationStore.cs` (línea 73)
  busca por los cuatro campos de la identidad. Es la que decide si hay que escribir una fila nueva.
- `ReserveAsync`, en el mismo archivo (línea 89), es la que la escribe y la que choca contra el
  índice parcial.
- **`GetExplanationsAsync` en `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs`
  (línea 268) es la lectura que la consola usa**, y es la más fácil de olvidar: si no filtra por
  idioma, un despliegue en portugués abre una alerta y le muestra el párrafo castellano que ya
  estaba escrito. La fila portuguesa existiría y nadie la vería. **Ese es el defecto de `E7D` otra
  vez**, esta vez del lado de la lectura.

Por el mismo camino baja hoy la versión de plantilla vigente:
`backend/src/Salvo.Application/Alerts/AlertProjection.cs`,
`backend/src/Salvo.Application/Alerts/GetAlertHandler.cs` y
`backend/src/Salvo.Application/Alerts/ReviewAlertHandler.cs`, que la pasa al detalle que devuelve la
revisión en sus líneas 55, 83, 119 y 139. `WrittenByAnotherTemplate` **no** se calcula en ninguno de
esos tres: sale de la comparación de la línea 89 de
`backend/src/Salvo.Application/Explanations/ExplanationViews.cs`, que es donde vivirá su equivalente
para el idioma si la tarea decide tenerlo. El camino se reserva entero: sub-reservar es lo que
rompió `E7A` a mitad de ejecución, y es la tercera vez que este `brief-check` lo encuentra.

**El décimo `ExplanationFailureCode` entra en esta misma migración.** Hoy una evaluación `e3-v1` sin
campos falla con `PROVIDER_UNAVAILABLE`, que **es falso**: no se llama a ningún proveedor, y un
analista que lea «el proveedor falló antes de responder» sale a depurar un proveedor que nunca se
invocó. Los nueve códigos están enumerados en `ck_alert_explanations_failure_code`, así que un
décimo exige tocar el `CHECK` — y ya que hay una migración, va en ella. El código nuevo necesita su
nombre de cable y su rótulo en los dos idiomas. **Ese rótulo va a
`EXPLANATION_FAILURE_LABELS`, en `frontend/src/lib/format.ts` (líneas 303 a 315), donde están los
nueve — no a `messages.ts`.** La decisión 57 gobierna `CONSOLE_CODES`, que es el catálogo de lo que
los endpoints emiten **como problema** y cuyo test lo afirma con `toEqual`; un código de fallo de
explicación es el valor de un campo dentro de un `200` y nunca fue de ese catálogo. Los nueve
rótulos existentes son además nueve literales en castellano dentro de `format.ts`, así que entran al
diccionario como todo lo demás.

**La versión de plantilla no sube.** El idioma no es una versión de plantilla: `e7-v2` en castellano
y `e7-v2` en portugués son dos filas que se distinguen por la columna nueva. Subir la versión
volvería «redactar con la plantilla vigente» un botón que ofrece cambiar de idioma, que no es lo que
ese botón significa.

#### 3. La plantilla del backend, en los dos idiomas

`DeterministicExplanationProvider` compone en el idioma del despliegue: los doce nombres de mes, las
tres palabras de severidad, y las seis frases de regla.

**Los textos dorados en castellano no cambian.** `ExplanationGoldenTests` tiene que pasar sin tocar
un solo carácter, igual que en `E9B`, y por la misma razón: es la señal más barata de que el
refactor no movió nada. Los dorados portugueses se agregan aparte.

**`SpanishNumberFormat` se renombra.** Va a formatear portugués, y el portugués de Brasil usa los
mismos separadores, así que el código no cambia una línea — pero el nombre pasa a ser mentira, y en
este proyecto un nombre que miente es un defecto. El comentario que explica por qué los dos idiomas
comparten formato numérico va en el tipo renombrado.

#### 4. El diccionario del frontend, con dientes

Los literales salen de los 52 archivos a un diccionario tipado. La forma exacta es decisión
delegada, con **una restricción que no lo es**: *una clave que falta en un idioma tiene que ser un
error de compilación, no un texto en el otro idioma ni una clave cruda en pantalla.* Un diccionario
que cae en silencio al castellano deja media consola sin traducir y ningún test en rojo, y eso es
peor que no traducir: promete algo que no cumple.

Entra `frontend/src/lib/api/messages.ts` con sus `CONSOLE_CODES`, que es una lista escrita a mano y
tiene su test de exactitud; entra `describeRecordError` de `frontend/src/app/import/actions.ts`, que
es el ítem «códigos de error de fila traducidos» del checklist; y entran las seis frases de señal y
`ruleLabel` de `frontend/src/lib/format.ts`, que nacieron en `E9B`.

**El idioma llega desde el servidor**, tomado de `capabilities`, y baja a los componentes. Si las
capacidades no se pueden leer, la consola se compone en `es`: el idioma es una preocupación de
presentación y un fallo ahí no puede dejar la página en blanco.

**Y acá hay un costo que se paga con los ojos abiertos.** El `<html lang>` vive en el layout raíz,
que hoy no consulta nada, y la raíz `/` **es estática a propósito**: el comentario de
`frontend/src/app/page.tsx` lo dice. Que el layout lea las capacidades vuelve dinámica toda la
consola, `/` incluida, porque el layout la envuelve. La alternativa sería que el proceso de Next
leyera `SALVO_LANGUAGE` por su cuenta, y eso rompe el origen único del punto 1 y abre justamente la
discrepancia que ese punto existe para cerrar. **Se elige volver dinámica la raíz**, y el comentario
de `page.tsx` se actualiza para decir por qué dejó de ser estática, en vez de quedar contradiciendo
al código. Son cinco rutas de una consola interna: no hay historia de CDN que se pierda.

**El diccionario cruza el borde del cliente como valores, nunca como objeto.** Ningún componente
cliente recibe el diccionario entero; `frontend/src/test/boundary.test.ts` lo hace cumplir igual, y
se escribe acá porque la forma se delega y ésta es la parte que no.

#### 5. El portugués, entregado y rotulado

El diccionario `pt` completo, y **un archivo de glosario** —clave, castellano, portugués— pensado
para que un hablante nativo lo corrija de a una fila sin abrir el código. El diccionario y el
glosario dicen, con todas las letras y en el archivo, que **no los revisó un hablante nativo**.

No se traducen: los identificadores de regla (`amount_anomaly` y compañía), los códigos de cable,
los nombres de comercio del corpus, ni los `merchantReferenceId`.

#### 6. El smoke prueba el interruptor, no solo el castellano

`scripts/smoke-ui.sh` tiene 41 llamadas a `expect_text` en castellano. La tarea **no las duplica a mano
en dos idiomas**: la corrida principal sigue siendo en `es` y se agrega **una pasada corta en `pt`**
sobre un puñado de pantallas, que es lo que convierte «es conmutable» en una afirmación verificada
de punta a punta en vez de una promesa del diccionario.

Y una comprobación que vale más que todas las demás juntas: **con la base ya sembrada y una
explicación escrita en castellano, cambiar `SALVO_LANGUAGE` a `pt` y pedir la explicación de la
misma alerta tiene que escribir una fila nueva en portugués, dejando la castellana intacta.** Esa
sola comprobación es la que distingue esta tarea de un `find` y `replace`.

#### 7. Blueprint y bitácora

- El idioma es **del despliegue**; por persona es post-MVP y depende de la autenticación. Dos
  personas del mismo comercio con idiomas distintos no se pueden atender sin identidad, y ése es el
  límite correcto para una instancia por comercio.
- `Accept-Language` descartado, con el motivo.
- El idioma entra en la identidad de la explicación y **no** en la versión de plantilla.

### Fuera

- **Toda la accesibilidad**: es `E9C2`, y va después a propósito, porque crea texto nuevo y con el
  diccionario ya puesto ese texto nace en los dos idiomas.
- El README, el guion, las capturas y el artículo para revisores: `E9D`. Las capturas se regeneran
  allá y siguen siendo en castellano.
- `tools/capturas/capturar.mjs`: sus ~13 anclas quedan como están, en castellano, porque la corrida de
  capturas es en `es`. Si el archivo tiene que cambiar, la tarea para y consulta.
- Un tercer idioma, y cualquier mecanismo de idioma por persona o por petición.
- Tocar el motor, el corpus, las métricas o el dashboard salvo por sus literales.
- `/orders`, el proveedor que se porta mal, y Anthropic.

### Paths autorizados

**Backend**

- `backend/src/Salvo.Domain/Explanations/**`
- `backend/src/Salvo.Infrastructure/Explanations/**`
- `backend/src/Salvo.Infrastructure/Persistence/**` — la configuración, la migración nueva, y las
  tres costuras de consulta del punto 2: `EfExplanationStore.cs` y `EfAlertStore.cs`
- `backend/src/Salvo.Infrastructure/DependencyInjection.cs`
- `backend/src/Salvo.Application/Explanations/**`
- `backend/src/Salvo.Application/Alerts/**` — `AlertProjection.cs`, `GetAlertHandler.cs` y
  `ReviewAlertHandler.cs`, por donde baja hoy la versión de plantilla vigente
- `backend/src/Salvo.Api/SystemEndpoints.cs` y `backend/src/Salvo.Api/Program.cs`
- `backend/tests/**`

**Frontend**

- `frontend/src/**` — es la tarea que toca los 52 archivos, y reservar menos sería mentir.
  **`frontend/src/lib/api/guards.ts` entra de lleno, no «solo por sus literales»**: el idioma viaja
  en `capabilities`, y `projectCapabilities` (línea 782) proyecta campo por campo y devuelve un
  literal con exactamente dos. En cuanto se recaptura el esquema, `Capabilities` gana un campo y ese
  archivo **deja de compilar** hasta que lo proyecte. Es la primera lección de la Etapa 7, textual:
  la guarda descarta lo que no conoce.
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, **solo recaptura**, y
  **solo por el campo nuevo de `capabilities`**: el contrato declara `failureCode` como
  `null | string` sin enumeración, así que el código décimo no viaja por ahí y la recaptura no lo
  trae. La exactitud de los códigos se afirma contra la lista escrita a mano de
  `frontend/src/app/alerts/[id]/explanation-block.test.tsx`, que hoy enumera los nueve: ése es el
  oráculo, y hay que agregarle el décimo.

**Otros**

- `.env.example`
- `scripts/smoke-ui.sh`
- `DesignAgent/Salvo-Blueprint.md`: las tres entradas del punto 7, **más §7 y §9**, que esta tarea
  desfasa y que por lo tanto se corrigen acá y no en `E9D`. §7 declara la identidad de
  `AlertExplanation` con cuatro campos y la unicidad parcial sobre dos, y las dos pasan a llevar el
  idioma; el bloque dotenv de §9 conserva `BUSINESS_TIMEZONE`, no tiene `SALVO_LANGUAGE`, y dice que
  «la Etapa 9 la saca o la conecta». `check-docs.sh` solo lee el README, así que la compuerta **no**
  detecta este desfase: lo detecta quien lo lea
- `Coordination/Handoffs/Claude.md`

Todo comportamiento modificado lleva su test, como exige `AGENTS.md`.

### Paths reservados por otros trabajos

- `tools/capturas/**`, `docs/**`, `README.md`, `scripts/capturas.sh`: `E9D`.
- Las correcciones de accesibilidad: `E9C2`.

**Cuidado con la compuerta**: `scripts/check-docs.sh` verifica que cada ruta y cada nombre de test
que el README cita exista. Renombrar un test que el README nombra rompe la compuerta, y el README
está reservado por `E9D`. Si hay que renombrar uno, la tarea para y consulta.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**. Nada de una biblioteca de i18n: el diccionario es un objeto tipado y
  agregar una dependencia para eso es superficie sin beneficio en una consola de cinco rutas.
- **Migraciones: sí**, una sola, con la columna de idioma en los dos índices y el `CHECK` del código
  de fallo. Es la única tarea de la etapa que las tiene permitidas.
- Levantar la API para recapturar el OpenAPI: autorizado.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR.
- **Acciones destructivas: ninguna.** `rm` está denegado y es regla del usuario: lo que haya que
  borrar se deja escrito en la entrega.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E9C1-IDIOMA.md` sin faltantes antes de empezar.
- [ ] `SALVO_LANGUAGE` con un valor desconocido **impide arrancar la API**, con un mensaje que
      nombra el valor recibido y los válidos.
- [ ] Sin la variable, la consola y la explicación salen en castellano, y `scripts/demo.sh` produce
      exactamente lo de hoy.
- [ ] **La comprobación central**: con una explicación ya escrita en castellano, cambiar a `pt` y
      pedir la explicación de la misma alerta escribe una fila nueva, y la castellana queda intacta.
- [ ] Dos reservas `PENDING` en idiomas distintos sobre la misma evaluación **no violan el índice**.
- [ ] **La lectura que la consola usa filtra por idioma**: un despliegue en portugués no muestra el
      párrafo castellano de una alerta ya explicada.
- [ ] `projectCapabilities` proyecta el campo nuevo, y una respuesta sin ese campo se descarta.
- [ ] `describeRecordError` sale en los dos idiomas, con su test — es el ítem «códigos de error de
      fila traducidos» del checklist.
- [ ] El rótulo del código nuevo está en `EXPLANATION_FAILURE_LABELS` de `frontend/src/lib/format.ts`
      y **no** en `CONSOLE_CODES`.
- [ ] §7 y §9 del Blueprint dicen lo que el código hace.
- [ ] `ExplanationGoldenTests` pasa **sin tocar un solo texto**.
- [ ] Una clave que falta en un idioma es **error de compilación**, no un texto en el otro idioma ni
      una clave cruda en pantalla.
- [ ] El décimo código de fallo existe, viaja, tiene rótulo en los dos idiomas y su test de
      exactitud, y una evaluación `e3-v1` lo usa en vez de mentir con `PROVIDER_UNAVAILABLE`.
- [ ] Las filas de explicación existentes migran a `es`.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios **después** de la
      migración nueva.
- [ ] El smoke corre en `es` completo y hace una pasada corta en `pt`.
- [ ] `<html lang>` sale del idioma del despliegue.
- [ ] `BUSINESS_TIMEZONE` ya no está en `.env.example`.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.
- [ ] Handoff con el glosario y las falsaciones.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9C1-IDIOMA.md` | Brief válido |
| `SALVO_LANGUAGE=fr` | La API no arranca, y dice cuáles valen |
| Sin variable | Todo en castellano, como hoy |
| Explicación en `es`, cambio a `pt`, misma alerta | Fila nueva; la castellana intacta |
| Dos `PENDING` en idiomas distintos | Sin violación de índice |
| Alerta explicada en `es`, despliegue en `pt` | La consola muestra el párrafo portugués |
| `projectCapabilities` sin el campo nuevo | La respuesta se descarta |
| `describeRecordError` en los dos idiomas | Con su test |
| `ExplanationGoldenTests` | Textos castellanos idénticos |
| Clave faltante en `pt` | Error de compilación |
| Evaluación `e3-v1` sin campos | El código nuevo, no `PROVIDER_UNAVAILABLE` |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `./scripts/smoke-ui.sh` | Verde en `es`, y la pasada `pt` verde |
| `grep -rn "BUSINESS_TIMEZONE" .` | Sin resultados fuera de documentación histórica |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E9C1-IDIOMA` | Con el glosario y las falsaciones |

**Falsaciones exigidas.** Cada una se rompe a propósito, se corre el test, se anota el error exacto,
y se deshace:

1. Sacar el idioma de `ux_alert_explanations_identity` → el despliegue en `pt` encuentra la fila
   castellana y no escribe nunca la portuguesa. **Es el defecto de `E7D` reproducido a pedido**, y
   la única forma de demostrar que el índice sirve para algo.
2. Sacar el idioma del índice parcial de `PENDING` → la segunda reserva choca contra el índice.
3. Hacer que una clave faltante caiga al castellano en vez de fallar → la pantalla se ve bien y
   ningún test se pone rojo. Anotar exactamente eso: que el modo de falla es invisible.
4. `SALVO_LANGUAGE=fr` → la API no arranca.
5. Dejar `PROVIDER_UNAVAILABLE` en la evaluación `e3-v1` → el test del código nuevo falla.
6. Sacar el filtro por idioma de `GetExplanationsAsync` → la fila portuguesa se escribe y la consola
   sigue mostrando la castellana. **La fila correcta existe y nadie la ve**: es el modo de falla más
   caro de la tarea, porque todo lo demás está verde.
7. Dejar `projectCapabilities` sin tocar tras la recaptura → no compila.

## Decisiones delegadas

- La forma del diccionario y de la tipificación, mientras una clave faltante no compile.
- Cómo baja el idioma desde el árbol de servidor a los componentes.
- El formato del archivo de glosario, mientras se pueda corregir fila por fila sin abrir código.
- Qué pantallas cubre la pasada `pt` del smoke.
- El nombre del tipo que reemplaza a `SpanishNumberFormat` y el del décimo código de fallo.
- El nombre de la columna de idioma y su representación en el cable.

## Detenerse y consultar si

- un texto castellano de `ExplanationGoldenTests` cambia;
- la migración obliga a reescribir alguna fila que no sea poner `es` en la columna nueva;
- hace falta una dependencia de i18n;
- hace falta tocar `tools/capturas/**` o renombrar un test que el README nombra;
- el idioma no puede llegar a `<html lang>` sin leer la variable desde el proceso de Next, o
  aparece un camino más barato que conserve el origen único sin volver dinámica la raíz;
- la pasada `pt` del smoke exige duplicar las 41 anclas.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **El glosario**, con su cantidad de entradas.
- El resultado de la comprobación central, con las dos filas mostradas.
- Las siete falsaciones, con el error exacto de cada una.
- Comandos y resultados exactos.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
