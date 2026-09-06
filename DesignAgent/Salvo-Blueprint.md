# Salvo — Blueprint del MVP

> Estado del documento: vigente
> Estado del proyecto: Etapa 3 integrada y verificada; Etapa 4 diseñada y aprobada, en ejecución
> Última actualización: 2026-09-02
> Seguimiento operativo: [[Salvo-Progress]]

Fuente de verdad del producto, alcance y arquitectura. Salvo es una consola antifraude B2B para un
comercio electrónico. El MVP usa un proveedor externo mock; Koin sandbox y Anthropic se difieren
hasta que el núcleo local funcione y se apruebe cada integración.

## Navegación rápida

- [[#1. Objetivo del MVP|Objetivo]]
- [[#2. Alcance aprobado|Alcance]]
- [[#4. Requisitos funcionales|Requisitos funcionales]]
- [[#5. Alineación con Koin|Alineación con Koin]]
- [[#6. Arquitectura|Arquitectura]]
- [[#7. Modelo de datos conceptual|Modelo de datos]]
- [[#10. Seguridad y privacidad|Seguridad]]
- [[#11. Etapas pequeñas y verificables|Etapas]]
- [[#13. Bitácora de decisiones|Bitácora]]

## 0. Cómo usar este documento

Este Blueprint define el alcance, las decisiones y las etapas verificables. No es un archivo de
instrucciones para el agente: las reglas permanentes de Codex viven en `AGENTS.md` en la raíz.

Cuando cambie una decisión:

1. actualizar la bitácora de este archivo;
2. actualizar los documentos derivados que hayan quedado desfasados;
3. ajustar `AGENTS.md` solo si cambia una regla permanente de implementación.

## 1. Objetivo del MVP

Salvo es una consola de operaciones antifraude para un comercio electrónico. Recibe pedidos
sintéticos o importados, calcula un riesgo local mediante reglas deterministas y auditables,
genera alertas para los casos sospechosos y permite que un analista marque el pedido como legítimo
o fraude. Un dashboard resume el riesgo y una evaluación reproducible mide precisión, recall y F1.

El proyecto existe como demostración de portfolio para una oportunidad profesional vinculada con
Koin. Por eso prioriza problemas reales de integración antifraude: contratos de datos, estados
asíncronos, correlación por identificadores, idempotencia, callbacks, reconciliación, seguridad y
degradación elegante.

### Principio rector

El `localRiskScore` lo calculan reglas deterministas. La IA no decide si un pedido es fraude y una
evaluación de Koin, si se habilita, se guarda como una fuente externa separada. Nunca se mezclan los
scores ni se presenta uno como si fuera el otro.

### Pitch breve

> Salvo es una consola antifraude para e-commerce. Puntúa pedidos con reglas explicables, administra
> alertas y su ciclo de revisión y demuestra una integración desacoplada con un proveedor externo.
> Funciona completamente con datos sintéticos y un simulador local; Anthropic y el sandbox de Koin
> son adaptadores opcionales, no dependencias del núcleo.

## 2. Alcance aprobado

### Incluido en el MVP

- Dataset sintético e-commerce, reproducible y etiquetado.
- Importación de pedidos normalizados mediante CSV o JSON.
- Baseline cronológico por comercio/comprador pseudónimo.
- Motor de reglas puro con señales legibles y configuración auditable.
- `localRiskScore` de 0 a 100 y umbral configurable.
- Alertas idempotentes y revisión transaccional.
- Feed, detalle y dashboard antifraude.
- Matriz de confusión, precisión, recall, F1, tasa de falsos positivos y barrido de umbral.
- Interfaz `IAntifraudProvider` con implementación `mock` local.
- Escenarios externos simulados: `approved`, `denied` y `received`.
- Persistencia de correlación y callback simulado idempotente.
- Tests unitarios y de integración sin red.
- README de portfolio con decisiones, métricas, arquitectura y guion de demo.

### Diferido hasta después del núcleo

- Anthropic para redactar explicaciones; el MVP inicial usa explicación determinista/mock.
- Cliente real del sandbox de Koin.
- Device fingerprint oficial de Koin.
- Callback público en Internet.
- Autenticación y multi-tenant real.
- Prometheus, Grafana y alertas operativas.
- PostgreSQL, despliegue y CI remoto.
- Consulta en lenguaje natural o NL→SQL.

### Fuera de alcance

- Pagos, captura o autorización reales.
- Datos personales o financieros reales.
- PAN, CVV, documentos de identidad o enlaces sensibles persistidos o registrados.
- Certificación/UAT o afirmaciones de integración oficial con Koin.
- Decisiones automáticas que bloqueen dinero o entrega de bienes reales.
- Modelos de machine learning entrenados.
- Tiempo real o streaming.

## 3. Usuario y flujo principal

### Persona

Analista de riesgo u operaciones de un comercio electrónico ficticio.

### Flujo

1. El analista carga el dataset demo o importa pedidos.
2. Salvo procesa los pedidos en orden temporal.
3. Para cada pedido calcula el baseline usando únicamente historia anterior.
4. Las reglas generan señales y `localRiskScore`.
5. Los pedidos que superan el umbral producen una alerta una sola vez.
6. El analista inspecciona señales y contexto.
7. El analista confirma que el pedido es legítimo o reporta fraude.
8. El dashboard y las métricas se actualizan.
9. Opcionalmente se ejecuta el proveedor externo mock para demostrar estados y callbacks.

## 4. Requisitos funcionales

### 4.1 Datos e importación

- Seed idempotente con 300 pedidos de una ventana fija de 120 días.
- Ground truth completo: 300 etiquetas separadas, con 18 fraudes y 282 casos legítimos.
- Campos mínimos normalizados:
  - `occurredAt` en UTC;
  - `amountCents` positivo para el total del pedido;
  - `currencyCode` ISO 4217;
  - `merchantId` y `merchantReferenceId` forman una referencia comercial estable y única;
  - `buyerReferenceId` pseudónimo;
  - `countryCode` ISO 3166-1 alpha-2;
  - `city`, `channel` y `deviceSessionId` opcionales.
- El parser acumula errores por fila y continúa.
- Una biblioteca mantenida de .NET resuelve quoting, BOM y delimitadores; la API valida los DTOs
  y el dominio vuelve a comprobar sus invariantes. El frontend valida anticipadamente solo para UX.

Contrato detallado aprobado para E2:

- `Order` contiene hechos inmutables; score, estado, evaluaciones y alertas se difieren.
- PK técnica `id`; unicidad natural sobre `(merchantId, merchantReferenceId)`.
- Repetir una referencia con el mismo contenido se omite; con contenido distinto produce conflicto.
- IDs sintéticos ASCII, en mayúsculas, de hasta 64 caracteres y con prefijos `MER_`, `ORD_`,
  `BUY_` y `DEV_` según corresponda.
- `occurredAt` y `createdAt` son `DateTimeOffset` en C#, se normalizan a UTC y se persisten como
  texto ISO 8601 canónico de longitud fija.
- El orden total es `occurredAt`, `merchantId`, `merchantReferenceId`.
- `amountCents` es `Int64` entre 1 y 1.000.000.000.000; el MVP admite `UYU`, `BRL` y `USD` sin
  conversión FX. Los historiales monetarios futuros se particionan por moneda.
- `channel` es nullable y admite `WEB`, `MOBILE_APP` o `MARKETPLACE`.
- `city` es nullable, máximo 80 caracteres, Unicode NFC, sin controles ni direcciones.
- No se persiste `description`; los campos desconocidos se rechazan.
- CSV y JSON comparten el mismo DTO normalizado mediante `POST /api/order-imports`, archivo
  `multipart/form-data` y formato explícito `CSV | JSON`.
- Límite de 5 MiB y 10.000 registros; CSV UTF-8 con BOM opcional, coma o punto y coma y quoting
  RFC 4180; JSON como array de objetos.
- Los registros válidos se escriben en una transacción; los inválidos se reportan por registro y
  no bloquean el resto. Un fallo de infraestructura revierte todo el subconjunto válido.
- Los errores usan códigos estables, no reproducen valores recibidos y detallan como máximo 1.000
  errores sin perder los conteos completos.

### 4.2 Baseline y scoring local

- Cada pedido solo puede observar pedidos anteriores.
- Estadísticas de importe sobre valores positivos del pedido.
- País habitual, comercios/compradores conocidos, frecuencia y horas habituales.
- Cold start: las reglas que dependen de historial no disparan antes de un mínimo configurable.
- Reglas iniciales:
  - `amount_anomaly`;
  - `velocity`;
  - `cross_border_velocity`;
  - `unusual_hour` usando una zona horaria de negocio explícita;
  - `new_buyer_high_value`;
  - `foreign_country`.
- Cada señal tiene `{ rule, weight, detail }` y `detail` es legible para una persona.
- Pesos, ventanas, mínimos y umbral viven en un `RuleConfig` central e inmutable.
- Reejecutar scoring produce el mismo resultado y no altera pedidos ya revisados sin una operación
  explícita de recalibración.

Diseño aprobado para E3:

- El motor ordena por `(occurredAt, merchantId, merchantReferenceId)` y evalúa por cohortes de
  `occurredAt`: todos los pedidos de un mismo instante observan exclusivamente timestamps menores.
  El desempate estabiliza la salida, pero no convierte eventos simultáneos en historia causal.
- El baseline mantiene importes por comercio/moneda y comprador/moneda, actividad reciente por
  comercio/comprador, países por comercio y comprador, compradores conocidos y franjas horarias
  del comercio. Las ventanas son `[occurredAt - window, occurredAt)`.
- `RuleConfig e3-v1` usa mediana temporal, multiplicadores racionales y configuración inmutable:
  importe 90 días con mínimo 3 y anomalía `3x`; velocity 10 minutos y cuarto pedido;
  cross-border 2 horas; horario 30 días, mínimo 20, franjas de 6 horas y rareza `<=10%` en
  `America/Montevideo`; nuevo comprador `2.5x`; país 90 días, mínimo 3 y dominancia `>=60%`.
- Pesos: `amount_anomaly=40`, `velocity=30`, `cross_border_velocity=40`, `unusual_hour=10`,
  `new_buyer_high_value=30`, `foreign_country=20`. El score se limita a 100 y el flag usa
  `score >= 60`.
- E3 produce assessments transitorios y no crea tablas, evaluaciones persistidas, alertas ni API.
  El lector usado por scoring no expone labels; evaluación los une por `orderId` solo después de
  calcular todos los scores.
- La calibración divide cohortes temporalmente: primeros dos tercios para barrer umbrales `0..100`
  y último tercio como holdout. Se maximiza F1, luego se minimiza FPR y finalmente se elige el
  umbral más alto. Una métrica sin denominador se representa como indefinida, no como cero.

### 4.3 Alertas y revisión

- Como máximo una alerta `OPEN` por pedido. Una evaluación marcada crea alerta solo si el pedido no
  tiene alerta abierta y no fue revisado, o si la banda de severidad vigente supera la de la última
  alerta; en ese caso la alerta nueva enlaza a la anterior mediante `supersedesAlertId`.
- `severity` se deriva del score mediante una función determinista y no se persiste. Sí se persiste
  `alertPolicyVersion`, para que una política futura no reclasifique alertas históricas.
- Estados de alerta: `OPEN`, `CONFIRMED_SAFE`, `REPORTED_FRAUD`. El veredicto es terminal: una
  alerta revisada no se reabre.
- La revisión actualiza la alerta y escribe su registro de auditoría en una única transacción de
  base de datos. El pedido no cambia: sus hechos son inmutables.
- El registro de auditoría guarda **qué explicación tenía delante** la analista, o ninguna. Sin
  ese dato, una revisión emitida con la explicación pendiente y otra emitida con la explicación
  lista son indistinguibles para siempre. Revisar nunca exige que exista una explicación.
- La transacción no basta por sí sola. `status` actúa como token de concurrencia para que dos
  revisiones simultáneas terminen en conflicto y no en sobrescritura silenciosa.
- El score copiado a la alerta es un snapshot auditable de la evaluación que la originó y no se
  actualiza. La lectura expone además la evaluación vigente y su divergencia de banda; revisar con
  bandas divergentes exige reconocerlo explícitamente.
- La métrica `amountAtRisk` suma alertas abiertas; el fraude ya reportado se presenta por separado.

### 4.4 Interfaz

- `/import`: carga demo o importa un archivo, presenta errores por fila y **dispara la corrida de
  scoring**, mostrando su resumen. Sin este paso no hay evaluaciones ni alertas: importar no
  procesa.
- `/alerts`: feed de alertas abiertas ordenadas por el score local de la evaluación vigente.
- `/alerts/[id]`: contexto, señales del snapshot, evaluación vigente, divergencia y acciones de
  revisión.
- `/dashboard`: alertas abiertas, monto en riesgo desglosado por moneda, fraude reportado por
  pedido, tasa de marcado, riesgo temporal y señales principales.
- **Toda pantalla de datos declara su procedencia**: de qué corrida es lo que muestra, y cuántos
  pedidos quedaron sin puntuar.
- El monto en riesgo nunca se totaliza entre monedas.
- Severidad expresada con texto además de color.
- Estados vacíos, carga y error accesibles. Hay tres estados vacíos distintos: sin pedidos, con
  pedidos y sin corrida, y con corrida y sin alertas abiertas.

### 4.5 Evaluación externa

- La evaluación de un proveedor externo es una **entidad propia**, `ExternalEvaluation`, con ciclo
  de vida mutable. No comparte tabla ni tipos con la evaluación local, que es append-only.
- La fila se **reserva y se persiste antes** de llamar al proveedor, con la referencia del pedido.
  Así el único parcial serializa las solicitudes concurrentes cuando todavía no hay nada del lado
  del proveedor.
- La correlación es **doble**: por `externalEvaluationId` y, si falta, por `referenceId`.
- Un fallo posterior al envío —timeout, `5xx`, respuesta ilegible— **no cierra** la evaluación:
  queda `PENDING` con `lastErrorCode`. Solo la cierran los fallos previos al envío y los rechazos
  definitivos del proveedor.

Tabla de transiciones:

| Origen | Mensaje | Efecto | Recibo | HTTP |
| --- | --- | --- | --- | --- |
| `PENDING` | terminal | Transiciona, con `settledAt` y `settledBy` | `APPLIED` | 200 |
| `PENDING` | `PENDING` | `lastErrorCode` si aplica | `NO_OP` | 200 |
| Terminal | el mismo terminal | Ninguno | `NO_OP` | 200 |
| Terminal | `PENDING` | Ninguno: llegó fuera de orden | `SUPERSEDED` | 200 |
| Terminal | otro terminal | Ninguno: el proveedor se contradice | `CONFLICTING` | 200 |
| — | sin fila correlacionable | Ninguno | `UNMATCHED` | 202 |

- El recibo y la transición se persisten en una **única** unidad de trabajo.
- La evaluación externa **no abre alertas**. El proveedor opina; el comercio decide.
- La divergencia entre el criterio local y el externo se **expone**, con las dos procedencias. Los
  scores no se comparan numéricamente entre sí: son escalas de sistemas distintos.
- La reconciliación es explícita y manual, como la corrida de scoring.

### 4.6 Evaluación

- Ground truth disponible solo en fixtures/seed y excluido de todas las features.
- Matriz TP/FP/FN/TN.
- Precisión, recall, F1 y tasa de falsos positivos.
- Barrido de umbral sobre scores ya calculados.
- Las métricas se calculan sobre procesamiento temporal válido; nunca usando un baseline construido
  con el futuro.

### 4.7 Explicabilidad

- La explicación es una entidad propia cuya identidad es la **evaluación**, no la alerta. La alerta
  es el contexto desde el que se pide.
- Se muestra la explicación de la evaluación del snapshot, que es la premisa sobre la que se formó
  el veredicto. Que esté **desactualizada** se calcula al leer y nunca se persiste, igual que la
  divergencia de banda.
- El grounding se **verifica sobre la salida**, no se confía al prompt. Tres capas:
  - **reglas**: todo nombre de regla mencionado pertenece a las señales de la evaluación;
  - **cifras**: todo número del texto está respaldado por un hecho del conjunto `ExplanationFacts`,
    construido en el dominio a partir de la evaluación y del pedido. Ese conjunto incluye el score,
    el umbral, el tope, el peso de cada señal y su suma, la cantidad de señales, todos los números
    de cada `detail`, el monto en centavos **y** en unidades, y el instante del pedido en UTC **y**
    en la zona horaria de negocio. Un token con `d` decimales está fundamentado si algún hecho `F`
    cumple `N == F` o `round(F, d) == N`. Las cifras escritas en letras no se validan, y se declara
    que no se validan;
  - **forma**: tope de longitud y ausencia de marcado.
- La validación vive en el **caso de uso**, entre el puerto y el almacén. En el adaptador, el
  proveedor determinista la pasaría por cortesía y no por construcción.
- Un texto rechazado no se persiste, no se registra y no llega al diagnóstico: para depurar alcanza
  con el token ofensor, nunca la frase.
- Al proveedor no entra **ningún texto que no escriba el motor**: quedan fuera la ciudad, los
  identificadores de comprador, comercio, sesión y comercio emisor, la nota de revisión y el
  veredicto externo. Las enumeraciones validadas sí entran.
- Generar es idempotente y no puede quedar trabado: la fila se reserva y se persiste antes de
  llamar, el asentamiento no depende de que el cliente siga esperando, `FAILED` se reintenta sobre
  la misma fila, una solicitud pendiente vencida es retomable y hay tope de intentos.
- Sin clave el sistema funciona con un proveedor determinista. Un `AI_PROVIDER` que nombre un
  adaptador inexistente falla al arrancar.

## 5. Alineación con Koin

Koin evalúa transacciones de comercios, no entrega movimientos bancarios de consumidores. Su flujo
publicado contempla pre-evaluación opcional, evaluación completa, estados síncronos y resultados
asíncronos mediante callback. La credencial privada y el identificador de organización se obtienen
durante onboarding.

### 5.1 Lo que demuestra el MVP sin afirmar integración oficial

- Contrato de proveedor externo detrás de una interfaz.
- `referenceId` estable por pedido, escrito antes de llamar al proveedor.
- `externalEvaluationId` para correlación, cuando el proveedor lo asigna.
- Correlación **doble**: por identificador externo y, si falta o todavía no llegó, por referencia.
- Estados externos separados: `PENDING`, `APPROVED`, `DENIED`, `ERROR`.
- Respuesta `received` tratada como pendiente.
- Callback idempotente y replay-safe.
- Reconciliación simulada mediante consulta de estado.
- Timeout y error de proveedor sin perder la evaluación local.
- Datos sintéticos y payloads sanitizados en logs.

### 5.2 Adaptadores

```csharp
public interface IAntifraudProvider
{
    Task<ExternalEvaluationResult> EvaluateAsync(
        ExternalEvaluationInput input,
        CancellationToken cancellationToken);

    Task<ExternalEvaluationResult> GetStatusAsync(
        ExternalEvaluationLookup lookup,
        CancellationToken cancellationToken);
}
```

`ExternalEvaluationLookup` lleva `ExternalEvaluationId` nullable y `ReferenceId`. Consultar solo por
identificador no permite reconciliar una evaluación cuyo identificador nunca llegó, que es
justamente el caso que la reconciliación existe para resolver.

- `MockAntifraudProvider`: obligatorio en el MVP, determinista y sin red. Su resultado se deriva de
  una función documentada de la referencia del pedido, con bandas declaradas, para que quien escriba
  la fixture controle la distribución.
- `KOIN_MODE=sandbox` **falla al arrancar** mientras no se cumplan los requisitos de §5.3.
- `KoinSandboxProvider`: posterior y activado solo si existen credenciales y contrato verificado.

El adaptador real deberá consultar el OpenAPI vigente. No se copiarán tipos manualmente desde
ejemplos narrativos si difieren del contrato publicado.

### 5.3 Requisitos antes de activar sandbox

- Private key y `org_id` facilitados por Koin.
- Base URL sandbox confirmada.
- Payload completo exigido por la versión elegida.
- Callback HTTPS públicamente accesible.
- Verificación del mecanismo oficial de autenticación/origen del callback.
- Device fingerprint oficial cuando el flujo web lo requiera.
- Casos sandbox de aprobación, rechazo y pendiente.
- Política de timeout, retry, backoff y reconciliación.

## 6. Arquitectura

Backend monolítico modular en ASP.NET Core y cliente web en Next.js. El backend concentra dominio,
casos de uso, persistencia e integraciones; Next.js es la capa de presentación y consume un contrato
HTTP/OpenAPI. No se crean microservicios para el MVP.

```mermaid
flowchart TD
    UI["Next.js / React UI"] -->|"HTTP + OpenAPI"| API["ASP.NET Core API"]
    API --> APP["Salvo.Application: casos de uso y puertos"]
    APP --> DOMAIN["Salvo.Domain: baseline, reglas, score, métricas"]
    API --> INFRA["Salvo.Infrastructure"]
    INFRA --> DB["EF Core / SQLite"]
    INFRA --> EXT["IAntifraudProvider"]
    EXT --> MOCK["Mock local"]
    EXT -. posterior .-> KOIN["Koin sandbox"]
    INFRA --> EXPLAIN["IExplanationProvider"]
    EXPLAIN --> DET["Explicación determinista"]
    EXPLAIN -. posterior .-> ANT["Anthropic"]
    CALLBACK["Callback externo"] --> API
```

### Capas

- `backend/src/Salvo.Domain/`: entidades, value objects, reglas, scoring y métricas puras.
- `backend/src/Salvo.Application/`: casos de uso, puertos y contratos internos.
- `backend/src/Salvo.Infrastructure/`: EF Core, repositorios y adaptadores mock/externos.
- `backend/src/Salvo.Api/`: endpoints HTTP, validación de transporte, OpenAPI y composición.
- `backend/tests/`: tests unitarios, de aplicación e integración.
- `frontend/src/`: App Router, componentes, cliente API y validación de UX.

`Salvo.Domain` no referencia ASP.NET Core, EF Core ni SDKs externos. La UI no contiene reglas de
fraude y no accede a la base de datos. Los endpoints delegan en casos de uso y los adaptadores no
filtran tipos o errores específicos del proveedor fuera de infraestructura.

## 7. Modelo de datos conceptual

### Order

- `id`: GUID interno y PK técnica
- `merchantId`
- `merchantReferenceId`; único dentro de `merchantId`
- `buyerReferenceId` pseudónimo
- `occurredAt`
- `amountCents`
- `currencyCode`
- `countryCode`, `city`, `channel`, `deviceSessionId`
- `createdAt`

Los hechos del pedido son inmutables. `localRiskScore`, estados, evaluaciones y alertas no se
preasignan como columnas nullable en E2; se incorporan cuando existan sus casos de uso y migraciones.

### OrderEvaluationLabel

- `orderId`: PK y FK a `Order`
- `isFraudLabel`
- `createdAt`

La etiqueta es ground truth exclusivo de demo/evaluación. No forma parte de `Order`, no aparece en
los contratos públicos CSV/JSON y solo el seed interno puede escribirla. El motor de scoring no la
recibe como feature.

### RiskEvaluation

- `id`
- `orderId`
- `source`: `LOCAL`. Permanece como columna porque forma parte del material que se hashea en el
  fingerprint; quitarla invalidaría todas las evaluaciones existentes
- `score`
- `status`: `APPROVED | DENIED`
- `signalsJson`
- `evaluationFingerprint`
- `ruleConfigVersion`
- timestamps

Es **append-only** y sin mutadores públicos. La evaluación de un proveedor externo **no** vive acá:
tiene naturaleza incompatible —mutable, reintentable, con identificadores de correlación— y vive en
`ExternalEvaluation`.

### ExternalEvaluation

- `id`, `orderId`
- `provider`: `EXTERNAL_MOCK | KOIN_SANDBOX`
- `referenceId`: la referencia estable del pedido, escrita en la reserva
- `externalEvaluationId` nullable
- `status`: `PENDING | APPROVED | DENIED | ERROR`, y token de concurrencia
- `score` nullable
- `errorCode` sanitizado, solo con `status = ERROR`
- `lastErrorCode`: fallo arrastrado por una fila que sigue `PENDING`
- `attemptCount`: sondeos de reconciliación
- `settledBy`: `SYNC | CALLBACK | RECONCILIATION`
- `requestedAt`, `updatedAt`, `settledAt` nullable

Único parcial de `(orderId, provider)` mientras `status = 'PENDING'`: un pedido puede tener varias
evaluaciones externas a lo largo del tiempo, pero solo una esperando respuesta.

### Alert

- `id`
- `orderId`
- `riskEvaluationId` único
- `riskScoreSnapshot`
- `signalsJsonSnapshot`
- `status`: `OPEN | CONFIRMED_SAFE | REPORTED_FRAUD`
- timestamps

La explicación **no vive acá**. `Alert` guarda un snapshot congelado que nunca se reescribe y su
`status` es token de concurrencia; un ciclo de vida reintentable —pendiente, listo, fallido,
reintentado— es otra entidad. Y `recommendedAction` no existe: es una biyección de la severidad,
y leída junto a la explicación se atribuye a la IA aunque la derive una tabla. Decisiones 51 y 53.

### AlertExplanation

- `id`
- `riskEvaluationId`, `provider`, `templateVersion`, `alertPolicyVersion`: juntos, la identidad.
  La alerta **no** forma parte de ella: todo lo que el proveedor recibe es función de la evaluación
- `providerVersion` opcional: el modelo concreto. Nunca parte de la identidad
- `requestedFromAlertId`: procedencia, no identidad
- `status`: `PENDING | READY | FAILED`. `FAILED` no es terminal
- `summary` y `referencedRules`, presentes **solo** con `READY`
- `failureCode` y `failureDetail`, que nunca contienen el texto rechazado
- `inputTokens`, `outputTokens`, `attemptCount`
- timestamps de solicitud y de asentamiento
- token de concurrencia

Únicos: total sobre la identidad —los reintentos ocurren sobre la misma fila, así que no bloquea
la regeneración— y parcial sobre `(riskEvaluationId, provider)` mientras esté `PENDING`.
`READY ⇔ summary` es una restricción de la base, no una promesa del manejador.

### CallbackReceipt

- `id`
- `provider`
- `deduplicationKey`, único junto con `provider`. Texto canónico
  `provider|externalEvaluationId|status|providerInstant`; nunca incluye el instante de recepción
- `externalEvaluationId` nullable, `referenceId` nullable
- `status`: `APPLIED | NO_OP | SUPERSEDED | CONFLICTING | UNMATCHED`
- `replayCount`, `lastSeenAt`
- `receivedAt`, `processedAt` nullable

No existe un estado `DUPLICATE`: un duplicado es la **ausencia** de una segunda fila, detectada por
violación de unicidad. El recibo y la transición se persisten juntos.

No se persiste el payload externo completo si contiene PII. Para debug se usan fixtures sintéticos
y logs redactados.

## 8. Stack y política de versiones

| Área | Elección |
| --- | --- |
| Backend | .NET 10 LTS + C# 14 + ASP.NET Core |
| Frontend | Node.js 24.20.0 + npm 11.19.0 + Next.js App Router + React |
| Lenguaje UI | TypeScript estricto |
| UI | Tailwind CSS |
| Persistencia | EF Core + SQLite local |
| Contrato HTTP | OpenAPI generado por ASP.NET Core; cliente TypeScript tipado |
| Validación | DTOs y dominio en .NET; Zod solo en bordes de UI que lo requieran |
| Tests backend | xUnit + tests de integración ASP.NET Core |
| Tests frontend | Vitest + Testing Library |
| Gráficos | SVG renderizado en el servidor; sin librería de gráficos |
| CSV | biblioteca .NET mantenida + validación autoritativa; no parser manual |
| IA posterior | cliente server-side detrás de `IExplanationProvider` |
| Koin posterior | `HttpClient` tipado detrás de `IAntifraudProvider` |

Reglas de versionado:

- `global.json` fija el SDK exacto de .NET 10 validado en la Etapa 1.
- Los proyectos usan `net10.0`, nullable habilitado y warnings tratados como errores.
- Los paquetes NuGet se fijan en `Directory.Packages.props` sin versiones flotantes.
- `.nvmrc` fija Node 24.20.0.
- `package-lock.json` se versiona.
- Las dependencias npm usan versiones exactas; no se usan rangos `latest` en instrucciones
  reproducibles.
- Las versiones exactas de Next.js, EF Core y demás paquetes se confirman en la Etapa 1 mediante
  smoke tests de ambas toolchains.
- El script de lint usa ESLint directamente, no `next lint`.
- Antes de escribir frontend se consulta la guía incluida en `node_modules/next/dist/docs/` para
  la versión instalada.

## 9. Variables de entorno y servicios

### Núcleo local

```dotenv
ASPNETCORE_URLS="http://127.0.0.1:5100"
ConnectionStrings__SalvoDb="Data Source=salvo.db"
SALVO_API_BASE_URL="http://127.0.0.1:5100"
BUSINESS_TIMEZONE="America/Montevideo"
```

### IA posterior

```dotenv
AI_PROVIDER="mock"
ANTHROPIC_API_KEY=""
ANTHROPIC_MODEL=""
```

### Koin posterior

```dotenv
KOIN_MODE="mock"
KOIN_BASE_URL="https://api-sandbox.koin.com.br"
KOIN_PRIVATE_KEY=""
KOIN_ORG_ID=""
KOIN_STORE_CODE=""
KOIN_CALLBACK_URL=""
KOIN_CALLBACK_SHARED_SECRET=
```

`.env.example` contiene nombres y valores seguros. `.env` nunca se versiona. Ninguna clave usa
prefijo `NEXT_PUBLIC_`.

Servicios/cuentas:

- Núcleo: ninguno externo.
- Anthropic: cuenta de API y límite de gasto, cuando se active.
- Koin: onboarding y credenciales sandbox, cuando se active.
- Callback Koin: despliegue o túnel HTTPS, solo en la etapa sandbox.
- Docker: solo para observabilidad post-MVP.

## 10. Seguridad y privacidad

- Datos 100% sintéticos durante el MVP.
- La API valida DTOs y el dominio valida invariantes; el frontend no sustituye esa validación.
- Secretos solo en el backend ASP.NET Core; Next.js no recibe claves de proveedores.
- Nunca registrar bodies completos de proveedor por defecto.
- No almacenar PAN, CVV ni documentos reales.
- Identificadores de comprador pseudónimos.
- Callbacks persistidos antes de responder `2xx` e idempotentes ante replay.
- Timeouts de red y errores sanitizados.
- La caída de IA o Koin no impide el scoring local.
- Sin autenticación, la app es solo local y no se publica con rutas mutables abiertas.
- Antes de datos reales o sandbox se revisan LGPD, retención, redacción y origen de callbacks.

## 11. Etapas pequeñas y verificables

### Etapa 0 — Documentación e instrucciones

- Normalizar documentos y crear `AGENTS.md`.
- No escribir código de aplicación.

Verificación: una sola fuente de verdad, sin contradicciones bloqueantes y sin código de aplicación
versionado.

### Etapa 1 — Fundaciones reproducibles

- Fijar SDK .NET, Node y dependencias exactas.
- Crear la solución con Domain, Application, Infrastructure, API y tests.
- Configurar ASP.NET Core, OpenAPI, EF Core/SQLite y un smoke test de persistencia.
- Crear el frontend Next.js sin mover ni borrar `DesignAgent/`.
- Configurar TypeScript, ESLint, Vitest y una prueba de UI.
- Añadir una compuerta local que ejecute verificaciones backend y frontend.

Verificación: API y frontend arrancan; `dotnet build`, `dotnet test`, `npm run check` y los builds
de producción pasan; smoke test EF Core/SQLite verde.

### Etapa 2 — Contrato y datos

- Implementar entidades, configuraciones EF Core, migración, fixtures y seed idempotente.
- Implementar importación en el backend con validación autoritativa y errores parciales.

Verificación: dos seeds dejan el mismo conteo; parser cubre quoting y errores parciales.

### Etapa 3 — Motor determinista

- Baseline temporal, reglas y score.
- Evaluación temprana y calibración.

Verificación: tests por regla y métricas reproducibles sin fuga temporal.

### Etapa 4 — Alertas y casos de uso

- Persistencia de evaluaciones.
- Alertas idempotentes.
- Revisión transaccional.

Verificación: reintentos no duplican y los estados permanecen consistentes.

### Etapa 5 — UI y dashboard

- Implementar en Next.js importación, feed, detalle, revisión y dashboard consumiendo la API.

Verificación: recorrido completo con datos, sin datos y con errores.

### Etapa 6 — Proveedor antifraude mock

- Interfaz, escenarios `approved/denied/received`, callback y reconciliación simulados.

Verificación: callbacks duplicados no repiten efectos y el estado pendiente puede finalizar.

### Etapa 7 — Explicabilidad

- Entidad propia con identidad por evaluación, ciclo de vida reintentable y grounding verificado
  sobre la salida contra hechos construidos en el dominio.
- Proveedor determinista, que pasa la misma validación que pasaría un modelo.
- Después, si se aprueba, adaptador Anthropic con salida estructurada. Las columnas y los códigos
  que ese adaptador necesita se ponen desde el principio, para no llegar con una migración.

Verificación: el sistema funciona sin key; la suite nunca depende de red; el texto generado no
puede cambiar ninguna superficie de decisión, y hay cuatro tests que fallan si lo hiciera.

### Etapa 8 — El argumento del proyecto

- README de portfolio, diagramas, capturas y guion de demo.
- Toda cifra del corpus concentrada en un bloque marcado, para que la Etapa 9 la actualice de una
  sola pasada.

Verificación: instalación desde cero, compuertas .NET y npm verdes, demo reproducible, y **cada
afirmación de hecho del README contrastada contra el código**.

### Etapa 9 — Corpus, idiomas y cierre

- Fixture enriquecida: comercios en mercados plausibles, varios arquetipos de fraude que cubran las
  seis reglas y las tres bandas, y **falsos negativos y falsos positivos deliberados**, para que las
  métricas midan el criterio y no el pipeline. Decidido con el usuario el 2026-09-06.
- Señales estructuradas: el motor emite campos tipados en vez de prosa, sube a `e3-v2` e invalida
  los fingerprints a propósito; la UI compone el texto y el portugués pasa a ser un diccionario más.
- Pasada de accesibilidad con lector de pantalla real, y traducción de los códigos de error de fila
  que hoy caen al inglés.
- Repaso final y actualización de todos los documentos con las cifras del corpus nuevo.

Verificación: las seis reglas y las tres bandas alcanzables desde la fixture; F1 deja de valer 1,00;
compuertas verdes y documentos coherentes con los datos.

### Post-MVP — Koin sandbox, auth, observabilidad y deploy

Cada capacidad se aprueba por separado. Activar el sandbox nunca es una condición para considerar
completo el MVP local.

## 12. Criterios globales de aceptación

- El motor local es determinista y auditable.
- No hay fuga temporal en baseline o evaluación.
- El seed y los efectos externos simulados son idempotentes.
- Scores local y externo permanecen separados.
- La app funciona sin Anthropic y sin Koin.
- No hay secretos ni PII real en repo, DB, fixtures o logs.
- Todas las mutaciones validan input en la API y preservan consistencia.
- `dotnet build`, `dotnet test`, `npm run check` y ambos builds de producción pasan.
- El README distingue claramente simulación, sandbox y producción.

## 13. Bitácora de decisiones

| # | Decisión | Razón | Fecha |
| --- | --- | --- | --- |
| 1 | Salvo pasa de B2C a consola antifraude B2B | Alinear el portfolio con el dominio y flujo de Koin | 2026-08-30 |
| 2 | Pedidos e-commerce sintéticos reemplazan movimientos bancarios | Koin evalúa transacciones de comercios; evita PII real | 2026-08-30 |
| 3 | Reglas deterministas calculan el riesgo local | Auditabilidad, testabilidad y explicabilidad | 2026-07-17 |
| 4 | Score local, evaluación Koin y alerta son conceptos separados | Evitar mezclar semánticas y preservar trazabilidad | 2026-08-30 |
| 5 | Koin mock es parte del MVP; sandbox es posterior | El acceso real exige onboarding, credenciales y HTTPS | 2026-08-30 |
| 6 | Anthropic es el proveedor previsto, pero se integra después del núcleo | La IA no debe ser una dependencia crítica | 2026-08-30 |
| 7 | NL→SQL sale del MVP | Menor riesgo y mayor foco en integración antifraude | 2026-08-30 |
| 8 | Sin auth, la aplicación permanece local | Evitar rutas mutables públicas sin aislamiento | 2026-08-30 |
| 9 | Baseline estrictamente temporal | Evitar fuga de información y métricas engañosas | 2026-08-30 |
| 10 | Node 24.20.0 y npm 11.19.0 como entorno base | Versiones instaladas y compatibles | 2026-08-30 |
| 11 | Versiones de dependencias fijadas tras smoke test | Reproducibilidad y control de cambios | 2026-08-30 |
| 12 | ASP.NET Core concentra backend y dominio; Next.js presenta la UI | Mostrar profundidad en C# y capacidad full-stack con fronteras claras | 2026-08-31 |
| 13 | EF Core/SQLite implementa persistencia local | Mantener el MVP portable y aprovechar transacciones y tooling del ecosistema .NET | 2026-08-31 |
| 14 | OpenAPI gobierna el contrato backend–frontend | Evitar contratos duplicados y mantener tipado el cliente TypeScript | 2026-08-31 |
| 15 | `Order` conserva hechos inmutables y usa unicidad `(merchantId, merchantReferenceId)` | Separar identidad interna de idempotencia comercial y permitir referencias repetidas entre comercios | 2026-08-31 |
| 16 | `isFraudLabel` vive en `OrderEvaluationLabel` y queda fuera del contrato público | Impedir que el ground truth alcance accidentalmente el scoring | 2026-08-31 |
| 17 | Fechas visibles como `DateTimeOffset` y persistidas en UTC ISO 8601 canónico | Mantener legibilidad y exigir ordenamiento temporal reproducible en SQLite | 2026-08-31 |
| 18 | La importación es estricta por schema y parcial por registro, con escritura atómica de válidos | Evitar pérdida silenciosa y estados técnicos incompletos | 2026-08-31 |
| 19 | El seed es una fixture fija de 300 pedidos y 300 etiquetas, activada explícitamente | Garantizar auditabilidad, ausencia de PII e idempotencia reproducible | 2026-08-31 |
| 20 | E2 excluye texto libre, FX, scoring y campos futuros sin caso de uso | Minimizar datos y preservar los límites entre etapas | 2026-08-31 |
| 21 | Pedidos con el mismo `occurredAt` se evalúan como una cohorte aislada | Impedir que un desempate técnico introduzca fuga temporal | 2026-08-31 |
| 22 | E3 usa `RuleConfig e3-v1` inmutable con medianas y factores racionales | Hacer cada decisión reproducible y auditable sin coma flotante | 2026-08-31 |
| 23 | Los seis pesos suman señales con cap 100 y flag inclusivo en 60 | Exigir corroboración sin convertir una señal aislada en alerta | 2026-08-31 |
| 24 | Horario inusual se aprende en franjas locales de seis horas | Modelar hábito temporal explícito sin fijar un horario comercial universal | 2026-08-31 |
| 25 | Scoring y lectura de labels usan fronteras separadas | Hacer estructural la prohibición de usar ground truth como feature | 2026-08-31 |
| 26 | Calibración usa un split temporal 2/3–1/3 y holdout sin retuning | Medir el umbral sin usar el futuro ni presentar el entrenamiento como evaluación | 2026-08-31 |
| 27 | E3 no persiste assessments ni crea alertas | Mantener RiskEvaluation y efectos idempotentes dentro de E4 | 2026-08-31 |
| 28 | `Order` permanece inmutable; el estado de revisión vive en `Alert` | El veredicto de un analista es juicio operativo, no un hecho del pedido; §4.3 se corrige | 2026-09-02 |
| 29 | `RiskEvaluation` es append-only con identidad por fingerprint de contenido, acotada a `source = LOCAL` | El motor depende del corpus, no solo de la config; el ciclo de vida externo es mutable y se decide en E6 | 2026-09-02 |
| 30 | La serialización canónica de señales vive en Domain y usa el orden de reglas del motor | Un recálculo desde la fila almacenada debe reproducir su propio fingerprint | 2026-09-02 |
| 31 | `ScoringRun` se persiste y define cuál es la evaluación vigente de un pedido | Un baseline puede volver a un estado anterior y hacer rebotar el fingerprint, dejando vigente un resultado obsoleto | 2026-09-02 |
| 32 | Como máximo una alerta `OPEN` por pedido; se re-alerta solo por escalada de banda | Evitar duplicados sin perder el fraude que revela una importación retroactiva | 2026-09-02 |
| 33 | Una alerta abierta no se actualiza, pero su divergencia con la evaluación vigente se expone y se reconoce al revisar | El snapshot es el registro auditable; ocultar el cambio de corpus induce decisiones falsas | 2026-09-02 |
| 34 | La severidad se deriva del score y se persiste `alertPolicyVersion` | Evitar que una política futura reclasifique retroactivamente alertas ya revisadas | 2026-09-02 |
| 35 | La revisión escribe alerta y auditoría en una transacción, con `status` como token de concurrencia | Una transacción atómica no protege un check-then-act; sin token, un veredicto pisa al otro | 2026-09-02 |
| 36 | La corrida de scoring invoca `TemporalRiskEngine` directamente; el camino de métricas queda separado | `EvaluateLocalRiskHandler` exige etiquetas completas que una importación no produce | 2026-09-02 |
| 37 | El dashboard operativo no lee la etiqueta; la calidad del criterio es otra superficie, tras `DemoData:Enabled` | La verdad de campo no existe en producción: un comercio conoce el veredicto de su analista, no cuáles pedidos eran fraude | 2026-09-03 |
| 38 | Esa frontera se verifica invirtiendo las etiquetas en la base y exigiendo una respuesta idéntica, no por reflexión sobre constructores | La reflexión no ve un `JOIN` en la implementación EF, ni una dependencia indirecta, ni el segundo puerto de etiquetas | 2026-09-03 |
| 39 | La corrida de scoring se dispara desde la consola y toda pantalla de datos declara su procedencia | §4.4 no decía dónde se procesaba: la analista importaba y el feed quedaba vacío para siempre | 2026-09-03 |
| 40 | Los tipos del cliente se generan desde OpenAPI y las guardas de ejecución proyectan en vez de comprobar | Evitar el contrato duplicado de la decisión 14, e impedir que un campo futuro llegue al navegador antes de decidir mostrarlo | 2026-09-03 |
| 41 | Las rutas de datos se declaran dinámicas y el build debe pasar sin API levantada | Next.js prerenderiza en build y congelaría un estado de error como HTML estático | 2026-09-03 |
| 42 | Los componentes cliente reciben primitivas; ningún objeto de API cruza la frontera servidor–cliente | En React Server Components toda prop de un componente cliente se serializa entera en el HTML | 2026-09-03 |
| 43 | El gráfico del dashboard es SVG renderizado en el servidor; Recharts sale del stack | Una librería de gráficos obliga a componente cliente y reabre la superficie que cierra la decisión 42 | 2026-09-03 |
| 44 | La evaluación externa es una entidad propia, con tipos propios, y no comparte tabla ni enumeración con la evaluación local | Una es función pura del corpus, idempotente y con identidad de contenido; la otra es una conversación mutable con un sistema remoto. Compartir tabla es la mezcla que el producto declara no hacer | 2026-09-04 |
| 45 | La fila de evaluación externa se reserva y se persiste antes de llamar al proveedor | Sin reserva, dos solicitudes concurrentes crean dos evaluaciones en el proveedor, y un callback previo al commit queda huérfano para siempre | 2026-09-04 |
| 46 | La correlación del callback es doble: por identificador externo y por referencia del pedido | El identificador no existe hasta que el proveedor responde, y hay estados en los que nunca llega | 2026-09-04 |
| 47 | Un fallo posterior al envío no cierra la evaluación externa; la cierra la reconciliación | Un timeout es indeterminado: cerrar en `ERROR` pierde el veredicto real y duplica la evaluación en el proveedor | 2026-09-04 |
| 48 | El recibo del callback y la transición se persisten en una única unidad de trabajo | Si el recibo sobrevive a una transición fallida, el reintento se ve como duplicado y la transición se pierde para siempre | 2026-09-04 |
| 49 | El disparador de callback de la demo es un endpoint de la API bajo bandera; el cliente elige qué evaluación, nunca qué estado | Un botón que compone el payload deja cerrar cualquier evaluación pendiente en el estado elegido, y obligaría a sacar el secreto de la API | 2026-09-04 |
| 50 | La evaluación externa no abre alertas y su score no se compara numéricamente con el local | El proveedor opina y el comercio decide; dos escalas de sistemas distintos no son comparables | 2026-09-04 |
| 51 | La explicación es una entidad propia y su identidad es la evaluación, no la alerta | `Alert` guarda un snapshot congelado con `status` como token de concurrencia; y con clave por alerta, escalar duplica filas y paga dos veces la misma redacción | 2026-09-05 |
| 52 | El grounding se verifica sobre la salida contra un conjunto de hechos construido en el dominio, con tokenizador declarado e igualdad por redondeo | Un prompt que pide no inventar cifras es una intención; y una validación literal contra «los datos suministrados» rechaza texto correcto por centavos, separadores, redondeo y zona horaria | 2026-09-05 |
| 53 | La IA no recomienda acciones, y la acción recomendada se elimina del alcance | Con tres bandas es una biyección de la severidad, mete prosa traducible dentro de la API, y leída junto a la explicación se atribuye a la IA aunque la derive una tabla | 2026-09-05 |
| 54 | Al input de un modelo no entra ningún texto que no escriba el motor | El criterio de «texto libre importado» es insuficiente: un identificador normalizado a mayúsculas admite una instrucción legible en su alfabeto, y la nota de revisión no viene de ningún archivo | 2026-09-05 |
| 55 | La generación reserva antes de llamar, asienta aunque el cliente aborte, reintenta sobre la misma fila y retoma la pendiente vencida | Sin eso, una petición cancelada por el navegador deja una fila pendiente huérfana que bloquea la evaluación para siempre, porque la etapa no tiene reconciliación | 2026-09-05 |
| 56 | El registro de revisión guarda qué explicación tenía delante la analista | Conservar la explicación desactualizada se justifica por el registro de lo que se pudo leer al decidir, y ese registro no existía | 2026-09-05 |
| 57 | El catálogo de mensajes de la consola es exactamente lo que los endpoints emiten como problema; un código que es el valor de un campo dentro de un `200` se rotula aparte | Mezclarlos rompe la aserción de exactitud del catálogo, o la obliga a debilitarse, que es peor | 2026-09-05 |
| 58 | Cambiar el texto que produce la plantilla sube su versión | `templateVersion` está en la identidad de la fila justamente para identificar qué plantilla escribió el texto: sin subirla, dos filas rotuladas igual llevan textos distintos y el campo deja de significar algo. Y como la API no reescribe una explicación ya escrita, el arreglo no alcanzaría a nada de lo guardado | 2026-09-06 |

## 14. Mapa de documentación

| Archivo | Responsabilidad |
| --- | --- |
| `AGENTS.md` | Instrucciones permanentes para Codex |
| `CLAUDE.md` | Entrada automática de Claude Code; importa contexto compartido |
| `ClaudeAgent/` | Workflow y plantillas específicas de Claude |
| `ClaudeAgent/Claude-Model-Policy.md` | Criterio de modelo y esfuerzo por tipo de tarea |
| `Coordination/` | Workboard, ownership y handoffs Codex–Claude |
| `Coordination/Task-Brief-Template.md` | Brief compartido para delimitar tareas de Codex o Claude |
| `DesignAgent/Salvo-Blueprint.md` | Fuente de verdad del diseño y roadmap |
| `DesignAgent/Salvo-Overview.md` | Resumen ejecutivo |
| `DesignAgent/Salvo-MOC.md` | Índice de documentación |
| `DesignAgent/Salvo-Getting-Started.md` | Preparación y flujo operativo |
| `DesignAgent/Salvo-Portability.md` | Portabilidad de proveedores/agentes |
| `DesignAgent/Salvo-Progress.md` | Estado operativo, checklists y evidencias por etapa |
| `DesignAgent/Salvo-Project-Instructions.md` | Instrucciones opcionales para sala de diseño |
| `README.md` | Cara pública del repositorio; crecerá en la Etapa 8 |

## 15. Referencias oficiales de Koin

- [Security Scheme](https://api-docs.koin.com.br/docs/security-scheme)
- [Antifraud Services Flow](https://api-docs.koin.com.br/docs/antifraud-services-flow-1)
- [Integration Requirements](https://api-docs.koin.com.br/docs/integration-requirements)
- [Create Evaluation](https://api-docs.koin.com.br/reference/createevaluationusingpost)
- [JavaScript Integration / fingerprint](https://api-docs.koin.com.br/reference/javascript-integration)

Estas páginas pueden cambiar. Antes de implementar el sandbox se revisará el OpenAPI vigente y se
registrará la versión/fecha usada por el adaptador.

Fin del Blueprint.
