# Salvo — Resumen ejecutivo

> Estado del documento: vigente
> Última actualización: 2026-09-06
> Fuente de verdad: [[Salvo-Blueprint]]
> Seguimiento: [[Salvo-Progress]] · Navegación: [[Salvo-MOC]]

## En una frase

Salvo es la consola antifraude de un comercio electrónico —no un proveedor antifraude— que puntúa
pedidos sintéticos mediante reglas deterministas, genera alertas cuya explicación se verifica antes
de guardarse, y demuestra un ciclo de integración externa con estados, callbacks e idempotencia.

## Por qué existe

Es un proyecto de portfolio orientado a una oportunidad profesional vinculada con Koin. Busca
demostrar criterio de dominio antifraude y capacidad full-stack sin depender de datos, pagos o
credenciales reales.

## Decisión central

- El motor local de reglas calcula `localRiskScore`.
- La evaluación externa es otra fuente, con su propia tabla, su propio ciclo de vida y su propio
  estado. Hoy la produce un mock determinista; un adaptador de Koin sería una sustitución, no un
  cambio de modelo.
- La IA no decide fraude, severidad ni bloqueo: solo puede redactar, y lo que redacta se verifica
  sobre la salida contra los hechos de la evaluación antes de persistirse. El texto que no pasa esa
  comprobación no se guarda, no se registra y no se muestra.
- Qué está vigente lo define la corrida de scoring, nunca «la evaluación más reciente».

## Alcance del MVP

- Pedidos e-commerce sintéticos e importación CSV/JSON.
- Baseline cronológico sin fuga de información futura.
- Reglas deterministas con configuración auditable.
- Alertas idempotentes y revisión transaccional.
- Feed, detalle y dashboard de riesgo.
- Evaluación mediante precisión, recall, F1 y falsos positivos, en una superficie separada del
  dashboard operativo.
- `IAntifraudProvider` con mock local, con cuatro desenlaces: aprobado, denegado, pendiente a la
  espera de callback, y error.
- Callback autenticado y reconciliación explícita, replay-safe.
- `IExplanationProvider` con plantilla determinista y verificación de grounding sobre la salida.
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
| Etapas 1 a 7 | Integradas y verificadas en `main` |
| Etapa 8 — El argumento del proyecto | En ejecución: diseño v2 aprobado y tareas despachadas |
| Etapa 9 — Corpus, idiomas y cierre | En ejecución |
| Anthropic | Decisión aparte. Hoy `AI_PROVIDER=anthropic` se niega a arrancar |
| Koin sandbox | Opcional, sujeto a onboarding |

## Próximo paso

Ejecutar `E9C-PORTUGUES-ACCESIBILIDAD`: el idioma pasa a ser del despliegue y entra en la identidad
de la explicación, la consola entera se traduce, y un recorrido con lector de pantalla produce una
lista de hallazgos con severidad antes de corregir nada. Después queda `E9D`, el repaso final.
Ninguna integración autoriza automáticamente la tarea siguiente.
