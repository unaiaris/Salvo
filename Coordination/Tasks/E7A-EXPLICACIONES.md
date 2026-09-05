# Salvo — Task brief `E7A-EXPLICACIONES`

## Identificación

- Work ID: `E7A-EXPLICACIONES`
- Etapa: 7
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-05
- Rama/worktree: `claude/e7a-explicaciones`
- Commit base: el commit de `main` que incorpora las decisiones 51–56 del Blueprint y este brief
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. El razonamiento de diseño está congelado en el
  diseño v2 y en este brief; queda criterio de ingeniería, pero es criterio fino: la conversión de
  zona horaria dentro de los hechos, la igualdad por redondeo, seis reglas de transición y un
  interceptor de comandos SQL. Si un test no puede volverse determinista, o la migración no
  conserva los fingerprints, detenerse y consultar tras dos intentos en vez de insistir.
- Dependencias: ninguna. `E7B-EXPLICACIONES-UI` depende de esta tarea integrada.

## Resultado esperado

Una analista puede pedir la explicación de una alerta. El sistema la genera con un proveedor
determinista, **verifica sobre el texto** que cada regla y cada cifra estén respaldadas por la
evaluación, y solo entonces la guarda. Si el proveedor falla, tarda o el navegador aborta, la fila
queda en un estado del que **siempre se puede salir**. El texto generado no puede cambiar ninguna
superficie de decisión, y hay cuatro tests que fallan si lo hiciera. El motor, el fingerprint, las
alertas, el dashboard y las métricas **no cambian en absoluto**.

## Contexto obligatorio

- `Coordination/Tasks/E7-DISENO.md` (**v2**), decisiones **D1 a D11** completas.
- `Coordination/Tasks/E7-revision-adversarial.md`, hallazgos **1, 2, 3, 4, 5, 7, 8, 9, 10 y 11**.
  Son el origen de las exigencias de esta tarea; leerlos evita reintroducir lo que ya se corrigió.
  En particular el hallazgo 1 trae la tabla de falsos rechazos verificada contra datos reales.
- `DesignAgent/Salvo-Blueprint.md`: §4.3, §4.7, §7 y decisiones **51–56** de la bitácora.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 7 — Explicabilidad».
- `AGENTS.md`, en particular las reglas nuevas sobre el input de un modelo y sobre el texto
  rechazado.
- Código existente, para reutilizar patrones probados y para entender qué **no** tocar:
  - `Salvo.Domain/Risk/TemporalRiskEngine.cs`: la forma exacta de cada `detail`. Es la fuente de
    `SignalFacts`;
  - `Salvo.Domain/Alerts/AlertPolicy.cs`: bandas, `FlagThreshold`, `ScoreCap`;
  - `Salvo.Domain/External/**` y `Salvo.Application/External/**`: el ciclo de vida en dos fases,
    la clasificación de fallos de `ExternalProviderExchange` y el registro condicional por bandera;
  - `Salvo.Infrastructure/Persistence/ExternalEvaluationConfiguration.cs` y
    `AlertConfiguration.cs`: restricciones `CHECK` de consistencia estado–columna, índices únicos
    parciales y tokens de concurrencia;
  - `Salvo.Infrastructure/DependencyInjection.cs`: el fallo al arrancar por configuración
    desconocida;
  - `backend/tests/**`: `ExternalEvaluationIsolationTests` (diferencial), `DashboardEndpointTests`
    (etiquetas invertidas), `ArchitectureSmokeTests`, `AlertSchemaTests`, `SalvoApiFactory`.

## Alcance

### Dentro

**1. Entidad y persistencia.**

`AlertExplanation` en Domain y tabla `alert_explanations`, con las columnas, las cinco
restricciones `CHECK` y los dos índices que D1 especifica. La identidad es
`(riskEvaluationId, provider, templateVersion, alertPolicyVersion)`; `requestedFromAlertId` es
procedencia y **no** entra en el único. Token de concurrencia con el molde de `AlertConfiguration`.

`alert_reviews` gana `explanation_id` nullable con clave foránea (D10).

Migración con `dotnet ef migrations add`. **Antes de aplicarla a `backend/src/Salvo.Api/salvo.db`,
copiar el archivo**: EF ejecuta `PRAGMA foreign_keys = 0` fuera de transacción.

**2. `SignalFacts` en Domain, declarado semilla de `e3-v2`.**

Un extractor por regla convierte el `detail` que emite `TemporalRiskEngine` en campos tipados
—`ratio`, `median`, `scope`, `historyCount`, `window`, `from`, `to`, `elapsedMinutes`, `share`,
`bucket`, según la regla—. Comentario en el archivo que diga que esto desaparece cuando el motor
emita los campos, y que la plantilla y los hechos sobreviven a esa eliminación.

**3. `ExplanationFacts` en Domain.**

El conjunto de cifras que una frase correcta puede legítimamente contener, construido desde la
evaluación y el pedido. Debe incluir, sin excepción: `score`, `FlagThreshold`, `ScoreCap`, el peso
de cada señal y la **suma** de los pesos antes del tope, la **cantidad** de señales, todos los
números extraídos de cada `detail` con el mismo tokenizador, `amountCents` **y** `amountCents / 100`
con 0, 1 y 2 decimales, y el instante del pedido descompuesto en año, mes, día, hora y minuto
**en UTC y en `BusinessTimeZone`**. Cada una de estas entradas cierra un falso rechazo concreto que
la revisión verificó con datos reales; quitar cualquiera reintroduce el defecto.

**4. El tokenizador, declarado y compartido.**

Normaliza a NFC, elimina del texto las cadenas de versión conocidas —`e3-v1`, `e4-v1`, `e7-v1`—
para que no aporten dígitos sueltos, y extrae los candidatos con un patrón único que entiende el
formato `es-UY`: miles con punto, decimal con coma. El mismo tokenizador se usa para construir los
hechos desde los `detail` y para leer el texto generado. Las cifras escritas en letras **no se
validan**, y eso se documenta en el código.

**5. Puerto, proveedor determinista y envoltorio de llamada.**

`IExplanationProvider` devuelve `{ summary, referencedRules[] }` —salida estructurada desde el
puerto, no desde el adaptador— y lleva **timeout explícito en el puerto**, aunque el determinista
no tenga red. `ExplanationExchange` con la misma taxonomía de fallos que
`ExternalProviderExchange`, o esa clase generalizada: **no copiarla en silencio**, porque tener dos
versiones de «qué significa un timeout» es el defecto que hay que evitar.

El proveedor determinista compone el resumen en castellano a partir de `SignalFacts`. Pasa la misma
validación que pasaría un modelo, porque la validación no vive en él.

**6. Validación de grounding en el caso de uso.**

Entre el puerto y el almacén, **nunca en el adaptador**. Tres capas, con la regla de igualdad
`N == F` o `round(F, d) == N` para `d ≤ 2`, y los códigos `NOT_GROUNDED_RULE`,
`NOT_GROUNDED_NUMBER`, `TOO_LONG` y `MALFORMED_OUTPUT`. El texto rechazado no se persiste, no se
registra y no llega a `failure_detail`: solo el token ofensor.

**7. Ciclo de vida, con las seis reglas de D6.**

Reservar y commitear antes de llamar, subiendo `attempt_count`; llamar con timeout; **asentar con
`CancellationToken.None`**; `FAILED → PENDING` sobre la misma fila con el token; `PENDING` vencida
—anterior a `ahora − 2 × timeout`— retomable; tope de 3 intentos con `ATTEMPT_LIMIT_REACHED`.

**8. Endpoint.**

`POST /api/alerts/{alertId}/explanation` con cuerpo `{ "regenerate": boolean }`, exactamente la
tabla estado × `regenerate` del diseño. `404 ALERT_NOT_FOUND` para la alerta inexistente. **No** hay
`GET` aparte: `AlertDetail` gana el sub-objeto `explanation`, con `isOutdated` **calculado al leer**
—jamás persistido—, y opcionalmente `currentExplanation`. Una alerta cerrada admite explicar su
premisa, no la evaluación vigente.

`AI_PROVIDER` con cualquier valor distinto de `mock` hace fallar el arranque, con o sin clave.

**9. Los cuatro tests de D8, más el resto de la verificación.**

Descritos en «Criterios de aceptación». Cada uno tiene que poder fallar, y hay que demostrarlo.

### Fuera

- El adaptador de Anthropic. Es una decisión aparte; este brief solo deja el esqueleto de D11.
- `recommendedAction`, en cualquier forma. Se eliminó del alcance por la decisión 53.
- Cualquier cambio en `TemporalRiskEngine`, `RuleConfig`, el fingerprint, la semántica de alertas,
  el dashboard o las métricas.
- Toda la superficie de consola: es `E7B-EXPLICACIONES-UI`.
- Regeneración automática al cambiar el corpus, y reconciliación en segundo plano.
- Explicaciones de evaluaciones externas o de pedidos sin alerta.

### Paths autorizados

- `backend/src/Salvo.Domain/Explanations/**`
- `backend/src/Salvo.Domain/Alerts/**`, solo para `alert_reviews.explanation_id`
- `backend/src/Salvo.Application/Explanations/**`
- `backend/src/Salvo.Application/Alerts/**`, solo para la revisión con `explanationId`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, **solo** recaptura y
  regeneración
- `.env.example`, solo si hace falta alinear `ANTHROPIC_MODEL`

### Paths reservados por otros trabajos

- Todo el resto de `frontend/**` y `scripts/**`: `E7B-EXPLICACIONES-UI`.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Crear la migración con `dotnet ef migrations add`: autorizado.
- **Antes de aplicar la migración a `backend/src/Salvo.Api/salvo.db`, copiar el archivo.**
- Levantar la API para recapturar el OpenAPI: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna. No borrar bases.
- Commits locales en la rama: autorizados, y **se pide commitear por partes**.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E7A-EXPLICACIONES.md` sin faltantes antes de empezar.
- [ ] `alerts` no gana ninguna columna, y `AlertSchemaTests` lo sigue afirmando, extendido para
      afirmar además que `alert_explanations` existe con sus dos índices.
- [ ] La restricción `READY ⇔ summary` está en la base: un `INSERT` directo de una fila `FAILED`
      con `summary` es rechazado por SQLite. Test que lo demuestre.
- [ ] **Test de mutación del grounding**: un proveedor de prueba devuelve un resumen con una cifra
      inventada y el resultado es `FAILED` con `NOT_GROUNDED_NUMBER` y `summary IS NULL`. La cifra
      se elige **en tiempo de ejecución, después de construir los hechos** —el menor entero positivo
      que no pertenece al conjunto—, nunca fijada en el código. Documentar en el handoff que el test
      **falla** si se quita la validación **del manejador**; quitarla del proveedor no debe bastar
      para ponerlo en verde.
- [ ] **Test de falsos rechazos**: el resumen del proveedor determinista sobre al menos tres
      pedidos con señales distintas pasa la validación. Debe incluir un caso con monto formateado en
      unidades, uno con porcentaje redondeado y uno con hora de negocio distinta de la hora UTC.
      Documentar que el test **falla** si se quita del conjunto de hechos el monto en unidades, o la
      hora en `BusinessTimeZone`, o la igualdad por redondeo.
- [ ] **Interceptor de comandos**: durante `POST …/explanation`, ningún `INSERT`/`UPDATE`/`DELETE`
      nombra otra tabla que `alert_explanations`, y ninguna lectura menciona
      `order_evaluation_labels`. Documentar que falla si el manejador escribe en `alerts`.
- [ ] **Diferencial ampliado**: dashboard, feed, métricas **y** `GET /api/alerts/{id}` con la clave
      `explanation` eliminada del JSON, byte a byte idénticos antes y después de generar todas las
      explicaciones. Documentar que falla si el almacén reescribe `alerts.signals_snapshot_json`.
- [ ] **Diferencial de etiquetas sobre el texto**: dos fábricas con el mismo corpus y las etiquetas
      invertidas en una producen resúmenes `READY` **idénticos**.
- [ ] **Proveedor espía**: el `ExplanationInput` serializado a JSON no contiene la ciudad centinela,
      `buyerReferenceId`, `merchantReferenceId`, `deviceSessionId`, `merchantId`, la nota de
      revisión, el veredicto externo ni `isFraudLabel`.
- [ ] Test de ciclo de vida: un `FAILED` se regenera sobre la **misma fila**; una `PENDING` vencida
      se retoma; una petición cancelada por el cliente deja la fila **asentada**, no `PENDING`; al
      cuarto intento el endpoint responde `409 EXPLANATION_ATTEMPTS_EXHAUSTED`.
- [ ] Test dorado del texto en castellano sobre un pedido **de la fixture** —`ORD_000011` o un
      escenario de `AlertTestCorpus`—, nunca sobre `ORD_900004`, que es una importación manual de la
      base local y no pertenece a `demo-orders.v1.json`.
- [ ] Test de la tabla de endpoints: las seis filas de estado × `regenerate`, con sus códigos.
- [ ] `AI_PROVIDER=anthropic` impide el arranque, con y sin clave. `AI_PROVIDER=cualquier-cosa`
      también.
- [ ] Revisar una alerta sin explicación sigue funcionando exactamente igual, y `explanationId` es
      opcional en el endpoint de revisión.
- [ ] Los 328 fingerprints existentes siguen idénticos tras la migración.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] Sin dependencias nuevas y sin cambios en `frontend/` fuera de los dos archivos autorizados.
- [ ] `/gate` en verde.
- [ ] Handoff generado con `/handoff E7A-EXPLICACIONES`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E7A-EXPLICACIONES.md` | Brief válido |
| Test de mutación del grounding | `FAILED` + `NOT_GROUNDED_NUMBER` + `summary IS NULL`; falla al quitar la validación del manejador |
| Test de falsos rechazos | Los tres resúmenes pasan; falla al mutilar el conjunto de hechos |
| Interceptor de comandos | Solo `alert_explanations` escrita; falla si el manejador escribe en `alerts` |
| Diferencial ampliado | Cuatro superficies idénticas; falla al reescribir el snapshot |
| Diferencial de etiquetas sobre el texto | Resúmenes idénticos con etiquetas invertidas |
| Proveedor espía | Ningún campo excluido en el JSON del input |
| Test de ciclo de vida | Cuatro escenarios: regeneración, vencida, cancelación, tope |
| Test de restricción `READY ⇔ summary` | `INSERT` directo rechazado por SQLite |
| Test dorado del texto | Resumen exacto sobre un pedido de la fixture |
| Test de fingerprints tras la migración | Los 328 idénticos |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios |
| `npm run api:types:check --prefix frontend` | Al día |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E7A-EXPLICACIONES` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- Estructura de puertos, repositorios y DTO, respetando las fronteras de `AGENTS.md`.
- Nombres de tablas y columnas, en `snake_case`.
- La expresión concreta del patrón del tokenizador, y cómo se comparte entre la construcción de
  hechos y la lectura del texto.
- La forma de cada extractor de `SignalFacts` y qué hace ante un `detail` que no reconoce —tiene
  que ser un fallo declarado, no un silencio.
- La redacción concreta de la plantilla en castellano, dentro del tope de longitud.
- El tope exacto de caracteres del resumen y la ventana de vencimiento, si `2 × timeout` resulta
  poco práctica en los tests.
- Cómo se simula la cancelación del cliente y el timeout del proveedor.
- Cómo se registra el interceptor de comandos sin afectar a las demás fábricas de test.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- la validación de grounding no puede ubicarse en el caso de uso sin romper una frontera de capas;
- el test de mutación pasa a verde quitando la validación **del proveedor** en vez del manejador:
  eso significa que la validación quedó en el lugar equivocado;
- un `detail` real del motor no puede convertirse en `SignalFacts` sin ambigüedad;
- la migración no puede agregar `alert_reviews.explanation_id` conservando los fingerprints y las
  revisiones existentes;
- hace falta tocar cualquier archivo de `frontend/` fuera de los dos autorizados;
- hace falta una dependencia nueva;
- aparece una contradicción entre el diseño v2 y el código real que no se resuelva con una
  corrección menor;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos y resultados exactos, incluidas las demostraciones de falsación: el test de mutación con
  la validación quitada del manejador, el de falsos rechazos con el conjunto de hechos mutilado, el
  interceptor con una escritura en `alerts`, y el diferencial con el snapshot reescrito.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`, generado con `/handoff`.
