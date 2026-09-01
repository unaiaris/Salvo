---
description: Valida un task brief contra Coordination/Task-Brief-Template.md antes de empezar a trabajar
allowed-tools: Bash(git status:*), Bash(git log:*), Bash(git show:*), Bash(git branch:*), Read
argument-hint: [ruta-del-brief]
---

Validar el task brief indicado en $ARGUMENTS (o pedirlo si falta) contra
`Coordination/Task-Brief-Template.md`, `AGENTS.md` y `DesignAgent/Salvo-Blueprint.md`, sin completar
huecos por cuenta propia ni generar el brief.

1. Leer `Coordination/Task-Brief-Template.md` para obtener la lista exacta de secciones
   obligatorias: Identificación, Resultado esperado, Contexto obligatorio, Alcance (Dentro/Fuera/
   Paths autorizados/Paths reservados), Acciones autorizadas, Criterios de aceptación, Verificación
   y evidencia, Decisiones delegadas, Detenerse y consultar si, Entrega requerida.
2. Leer el brief indicado y verificar, sección por sección, que cada una esté presente y completa
   (no vacía, no con placeholders sin rellenar).
3. Verificar que el commit base declarado exista en el repositorio:
   `git show --stat <commit-base>` o `git branch --contains <commit-base>`. Si no existe o el
   comando falla, reportarlo como falta.
4. Leer `Coordination/Workboard.md` y verificar que los paths autorizados del brief no se solapen
   con paths reservados de otro trabajo en estado `Asignada`, `En curso` o `Lista para integrar`.
5. Leer `AGENTS.md` y `DesignAgent/Salvo-Blueprint.md` y verificar que el alcance del brief no los
   contradiga (etapa autorizada, decisiones invariantes, reglas de dominio, seguridad, autonomía).
6. Reportar el resultado:
   - Si todo está completo, existe el commit base, no hay solapamiento y no hay contradicción:
     reportar el brief como válido, listando las secciones y verificaciones confirmadas.
   - Si falta algo: detenerse y listar exactamente qué sección, dato o verificación falta o falló,
     sin inventar el contenido faltante ni proponer una versión completada del brief.
7. No editar el brief, el Workboard, el Progress ni el Blueprint como parte de esta validación.
