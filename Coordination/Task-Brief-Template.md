# Salvo — Plantilla compartida de tarea

Copiar y completar esta plantilla al asignar trabajo a Codex o Claude. El brief define el resultado
y los límites de una tarea; no modifica el estado canónico por sí mismo.

## Identificación

- Work ID:
- Etapa:
- Tipo: `implementación | corrección | revisión | diagnóstico | plan`
- Propietario: `Codex | Claude`
- Coordinador:
- Fecha:
- Rama/worktree:
- Commit base:
- Modelo y esfuerzo acordados:
- Dependencias:

## Resultado esperado

Una frase verificable que describa qué debe quedar cierto al terminar.

## Contexto obligatorio

- Sección del Blueprint:
- Entrada/checklist del Progress:
- Tests, contratos o documentación relacionados:

## Alcance

### Dentro

-

### Fuera

-

### Paths autorizados

-

### Paths reservados por otros trabajos

-

## Acciones autorizadas

- Ediciones locales permitidas:
- Instalación o actualización de dependencias:
- Escrituras externas:
- Acciones destructivas:

Si un campo queda vacío, se aplica la política de autonomía de `AGENTS.md`; el silencio nunca
autoriza escrituras externas, acciones destructivas ni ampliaciones de alcance.

## Criterios de aceptación

- [ ]

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
|  |  |

## Decisiones delegadas

Decisiones menores que el propietario puede tomar sin consulta, siempre dentro del alcance.

## Detenerse y consultar si

- una elección cambia producto, arquitectura o alcance material;
- se necesita una credencial, escritura externa o acción destructiva no autorizada;
- aparece solapamiento con otra tarea o cambios ajenos en paths autorizados;
- tras agotar alternativas seguras no puede verificarse un criterio obligatorio.

## Entrega requerida

- Resumen del resultado.
- Archivos modificados o revisados.
- Comandos/comprobaciones y resultados.
- Supuestos, decisiones, riesgos y pendientes.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/<Agente>.md` cuando la tarea se ejecute en una rama/worktree
  separado o requiera integración.
