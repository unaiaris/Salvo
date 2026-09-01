---
description: Arma la entrada de handoff de la tarea actual y la agrega a Coordination/Handoffs/Claude.md
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git show:*), Bash(git branch:*), Read, Edit
argument-hint: [work-id]
---

Armar una entrada de handoff siguiendo exactamente la estructura de
`ClaudeAgent/Claude-Handoff-Template.md` y agregarla al final de `Coordination/Handoffs/Claude.md`,
sin sobrescribir entradas previas.

Work ID objetivo: $ARGUMENTS (si falta, inferirlo del nombre de la rama actual o preguntar).

1. Leer `ClaudeAgent/Claude-Handoff-Template.md` para confirmar la estructura exacta de secciones.
2. Leer el estado final de `Coordination/Handoffs/Claude.md` para saber dónde termina el archivo y
   no pisar entradas existentes.
3. Obtener rama, commit base y commit final únicamente de Git, nunca de memoria ni del task brief:
   - `git branch --show-current`
   - `git log --oneline` para listar los commits de la rama.
   - El commit base es el commit anterior al primer commit propio de esta rama (o el declarado en
     el task brief si coincide con lo que muestra `git log`); si hay discrepancia, detenerse y
     reportarla en vez de adivinar.
   - El commit final es `git rev-parse HEAD` en el momento de armar el handoff.
4. Revisar `git status --porcelain` y `git diff --stat` para listar con precisión los archivos
   modificados; no describir cambios que no aparezcan en Git.
5. Completar cada sección de la plantilla con datos verificados de esta sesión: identificación,
   resultado, archivos modificados, verificación (comandos y resultados reales que se ejecutaron,
   no supuestos), decisiones y supuestos, riesgos o pendientes, e integración.
6. No incluir secretos, PII, tokens, payloads reales ni logs sin redactar.
7. Agregar la entrada completa al final de `Coordination/Handoffs/Claude.md` mediante una edición
   que solo añade contenido; no reescribir ni reordenar entradas anteriores.
8. Reportar el estado final declarado (`Lista para integrar | Parcial | Bloqueada`) y confirmar que
   la entrada quedó agregada.
