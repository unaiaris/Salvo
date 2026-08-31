# E3-MOTOR-DETERMINISTA — Baseline, reglas y evaluación temporal

> Estado: Lista para integrar

## Identificación

- Work ID: E3-MOTOR-DETERMINISTA
- Etapa: 3
- Tipo: implementación
- Propietario: Codex
- Coordinador: Codex en sesión con el usuario
- Fecha: 2026-08-31
- Rama/worktree: `codex/e3-motor-determinista`
- Commit base: `4053f8f755ae7095615182430f73a46bafa0385b`
- Dependencias: E2 integrada y verificada; diseño E3 aprobado por el usuario

## Resultado esperado

El backend calcula assessments locales reproducibles con historia estrictamente anterior, seis
reglas auditables y configuración inmutable; evalúa scores contra labels mediante una frontera
separada y produce métricas temporales reproducibles sin persistir evaluaciones ni crear alertas.

## Contexto obligatorio

- Blueprint: secciones 4.2, 4.5, 6, 7, 10, Etapa 3 y decisiones 21–27.
- Progress: checklist `Etapa 3 — Motor determinista`.
- Contrato E2: `Order` inmutable, orden total, labels separados e índices temporales.
- Narrativa: `DesignAgent/Salvo-Interview-Prep.md`.

## Alcance

### Dentro

- Baseline temporal por cohortes de `occurredAt`.
- `RuleConfig e3-v1` central, validado e inmutable.
- Reglas `amount_anomaly`, `velocity`, `cross_border_velocity`, `unusual_hour`,
  `new_buyer_high_value` y `foreign_country`.
- Score 0–100, señales legibles y flag transitorio con umbral inclusivo.
- Lectores separados de pedidos y labels.
- Matriz de confusión, precision, recall, F1, FPR y barrido de umbral.
- Split temporal de calibración y holdout.
- Tests unitarios e integración sobre la fixture E2.
- Ayuda memoria y evidencia de entrega.

### Fuera

- Persistencia de `RiskEvaluation`, migraciones o modificación del schema.
- Alertas, severidad, revisión o estados antifraude.
- Endpoints, OpenAPI o UI.
- Anthropic, Koin, proveedores, callbacks o red externa.
- Etapa 4 o posteriores.

### Paths autorizados

- `backend/src/Salvo.Domain/**`
- `backend/src/Salvo.Application/**`
- `backend/src/Salvo.Infrastructure/**`, únicamente lectores y composición necesarios
- `backend/tests/**`
- `DesignAgent/**` y `Coordination/**`

### Paths reservados por otros trabajos

- Ninguno. No hay trabajo paralelo activo.

## Acciones autorizadas

- Ediciones locales permitidas: sí, dentro de los paths indicados.
- Instalación o actualización de dependencias: no prevista; consultar antes de añadir una.
- Escrituras externas: no.
- Acciones destructivas: no.

## Configuración aprobada

- Cohortes simultáneas aisladas; ventanas `[inicio, presente)`.
- Amount: 90 días, mínimo 3, mediana comprador/moneda con fallback comercio/moneda, factor `3/1`.
- Velocity: 10 minutos, dispara el cuarto pedido.
- Cross-border: dos horas para el mismo comercio/comprador.
- Unusual hour: 30 días, mínimo 20, buckets de seis horas, `<=10%`, `America/Montevideo`.
- New buyer high value: comprador sin historia, mediana comercio/moneda y factor `5/2`.
- Foreign country: 90 días, mínimo 3 y país dominante `>=60%`.
- Pesos: 40, 30, 40, 10, 30 y 20; cap 100; flag `score >= 60`.
- Calibración: primeros dos tercios de cohortes; holdout final; maximizar F1, minimizar FPR y elegir
  el umbral más alto.

## Criterios de aceptación

- [x] Ningún pedido observa timestamps iguales o posteriores.
- [x] Permutar la entrada no cambia assessments ni señales.
- [x] El scorer no acepta ni consulta `OrderEvaluationLabel`.
- [x] Cada regla tiene tests positivos, negativos, límites y cold start.
- [x] Score, cap, señal y umbral son deterministas y auditables.
- [x] Repetir scoring produce exactamente la misma salida y ninguna escritura.
- [x] Métricas, split y threshold sweep son reproducibles y sin fuga.
- [x] La fixture E2 produce evidencia documentada sin presentar generalización.
- [x] No existen migraciones, tablas, endpoints, alertas ni dependencias nuevas.
- [x] La compuerta full-stack permanece verde.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `dotnet build Salvo.slnx --configuration Release --no-restore` | 0 warnings y 0 errores |
| `dotnet test Salvo.slnx --configuration Release --no-build --no-restore` | Suite E1–E3 verde |
| `dotnet ef migrations has-pending-model-changes` | Ningún cambio pendiente |
| Tests temporales y por regla | Positivos, negativos, cold start, cohortes y límites verdes |
| Fixture demo | Scores y métricas reproducibles; labels fuera del scorer |
| `./scripts/check.sh` | Compuerta backend y frontend verde |

## Decisiones delegadas

- Nombres internos y organización de clases dentro de las capas aprobadas.
- Builders de tests y estructuras efímeras para mantener el motor puro.
- Formato exacto del detalle legible si conserva evidencia, estabilidad y minimización de datos.

## Detenerse y consultar si

- se necesita persistir scores, cambiar schema o crear una migración;
- una regla requiere labels, futuro o semántica distinta de la aprobada;
- se propone incorporar API, alertas, UI, proveedores o E4;
- aparece trabajo ajeno en paths reservados;
- una compuerta obligatoria no puede verificarse tras agotar alternativas seguras.

## Entrega requerida

- Resultado, archivos, comandos, decisiones, métricas provisionales, riesgos y pendientes.
- Mapa por capas y speech actualizado en la ayuda memoria.
- Handoff en `Coordination/Handoffs/Codex.md`.
- Estado: `Lista para integrar | Parcial | Bloqueada`.

Estado de entrega: `Lista para integrar`. Commit de implementación `c3a765c`; compuerta completa
verde en la rama con 42 tests .NET, modelo EF sin cambios, 3 tests frontend y build de producción.
