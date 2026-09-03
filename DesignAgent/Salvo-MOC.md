# Salvo — Mapa de contenido

> Estado del documento: vigente
> Última actualización: 2026-08-31
> Fuente de verdad: [[Salvo-Blueprint]]

Índice principal de la documentación de diseño y ejecución.

## Documentos

- [[Salvo-Blueprint|Blueprint]] — alcance, arquitectura, modelo, seguridad, etapas y decisiones.
- [[Salvo-Overview|Overview]] — resumen ejecutivo.
- [[Salvo-Progress|Progress]] — estado vivo, checklists, compuertas y evidencias.
- [[Salvo-Getting-Started|Getting Started]] — requisitos y flujo operativo.
- [[Salvo-Portability|Portabilidad]] — separación entre agente, IA y proveedor antifraude.
- [[Salvo-Project-Instructions|Project Instructions]] — instrucciones opcionales para una sala de diseño.
- `../AGENTS.md` — instrucciones permanentes para Codex.
- `../CLAUDE.md` — memoria raíz para Claude Code.
- `../ClaudeAgent/README.md` — kit operativo y plantillas de Claude.
- `../ClaudeAgent/Claude-Model-Policy.md` — criterio para elegir modelo y esfuerzo por tipo de
  tarea.
- `../Coordination/README.md` — protocolo Codex–Claude, Workboard y handoffs.
- `../Coordination/Task-Brief-Template.md` — resultado, límites y verificación de cada tarea.
- `../README.md` — entrada pública al repositorio.

## Secciones del Blueprint

- [[Salvo-Blueprint#1. Objetivo del MVP|Objetivo]]
- [[Salvo-Blueprint#2. Alcance aprobado|Alcance]]
- [[Salvo-Blueprint#3. Usuario y flujo principal|Usuario y flujo]]
- [[Salvo-Blueprint#4. Requisitos funcionales|Requisitos funcionales]]
- [[Salvo-Blueprint#5. Alineación con Koin|Alineación con Koin]]
- [[Salvo-Blueprint#6. Arquitectura|Arquitectura]]
- [[Salvo-Blueprint#7. Modelo de datos conceptual|Modelo de datos]]
- [[Salvo-Blueprint#8. Stack y política de versiones|Stack y versiones]]
- [[Salvo-Blueprint#9. Variables de entorno y servicios|Variables y servicios]]
- [[Salvo-Blueprint#10. Seguridad y privacidad|Seguridad]]
- [[Salvo-Blueprint#11. Etapas pequeñas y verificables|Etapas]]
- [[Salvo-Blueprint#12. Criterios globales de aceptación|Aceptación]]
- [[Salvo-Blueprint#13. Bitácora de decisiones|Bitácora]]
- [[Salvo-Blueprint#14. Mapa de documentación|Responsabilidades documentales]]
- [[Salvo-Blueprint#15. Referencias oficiales de Koin|Referencias de Koin]]

## Roadmap

### MVP

1. Fundaciones reproducibles.
2. Contrato y datos sintéticos.
3. Motor determinista.
4. Alertas y casos de uso.
5. UI y dashboard.
6. Proveedor antifraude mock.
7. Explicabilidad determinista y, si se aprueba, Anthropic.
8. Calidad y portfolio.

### Post-MVP

- Koin sandbox y fingerprint oficial.
- Auth y multi-tenant.
- Observabilidad.
- PostgreSQL y despliegue.

## Regla de mantenimiento

Una decisión de producto o arquitectura se anota primero en la bitácora del Blueprint. Una regla
permanente de implementación se refleja además en `AGENTS.md`. El avance y sus evidencias se
registran en [[Salvo-Progress]]. Los demás documentos son derivados.
