# Salvo — Task brief `E11B-ADAPTADOR-ANTHROPIC`

## Identificación

- Work ID: `E11B-ADAPTADOR-ANTHROPIC`
- Etapa: 11 — la IA de verdad
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-10-01
- Rama/worktree: `claude/e11b-anthropic`
- Commit base: **lo escribe el primer commit de la rama**, con lo que devuelva
  `git merge-base main HEAD`. El coordinador acepta que este campo no lleve SHA, por la lección de
  `E8B`: el commit que lo escribiera en `main` movería la punta y lo volvería falso.
- Integración: **por merge, nunca por rebase.**
- Modelo y esfuerzo acordados: **Opus 5.5 · `high`**, la fila «Implementación con ambigüedad real
  dentro del alcance» de `ClaudeAgent/Claude-Model-Policy.md`: el diseño fija las decisiones, pero
  la forma del desenlace del puerto, la regla de selección de la consola y la taxonomía de errores
  piden criterio dentro del alcance. El `brief-check`, con un modelo distinto.
- Dependencias: `E11A-INTEGRACION-CONTINUA`, verificada. **`E11C-CORRIDA-REAL` depende de ésta.**

## Resultado esperado

**Un modelo de Anthropic puede redactar la explicación de una alerta, gobernado por el mismo
verificador que gobierna a la plantilla**, y nada de lo que hoy funciona sin clave cambia de
comportamiento. Al terminar:

- `AI_PROVIDER=anthropic`, con `ANTHROPIC_API_KEY` y `ANTHROPIC_MODEL`, registra un adaptador real.
  Sin ellos, el arranque falla nombrando la variable y nunca su valor.
- **Ningún test, ni la compuerta, ni el smoke, ni la CI tocan la red de Anthropic.** El adaptador se
  prueba contra un transporte simulado que reproduce las formas documentadas de la API.
- La consola dice quién escribió cada párrafo y nunca esconde un texto aceptado detrás de un
  intento fallido.
- Queda un script para la corrida real, **que esta tarea no corre**: lo corre el coordinador en
  `E11C`, con su clave.

**Lo que esta tarea no puede verificar es que la API real se comporte como el simulador.** Eso es
`E11C`. Por eso cada forma que el simulador reproduce sale de una página oficial citada, leída el día
que se ejecute.

## Contexto obligatorio

- `Coordination/Tasks/E11-DISENO.md`, **entero**. Es la especificación: las decisiones D1 a D15
  están ahí con su motivo, y este brief no las repite, las ordena y las vuelve verificables.
- `Coordination/Tasks/E11-revision-adversarial.md`: por qué cada decisión es la que es.
- `DesignAgent/Salvo-Blueprint.md`, §4.7 «Explicabilidad», §7 «AlertExplanation», §9 «IA
  posterior», y las **decisiones 73 a 80** de la bitácora, que registran este diseño.
- `AGENTS.md`, «Decisiones invariantes», en especial: un texto rechazado no se persiste, no se
  registra y no llega al diagnóstico; y si el modelo no pasa el verificador, se cambia el prompt,
  nunca el verificador.
- El código que la tarea toca, **leído antes de cambiarlo**:
  - `backend/src/Salvo.Application/Explanations/` entero, y `Providers/ProviderCall.cs`, que **no se
    toca** pero cuyo comentario explica por qué un adaptador no puede avisar lanzando.
  - `backend/src/Salvo.Domain/Explanations/AlertExplanation.cs`, `NumberTokenizer.cs`,
    `ExplanationProvider.cs` y `ExplanationGrounding.cs`.
  - `backend/src/Salvo.Infrastructure/Explanations/` entero, y `AddExplanationProvider` en
    `DependencyInjection.cs`.
  - `EfAlertStore.GetExplanationsAsync`, que hoy elige la fila más recientemente pedida.
  - `frontend/src/app/alerts/[id]/explanation-*.tsx` y las claves de explicación de los dos
    diccionarios.
- La documentación oficial de Anthropic que el diseño cita en «Datos verificados». **Se vuelve a abrir
  el día que se ejecute** y la tabla se rehace en el handoff.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Una corrección al diseño, hecha por este brief

D6 dice que `FailureDetail` «se ve en la consola». **No es cierto hoy**: la columna existe y se
guarda, pero `AlertExplanationView` no la lleva y el frontend no la conoce. Esta tarea la agrega a la
vista y la muestra junto al estado de un intento fallido. No es un riesgo nuevo: `FailureDetail`
nunca lleva texto del modelo —solo el código, la clasificación, el `request-id` y, para un rechazo
del verificador, el token ofensor—, y un test lo afirma.

## Alcance

### Dentro, en este orden de commits

El orden es obligatorio: pone las protecciones **antes** que lo que protegen.

1. **El campo «Commit base»** del brief.
2. **D15 — los scripts no heredan el proveedor.** `smoke-ui.sh`, `demo.sh`, `capturas.sh` y
   `hornear-base.sh` exportan `AI_PROVIDER=mock`, una línea cada uno, con un comentario que diga por
   qué. Va primero porque, desde el commit del adaptador, una terminal con la clave exportada haría
   gastar al smoke y hornearía texto de un modelo en la imagen pública.
3. **D7 — los tokens en todo desenlace.** `Fail` recibe tokens, `Retake` deja de borrarlos y la fila
   acumula entre intentos. Sin migración.
4. **D1 — el desenlace cerrado del puerto.** `ExplanationDraft` se reemplaza por un resultado con
   desenlace `Drafted | Refused | Malformed | Unavailable`, más el borrador cuando lo hay, los tokens
   en todo desenlace, `ProviderVersion` tomado de la respuesta y un diagnóstico corto sin texto del
   modelo. `ExplanationExchange` asigna el código; el adaptador no puede nombrar códigos del
   verificador, del ciclo de vida ni del sistema. **`ProviderCall` no cambia un byte.** Cambian con
   el puerto `DeterministicExplanationProvider` y los proveedores de prueba de
   `ExplanationTestCorpus`. `MalformedOutput` amplía su significado a «cortado o ilegible», con su
   documentación y su rótulo en los dos diccionarios.
5. **D10, primera parte — `e3-v2` entra en `NumberTokenizer.VersionStrings`.** Un defecto latente desde
   `E9B`, con su test.
6. **D13 — el agujero semántico, fijado como clase.** Tests de caracterización que **afirman que
   pasan** el verificador: inversión, atribución cruzada y cifras en palabras, nombrados por la clase.
   Antes del adaptador, porque describen el verificador que el adaptador va a enfrentar.
7. **El adaptador**, con D2, D3, D4, D5, D6 y la segunda parte de D10:
   - `HttpClient` directo, de vida larga, en un servicio singleton, **sin** `IHttpClientFactory` ni
     tubería de registro HTTP. Ninguna dependencia nueva.
   - Cabecera `anthropic-version`; salida estructurada con `summary` y `referencedRules`,
     `additionalProperties: false`, y `referencedRules` como `enum` de las reglas que dispararon;
     normalización a minúsculas; `stop_reason` leído **antes** de deserializar.
   - Modelo desde `ANTHROPIC_MODEL`; `TemplateVersion` = **`anthropic-p1`**; `ProviderVersion` desde
     el campo `model` de la **respuesta**. El modelo **nunca** entra en la identidad.
   - `max_tokens` = 1.024, con el margen que pide D6.
   - **La hoja de hechos**, construida con `ExplanationFigures` y el vocabulario de la plantilla, sin
     versiones, sin centavos y sin instantes ISO, y un prompt que exige cifras en dígitos.
   - La taxonomía de D6, entera, a `FailureDetail`. Se leen `error.type`, `error.details.error_code` y
     la presencia de `retry-after`; **nunca `message`**. Un log `Warning` para toda respuesta que no
     sea `2xx`, sin cuerpo y sin cabeceras.
   - **Sin razonamiento extendido.** Si la documentación del día dice que Sonnet 5.5 lo aplica por
     defecto, se dice en el handoff y se consulta, porque cambia el costo.
8. **D8 y D9 — el secreto y la instancia pública.** Un solo lector de `ANTHROPIC_API_KEY`, en
   `AddExplanationProvider`. Falla al arrancar sin clave o sin modelo. `SharedInstance:Enabled=true`
   con `AI_PROVIDER=anthropic` detiene el arranque **aunque la clave esté**. La documentación de
   `ExplanationProvider.Anthropic` y el mensaje de error que hoy dicen que el adaptador no existe se
   reescriben.
9. **D11 — la selección de la consola, y `FailureDetail` en la vista.** La regla de tres pasos del
   diseño. Dónde vive —la lectura del almacén o la proyección— lo decide la tarea. La forma de la
   respuesta cambia, así que el contrato se recaptura: `frontend/openapi/salvo-openapi.json`,
   `schema.d.ts`, `guards.ts`, y `OpenApiDriftTests` en verde.
10. **D12 — la consola dice quién escribió qué.** Una oración por proveedor en los dos idiomas. La de
    la plantilla **no cambia una letra**: el smoke la comprueba. La del modelo nombra el modelo
    —desde `ProviderVersion`— y la versión del prompt. «Redactar con la plantilla vigente» pasa a
    nombrar al escritor vigente. El estado del intento vigente se muestra junto al texto aceptado de
    otro escritor cuando D11 lo pide, con su código y su `FailureDetail`.
11. **El script de la corrida real**, `scripts/explicar-con-anthropic.sh`, para `E11C`:
    - Es el **único** script que carga un archivo de entorno: `set -a; . "$archivo"; set +a`, con el
      archivo en `SALVO_ENV_FILE` y `.env` por omisión. No imprime ni exporta más de lo que la API
      necesita, y **nunca imprime el valor de la clave**.
    - Se niega a arrancar, nombrando la variable, si falta la clave o el modelo.
    - Levanta la API sobre una base nueva y sembrada, como `demo.sh`, pide la explicación de cada
      alerta, y reintenta las `FAILED` dentro del tope de tres intentos de la fila: nunca más.
    - Escribe en `docs/explicaciones-modelo/AAAA-MM-DD/` lo que D16 permite publicar y nada más: los
      textos `READY` con modelo, versión de prompt y fecha; de cada `FAILED`, el código y
      `FailureDetail`; los tokens de cada fila, leídos de la base con `node:sqlite` como hace el
      smoke; y el costo calculado con los precios que imprime **junto a la fecha en que se leyeron**.
    - **El agente no lo corre contra Anthropic.** Lo corre solo con un archivo de entorno vacío que él
      mismo crea, para ver que se niega, y con `bash -n`.
12. **Los documentos que esta tarea vuelve falsos**, y solo ésos: `.env.example`
    (`ANTHROPIC_MODEL=claude-sonnet-5-5`), las afirmaciones del `README.md` sobre el redactor, sobre
    `AI_PROVIDER=anthropic` y sus «Límites declarados» —con el agujero de D13 dicho como clase—, y las
    menciones a Anthropic de `Salvo-Getting-Started.md` y `Salvo-Portability.md`. El Blueprint, el
    Overview, el Progress, el Workboard y `AGENTS.md` los cierra el coordinador al integrar.

### Las falsaciones que la entrega tiene que mostrar

Un test que nunca se vio fallar no está probado. Cada una se hace revirtiendo **solo** la línea que
protege, viendo el rojo con el nombre del test, y restaurando:

| Qué se rompe a propósito | Qué tiene que ponerse rojo |
| --- | --- |
| El adaptador reintenta una vez ante un 529 | El test que cuenta exactamente una petición por intento (D5) |
| Un `ILogger` registra `HttpRequestMessage.ToString()` | El espía de logs a nivel `Trace`, en el camino que funciona (D8) |
| Se quita la guarda de `SharedInstance` | El test que arranca con la bandera **y** con una clave presente (D9) |
| La hoja de hechos lleva el monto en centavos | El test de propiedad: todo número de la hoja está fundamentado (D10) |
| `GetExplanationsAsync` vuelve a elegir la más reciente | El test con una `READY` de la plantilla y una `FAILED` del modelo (D11) |
| `Retake` vuelve a borrar los tokens | El test de acumulación entre intentos (D7) |

### Fuera

- **Cambiar el verificador** —`ExplanationGrounding`, `ExplanationFacts`, sus tolerancias— para que un
  texto pase. Es la regla de la etapa.
- **Tocar `ProviderCall`** o cualquier pieza del proveedor antifraude.
- **Llamar a la API real** desde un test, desde la compuerta, desde el smoke o desde cualquier
  comando que corra el agente.
- Streaming, herramientas, varias llamadas por intento, caché de prompt.
- El SDK oficial de C#: es candidata registrada, no esta tarea.
- La verificación por señal: es candidata registrada.
- Migraciones. Ninguna decisión del diseño las necesita.
- La regla de dispositivo y todo lo demás de «Mejoras para Salvo».

### Paths autorizados

- `backend/src/Salvo.Application/Explanations/**`.
- `backend/src/Salvo.Application/Alerts/AlertProjection.cs`, `AlertContext.cs` y `GetAlertHandler.cs`,
  **solo** para D11.
- `backend/src/Salvo.Domain/Explanations/**`, **excepto** `ExplanationGrounding.cs` y
  `ExplanationFacts.cs`, que solo se leen.
- `backend/src/Salvo.Infrastructure/Explanations/**`, incluidos los archivos nuevos del adaptador.
- `backend/src/Salvo.Infrastructure/DependencyInjection.cs`, **solo** `AddExplanationProvider` y lo
  que registre.
- `backend/src/Salvo.Infrastructure/Persistence/EfAlertStore.cs`, **solo** `GetExplanationsAsync`, y
  `EfExplanationStore.cs` si D11 lo pide.
- `backend/src/Salvo.Api/**`, **solo** si la forma de la respuesta o el registro lo exigen.
- `backend/tests/**`.
- `frontend/openapi/salvo-openapi.json`, `frontend/src/lib/api/**`, `frontend/src/test/**`.
- `frontend/src/app/alerts/[id]/explanation-*`.
- `frontend/src/lib/i18n/es.ts`, `pt.ts` y `glosario-pt.md`, **solo** las claves de la explicación y el
  rótulo de `MalformedOutput`.
- `scripts/smoke-ui.sh`, `scripts/demo.sh`, `scripts/capturas.sh` y `scripts/hornear-base.sh`, **solo**
  la línea de D15 y su comentario. `scripts/explicar-con-anthropic.sh`, nuevo.
- `.env.example`, `README.md`, `DesignAgent/Salvo-Getting-Started.md` y
  `DesignAgent/Salvo-Portability.md`, **solo** lo del punto 12.
- `Coordination/Handoffs/Claude.md`, entrada nueva.
- `Coordination/Tasks/E11B-ADAPTADOR-ANTHROPIC.md`, **solo** el campo «Commit base».

### Paths reservados por otros trabajos

Ninguno en uso. `E11C` no tiene brief y empieza cuando ésta esté integrada.

## Acciones autorizadas

- Ediciones locales: sí, en los paths de arriba.
- Dependencias: **no**. Ni el SDK, ni `Microsoft.Extensions.Http`, ni ningún paquete de npm.
- Red: **solo** leer la documentación oficial de Anthropic y de las herramientas del proyecto.
- **Secretos: ninguno, nunca.** El agente no lee `.env` —la configuración lo deniega a propósito—, no
  pide la clave, no la escribe en ningún archivo, test, fixture, log ni handoff. Los tests usan un
  valor ficticio evidente, construido en el propio test.
- Escrituras externas: **no**. El agente no hace push.
- Acciones destructivas: **no**.

## Criterios de aceptación

- [ ] Los once commits del orden de «Dentro», en ese orden, cada uno con la compuerta verde.
- [ ] D15: los cuatro scripts exportan `AI_PROVIDER=mock`, y el commit va antes que el adaptador.
- [ ] D1: el puerto devuelve el desenlace cerrado; `ProviderCall` y el antifraude no cambian un byte
      (`git diff` vacío sobre esos archivos); `AProviderThatThrowsIsRecordedRatherThanRaised` sigue
      verde.
- [ ] D7: tokens en `Fail`, `Retake` no los borra, la fila acumula; con su falsación.
- [ ] D10: `e3-v2` en `VersionStrings`, con su test; la propiedad de la hoja de hechos, con su
      falsación; el espía extendido al **cuerpo HTTP** que sale.
- [ ] D13: los tres tests de caracterización, nombrados por la clase, en verde.
- [ ] D5: una petición por intento, afirmada contando peticiones, con su falsación.
- [ ] D6: **un test por fila** de la taxonomía, contra el transporte simulado, con las formas de la
      documentación del día; y uno que afirma que `FailureDetail` nunca lleva texto del modelo.
- [ ] D8: el espía a nivel `Trace`, camino feliz y camino que falla, buscando la clave en cada
      mensaje y en el `ToString()` de cada excepción; con su falsación. El arranque sin clave o sin
      modelo falla nombrando la variable.
- [ ] D9: el arranque con `SharedInstance:Enabled=true`, `AI_PROVIDER=anthropic` **y** clave
      falla; con su falsación.
- [ ] D11: los tres pasos de la regla, cada uno con su test; con su falsación.
- [ ] D12: el test del bloque de explicación cubre la oración de la plantilla y la del modelo, en los
      dos idiomas; la de la plantilla, sin una letra cambiada.
- [ ] El contrato recapturado y `OpenApiDriftTests` verde.
- [ ] El script se niega sin clave y pasa `bash -n`; **no se corrió contra Anthropic**.
- [ ] `./scripts/check.sh` y `./scripts/smoke-ui.sh` verdes, **sin clave en el entorno**.

## Verificación y evidencia

| Comprobación | Resultado esperado |
| --- | --- |
| `./scripts/check.sh` | Verde |
| `./scripts/smoke-ui.sh` | Verde, con la frase de la plantilla intacta |
| Las seis falsaciones | Rojo con el nombre del test, y verde al restaurar |
| `git diff <base>..HEAD -- backend/src/Salvo.Application/Providers backend/src/Salvo.Application/External` | Vacío |
| `git diff <base>..HEAD -- backend/src/Salvo.Domain/Explanations/ExplanationGrounding.cs backend/src/Salvo.Domain/Explanations/ExplanationFacts.cs` | Vacío |
| `git grep` del valor ficticio de la clave fuera de los tests | Sin resultados |
| `scripts/explicar-con-anthropic.sh` con un archivo de entorno vacío | Se niega, nombra la variable, no imprime ningún valor |
| La corrida de GitHub Actions al subir la rama | Verde: la CI no tiene clave, y nada debe pedirla |

## Decisiones delegadas

- La forma exacta del desenlace del puerto, mientras sea cerrada y por valor.
- Dónde vive la regla de D11.
- La redacción del prompt `anthropic-p1`, de la hoja de hechos y de las oraciones de D12.
- El nombre y la forma del archivo que escribe el script, dentro de la carpeta fechada.
- Si `anthropic-p1` entra también en `VersionStrings`, con el motivo.
- Cómo se cuenta el plazo del puerto para el adaptador: el valor actual de 15 s se conserva salvo que
  la documentación justifique otro, y la primera petición con un esquema nuevo es la lenta.

## Detenerse y consultar si

- **la documentación del día contradice el diseño**: el ID del modelo, el parámetro de la salida
  estructurada, la forma de los errores, los `stop_reason`, la cabecera de versión;
- algo exige **cambiar el verificador** o **tocar `ProviderCall`**;
- algo exige una **migración** o una **dependencia**;
- un paso pide la **clave**, leer `.env` o llamar a la API real;
- la frase de la plantilla o las anclas del smoke tienen que cambiar;
- Sonnet 5.5 aplica razonamiento extendido por defecto;
- un test existente cae y arreglarlo exige tocar algo fuera de los paths autorizados.

## Entrega requerida

- Resumen, archivos tocados, y los once commits.
- **La tabla de «Datos verificados» rehecha**, con la fecha de lectura y lo que cambió.
- Las seis falsaciones, con su salida.
- Una muestra de la hoja de hechos para una alerta del corpus, y el prompt `anthropic-p1` entero: los
  dos van a la API con la evaluación y nada más.
- El costo estimado por explicación y el techo de la corrida, recalculados con `max_tokens` y los
  precios del día.
- Supuestos, riesgos y pendientes, y lo que `E11C` tiene que mirar primero.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
