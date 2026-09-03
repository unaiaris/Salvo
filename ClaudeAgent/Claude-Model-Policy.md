# Salvo — Política de modelo y esfuerzo

> Estado del documento: vigente
> Última actualización: 2026-09-02
> Aplicación: toda sesión de Claude Code o Claude.ai que trabaje sobre este repositorio

Elegir modelo y esfuerzo es una decisión de coordinación, no una preferencia de la sesión. Este
documento fija el criterio; la elección concreta se registra en el task brief de cada tarea.

## Protocolo

1. Antes de iniciar una tarea o etapa, el coordinador recomienda modelo y esfuerzo con su
   justificación.
2. El usuario decide. La decisión queda registrada en el task brief.
3. Si durante la tarea el trabajo resulta materialmente distinto al previsto —aparece una decisión
   de arquitectura donde se esperaba ejecución— el agente se detiene y lo reporta en vez de
   continuar con un modelo inadecuado.

## Una tarea puede cambiar de tipo a mitad de sesión

El modelo se elige por **el tipo de trabajo, no por la sesión**. Una sesión abierta con Fable 5.1 ·
`xhigh` para revisar un diseño sigue en Fable · `xhigh` cuando después se le pide transcribir un
informe a Markdown, que es trabajo mecánico. Nadie lo nota, porque el resultado es correcto; lo que
se pierde es dinero y tiempo.

Caso real, 2026-09-02: tras la revisión adversarial de la Etapa 4 con Fable · `xhigh` se pidió, en
la misma sesión, guardar la salida como archivo. Copiar y pegar texto es `Haiku 4.5 · low` según la
última fila de la matriz. El coordinador no lo advirtió y el usuario lo señaló.

Reglas que salen de ahí:

1. **El cambio de tipo se detecta en el pedido, no al terminar.** Antes de despachar una petición
   dentro de una sesión ya abierta, comparar el tipo de trabajo con la fila de la matriz que aplica.
2. **Bajar de modelo también es una decisión de coordinación.** Cuando el coordinador nota el
   cambio, entrega el comando concreto —`/model haiku` y `/effort low`— en vez de mencionar que
   convendría bajar.
3. **La transición vale la pena a partir de trabajo no trivial.** Para un pedido de una línea, el
   costo de cambiar y volver supera el ahorro; para transcribir un informe, formatear un handoff o
   leer un log largo, no.
4. **Al volver al trabajo de fondo, restituir el modelo.** Quedarse en `haiku · low` para la tarea
   siguiente de diseño es el error simétrico y más caro.

## Principio

**El rigor del brief es lo que habilita modelos más baratos.** Un brief que fija resultado, paths,
criterios de aceptación y tabla de verificación convierte la implementación en ejecución fiel. El
trabajo difícil —decidir— ya ocurrió.

Corolario: gastar el modelo caro donde se decide, no donde se tipea. Un modelo de razonamiento
fuerte diseñando una etapa vale más que el mismo modelo implementando un brief que ya dice qué
hacer.

## Modelos disponibles

| Modelo | Costo (in/out por MTok) | Contexto | Perfil |
| --- | --- | --- | --- |
| Fable 5.1 | $10 / $50 | 1M | Razonamiento exigente y trabajo agéntico de horizonte largo |
| Opus 5 | $5 / $25 | 1M | Coding agéntico complejo; punto de partida recomendado por defecto |
| Sonnet 5 | $2 / $10 | 1M | Mejor equilibrio velocidad/inteligencia |
| Haiku 4.5 | $1 / $5 | 200K | Lo más rápido y económico |

Niveles de esfuerzo: `low`, `medium`, `high`, `xhigh`, `max`. El default es `high`. La documentación
advierte que `max` puede presentar rendimientos decrecientes.

Los precios son de lista de API. Bajo suscripción se traducen en velocidad de consumo de cuota; el
criterio de asignación no cambia.

## Matriz por tipo de trabajo

| Tipo de trabajo | Modelo | Esfuerzo | Razón |
| --- | --- | --- | --- |
| Diseño de etapa: invariantes, concurrencia, idempotencia, consistencia transaccional | Fable 5.1 | `high` | Un invariante mal razonado se paga en todas las etapas siguientes |
| Revisión adversarial de un handoff o búsqueda de fuga temporal | Fable 5.1 | `xhigh` | Buscar el caso que rompe la invariante es razonamiento exigente por definición |
| Implementación con brief cerrado | Sonnet 5 | `high` | El brief no deja decisiones abiertas; es ejecución fiel |
| Implementación con ambigüedad real dentro del alcance | Opus 5 | `high` | Hay criterio que aplicar sin salir del brief |
| Etapa completa de punta a punta en una sesión | `opusplan` | `high` | Planifica con Opus y ejecuta con Sonnet automáticamente |
| Documentación, tooling y correcciones acotadas | Sonnet 5 | `medium` | Alcance cerrado y verificable |
| Mecánico: correr la compuerta, leer logs, formatear un handoff | Haiku 4.5 | `low` | Sin decisiones que tomar |

## Cómo se aplica

- En sesión: `/model sonnet` y `/effort medium`.
- Al arrancar: `claude --model sonnet --effort medium`.
- En el selector de `/model`, las flechas izquierda y derecha ajustan el esfuerzo.

## Mantenimiento

El catálogo de modelos cambia. Antes de fijar la elección de una etapa nueva, verificar el lineup
vigente en la documentación oficial en vez de asumir esta tabla. Si cambia, actualizar este archivo
y registrar el cambio en el registro de actividad del Progress.

Referencias: documentación de modelos de Claude y configuración de modelo de Claude Code.
