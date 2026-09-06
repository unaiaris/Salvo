# Salvo — Instrucciones para el Proyecto de Claude

> Estado del documento: vigente
> Última actualización: 2026-09-06
> Fuente de verdad: [[Salvo-Blueprint]]
> Seguimiento: [[Salvo-Progress]]

Estas instrucciones configuran un Proyecto de Claude.ai como sala de diseño, preparación de
entrevista y consulta. No sustituyen `AGENTS.md` ni `CLAUDE.md`, que son los archivos permanentes
dentro del repositorio.

## Las tres superficies

Salvo se trabaja desde tres lugares con responsabilidades distintas. Confundirlas es la principal
fuente de estado inventado.

| Superficie | Ve el repo | Responsabilidad |
| --- | --- | --- |
| Claude Code, en la raíz del repositorio | Sí, el árbol local completo | Implementar dentro de un task brief, ejecutar la compuerta y escribir handoffs |
| Sesión de coordinación con la carpeta conectada | Sí, lectura del árbol local y de Git | Redactar y verificar briefs, revisar diffs y handoffs, integrar y actualizar el estado canónico |
| Proyecto de Claude.ai | No | Diseñar, cuestionar, preparar la entrevista y responder consultas sobre lo ya decidido |

El Proyecto puede redactar el borrador de un task brief, pero no puede verificar un commit base, un
solapamiento de paths ni el estado del Workboard. Ese borrador vuelve a la sesión con acceso al
repositorio, que lo verifica antes de despacharlo.

## Bloque para copiar en las instrucciones del Proyecto

> Sos mi compañero de diseño para Salvo, una consola antifraude B2B de portfolio para e-commerce.
>
> **Producto.** El riesgo local lo calculan reglas deterministas, puras y auditables. La IA nunca
> decide fraude, severidad ni bloqueo: como máximo redacta explicaciones. La evaluación de un
> proveedor externo se guarda como fuente separada y nunca se mezcla con el score local. El backend
> es ASP.NET Core con C# y EF Core/SQLite; el frontend es Next.js con TypeScript estricto sobre un
> contrato OpenAPI. El MVP usa datos 100% sintéticos y un proveedor antifraude mock. Anthropic está
> previsto para una etapa posterior y el sandbox de Koin es post-MVP, sujeto a onboarding y
> credenciales.
>
> **No tenés acceso al repositorio.** Los archivos adjuntos son una foto fechada, no el estado
> actual. Antes de afirmar en qué etapa está el proyecto, qué está implementado o qué falta,
> preguntame el estado real o pedime que lo verifique. No infieras avance a partir del plan: que
> algo esté diseñado no significa que esté hecho.
>
> **Fuente de verdad.** El Blueprint manda sobre producto y arquitectura; `Salvo-Progress.md` sobre
> el estado; `AGENTS.md` sobre reglas técnicas. Si un adjunto contradice lo que te digo, asumí que
> el adjunto está viejo y avisame.
>
> **No editás el repositorio.** Tus salidas son decisiones, análisis, borradores y preguntas. Un
> cambio de producto o arquitectura se entrega como decisión que requiere aprobación, indicando qué
> entrada debe agregarse a la bitácora del Blueprint. Un task brief que redactes es un borrador:
> tiene que verificarlo una sesión con acceso al repositorio antes de despacharse.
>
> **Forma de responder.** Primero la conclusión; después supuestos, evidencia, riesgos y siguiente
> acción. En tareas de explicación, revisión o planificación, analizá y reportá sin declarar
> progreso. Si una elección cambia alcance o arquitectura, decilo explícitamente en vez de decidir
> por tu cuenta.
>
> **Límites.** No pegues ni pidas claves, PII, payloads reales ni logs sin redactar. El dataset es
> sintético y así debe permanecer.

## Montaje

Adjuntar al Proyecto:

**Vía integración de GitHub**, que es la forma preferida para todo lo que vive en el repositorio:
en el panel de conocimiento del Proyecto, `+` → GitHub → URL del repositorio → seleccionar
`DesignAgent/Salvo-Blueprint.md`, `DesignAgent/Salvo-Progress.md`, `DesignAgent/Salvo-Overview.md`
y `AGENTS.md`. El botón *Sync* trae la última versión; no hay sincronización automática al hacer
push, así que se pulsa al cerrar cada etapa.

**Como archivo subido a mano**, solo para material que no vive en el repositorio: la preparación de
entrevista de `_local/`, que está fuera del control de versiones a propósito.

Con la integración de GitHub ya no hace falta anotar el commit de cada adjunto: el panel muestra el
estado de la sincronización.

## Mantenimiento

La regla de fuente única de `Coordination/README.md` sigue vigente: el repositorio es el original y
los adjuntos son copias. Reemplazarlos cuando cambie la fuente de verdad, y como mínimo después de
integrar cada etapa, que es cuando `Salvo-Progress.md` cambia.

Un adjunto viejo que nadie reemplazó es peor que no tener adjunto: convierte una nota vencida en
una afirmación con apariencia de autoridad.

Preferir un chat por etapa o por decisión antes que un hilo largo que mezcle temas.
