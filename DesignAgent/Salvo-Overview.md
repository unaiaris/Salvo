# Salvo — Resumen ejecutivo

> Estado del documento: vigente
> Última actualización: 2026-08-31
> Fuente de verdad: [[Salvo-Blueprint]]
> Seguimiento: [[Salvo-Progress]] · Navegación: [[Salvo-MOC]]

## En una frase

Salvo es una consola antifraude B2B para e-commerce que puntúa pedidos sintéticos mediante reglas
deterministas, genera alertas explicables y demuestra un ciclo de integración externa con estados,
callbacks e idempotencia.

## Por qué existe

Es un proyecto de portfolio orientado a una oportunidad profesional vinculada con Koin. Busca
demostrar criterio de dominio antifraude y capacidad full-stack sin depender de datos, pagos o
credenciales reales.

## Decisión central

- El motor local de reglas calcula `localRiskScore`.
- Una evaluación externa de Koin es otra fuente y conserva su propio estado/score.
- La IA no decide fraude; Anthropic se prevé únicamente para redactar explicaciones después de que
  el núcleo funcione sin IA.

## Alcance del MVP

- Pedidos e-commerce sintéticos e importación CSV/JSON.
- Baseline cronológico sin fuga de información futura.
- Reglas deterministas con configuración auditable.
- Alertas idempotentes y revisión transaccional.
- Feed, detalle y dashboard de riesgo.
- Evaluación mediante precisión, recall, F1 y falsos positivos.
- `IAntifraudProvider` con mock local para `approved`, `denied` y `received`.
- Callback y reconciliación simulados, replay-safe.
- Tests sin red y README de portfolio.

## Fuera del MVP inicial

- Anthropic real.
- Sandbox, fingerprint o certificación Koin.
- Autenticación y publicación con rutas mutables.
- Consulta NL→SQL.
- Observabilidad, Postgres y despliegue.

## Arquitectura

Backend monolítico modular con ASP.NET Core 10, C# 14 y EF Core/SQLite; frontend Next.js con React
y TypeScript estricto. OpenAPI gobierna el contrato entre ambos. El dominio puro se separa de casos
de uso, frameworks y adaptadores externos. La aplicación funciona con proveedores mock y sin cuentas
externas.

## Estado

| Elemento | Estado |
| --- | --- |
| Diseño B2B y alcance | Aprobado |
| Blueprint e instrucciones Codex | Actualizados |
| Kit Claude y coordinación paralela | Preparados |
| Código de aplicación | No iniciado |
| Anthropic | Previsto para una etapa posterior |
| Koin sandbox | Opcional, sujeto a onboarding |

## Próximo paso

Etapa 1: fijar SDKs y dependencias, crear la solución ASP.NET Core y el frontend Next.js, configurar
EF Core/SQLite y verificar builds, tests, OpenAPI y arranque de ambas toolchains. El alcance,
checklist y evidencia requerida están en [[Salvo-Progress#Próximo bloque de trabajo — Etapa 1]].
