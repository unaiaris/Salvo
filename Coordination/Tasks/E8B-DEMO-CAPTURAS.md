# Salvo — Task brief `E8B-DEMO-CAPTURAS`

## Identificación

- Work ID: `E8B-DEMO-CAPTURAS`
- Etapa: 8
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-06
- Rama/worktree: `claude/e8b-demo-capturas`
- Commit base: `24a9ea9`, el `HEAD` de `main` que cierra `E8A-README-DIAGRAMAS`. `E8A` se integró en
  el merge `1243d54` y `24a9ea9` es el commit de registro que lo sigue; es el `merge-base` real de
  `claude/e8b-demo-capturas`
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. Parece mecánica y no lo es: dos scripts que
  orquestan una base temporal migrada, dos procesos en puertos propios, estados preparados por API y
  un navegador, **sin poder borrar nada**. Es la misma clase de trabajo que `smoke-ui.sh`.
- Dependencias: `E8A-README-DIAGRAMAS` integrada en `main`.

## Resultado esperado

Cualquiera que clone el repositorio puede regenerar las seis capturas con un comando, y ensayar la
demo tantas veces como quiera sobre una base nueva sin tocar la suya. El guion de demo dura diez
minutos y sirve tanto para grabar como para hablar en una entrevista.

## Contexto obligatorio

- `Coordination/Tasks/E8-DISENO.md` (v2), decisiones **D5, D6, D8 y D9**.
- `Coordination/Tasks/E8-revision-adversarial.md`, hallazgos **1, 2, 3, 4, 11 y 12**, y su tercera
  parte, «Playwright, o cómo sacar las capturas sin mentir». Son el origen de casi todo lo que este
  brief exige.
- `Coordination/Handoffs/Claude.md`, entrada de `E8A`.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 8 — El argumento del proyecto»: los ítems de
  capturas y guion de demo, que son los que esta tarea cierra.
- `scripts/smoke-ui.sh`, que es el molde: base temporal con `ConnectionStrings__SalvoDb`, migración
  con `dotnet ef database update --connection`, dos procesos en puertos propios, y limpieza.
- `_local/muestras/`, sus cuatro archivos y su README, que documenta los códigos esperados de cada
  uno.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

**1. `docs/muestras/`**, con las muestras hoy no versionadas y su README: el archivo válido, el que
tiene errores por fila, el de hora inusual y el de raíz inválida. Son cien por ciento sintéticos.
Copiarlos desde `_local/muestras/` **sin modificar su contenido**, y dejar el original donde está.
El README de `docs/muestras/` explica qué demuestra cada uno y qué códigos de error espera.

**2. `scripts/demo.sh`.** Levanta API y consola sobre una **base nueva con fecha en el nombre**, con
el molde de `smoke-ui.sh`: crea la base, la migra con `--connection`, siembra el corpus y ejecuta la
corrida por `curl`, y deja las dos URLs impresas. **No borra nada**: `rm` está denegado y es una
regla del proyecto. La base anterior queda donde estaba. Existe porque el guion es destructivo por
diseño —el veredicto es terminal, la explicación escrita no se regenera y el seed es idempotente—,
así que sin reset no hay segundo ensayo.

**3. `tools/capturas/`**, paquete propio con su `package.json` y su lockfile, **fuera de
`frontend/`**, con Playwright en **versión exacta**. No toca `frontend/package-lock.json` ni
`npm ci` ni la compuerta. Verificar si el paquete descarga navegadores al instalarse y, si lo hace,
dejar la descarga como paso explícito del script, no como efecto de la instalación.

**4. `scripts/capturas.sh`.** Prepara el estado por API y fotografía. Cinco de las seis tomas no
necesitan interacción: el estado se crea con `curl` y la página lo lee de la base. La sexta sí,
porque el resultado de una importación vive en memoria del cliente y no hay tabla de importaciones.

| Archivo | Toma | Cómo se prepara |
| --- | --- | --- |
| `01-cola.png` | `/alerts` | seed + corrida |
| `02-detalle.png` | `/alerts/[id]` de **`ORD_000011`** | el id se resuelve por API, nunca se fija |
| `03-explicacion.png` | El bloque de explicación | `POST …/explanation` |
| `04-dashboard.png` | `/dashboard`, con «Calidad del criterio» y su advertencia | seed + corrida |
| `05-import.png` | `/import` con filas rechazadas | **enviar el formulario** con la muestra versionada |
| `06-divergencia.png` | La divergencia **de criterio** | `POST …/external-evaluations` sobre un pedido que el mock aprueba |

Ancho 1280, factor de escala 2, nombres fijos, y **texto alternativo por captura** para el README.
El script **sobrescribe**, no borra. Las capturas van a `docs/capturas/`.

**5. La nota de regenerables**, junto a las capturas: qué comando las produce, que el orden dentro de
cada banda cambia entre regeneraciones —el id de una alerta es un `Guid.NewGuid()` y el desempate del
orden también—, y que los instantes en pantalla cambian.

**6. El guion de demo**, `docs/guion-demo.md`, con los siete bloques de D6, sus tiempos, la frase que
hay que decir, el clic que hay que dar y una línea «si falla» por bloque. Con la precondición escrita
—`scripts/demo.sh` primero— y las mismas marcas `<!-- corpus:inicio -->` / `<!-- corpus:fin -->` del
README alrededor de sus cifras.

Dos precisiones que el guion **no puede equivocar**, porque la revisión las verificó contra el
código: los callbacks se piden y entregan **desde `/import`** —sobre una misma alerta no se pueden
mostrar callback y divergencia a la vez—, y la pantalla de auditoría muestra transición, instante y
nota pero **no** `explanationId`, así que ese dato se enseña con un `curl` o se declara parcial.

**7. Nada del guion ni de las capturas cita una alerta por su URL ni por su posición en la cola.**
Se nombran pedidos: «la fila de `ORD_000011`».

**8. El README enlaza las capturas y el guion**, con su texto alternativo, y documenta los dos
comandos nuevos, incluida la descarga inicial de Chromium.

### Fuera

- Cualquier cambio de comportamiento, contrato, esquema o interfaz.
- Fijar el desempate del orden de la cola: es código de producto y le toca a la Etapa 9.
- Pintar `explanationId` en el panel de revisión: mismo motivo.
- Borrar bases, archivos o capturas viejas.
- Tocar `frontend/package.json` o su lockfile.
- El README fuera de las secciones de capturas, guion y comandos.

### Paths autorizados

- `docs/**`
- `tools/capturas/**`
- `scripts/demo.sh`, `scripts/capturas.sh`
- `README.md`, solo las secciones del punto 8
- `Coordination/Handoffs/Claude.md`

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- **Dependencia nueva autorizada: Playwright**, en `tools/capturas/`, con versión exacta y lockfile
  propio, aprobada por nombre y motivo en el diseño v2. Ninguna otra.
- Descargar el Chromium de Playwright: autorizado, como paso explícito del script.
- Levantar API y consola en puertos propios: autorizado.
- Escrituras externas: ninguna. No `git push`, no PR.
- **Acciones destructivas: ninguna.** No borrar bases ni archivos; sobrescribir en el mismo nombre.
- Commits locales en la rama: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E8B-DEMO-CAPTURAS.md` sin faltantes antes de empezar.
- [ ] `frontend/package.json` y `frontend/package-lock.json` **no cambian**. `npm ci` y `/gate`
      siguen exactamente como estaban.
- [ ] `./scripts/demo.sh` deja una base nueva funcionando y **la base anterior intacta**. Verificarlo
      comparando el archivo anterior antes y después.
- [ ] `./scripts/capturas.sh` produce las seis capturas desde una base nueva. Correrlo **dos veces
      seguidas** y confirmar que la segunda no falla y no borra nada.
- [ ] La captura 5 muestra registros rechazados de verdad, producidos por un envío real del
      formulario con la muestra versionada. No se compone la pantalla de ninguna otra forma.
- [ ] La captura 6 es la divergencia **de criterio**, no la de banda, y el pedido elegido es uno que
      el mock aprueba de forma determinista.
- [ ] Ninguna captura ni ningún paso del guion cita una URL de alerta ni una posición en la cola.
- [ ] Las cifras del guion están dentro de las marcas de corpus.
- [ ] El guion dice que los callbacks se manejan desde `/import` y qué muestra —y qué no— el panel de
      auditoría.
- [ ] `/gate` verde y `./scripts/smoke-ui.sh` verde, sin cambios en su cantidad de comprobaciones.
- [ ] `git status --porcelain` solo con paths autorizados.
- [ ] Handoff generado con `/handoff E8B-DEMO-CAPTURAS`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E8B-DEMO-CAPTURAS.md` | Brief válido |
| `git diff --stat` sobre `frontend/` | Sin cambios salvo lo autorizado |
| `./scripts/demo.sh` | Base nueva arriba; la anterior con el mismo tamaño y mtime |
| `./scripts/capturas.sh` dos veces | Seis PNG las dos veces; nada borrado |
| Inspección de `05-import.png` | Registros rechazados reales |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E8B-DEMO-CAPTURAS` | Entrada agregada |

## Decisiones delegadas

- La estructura interna de los dos scripts y qué comparten con `smoke-ui.sh`.
- Cómo se resuelve el id de la alerta de `ORD_000011` por API.
- Qué pedido concreto se usa para la divergencia de criterio, mientras sea determinista.
- La redacción del guion y de los textos alternativos.
- El nombre de la base con fecha y dónde vive.

## Detenerse y consultar si

- Playwright no puede instalarse en `tools/capturas/` sin tocar nada de `frontend/`;
- alguna captura no puede producirse sin fabricar la pantalla;
- `demo.sh` no puede crear una base nueva sin borrar la anterior;
- el pedido elegido para la divergencia no diverge de forma determinista;
- hace falta cualquier dependencia además de Playwright.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- Las seis capturas, y la salida de las dos corridas seguidas de `capturas.sh`.
- La comprobación de que la base anterior quedó intacta.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
