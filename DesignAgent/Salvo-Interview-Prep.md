# Salvo — Ayuda-memoria para una entrevista con Koin

> Estado del documento: vigente
> Última actualización: 2026-08-31
> Fuente de verdad: [[Salvo-Blueprint]]
> Alcance: preparación general; ajustar cuando se disponga de la descripción concreta del puesto

## Cómo usar esta guía

No memorizar respuestas palabra por palabra. Preparar tres niveles:

1. pitch de 30 segundos;
2. explicación técnica de 90 segundos;
3. dos o tres decisiones que se puedan profundizar con ejemplos y trade-offs.

La regla de honestidad es central: el MVP integra un proveedor antifraude mock inspirado en un ciclo
real. No afirmar que existe una integración, homologación o certificación oficial con Koin mientras
no se haya ejecutado su sandbox y proceso de onboarding.

## Pitch de 30 segundos

> Salvo es una consola antifraude para e-commerce. Recibe pedidos sintéticos, calcula un score local
> mediante reglas deterministas y auditables, genera alertas y permite revisarlas. También modela una
> integración desacoplada con un proveedor externo: referencias estables, estados pendientes,
> callbacks idempotentes y reconciliación. Funciona sin servicios externos; Anthropic y el sandbox
> de Koin están previstos como adaptadores posteriores.

## Explicación técnica de 90 segundos

> Construí el backend como un monolito modular en ASP.NET Core y C#, con EF Core/SQLite para una
> demo portable, y separé la interfaz en Next.js/React mediante un contrato OpenAPI. El dominio
> procesa pedidos en orden
> temporal, construye un baseline únicamente con historia anterior y ejecuta reglas puras como
> anomalía de monto, velocity o cambio de país. El resultado local se persiste como una evaluación
> independiente y, si supera el umbral, crea una alerta idempotente. La revisión actualiza pedido y
> alerta en una transacción de base de datos. Para demostrar integración externa definí un puerto
> `IAntifraudProvider`: el MVP usa un mock determinista con estados approved, denied y received,
> correlación por reference ID, callback replay-safe y reconciliación. Un adaptador Koin real puede
> agregarse después sin mezclar el score externo con el local ni cambiar las reglas de negocio.

## Por qué el proyecto encaja con Koin

| Tema del dominio Koin | Qué demuestra Salvo | Límite honesto actual |
| --- | --- | --- |
| Evaluación antifraude e-commerce | Pedidos, score, estados y alertas | Dataset completamente sintético |
| Correlación | Referencia estable y evaluación externa separada | El ID externo procede del mock |
| Respuesta asíncrona | Estado pendiente, callback y reconciliación | Callback local/simulado |
| Idempotencia | Seed, evaluación, alerta y callback replay-safe | Sin SLA de producción |
| Riesgo y falsos positivos | Precision, recall, F1 y threshold sweep | Reglas y labels sintéticos |
| Integración desacoplada | Puerto y adaptadores de infraestructura | Sandbox Koin aún no activado |
| Seguridad de datos | Pseudónimos, redacción y secretos server-side | Sin PII ni pagos reales |
| Degradación elegante | El scoring local funciona sin proveedor/IA | No mide disponibilidad real externa |

## Decisiones arquitectónicas clave

### ¿Por qué reglas y no un LLM para decidir fraude?

El veredicto local necesita ser repetible, testeable y auditable. Un LLM puede traducir señales a
lenguaje humano, pero no debe decidir fraude, severidad o bloqueo. Si Anthropic falla, el score y las
alertas continúan funcionando.

### ¿Por qué separar el score local del resultado de Koin?

Son motores distintos con datos, políticas y semánticas propias. Guardarlos como evaluaciones
separadas preserva trazabilidad y permite comparar resultados sin presentar una decisión local como
si fuera del proveedor externo.

### ¿Por qué un mock antes que el sandbox?

El sandbox real depende de onboarding, credenciales, contrato vigente, fingerprint y un callback
HTTPS. El mock permite desarrollar y probar estados, replays, errores y reconciliación de manera
determinista. Después el adaptador real reemplaza infraestructura, no dominio.

### ¿Por qué un monolito modular?

Reduce costo operacional y acelera el MVP. Las fronteras internas conservan los beneficios de una
arquitectura limpia y permiten extraer un servicio si el volumen o la organización lo justifican.
Crear microservicios ahora añadiría red, despliegue y consistencia distribuida sin necesidad real.

### ¿Por qué ASP.NET Core para el backend y Next.js para la UI?

El backend concentra las decisiones críticas en C#: dominio, transacciones, idempotencia,
persistencia e integraciones. ASP.NET Core y EF Core permiten demostrar profundidad en el ecosistema
principal del proyecto. Next.js aporta una UI moderna sin convertir el navegador en fuente de
verdad. OpenAPI mantiene la frontera tipada y evita duplicar contratos.

### ¿Por qué SQLite y no Postgres?

SQLite hace la demo portable y reproducible. El modelo evita depender de extensiones específicas y
la migración a Postgres queda para el momento en que se necesiten concurrencia, despliegue o mayor
volumen. No se presenta SQLite como elección de producción para un sistema antifraude real.

## Etapa 2 — contrato y datos

> Estado: integrada en `main` mediante `4b7bf54` y verificada con la compuerta full-stack.

### Speech de 45 segundos

> En la capa de datos separé los hechos inmutables del pedido de cualquier resultado antifraude. El
> pedido tiene una PK interna, pero la idempotencia usa la referencia estable del comercio. Si el
> mismo payload llega otra vez se omite; si reutiliza la referencia con datos distintos, se informa
> un conflicto y nunca se sobrescribe. El ground truth vive en una tabla separada y no aparece en la
> importación pública, para impedir que el scoring lo use como feature. CSV y JSON convergen en el
> mismo contrato de aplicación: los errores se acumulan por registro y los válidos se escriben de
> forma atómica. El seed es una fixture fija de 300 pedidos, completamente sintética y repetible.

### Decisiones para profundizar

#### ¿Por qué la referencia única es compuesta?

`merchantReferenceId` pertenece al namespace de un comercio. Dos comercios pueden usar `ORD_001`,
pero uno solo no puede reutilizarlo para pedidos distintos. Por eso la PK técnica es un GUID y la
restricción natural es `(merchantId, merchantReferenceId)`.

#### ¿Por qué separar `isFraudLabel`?

La etiqueta es ground truth de evaluación, no un hecho operativo ni una señal. Vive en
`OrderEvaluationLabel`, no en `Order`; los DTOs públicos no la aceptan y solo el seed interno puede
escribirla. Esta separación hace más difícil introducir fuga de información por accidente.

#### ¿Por qué el pedido es inmutable?

Importe, moneda, comprador y momento describen el evento recibido. Una reimportación con la misma
clave y datos diferentes es un conflicto, no una actualización. Score, estado, evaluación y alerta
se modelan después como conceptos propios, en lugar de preasignar columnas nullable sin semántica.

#### ¿Cómo se conserva la visibilidad temporal en SQLite?

API y dominio usan `DateTimeOffset` con offset explícito. Persistencia normaliza a UTC ISO 8601
canónico, legible y de longitud fija. Los tests de integración demuestran orden y rangos en la
base. Los empates se resuelven mediante `merchantId` y `merchantReferenceId`, no mediante un GUID
aleatorio.

#### ¿Cómo funciona una importación parcial?

Los problemas estructurales invalidan el archivo. Los errores de contenido se acumulan por
registro, mientras los registros válidos forman una única transacción. Un fallo técnico revierte
ese subconjunto completo. Los duplicados idénticos se contabilizan como skips y una referencia con
contenido distinto produce `REFERENCE_CONFLICT`.

#### ¿Cómo se minimizan datos y alcance?

No se almacena descripción libre, dirección, email, teléfono, pago ni documento. Los IDs son tokens
sintéticos prefijados. El MVP admite UYU, BRL y USD sin conversión FX; cualquier futuro baseline de
importe se particiona por moneda. Ciudad es una localidad opcional, nunca una dirección.

### Recorrido real por capas

| Capa | Código para mostrar | Responsabilidad y evidencia |
| --- | --- | --- |
| `Salvo.Domain` | `Orders/Order.cs`, `OrderDraft.cs`, `IsoCountryCodes.cs` | La fábrica acumula errores, normaliza IDs/ciudad/UTC y crea el pedido solo si todas las invariantes pasan. `OrderTests` cubre límites, normalización e igualdad de hechos. |
| `Salvo.Application` | `Orders/Imports/ImportOrdersHandler.cs` | Une ambos formatos, valida tipos, clasifica duplicado frente a conflicto y limita la respuesta a 1.000 errores sin conocer HTTP, CsvHelper ni EF. |
| `Salvo.Application` | `Orders/Seed/SeedDemoOrdersHandler.cs` | Valida la forma 300/300/18, deriva GUID deterministas, compara hechos existentes y nunca sobrescribe conflictos. |
| `Salvo.Infrastructure` | `Imports/CsvOrderImportParser.cs` y `JsonOrderImportParser.cs` | Convierte sintaxis externa a registros crudos; los problemas estructurales detienen el documento y los de contenido siguen por registro. |
| `Salvo.Infrastructure` | `Persistence/Configurations/*`, converters y `EfOrderDataStore.cs` | Repite constraints críticos, guarda fechas UTC legibles, define claves/índices y deja `SaveChanges` como frontera transaccional. |
| Migración | `20260831151027_InitialOrderContractData.cs` | Crea únicamente `orders` y `order_evaluation_labels`; reemplaza el checkpoint de E1 y no se ejecuta automáticamente al arrancar. |
| `Salvo.Api` | `OrderEndpoints.cs` | Valida multipart, tamaño y formato; traduce errores de documento a `ProblemDetails`; solo mapea el seed si `DemoData:Enabled` está activo. |
| Fixture demo | `Seed/Fixtures/demo-orders.v1.json` | Contiene 300 pedidos pseudónimos, 18 labels positivos y timestamps fijos; es un recurso embebido, sin generación aleatoria ni red. |

### Recorrido de cinco minutos para explicar el código

1. Abrir `OrderEndpoints.cs`: la API solo protege la frontera de transporte —5 MiB,
   `multipart/form-data` y `CSV|JSON`— y delega el comportamiento.
2. Seguir a `OrderImportParser`: el dispatcher elige infraestructura CSV o JSON; ambos producen el
   mismo `RawOrderImportRecord`, por lo que el caso de uso no duplica reglas.
3. Entrar en `ImportOrdersHandler`: primero reúne errores por registro, luego llama a `Order.Create`,
   deduplica por `(merchantId, merchantReferenceId)` y persiste todos los válidos juntos.
4. Mostrar `Order.Create`: aquí viven las invariantes autoritativas. La API no puede saltárselas y
   EF tampoco contamina el dominio.
5. Mostrar `OrderConfiguration` y `UtcDateTimeOffsetConverter`: SQLite recibe UTC ISO fijo; los
   índices soportan orden cronológico y futura historia por comprador/moneda.
6. Mostrar `SeedDemoOrdersHandler`: IDs, tiempos y labels son reproducibles; la segunda ejecución
   compara y omite, mientras un hecho distinto falla sin sobrescribir.
7. Cerrar con `OrderImportEndpointTests`, `OrderPersistenceTests` y `DemoSeedTests`: los tests hacen
   ejecutable el relato —formatos, límites, rollback, orden temporal, idempotencia y privacidad—.

La evidencia integrada es 16 tests de integración, 8 de dominio, modelo EF sin cambios pendientes y
compuerta full-stack verde sobre `main`.

## Etapa 3 — motor determinista

> Estado: integrada en `main` mediante `809ff75` y verificada con la compuerta full-stack.

### Speech de 60 segundos

> El motor reproduce el orden de tiempo de evento y trata todos los pedidos con el mismo timestamp
> como una cohorte: ninguno puede observar a otro evento simultáneo. Cada assessment usa únicamente
> pedidos anteriores y una configuración inmutable versionada. Las seis reglas generan señales con
> peso y evidencia legible; el score suma esas señales, se limita a 100 y requiere 60 para marcarse.
> El scorer solo recibe pedidos, nunca labels. Después, otro caso de uso une los scores ya calculados
> con el ground truth, calibra el umbral sobre los primeros dos tercios temporales y mide una vez el
> tercio final. En la fixture sintética el holdout da 6 TP, 0 FP y 0 FN, pero lo presento como una
> prueba reproducible del pipeline, no como rendimiento generalizable: el dataset fue diseñado para
> ser separable y las reglas de velocity se validan con escenarios específicos.

### Configuración y resultado provisional

| Regla | Semántica resumida | Peso |
| --- | --- | ---: |
| `amount_anomaly` | `>=3x` mediana temporal por comprador/moneda, con fallback al comercio | 40 |
| `velocity` | Cuarto pedido del comprador en diez minutos | 30 |
| `cross_border_velocity` | País distinto dentro de dos horas | 40 |
| `unusual_hour` | Franja local rara tras 20 pedidos anteriores | 10 |
| `new_buyer_high_value` | Comprador nuevo y `>=2.5x` mediana del comercio | 30 |
| `foreign_country` | País distinto del habitual con dominancia mínima de 60% | 20 |

- Umbral: `score >= 60`; ninguna señal aislada marca un pedido.
- Scores de la fixture: 266 en 0, 16 en 20, 13 en 60 y 5 en 90.
- Calibración: 12 TP, 0 FP, 0 FN y 188 TN.
- Holdout: 6 TP, 0 FP, 0 FN y 94 TN.
- Los 18 positivos sintéticos combinan importe anómalo y país extranjero. La fixture no contiene
  ráfagas suficientes para medir empíricamente todas las reglas.

### Recorrido real por capas

| Capa | Código para mostrar | Responsabilidad |
| --- | --- | --- |
| `Salvo.Domain/Risk` | `RuleConfig.cs`, `TemporalRiskEngine.cs` | Cohortes temporales, baseline efímero, seis reglas, señales y score puro. |
| `Salvo.Domain/Evaluation` | `RiskMetricsEvaluator.cs` | Matriz, métricas, sweep 0–100 y split temporal sin dependencias de EF. |
| `Salvo.Application/Risk` | `EvaluateLocalRiskHandler.cs` | Calcula scores antes de pedir labels; exige cobertura completa y evalúa holdout. |
| `Salvo.Infrastructure` | `EfRiskOrderReader.cs`, `EfEvaluationLabelReader.cs` | Lectores separados y read-only sobre el schema E2. |
| Tests | `TemporalRiskEngineTests.cs`, `RiskMetricsEvaluatorTests.cs`, `RiskEvaluationTests.cs` | Futuro, cohortes, orden, reglas, cold start, límites, idempotencia y fixture completa. |

### Decisiones para profundizar

#### ¿Por qué agrupar timestamps iguales?

El orden por referencia resuelve reproducibilidad, no causalidad. Si se actualizara el baseline
después de cada pedido, un evento simultáneo podría convertirse artificialmente en historia del
siguiente. El motor evalúa el grupo completo y solo entonces actualiza el estado.

#### ¿Cómo se impide el uso del label?

`TemporalRiskEngine.Score` acepta únicamente pedidos y `RuleConfig`. Aplicación usa un puerto para
pedidos y otro para labels; consulta los labels después de obtener todos los assessments. Un test de
arquitectura protege esta firma y la evaluación rechaza cobertura incompleta.

#### ¿Por qué mediana y factores racionales?

La mediana tolera mejor los propios outliers que intentamos detectar. Los factores `3/1` y `5/2`
se comparan con aritmética decimal exacta, sin una decisión de borde dependiente de `double`.

#### ¿Qué significa la métrica perfecta actual?

Demuestra que el procesamiento, la separación de labels y el split son reproducibles sobre la
fixture conocida. No estima desempeño real: el dataset es pequeño, sintético y deliberadamente
separable. Un piloto real exigiría datos representativos, costos de negocio y validación temporal
externa.

## Preguntas técnicas probables

### ¿Cómo evitás fuga temporal en el motor?

Cada pedido se procesa en orden y su baseline usa únicamente pedidos anteriores. El ground truth se
consulta después para evaluar; nunca entra en las reglas. Incluir el futuro produciría métricas
artificialmente buenas y haría imposible reproducir una decisión tomada en tiempo real.

### ¿Qué reglas usa el MVP?

- `amount_anomaly`: importe fuera del baseline histórico.
- `velocity`: demasiados pedidos dentro de una ventana corta.
- `cross_border_velocity`: países incompatibles en poco tiempo.
- `unusual_hour`: horario atípico en una zona horaria explícita.
- `new_buyer_high_value`: comprador nuevo con importe elevado.
- `foreign_country`: país diferente del habitual.

Los pesos y umbrales viven en configuración y cada señal incluye un detalle legible.

### ¿Cómo calibrás el umbral?

Ejecuto un barrido de umbral sobre datos etiquetados y comparo precisión, recall, F1 y tasa de falsos
positivos. No existe un umbral universal: el punto operativo depende del costo de rechazar una venta
legítima frente al costo del fraude. Accuracy no es suficiente en una clase desbalanceada.

### ¿Cómo modelás `received` o una respuesta pendiente?

Persisto la evaluación como `PENDING`, junto con la referencia del comercio y el ID de evaluación
externo. La decisión final llega por callback o por reconciliación mediante consulta de estado. El
pedido no se trata como aprobado o rechazado mientras el resultado siga pendiente.

### ¿Qué ocurre si llega dos veces el mismo callback?

El callback tiene una clave de deduplicación estable. Primero se persiste el recibo y después se
aplica la transición dentro de una transacción. Un replay encuentra la clave ya procesada y devuelve
éxito sin repetir efectos.

### ¿Cómo manejás timeouts y retries?

Toda llamada externa tiene timeout. Solo reintento errores transitorios y únicamente cuando la
operación tiene una clave idempotente. Uso backoff con jitter y un límite; si se agota, conservo un
estado reconciliable y genero observabilidad, en lugar de ocultar el fallo con retries infinitos.

### ¿Cómo evitás estados inconsistentes?

Los cambios relacionados se ejecutan en una transacción de DB. Por ejemplo, revisar una alerta
actualiza el estado de la alerta y del pedido juntos. Las transiciones válidas se centralizan en un
caso de uso y las rutas/API no duplican esa lógica.

### ¿Cómo protegés datos sensibles?

El MVP usa pseudónimos y datos sintéticos. Las keys permanecen server-side; no registro payloads
completos, documentos, PAN, CVV ni links sensibles. Antes de integrar datos reales revisaría LGPD,
base legal, minimización, retención, redacción, permisos y mecanismo oficial de validación de origen
de callbacks.

### ¿Qué pasa si Koin o Anthropic no están disponibles?

El motor local y las alertas siguen operativos. El proveedor externo conserva un estado de error o
pendiente que puede reconciliarse. Las explicaciones usan por defecto una implementación
determinista; tests y demo no requieren API key ni red.

### ¿Cómo escalarías el diseño?

Primero migraría a Postgres y añadiría auth/multi-tenant. Si el volumen lo exige, separaría ingesta y
evaluación mediante una cola, con claves idempotentes, outbox y workers. Añadiría métricas de latencia,
errores, pendientes antiguos, callbacks duplicados y tasa de decisiones, además de runbooks.

### ¿Cómo probarías una integración real?

Tests de contrato sobre el OpenAPI publicado por ASP.NET Core, fixtures sanitizados y escenarios de
sandbox para aprobado/rechazado/pendiente, callback duplicado, timeout, error 5xx y reconciliación.
Para un go-live real seguiría los casos de UAT y criterios de certificación entregados por Koin.

## IA y Anthropic

### ¿Por qué usar Anthropic?

Para convertir señales técnicas en una explicación corta y comprensible, mediante salida
estructurada y grounding. Es una mejora de experiencia, no una dependencia del veredicto.

### ¿Cómo limitás alucinaciones y prompt injection?

- Solo se envían señales y campos mínimos necesarios.
- Textos externos se delimitan como datos no confiables.
- La salida debe cumplir un schema.
- La severidad y acción crítica se derivan de código determinista.
- La respuesta se valida antes de persistir o mostrar.

### ¿Cómo controlás costos?

No se explica una alerta dos veces, se acota el output, se registran métricas de uso sin contenido
sensible y se configura un límite de gasto. El proveedor mock permite desarrollo y tests gratuitos.

## Historias STAR para preparar

### Idempotencia

- **Situación:** un evento o callback puede repetirse.
- **Tarea:** evitar alertas o transiciones duplicadas.
- **Acción:** clave de negocio estable, restricción única y transacción.
- **Resultado:** repetir la operación conserva exactamente un efecto.

Conectar con experiencia previa en Kafka/Inbox-Outbox si corresponde, usando un ejemplo real y
cuantificable.

### Degradación elegante

- **Situación:** un proveedor externo puede fallar o demorar.
- **Tarea:** conservar una decisión local y trazabilidad.
- **Acción:** separar dominio/adaptador, usar timeout y estado reconciliable.
- **Resultado:** el flujo local continúa y el fallo externo permanece visible.

### Calidad de evaluación

- **Situación:** fraude es una clase desbalanceada.
- **Tarea:** evitar una métrica engañosa.
- **Acción:** usar precision/recall/F1 y validación temporal.
- **Resultado:** el umbral se discute como trade-off de negocio, no como número mágico.

## Preguntas inteligentes para hacer a Koin

Elegir tres o cuatro según el rol y el momento:

- ¿Qué parte del ciclo antifraude tendría mayor ownership esta posición: integración, motor de
  decisión, plataforma, operaciones o experiencia del comercio?
- ¿Cómo se reparten las decisiones síncronas, análisis pendientes y reconciliación en el sistema real?
- ¿Qué señales operativas consideran más importantes para detectar degradación antes de afectar a los
  comercios?
- ¿Cómo es el proceso de certificación/UAT para nuevas integraciones o cambios de contrato?
- ¿Qué trade-off entre conversión y prevención de fraude aparece con más frecuencia en el equipo?
- ¿Cómo gestionan evolución y compatibilidad de contratos entre países o productos?
- ¿Qué esperan que una persona en este rol pueda entregar durante sus primeros 90 días?
- ¿Cómo colaboran ingeniería, riesgo, producto y operaciones cuando una regla o proveedor cambia?

Evitar preguntas cuya respuesta esté claramente en la documentación pública, salvo que se usen para
profundizar en decisiones internas o trade-offs.

## Límites que conviene declarar con claridad

- “El proveedor del MVP es un mock; no estoy presentándolo como integración oficial.”
- “Todavía no procesé datos reales ni ejecuté la certificación de Koin.”
- “El motor local demuestra arquitectura y evaluación, no pretende reemplazar un producto antifraude
  de producción.”
- “Anthropic está previsto, pero el sistema se diseñó para funcionar sin el LLM.”

Esta claridad suele fortalecer la conversación: muestra criterio, conocimiento del alcance y respeto
por requisitos de producción.

## Checklist antes de la entrevista

- [ ] Adaptar la guía a la descripción exacta del puesto.
- [ ] Ensayar pitch de 30 y 90 segundos.
- [ ] Preparar dos historias STAR reales y cuantificables.
- [ ] Poder dibujar el flujo local y el flujo externo en una hoja.
- [ ] Llevar las métricas reales del motor cuando existan.
- [ ] Saber qué partes son mock, sandbox o producción.
- [ ] Revisar documentación y novedades públicas de Koin el día anterior.
- [ ] Preparar tres preguntas para el equipo.

## Referencias oficiales

- [Koin Antifraude](https://koinlatam.com/)
- [Security Scheme](https://api-docs.koin.com.br/docs/security-scheme)
- [Antifraud Services Flow](https://api-docs.koin.com.br/docs/antifraud-services-flow-1)
- [Integration Requirements](https://api-docs.koin.com.br/docs/integration-requirements)
- [Create Evaluation](https://api-docs.koin.com.br/reference/createevaluationusingpost)
