# Salvo — Mapa de contenido

> Estado del documento: vigente
> Última actualización: 2026-09-30
> Fuente de verdad: [[Salvo-Blueprint]]

Índice principal de la documentación de diseño y ejecución.

## Por dónde empezar

Cuatro recorridos, según qué quieras entender. Ninguno pide leerlos todos.

- **El producto, en veinte minutos.** La [instancia pública](https://salvo-k6wk.onrender.com) con el
  `../docs/guion-demo.md` al lado, y después `../README.md`. El guion dice qué mirar y en qué orden;
  el README explica por qué está hecho así.
- **Las decisiones, y por qué.** La bitácora del Blueprint (sección 13) es la lista completa. Para
  ver cómo se tomaron, `../Coordination/Tasks/E<N>-DISENO.md` junto a su
  `E<N>-revision-adversarial.md`: el diseño y el modelo que lo atacó, uno al lado del otro.
- **Cómo se construyó.** `../Coordination/Workboard.md`, sección de lecciones — es lo más denso del
  repositorio por línea. Después, la entrada de una tarea cualquiera en
  `../Coordination/Handoffs/Claude.md`, con sus mediciones y sus falsaciones.
- **Cómo se verifica lo que se afirma.** `../scripts/check.sh` y `../scripts/check-docs.sh`, con la
  sección «Límites declarados» del README, que es la lista de lo que el proyecto decidió no hacer.

## Documentos

- [[Salvo-Blueprint|Blueprint]] — alcance, arquitectura, modelo, seguridad, etapas y decisiones.
- [[Salvo-Overview|Overview]] — resumen ejecutivo.
- [[Salvo-Progress|Progress]] — estado vivo, checklists, compuertas y evidencias.
- [[Salvo-Getting-Started|Getting Started]] — requisitos y flujo operativo.
- [[Salvo-Portability|Portabilidad]] — separación entre agente, IA y proveedor antifraude.
- [[Salvo-Project-Instructions|Project Instructions]] — instrucciones opcionales para una sala de diseño.
- `../docs/guion-demo.md` — el recorrido de demostración, paso a paso, con qué mirar en cada pantalla.
- `../docs/capturas/` — las seis capturas del README, regeneradas desde una base nueva.
- `../AGENTS.md` — reglas permanentes de implementación. Rigen a los dos agentes, porque
  `../CLAUDE.md` las importa.
- `../CLAUDE.md` — memoria raíz de Claude Code; importa las reglas y el estado compartidos.
- `../ClaudeAgent/README.md` — kit operativo y plantillas de Claude.
- `../ClaudeAgent/Claude-Workflow.md` — workflow permanente: preflight, modos, verificación y
  handoff.
- `../ClaudeAgent/Claude-Handoff-Template.md` — forma de una entrega.
- `../ClaudeAgent/Claude-Model-Policy.md` — criterio para elegir modelo y esfuerzo por tipo de
  tarea.
- `../Coordination/README.md` — protocolo Codex–Claude, Workboard y handoffs.
- `../Coordination/Workboard.md` — tareas, estados, propietarios y paths reservados.
- `../Coordination/Tasks/` — el diseño de cada etapa, su revisión adversarial y el brief de cada
  tarea.
- `../Coordination/Handoffs/` — la entrega de cada tarea, con sus comandos y las tablas de
  falsación.
- `../Coordination/Task-Brief-Template.md` — resultado, límites y verificación de cada tarea.
- `../README.md` — entrada pública al repositorio: el argumento, los diagramas y cómo se verifica.

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
7. Explicabilidad determinista. Cerró con plantilla propia; Anthropic quedó como decisión aparte.
8. El argumento del proyecto: README, diagramas, capturas y guion de demo.
9. Corpus, idiomas y cierre: fixture enriquecida, señales estructuradas, portugués y accesibilidad.
10. La instancia pública: un contenedor con los dos procesos, la base sembrada horneada adentro, el
    reinicio por antigüedad, y el despliegue en un plan gratuito.

**Las diez etapas están integradas y verificadas**, y el MVP está publicado en
https://salvo-k6wk.onrender.com. El estado vigente vive en [[Salvo-Progress]]; esta lista solo dice
qué es cada etapa.

### Post-MVP

- Koin sandbox y fingerprint oficial.
- Auth y multi-tenant.
- Observabilidad.
- PostgreSQL y despliegue.

## Regla de mantenimiento

Una decisión de producto o arquitectura se anota primero en la bitácora del Blueprint. Una regla
permanente de implementación se refleja además en `AGENTS.md`. El avance y sus evidencias se
registran en [[Salvo-Progress]]. Los demás documentos son derivados.
