# Salvo — Task brief `E7D-PLANTILLA-VIGENTE`

## Identificación

- Work ID: `E7D-PLANTILLA-VIGENTE`
- Etapa: 7
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-06
- Rama/worktree: `claude/e7d-plantilla-vigente`
- Commit base: `459be2c`, el `HEAD` de `main` que cierra `E7C-PULIDO-EXPLICACION`
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. El trabajo es chico, pero lo que se corrige es
  una costura entre dos caminos que hoy se contradicen sobre cuál es «la» explicación, y el arreglo
  tiene que dejarlos de acuerdo sin romper la razón por la que la API se niega a reescribir un texto
  ya escrito. Es el mismo argumento que sostuvo Opus en `E7B`.
- Dependencias: ninguna. `E7A`, `E7B` y `E7C` integradas.

## Resultado esperado

Cuando la explicación que la consola muestra la escribió una plantilla anterior a la vigente, la
analista puede pedir que se redacte de nuevo con la actual. La fila vieja **no se toca**: la nueva se
escribe al lado y la lectura se queda con la más reciente, que es lo que el código ya hace. Nada del
grounding, del ciclo de vida ni de la política de no reescribir una explicación de la misma versión
cambia.

## El defecto, con su evidencia

`E7C` subió la plantilla a `e7-v2` para que el arreglo del texto alcanzara a lo ya escrito. No lo
alcanza, porque los dos caminos no coinciden en qué fila es la vigente:

- **Escritura.** `EfExplanationStore.FindAsync` busca por la identidad completa, `templateVersion`
  incluida. Con la plantilla en `e7-v2` no encuentra fila y generaría una nueva.
- **Lectura.** `EfAlertStore.GetExplanationsAsync` agrupa por `RiskEvaluationId` **sin filtrar por
  versión de plantilla**, así que devuelve la fila `e7-v1`.
- **UI.** `explanation-block.tsx`, función `Actions`: el botón solo aparece si no hay explicación o
  si la que hay falló con intentos disponibles. Lo que lee está `READY`, así que no hay botón.

Verificado en `backend/src/Salvo.Api/salvo.db`: una sola fila, `e7-v1`, `READY`, con el texto
anterior al pulido. El único camino de escritura es `POST /api/alerts/{alertId}/explanation` y la
consola es su único llamador: no hay ninguna otra superficie que pueda crear la fila `e7-v2`.

El comentario de `Actions` enuncia la regla que quedó incompleta: «sobre una explicación escrita la
API se niega». Es cierto **para la misma versión de plantilla**, y falso cruzando un cambio de
versión.

## Contexto obligatorio

- `DesignAgent/Salvo-Blueprint.md`, **§4.7 Explicabilidad** y **decisión 58**, que es la que crea
  esta situación: cambiar el texto que produce la plantilla sube su versión.
- **Decisión 33** y `AlertProjection.IsOutdated`: el molde de un dato que se calcula al leer y no se
  persiste nunca.
- `Coordination/Tasks/E7-DISENO.md` (v2), **D3** y **D6**, en particular por qué la API se niega a
  reescribir una explicación ya escrita.
- `Coordination/Handoffs/Claude.md`, entradas de `E7A`, `E7B` y `E7C`.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 7 — Explicabilidad».
- Código:
  - `backend/src/Salvo.Infrastructure/Persistence/EfExplanationStore.cs`, `FindAsync`;
  - `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs`, `GetExplanationsAsync`;
  - `backend/src/Salvo.Application/Explanations/ExplanationViews.cs`, que ya expone
    `TemplateVersion` y `IsOutdated`;
  - `backend/src/Salvo.Application/Alerts/AlertProjection.cs`;
  - `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs:41`, la
    versión vigente;
  - `frontend/src/app/alerts/[id]/explanation-block.tsx`, `explanation-actions.tsx`,
    `explanation-action.ts`;
  - `frontend/src/lib/api/guards.ts:384`, que ya proyecta `templateVersion`.

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**

## Alcance

### Dentro

**1. La lectura dice si el texto lo escribió una plantilla anterior.** Un campo booleano en la vista
de la explicación —`writtenByAnOlderTemplate` o el nombre que corresponda—, **calculado al leer
comparando con la versión que el proveedor registrado declara hoy**, y nunca persistido. Es la forma
de `IsOutdated` y de `HasBandDivergence`.

La versión vigente la declara el proveedor inyectado, no una constante copiada: el día que el
proveedor sea otro, la comparación tiene que seguir siendo cierta.

**2. La consola ofrece redactar de nuevo, sin alarma.** Decidido con el usuario: **botón discreto,
sin aviso ni insignia**. Un cambio de redacción no invalida el contenido, y un cartel de
«desactualizada» sería ruido alarmante para un arreglo cosmético.

- La letra chica que hoy dice quién redactó y cuándo nombra además la versión de plantilla.
- Aparece un botón de volver a redactar con la plantilla vigente, con su propio rótulo — no el de
  «reintentar», que significa otra cosa.
- **El botón manda `regenerate: false`.** No es para esquivar la rama de conflicto: es la lectura
  honesta de lo que ocurre. Cruzando un cambio de versión no se reemplaza nada, se pide la
  explicación de la plantilla vigente, que no existe. `regenerate: true` significa «reemplazá lo que
  hay», y acá no hay nada que reemplazar. Si al implementarlo esto resulta falso —si con la versión
  vigente sí existe una fila—, parar y consultar.
- El aviso posterior dice qué pasó, con el criterio que `E7C` fijó: no repite ninguna oración de la
  leyenda del bloque.

**3. La API acepta ese pedido y conserva la fila vieja.** Con la plantilla vigente sin fila, el
`POST` genera una nueva; la anterior queda intacta y la lectura se queda con la más reciente, que es
lo que `GetExplanationsAsync` ya hace. Revisar que la tabla de estado × `regenerate` siga siendo
cierta y, si alguna de sus filas cambia de sentido cruzando versiones, corregirla y decirlo.

### Fuera

- Regeneración automática o masiva al subir la versión. La analista pide; el sistema no reescribe
  solo.
- Cualquier cambio en el grounding, el conjunto de hechos, el tokenizador o el ciclo de vida.
- Reescribir o borrar la fila vieja. Es el registro de lo que se pudo haber leído al decidir, y
  `alert_reviews.explanation_id` la referencia.
- Que la API acepte reescribir una explicación `READY` **de la misma versión**. Esa negativa se
  conserva tal cual.
- Un aviso, insignia o cartel de «desactualizada por plantilla».
- Migraciones: ninguna. El campo se calcula, no se guarda.
- El texto de la plantilla, que `E7C` ya dejó como corresponde.

### Paths autorizados

- `backend/src/Salvo.Application/**`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `frontend/**`
- `scripts/smoke-ui.sh`

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**. Migraciones: **no**.
- Levantar la API para recapturar el OpenAPI: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna. No borrar ni recrear bases.
- Commits locales en la rama: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E7D-PLANTILLA-VIGENTE.md` sin faltantes antes de empezar.
- [ ] El campo nuevo se calcula al leer y **no** existe como columna. Un test lo afirma sobre el
      esquema, con el molde de `AlertSchemaTests`.
- [ ] La versión vigente sale del proveedor registrado, no de una constante duplicada. Un test falla
      si se registra un proveedor con otra versión y la comparación no lo sigue.
- [ ] **Test de extremo a extremo del caso que motiva la tarea**: con una fila `READY` de una versión
      anterior, el `POST` genera una fila nueva, la anterior sigue existiendo sin cambios, y la
      lectura devuelve la nueva. Documentar que el test **falla** si la lectura se queda con la vieja.
- [ ] La negativa a reescribir una `READY` **de la misma versión** sigue vigente, con su test.
- [ ] El botón aparece solo cuando la versión difiere, y no aparece sobre una `READY` de la versión
      vigente. Documentar la falsación.
- [ ] Ningún aviso ni insignia de «desactualizada por plantilla» en la pantalla.
- [ ] El aviso posterior no repite ninguna oración de la leyenda, con el test que `E7C` dejó.
- [ ] La fila vieja nunca se modifica ni se borra: afirmarlo sobre la base tras el segundo pedido.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] `npm run api:types:check --prefix frontend` al día tras recapturar.
- [ ] `/gate` y `./scripts/smoke-ui.sh` en verde.
- [ ] Handoff generado con `/handoff E7D-PLANTILLA-VIGENTE`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E7D-PLANTILLA-VIGENTE.md` | Brief válido |
| Test de esquema | El campo nuevo no es columna |
| Test del proveedor con otra versión | La comparación lo sigue |
| Test de extremo a extremo | Fila nueva, vieja intacta, lectura devuelve la nueva; falla si devuelve la vieja |
| Test de negativa sobre la misma versión | Sigue en `409` |
| Test del botón | Aparece solo cruzando versiones; falsación documentada |
| Test del aviso | Sin oraciones repetidas de la leyenda |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `npm run api:types:check --prefix frontend` | Al día |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E7D-PLANTILLA-VIGENTE` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- El nombre del campo y del rótulo del botón, en castellano y coherentes con `format.ts`.
- Si la comparación vive en la proyección o en el manejador de lectura.
- Cómo se inyecta la versión vigente en la proyección sin que Application dependa de Infrastructure.
- La redacción del aviso posterior y de la letra chica que nombra la versión.
- Qué comprobación agrega el smoke, siempre que verifique contenido y no solo un `200`.

## Detenerse y consultar si

- hacer visible el botón obliga a debilitar la negativa a reescribir una `READY` de la misma versión;
- el campo nuevo no puede calcularse sin persistir nada;
- la versión vigente no puede leerse del proveedor registrado sin romper una frontera de capas;
- la tabla de estado × `regenerate` deja de ser cierta cruzando versiones y la corrección cambia el
  contrato de forma material;
- hace falta una migración o una dependencia nueva.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- El estado de `alert_explanations` para la alerta de prueba antes y después: las dos filas, con sus
  versiones, estados e instantes.
- Comandos y resultados exactos, con las falsaciones documentadas.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
