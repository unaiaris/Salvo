# Salvo — Task brief `E9A-FIXTURE`

## Identificación

- Work ID: `E9A-FIXTURE`
- Etapa: 9
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-07
- Rama/worktree: `claude/e9a-fixture`
- Commit base: se escribe **en la rama**, después de crearla. El commit que declara la base pasa a
  ser la base si va en `main`: lección de `E8B`.
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. Con una advertencia honesta sobre dónde está el
  riesgo: no en la capacidad del modelo sino en la **búsqueda aritmética**. Cada arquetipo tiene que
  caer en una celda exacta de la matriz, y eso se consigue tanteando montos, instantes y referencias
  contra un motor que ya existe. Si un arquetipo no cae donde el brief dice después de tres
  intentos, **parar y consultar**: probablemente el arquetipo esté mal especificado, no mal
  construido.
- Dependencias: ninguna. `E9B`, `E9C` y `E9D` dependen de esta integrada, en ese orden.

## Resultado esperado

El corpus de demostración deja de recuperar sus propias etiquetas. Las seis reglas y las tres bandas
son alcanzables, hay fraude que el motor no ve y pedidos legítimos que sí marca, y **el dashboard
muestra los que el proveedor externo denegó sin que hubiera alerta local** — que es la única forma
de que el argumento del efecto de red se vea en pantalla y no solo en el README.

## Contexto obligatorio

- `Coordination/Tasks/E9-DISENO.md` (**v2**), decisiones **D1 a D4, D8, D9 y D10**.
- `Coordination/Tasks/E9-revision-adversarial.md`, **la primera parte entera** —es el criterio de
  dominio del que salen los siete arquetipos, con la razón por la que cada uno es invisible— y los
  hallazgos **2, 3, 6, 9 y 10**.
- `DesignAgent/Salvo-Blueprint.md`: §4.1 —`countryCode` es el país de la sesión—, §4.2, §7 —qué
  significa `isFraudLabel`—, §11 y las decisiones 23, 26, 37 y 63.
- `Coordination/Handoffs/Claude.md`, entrada de `E8A`: **el inventario de cifras del corpus** que
  esta tarea invalida.
- Código: `TemporalRiskEngine`, `RiskMetricsEvaluator`, `SeedDemoOrdersHandler`,
  `EmbeddedDemoOrderSource`, `MockAntifraudProvider`, `GetDashboardHandler`, `DashboardViews`,
  `divergence.ts`, y el panel del dashboard en `panels.tsx`.

**Antes de escribir cualquier afirmación, abrir el archivo que la sostiene.**

## Alcance

### Dentro

**1. `demo-orders.v2.json`.** Restricciones duras, todas verificadas contra el código:

- **Los mismos tres `merchantId` y las mismas 300 referencias** `ORD_000001`–`ORD_000300`. No es
  cosmético: la referencia es `merchantId:merchantReferenceId`, así que renombrar un comercio anula
  el guardián de conflicto y deja convivir dos corpus contradictorios.
- **Países en el corredor UTC−3** —Uruguay, Brasil, Argentina—. Fuera de él las franjas horarias
  dejan de coincidir con `BusinessTimeZone` y la explicación diría «franja de 18:00 a 24:00, hora
  del comercio» sobre un comercio que cerró a las 18.
- **Al menos siete fraudes en el último tercio por instantes distintos.** La división temporal toma
  los primeros dos tercios de los instantes como calibración; con tres falsos negativos en el
  holdout, con menos de siete el recall no llega a 0,5.
- **Un comercio con al menos 21 pedidos en los 30 días previos al pedido raro y no más de 2 en su
  franja**, para que `unusual_hour` sea alcanzable. Hoy no lo es por dos motivos: la franja más rara
  está en 16,7 % contra un umbral de 10 %, y veinte de cada cien pedidos no llegan al mínimo de
  veinte y la regla ni evalúa.

**2. Los siete arquetipos, con su celda escrita antes de correr el motor.** Están en la primera
parte de la revisión adversarial con su razón. El brief no los repite para que se copien: se leen
ahí, **incluida la explicación de por qué cada uno es invisible o se marca**, porque esa explicación
es lo que hay que preservar al construirlos.

La entrega incluye una tabla, hecha **antes** de la primera corrida, con una fila por arquetipo:
referencia, comercio, comprador, monto, instante, país, dispositivo, etiqueta esperada, reglas que
deberían dispararse, score esperado y celda de la matriz. Después, la misma tabla con lo que el
motor produjo. **Las dos tablas juntas son el entregable**: si difieren, la diferencia se explica.

**3. Restricciones de referencia que el corpus tiene que respetar**, porque el proveedor simulado
decide por los dígitos finales:

- **Los tres falsos negativos** llevan referencias en la banda que el mock **deniega**, para que el
  panel del punto 5 los muestre.
- **El pedido de la demo** —el que aparece en el guion, en las capturas, en las muestras y en un
  dorado— tiene que identificar exactamente una alerta, ser **crítico**, estar **aprobado** por el
  mock para que la captura de divergencia de criterio exista, y tener un ratio **con decimal**.
- **Al menos una alerta en la banda que el mock deja pendiente**, con el resto que hace que el
  callback la cierre en denegado, para que el guion siga pudiendo mostrar el callback.

**4. El seed versionado, y el aviso antes del clic.** Hoy `ValidateDocumentShape` exige
`version == "1"`, exactamente 300 pedidos y exactamente 18 fraudes, y corre **antes** de mirar la
base; el endpoint solo atrapa el conflicto, así que un v2 devuelve un `500` incluso sobre una base
vacía. La tarea entrega el validador versionado, el nombre del recurso, `DatasetVersion`, y un
**ensayo**: la misma comparación sin escribir, que la consola consulta al abrir `/import` para
rotular el botón. El código nuevo va a `messages.ts` y a su test de exactitud, por la decisión 57.

**5. El panel «Denegados por el proveedor sin alerta local»** en el dashboard: conteo y pedidos, sin
acciones y sin estado. Es la superficie que hace visible el argumento del corpus. El dashboard hoy
no tiene ninguna sección de evaluación externa.

**6. El aviso de divergencia corregido.** Compara identificadores de evaluación, así que un cambio
de versión del motor hará que **toda** alerta existente diga «la evaluación cambió (60 → 60) sin
cambiar de banda». Tiene que comparar score y señales. Se arregla acá porque `E9B` lo va a disparar.

**7. Todos los tests fijados al corpus.** El inventario de `E8A` más los que la revisión agregó:
distribución de scores y matrices, histograma y fingerprint dorado, conteos del mock, tests del
dashboard y de métricas, `DemoSeedTests`, orden del feed, los dos dorados de explicación, y las
fixtures del frontend.

**8. La matriz se publica con conteos y `n`.** No una razón con dos decimales: con cien pedidos de
holdout cada falso negativo mueve el recall doce puntos.

**9. Si el barrido elige un umbral distinto de 60, se deja.** Decidido con el usuario y registrado
como decisión 63: el umbral es una política de negocio, no el resultado del barrido. La tarea
**reporta** qué eligió el barrido; el README y el guion lo explican en `E9D`.

### Fuera

- `e3-v2` y cualquier cambio al motor o a `RuleConfig`. El motor queda **quieto**: es la mitad del
  argumento del orden de la etapa.
- El portugués, la accesibilidad y el repaso de documentos.
- `/orders`, el proveedor que se porta mal, y Anthropic.
- El desempate de la cola por identificador aleatorio y pintar `explanationId` en el panel de
  revisión: los dos quedan afuera de la etapa, dichos.
- Borrar bases de datos: `rm` está denegado y es regla del usuario. Se listan.

### Paths autorizados

- `backend/src/Salvo.Infrastructure/Seed/**`
- `backend/src/Salvo.Application/Orders/Seed/**`
- `backend/src/Salvo.Application/Dashboard/**` y `backend/src/Salvo.Api/**`
- `backend/tests/**`
- `frontend/src/app/dashboard/**`, `frontend/src/app/alerts/[id]/divergence.ts`,
  `frontend/src/lib/api/messages.ts` y `format.ts`, `frontend/src/test/fixtures.ts`
- `frontend/openapi/salvo-openapi.json` y `frontend/src/lib/api/schema.d.ts`, solo recaptura
- `scripts/smoke-ui.sh`, solo si una comprobación fija una cifra del corpus
- `Coordination/Handoffs/Claude.md`

### Paths reservados por otros trabajos

- `backend/src/Salvo.Domain/Risk/**` y `Explanations/**`: `E9B`.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**.
- Migraciones: **no**. El panel es una lectura.
- Levantar la API para recapturar el OpenAPI: autorizado.
- Crear bases nuevas con `scripts/demo.sh`: autorizado. **No borrar ninguna.**
- Escrituras externas: ninguna. No `git push`, no PR.
- Commits locales: autorizados, y se pide commitear por partes.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E9A-FIXTURE.md` sin faltantes antes de empezar.
- [ ] La tabla de arquetipos **esperada** existe en un commit anterior al de la fixture. Es la
      falsación de esta tarea: predecir la celda y después medirla, no medir y llamarlo predicción.
- [ ] Los siete arquetipos caen en su celda, o la diferencia está explicada.
- [ ] Las seis reglas aparecen en al menos una evaluación, y las tres bandas en al menos una alerta.
- [ ] **F1 sobre el holdout es menor que 1,00**, con al menos tres falsos negativos y tres falsos
      positivos.
- [ ] Sembrar el corpus v2 sobre una base con el v1 **no devuelve `500`**: devuelve el conflicto con
      su código, y la consola lo dice antes del clic.
- [ ] Sembrar el v2 sobre una base vacía funciona.
- [ ] El panel del dashboard muestra los tres falsos negativos denegados por el proveedor.
- [ ] El aviso de divergencia no aparece cuando el score y las señales no cambiaron.
- [ ] `dotnet ef migrations has-pending-model-changes` no reporta cambios.
- [ ] `/gate` y `./scripts/smoke-ui.sh` verdes.
- [ ] Handoff con **las dos tablas**, la matriz con conteos y `n`, el umbral que eligió el barrido, y
      la lista de bases `.db` para que el usuario borre lo que quiera.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E9A-FIXTURE.md` | Brief válido |
| Tabla esperada, commiteada antes | Existe y precede a la fixture |
| Corrida sobre el corpus v2 | Seis reglas y tres bandas presentes |
| Métricas del holdout | F1 < 1,00; FN ≥ 3; FP ≥ 3 |
| Seed v2 sobre base con v1 | Conflicto con código, no `500`; avisado antes del clic |
| Seed v2 sobre base vacía | 300 pedidos, sin conflicto |
| Panel del dashboard | Los tres falsos negativos, denegados por el proveedor |
| Aviso de divergencia | Ausente cuando score y señales no cambian |
| `/gate` y `./scripts/smoke-ui.sh` | Verdes |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E9A-FIXTURE` | Con las dos tablas y la matriz |

## Decisiones delegadas

- Los montos, instantes, compradores y dispositivos concretos de cada arquetipo.
- Qué comercio se vuelve «de horario comercial» y cómo se reordenan sus pedidos.
- La forma del ensayo del seed y el nombre de su código de conflicto.
- La disposición del panel en el dashboard.
- Qué comprueba el smoke sobre el panel.

## Detenerse y consultar si

- un arquetipo no cae en su celda tras tres intentos;
- respetar las 300 referencias y los tres comercios impide construir algún arquetipo;
- el barrido elige un umbral distinto de 60 **y** eso rompe una aserción que no es del corpus;
- el panel exige una migración o una consulta que el dashboard no puede hacer barata;
- hace falta tocar el motor, `RuleConfig` o el dominio de explicaciones;
- la fixture no puede tener siete fraudes en el último tercio sin deformar la distribución temporal.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- **Las dos tablas de arquetipos**, esperada y obtenida, con las diferencias explicadas.
- La matriz de confusión de las dos cohortes, con conteos y `n`.
- El umbral que eligió el barrido.
- Comandos y resultados exactos, con las falsaciones documentadas.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
