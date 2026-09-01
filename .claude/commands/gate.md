---
description: Ejecuta la compuerta full-stack (./scripts/check.sh) y reporta el resultado real
allowed-tools: Bash(./scripts/check.sh:*), Bash(git rev-parse:*), Bash(git branch:*)
---

Ejecutar la compuerta full-stack del repositorio Salvo desde la raíz, sin reimplementar ni ejecutar
sueltos los pasos que ya orquesta `scripts/check.sh`.

1. Confirmar rama y commit actuales:
   - `git branch --show-current`
   - `git rev-parse HEAD`
2. Ejecutar `./scripts/check.sh` una sola vez, capturando toda la salida.
3. Reportar el resultado real, no uno asumido:
   - Si el script termina con código de salida `0`: reportar **compuerta verde** y resumir en una
     línea el resultado de cada paso que el script imprime (restore, build, migraciones
     pendientes, tests .NET, `npm run check`, build de producción de Next.js).
   - Si el script falla en cualquier paso: reportar **compuerta roja**, indicar en qué paso falló
     (según la última línea de comando ejecutada antes del error, visible en la salida de
     `set -x`/el propio script) y pegar el mensaje de error relevante sin truncar información
     necesaria para diagnosticar.
4. No marcar la tarea o la rama como "lista para integrar" si la compuerta no terminó en verde.
5. No modificar código para "arreglar" un fallo como parte de este comando: `/gate` solo ejecuta y
   reporta. Si el usuario pide corregir el fallo, es un paso separado y explícito.
