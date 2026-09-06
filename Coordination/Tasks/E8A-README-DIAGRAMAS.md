# Salvo — Task brief `E8A-README-DIAGRAMAS`

## Identificación

- Work ID: `E8A-README-DIAGRAMAS`
- Etapa: 8
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-06
- Rama/worktree: `claude/e8a-readme-diagramas`
- Commit base: el `HEAD` de `main` que incorpora el diseño v2 y este brief
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. Es escritura de argumento, que es donde el
  modelo pesa más, y además lleva cuatro diagramas que tienen que ser correctos contra el código y
  un script que comprueba lo que el documento afirma. Es la tarea de la etapa donde un error se
  publica.
- Dependencias: ninguna. `E8B-DEMO-CAPTURAS` depende de esta integrada.

## Resultado esperado

El README es el argumento del proyecto y **cada afirmación de hecho suya se sostiene contra el
código**: las rutas existen, los tests citados existen, y un script lo comprueba en cada corrida.
Los documentos derivados dejan de contradecir al estado real. Ninguna línea de código de producción
cambia.

## Contexto obligatorio

- `Coordination/Tasks/E8-DISENO.md` (**v2**), decisiones **D1 a D9**.
- `Coordination/Tasks/E8-revision-adversarial.md`, **completa**. Su segunda mitad es el inventario de
  lo que era falso en cada documento, con línea y fuente: es la lista de trabajo de esta tarea para
  los documentos derivados. El coordinador ya corrigió Blueprint, Progress, Workboard, `AGENTS.md` y
  las cabeceras de los derivados; lo que queda de esa lista es contenido.
- `DesignAgent/Salvo-Blueprint.md`: §1, §2, §3 —ya corregido—, §4, §6, §7, §11 y la bitácora.
- `README.md` actual, para saber qué se reemplaza.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.** No es una frase de
estilo: los tres últimos fallos del proyecto fueron afirmaciones que nadie contrastó.

## Alcance

### Dentro

**1. El README reescrito**, con las nueve secciones de D1 y en ese orden. Las que no existen hoy y
son el corazón de la tarea: «Simulación, sandbox y producción», «Cinco decisiones con su porqué»
—las 29/31, 33, 37/38, 45/47 y 52/54— y «Cómo se verifica», anclada a tests con nombre.

El pitch dice que Salvo no es un **proveedor** antifraude, no que no sea un motor: Salvo sí tiene un
motor de reglas deterministas, y decir lo contrario contradice el §1 del Blueprint.

**2. Las cifras del corpus entre `<!-- corpus:inicio -->` y `<!-- corpus:fin -->`**, con una nota
que diga que el bloque se regenera. Fuera del bloque, ninguna cifra del corpus. Las estructurales
—seis reglas, sus pesos, umbral 60, tres bandas— no son del corpus.

**3. El README no cita cantidades de tests.** Cita los comandos. En dos días ese número cambió
cuatro veces.

**4. Los cuatro diagramas Mermaid** de D4, cada uno con el trabajo que D4 le asigna. El de
verificación de una explicación reemplaza al ciclo de vida, que cabe en una oración. Ningún diagrama
lleva cifras del corpus.

**5. `scripts/check-docs.sh`**, en bash y Node sin dependencias, con el molde de `smoke-ui.sh` y de
`check-openapi-types.mjs`: extrae del README cada ruta entre acentos graves y afirma que existe, y
para cada nombre de test citado hace `grep` en `backend/tests` y `frontend/src`. Falla nombrando lo
que no encontró. Se integra en `check.sh` si no encarece la compuerta de forma apreciable.

**6. La tabla de verificación**, en el handoff: cada afirmación de hecho del README con su
`archivo:línea`, el comando que la produce, o el **test que la afirma**. Una afirmación sin fuente se
elimina o se convierte en pregunta. Incluye una fila por diagrama, «renderizado en la vista previa
de GitHub», porque un error de sintaxis Mermaid se renderiza como bloque de código sin aviso y nada
del repositorio lo detecta.

**7. El inventario de cifras del corpus que viven fuera del README**, también en el handoff, para
que la Etapa 9 empiece con la lista. La revisión adversarial ya tiene el borrador en su hallazgo 7;
hay que verificarlo, no copiarlo.

**8. Los documentos derivados**, con el inventario de la revisión: `Salvo-Overview.md`,
`Salvo-MOC.md`, `Salvo-Getting-Started.md` y `Salvo-Portability.md`. Sus cabeceras y estados ya se
corrigieron; queda el contenido — el roadmap del MOC, la tabla de comandos de Getting-Started a la
que le faltan tres, el contrato conceptual de `IExplanationProvider` en Portability, y las variables
de callback que ningún documento nombra bien.

**9. Corrección de arrastre autorizada**: tres comentarios de código que nombran a la «Etapa 8» para
lo que ahora es la 9 — `quality-section.tsx:26`, `SignalFacts.cs:22` — y uno que dice que `/import` y
`/dashboard` «are linked before they exist» cuando existen desde E5C (`console-header.tsx:10-12`).
Solo el texto de esos comentarios.

### Fuera

- `DesignAgent/Salvo-Blueprint.md`, `Salvo-Progress.md` y `Coordination/Workboard.md`: son estado
  canónico del coordinador y ya se sincronizaron.
- Capturas, guion de demo, `tools/capturas/`, `scripts/demo.sh` y `docs/muestras/`: son `E8B`.
- Cualquier cambio de comportamiento, contrato, esquema o interfaz.
- Badges, catálogo de features y sección de «tecnologías usadas».
- Métricas desarrolladas: entran como límite declarado con su advertencia, porque la Etapa 9 las va
  a cambiar enteras.

### Paths autorizados

- `README.md`
- `scripts/check-docs.sh`, y `scripts/check.sh` solo para invocarlo
- `DesignAgent/Salvo-Overview.md`, `Salvo-MOC.md`, `Salvo-Getting-Started.md`, `Salvo-Portability.md`
- `Coordination/Handoffs/Claude.md`
- `frontend/src/app/dashboard/quality-section.tsx`,
  `frontend/src/components/console-header.tsx` y
  `backend/src/Salvo.Domain/Explanations/SignalFacts.cs`, **solo** el texto de los comentarios del
  punto 9

### Paths reservados por otros trabajos

- `tools/**`, `docs/**` y `scripts/demo.sh`, `scripts/capturas.sh`: `E8B`.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**. Migraciones: **no**. Recaptura de OpenAPI: **no**.
- Escrituras externas: ninguna. No `git push`, no PR.
- Acciones destructivas: ninguna.
- Commits locales en la rama: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E8A-README-DIAGRAMAS.md` sin faltantes antes de empezar.
- [ ] El README no contiene ninguna afirmación falsa. Las conocidas del actual —«cinco etapas», «las
      Etapas 6 a 8 pendientes», «cuatro rutas en tres escenarios», Anthropic «previsto para una etapa
      posterior»— desaparecen, y ninguna nueva las reemplaza.
- [ ] `scripts/check-docs.sh` **falla** si se agrega al README una ruta inexistente y si se cita un
      test que no existe. Documentar las dos falsaciones.
- [ ] Toda cifra del corpus está dentro del bloque marcado. Un `grep` de dígitos fuera del bloque no
      encuentra ninguna cifra dependiente del corpus; las estructurales sí pueden estar.
- [ ] El README no cita ninguna cantidad de tests.
- [ ] Los cuatro diagramas renderizan en la vista previa de GitHub. Confirmarlo explícitamente.
- [ ] La sección «Simulación, sandbox y producción» existe y dice el hecho, no la promesa: nombrar
      `KOIN_MODE=sandbox` o `AI_PROVIDER=anthropic` hace fallar el arranque.
- [ ] Las cinco decisiones citadas son las 29/31, 33, 37/38, 45/47 y 52/54, con su número.
- [ ] El mapa de documentación incluye `Coordination/Tasks/`, `Coordination/Handoffs/`,
      `Coordination/Workboard.md`, `ClaudeAgent/Claude-Model-Policy.md` y
      `DesignAgent/Salvo-Project-Instructions.md`.
- [ ] Los cuatro documentos derivados dejan de contradecir el estado real.
- [ ] `/gate` en verde. `git status --porcelain` solo con paths autorizados.
- [ ] Handoff con la tabla de verificación y el inventario de cifras.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E8A-README-DIAGRAMAS.md` | Brief válido |
| `./scripts/check-docs.sh` | Verde; falla al inyectar una ruta y un test inexistentes |
| `grep` de cifras fuera del bloque marcado | Ninguna dependiente del corpus |
| Vista previa de GitHub de los cuatro diagramas | Los cuatro renderizan |
| `/gate` | Compuerta full-stack verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E8A-README-DIAGRAMAS` | Con tabla de verificación e inventario |

## Decisiones delegadas

- La redacción, el orden interno de cada sección y el largo.
- La forma concreta de cada diagrama, mientras cumpla el trabajo que D4 le asigna.
- Cómo extrae `check-docs.sh` rutas y nombres, y qué considera «ruta».
- Si `check-docs.sh` entra en `check.sh` o se corre aparte, según lo que cueste.
- Qué partes del contenido de los derivados se reescriben y cuáles se dejan.

## Detenerse y consultar si

- una afirmación del diseño v2 resulta falsa contra el código;
- una de las cinco decisiones citadas no dice en la bitácora lo que el diseño supone;
- `check-docs.sh` no puede fallar de forma demostrable;
- hace falta tocar Blueprint, Progress o Workboard;
- hace falta una dependencia nueva o un cambio de comportamiento.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **La tabla de verificación completa** y el inventario de cifras fuera del README.
- Las dos falsaciones de `check-docs.sh`, con su salida.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
