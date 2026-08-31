# E2-CONTRACT-DATA — Contrato y datos sintéticos

> Estado: Propuesta con schema aprobado; implementación pendiente de autorización explícita

## Identificación

- Work ID: E2-CONTRACT-DATA
- Etapa: 2
- Tipo: implementación
- Propietario: sin asignar
- Coordinador: Codex en sesión con el usuario
- Fecha: 2026-08-31
- Rama/worktree: no creado; propuesta `codex/e2-contract-data`
- Commit base: `66f0949eee6b3c220cf66ed00da1d0abd11498dc`
- Dependencias: E1 integrada y verificada; schema E2 aprobado; inicio aún no autorizado

## Resultado esperado

El backend persiste pedidos sintéticos normalizados mediante una migración reproducible, importa
CSV/JSON con errores parciales y carga una fixture demo idempotente de 300 pedidos y 300 etiquetas,
sin incorporar scoring, alertas, proveedores externos ni datos reales.

## Contexto obligatorio

- Blueprint: secciones 4.1, 6, 7, 8, 10, Etapa 2 y decisiones 15–20 de la bitácora.
- Progress: checklist `Etapa 2 — Contrato y datos`.
- Fundación existente: `SalvoDbContext`, integración ASP.NET Core y tests SQLite de E1.
- Documentación narrativa: `DesignAgent/Salvo-Interview-Prep.md`.

## Alcance

### Dentro

- Entidad `Order` con hechos inmutables y validación de dominio.
- Entidad separada `OrderEvaluationLabel`, accesible solo por el seed/evaluación.
- Configuración EF Core, primera migración real, checks, claves e índices.
- Contratos y caso de uso compartido de importación CSV/JSON.
- Endpoint `POST /api/order-imports` con formato explícito y errores parciales.
- Fixture `demo-orders.v1.json`, caso de uso `SeedDemoOrders` y endpoint local configurable
  `POST /api/demo-data/seed`.
- Tests de dominio, parsing, aplicación, persistencia, migración, API, idempotencia y privacidad.
- Tooling EF y biblioteca CSV mantenida con versiones exactas y locks actualizados.
- Ayuda memoria con speech, mapa por capas y referencias a tests/evidencias reales.

### Fuera

- Baseline, reglas, scoring, calibración o métricas de evaluación.
- Risk evaluations, alertas, revisión o estados antifraude.
- UI de importación, feed, detalle o dashboard.
- Anthropic, Koin, proveedor mock, callbacks o red externa.
- Autenticación, Postgres, despliegue, observabilidad o datos reales.
- Conversión FX, geocodificación o texto libre de descripción.

### Paths autorizados al iniciar

- `backend/src/Salvo.Domain/**`
- `backend/src/Salvo.Application/**`
- `backend/src/Salvo.Infrastructure/**`
- `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `Directory.Packages.props`
- `.config/dotnet-tools.json`
- `Salvo.slnx`
- `scripts/check.sh`
- `.gitignore`, `.env.example` y documentación operativa si el cambio es necesario y acotado
- `AGENTS.md`, `DesignAgent/**` y `Coordination/**` para sincronización de estado y narrativa

### Paths reservados por otros trabajos

- Ninguno mientras no exista otra tarea activa. Migraciones, paquetes, solución y configuración
  central se mantienen serializados.

## Schema aprobado

### `orders`

| Campo | Regla |
| --- | --- |
| `id` | GUID interno; PK técnica |
| `merchantId` | requerido, `MER_`, ASCII mayúsculo, máximo 64 |
| `merchantReferenceId` | requerido, `ORD_`, ASCII mayúsculo, máximo 64 |
| `buyerReferenceId` | requerido, `BUY_`, ASCII mayúsculo, máximo 64 |
| `occurredAt` | `DateTimeOffset`; offset explícito; UTC ISO 8601 canónico en SQLite |
| `amountCents` | `Int64`; entre 1 y 1.000.000.000.000 |
| `currencyCode` | `UYU`, `BRL` o `USD`; sin FX |
| `countryCode` | ISO 3166-1 alpha-2 desde allowlist congelada |
| `city` | nullable; NFC, espacios normalizados, sin controles, máximo 80 |
| `channel` | nullable; `WEB`, `MOBILE_APP` o `MARKETPLACE` |
| `deviceSessionId` | nullable, `DEV_`, ASCII mayúsculo, máximo 64 |
| `createdAt` | `DateTimeOffset` asignado mediante `TimeProvider`; UTC canónico en SQLite |

- PK: `id`.
- Unicidad natural: `(merchantId, merchantReferenceId)`.
- Orden total: `(occurredAt, merchantId, merchantReferenceId)`.
- Índice cronológico sobre ese orden.
- Índice de baseline futuro sobre
  `(merchantId, buyerReferenceId, currencyCode, occurredAt, merchantReferenceId)`.
- No existen `description`, `updatedAt`, label, score, estado, evaluación ni alerta en `Order`.

### `order_evaluation_labels`

- `orderId`: PK y FK requerida a `orders`; E2 no incorpora operaciones de borrado.
- `isFraudLabel`: booleano requerido.
- `createdAt`: UTC ISO 8601 canónico.
- No se expone en DTOs públicos ni se entrega al scoring.

## Contrato de importación

- Requeridos: `merchantId`, `merchantReferenceId`, `buyerReferenceId`, `occurredAt`, `amountCents`,
  `currencyCode`, `countryCode`.
- Opcionales: `city`, `channel`, `deviceSessionId`.
- Campos desconocidos rechazados; headers CSV ausentes, duplicados o desconocidos son estructurales.
- `POST /api/order-imports` recibe `multipart/form-data` con `file` y `format: CSV | JSON`.
- Máximo 5 MiB y 10.000 registros; CSV UTF-8 con BOM opcional, coma o punto y coma y RFC 4180;
  JSON es un array de objetos.
- Duplicado idéntico: skip y conteo. Misma clave con contenido distinto: `REFERENCE_CONFLICT`.
- Los válidos se escriben en una transacción; un fallo técnico los revierte en conjunto.
- Respuesta parcial con conteos, códigos estables y hasta 1.000 detalles sin valores recibidos.

## Seed aprobado

- Fixture versionada `demo-orders.v1.json` con 300 pedidos en una ventana fija de 120 días.
- 300 labels: 18 `true` y 282 `false`.
- Tres comercios sintéticos con UYU, BRL y USD, IDs prefijados y campos opcionales representados.
- IDs y timestamps reproducibles; ninguna dependencia de fecha actual, red o generadores de PII.
- Primera ejecución inserta el dataset; la segunda no cambia conteos, IDs, timestamps ni etiquetas.
- Un conflicto con una referencia existente se informa y nunca sobrescribe.

## Migración aprobada

- Eliminar `FoundationCheckpoint` y reemplazar `EnsureCreated` por migraciones.
- Crear `InitialOrderContractData` y aplicar migraciones en tests SQLite reales.
- Fijar `dotnet-ef` y `Microsoft.EntityFrameworkCore.Design` a la versión EF de la solución.
- No migrar automáticamente al arrancar la API.
- No borrar automáticamente una DB local preexistente de E1; documentar el reset explícito.

## Acciones autorizadas

- Ediciones locales permitidas: no hasta una autorización posterior de inicio; después, solo dentro
  de los paths indicados.
- Instalación o actualización de dependencias: al iniciar, solo versiones exactas necesarias para
  EF tooling y CSV, con locks y smoke tests.
- Escrituras externas: no; no publicar, desplegar ni hacer push sin autorización separada.
- Acciones destructivas: no; no borrar bases locales ni artefactos del usuario automáticamente.

## Criterios de aceptación

- [ ] El dominio rechaza invariantes inválidas y normaliza las aprobadas sin depender de EF/API.
- [ ] La DB repite claves, checks, longitudes y enums críticos.
- [ ] SQLite ordena y filtra fechas en DB con offsets normalizados y orden total determinista.
- [ ] Dos comercios pueden compartir `merchantReferenceId`; uno solo no puede duplicarlo.
- [ ] Un duplicado idéntico no crea efectos; un payload distinto con la misma clave entra en
      conflicto.
- [ ] CSV cubre BOM, delimitadores, quoting, escaped quotes, multiline y errores parciales.
- [ ] JSON cubre objetos válidos/inválidos, campos desconocidos y documento malformado.
- [ ] Los registros válidos de una importación parcial son atómicos ante fallos técnicos.
- [ ] El seed deja 300 pedidos, 300 labels y 18 fraudes; repetirlo conserva el mismo estado completo.
- [ ] El contrato público, fixture, errores y logs no contienen PII ni datos financieros reales.
- [ ] `isFraudLabel` no aparece en `Order` ni en los DTOs públicos.
- [ ] La migración crea el schema desde una DB vacía y no deja cambios de modelo pendientes.
- [ ] La ayuda memoria explica cada capa, decisión, trade-off y test sin afirmar trabajo no integrado.
- [ ] La compuerta full-stack permanece verde.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `dotnet restore Salvo.slnx --locked-mode` | Paquetes y herramientas fijados; locks válidos |
| `dotnet build Salvo.slnx --configuration Release --no-restore` | 0 warnings, 0 errores |
| `dotnet test Salvo.slnx --configuration Release --no-build --no-restore` | Suite E1+E2 verde y sin red |
| `dotnet ef migrations has-pending-model-changes` con projects explícitos | Ningún cambio pendiente |
| Inspección SQLite temporal | Tablas, checks, FKs e índices exactos |
| Seed ejecutado dos veces | Estado completo idéntico; 300/300/18 |
| Importación CSV/JSON mixta | Conteos y errores parciales exactos |
| Auditoría de fixture/schema/logs | Sin campos o patrones de PII/pagos reales |
| `./scripts/check.sh` | Compuerta backend y frontend verde |

## Decisiones delegadas al iniciar

- Nombres internos de clases y carpetas que respeten las capas aprobadas.
- Organización de tests y builders sin ampliar el schema ni los contratos.
- Detalles de implementación del converter temporal, siempre que pasen consultas SQLite en DB.
- Biblioteca CSV mantenida y versión exacta compatible, tras verificación y lock.

## Detenerse y consultar si

- SQLite no traduce correctamente orden o rangos sobre la representación temporal aprobada;
- una librería obliga a relajar el contrato, leer archivos completos sin límite o filtrar tipos fuera
  de infraestructura;
- cumplir idempotencia requiere actualizar pedidos existentes o cambiar la clave natural;
- se propone exponer labels, añadir reglas/scoring o ampliar monedas/campos;
- una base local requiere borrado o aparece trabajo ajeno en paths autorizados;
- cualquier test obligatorio no puede verificarse tras agotar alternativas seguras.

## Entrega requerida

- Resultado, archivos, comandos, decisiones, riesgos y pendientes.
- Mapa de código por capa y speech actualizado en la ayuda memoria.
- Handoff en `Coordination/Handoffs/<Agente>.md` si se usa rama/worktree separado.
- Estado de implementación: `Lista para integrar | Parcial | Bloqueada`.

Estado actual: `Propuesta`. Este brief documenta el diseño aprobado, pero no asigna ni inicia E2.
