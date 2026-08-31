# Salvo — Workflow permanente para Claude

> Aplicación: toda tarea ejecutada por Claude sobre este repositorio
> Fuentes canónicas: `AGENTS.md`, Blueprint, Progress y Workboard

## Rol

Claude actúa como agente de implementación o revisión dentro de un trabajo previamente asignado. No
es el coordinador por defecto ni toma ownership implícito de una etapa completa. La política de
autonomía y aprobaciones de `AGENTS.md` se aplica a todos los tipos de tarea.

## Preflight

Antes de editar:

1. leer el task brief compartido y extraer ID, resultado, tipo, alcance y criterios de aceptación;
2. comprobar el Workboard y el estado integrado del Progress;
3. registrar rama/worktree y commit base;
4. inspeccionar cambios existentes;
5. leer el Blueprint y los tests relevantes;
6. enumerar los archivos previstos;
7. detenerse si el alcance se solapa con otra tarea activa.

## Modos de trabajo

### Modo secuencial

Solo hay un agente activo. Claude actualiza `Salvo-Progress.md` o el Workboard únicamente cuando el
task brief le asigna explícitamente la coordinación; en caso contrario entrega evidencia mediante
handoff.

### Modo paralelo

Codex y Claude trabajan en ramas o worktrees distintos.

- El coordinador asigna IDs y ownership en el Workboard.
- Claude modifica únicamente los paths de su task brief.
- Claude no actualiza el estado canónico del Progress ni el Workboard en su rama.
- Claude registra el resultado en `Coordination/Handoffs/Claude.md`.
- El coordinador integra, ejecuta la compuerta conjunta y recién entonces actualiza el estado.

## Implementación

- Mantener una tarea por concern.
- Respetar arquitectura, seguridad y reglas de dominio de `AGENTS.md`.
- No añadir features “útiles” fuera de alcance.
- Preservar cambios ajenos y detenerse ante solapamientos.
- Añadir o actualizar tests en proporción al cambio.
- No depender de Anthropic o Koin en tests.
- No incluir secretos, PII o payloads externos reales.

## Git y archivos

- Rama sugerida: `claude/<work-id>-<slug>`.
- Registrar siempre el commit base; un handoff sin base no es integrable de forma segura.
- No reescribir historia, hacer force push, borrar ramas o resolver conflictos de otro agente sin
  autorización.
- No editar archivos reservados a otra tarea activa.
- Si aparecen cambios inesperados, detenerse y documentar el conflicto.

## Verificación

Ejecutar primero la prueba más específica y después la compuerta de la etapa. Registrar comandos y
resultados exactos. No declarar “completado” si una comprobación obligatoria no se ejecutó.

Estados permitidos para la entrega:

- `Lista para integrar`: implementación y verificaciones de la tarea pasan.
- `Parcial`: existe progreso útil, pero faltan criterios.
- `Bloqueada`: requiere decisión, credencial o cambio externo.

Estos estados describen la rama de Claude; no sustituyen el estado integrado del Workboard.

## Handoff

El handoff debe incluir:

- ID y objetivo;
- base y commit final;
- resumen de cambios;
- archivos modificados;
- comandos y resultados;
- decisiones o supuestos;
- riesgos y trabajo pendiente;
- orden o instrucciones de integración;
- posibles conflictos con otras tareas.

Usar `Claude-Handoff-Template.md` y agregar la entrada final a
`Coordination/Handoffs/Claude.md`. El task brief inicial se basa en
`Coordination/Task-Brief-Template.md`.
