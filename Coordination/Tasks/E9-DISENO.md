# Salvo — Diseño de la Etapa 9: corpus, idiomas y cierre

> Estado: propuesta v1, pendiente de revisión adversarial y de aprobación del usuario
> Fecha: 2026-09-06
> Base: `main` tras el cierre de la Etapa 8
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §11 «Etapa 9», y las candidatas registradas
> en `Coordination/Workboard.md`

## Qué está en juego

Es la última etapa, y es la única que toca **datos** en vez de mecanismos. Hasta acá el proyecto
construyó un sistema que se sabe correcto; esta etapa decide qué puede **afirmar sobre sí mismo**.

Hoy no puede afirmar casi nada sobre su criterio de detección. La fixture fue construida para que
las reglas recuperen sus propias etiquetas: F1 vale 1,00, tres de las seis reglas nunca abren una
alerta, y `unusual_hour` es estructuralmente inalcanzable. El producto lo declara con honestidad
—la advertencia está en el dashboard y en el README—, pero declarar un límite no es lo mismo que
no tenerlo.

## La restricción que ordena toda la etapa

**Una fixture nueva no se puede sembrar sobre ninguna base que tenga la vieja.**
`SeedDemoOrdersHandler` compara cada referencia contra lo que ya existe y **lanza
`DemoSeedConflictException`** si los datos de negocio difieren; lo mismo si cambió la etiqueta
(`SeedDemoOrdersHandler.cs:36-66`). No saltea en silencio: se niega. Es la inmutabilidad del pedido
funcionando exactamente como se diseñó.

Consecuencias, todas de diseño y no de implementación:

- La base local del usuario —328 pedidos, 21 alertas, dos explicaciones, importaciones manuales—
  **queda incompatible con la fixture nueva**. No se pierde: deja de ser la base de demostración.
- El corpus nuevo es `demo-orders.v2.json` y **reusa el mismo espacio de referencias**
  `ORD_000001`–`ORD_000300`. La alternativa —referencias nuevas para que convivan— produciría un
  corpus de 600 pedidos donde la mitad contradice a la otra.
- El mensaje del conflicto tiene que **nombrar la causa**. Hoy dice «el conjunto de demostración
  entra en conflicto con una referencia existente», que es cierto y no ayuda. Tiene que decir que
  esa base tiene la versión anterior del corpus y que hace falta una base nueva.

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | **`e3-v2` primero, la fixture después.** Una pieza móvil por vez | Ambas invalidan fingerprints |
| D2 | La fixture nueva exige una base nueva, y el sistema lo dice con esas palabras | La restricción de arriba |
| D3 | El corpus v2 tiene **falsos negativos y falsos positivos deliberados** | Decidido con el usuario |
| D4 | Los arquetipos se eligen por lo que **enseñan**, no por cubrir casillas | D3 |
| D5 | El motor emite **campos tipados**; la interfaz compone el texto | Candidata registrada |
| D6 | El portugués es un diccionario, y el idioma se elige por configuración | Koin |
| D7 | La accesibilidad se verifica con un lector de pantalla real, no con un linter | Candidata registrada |
| D8 | Fuera: `/orders`, el proveedor que se porta mal, y Anthropic | Alcance |
| D9 | El repaso final de documentos usa el **inventario que `E8A` entregó** | Etapa 8 |
| D10 | Partición en cuatro tareas, en orden obligatorio | — |

---

## D1 — El orden importa más que en ninguna etapa anterior

Dos cambios de esta etapa invalidan los mismos artefactos: los fingerprints de las evaluaciones,
los dos textos dorados de explicación, los conteos del proveedor simulado y las cifras de todo
documento público.

- **`e3-v2`** cambia lo que el motor *escribe* en cada señal, con el corpus quieto.
- **La fixture v2** cambia los *datos*, con el motor quieto.

Hacerlos juntos ahorra una invalidación y cuesta mucho más: cuando algo falle, habrá dos causas
posibles y ninguna forma barata de distinguirlas. Se hacen en ese orden, y **`e3-v2` va primero**
porque es un cambio de código verificable contra un corpus conocido, mientras que la fixture es un
cambio de datos que se verifica mejor contra un motor estable.

## D2 — La base nueva es parte del entregable, no un efecto colateral

La etapa entrega, además del corpus: el mensaje de conflicto que nombra la causa, una línea en el
README que diga que actualizar el corpus exige una base nueva, y `scripts/demo.sh` ya sirve para
crearla. La base vieja del usuario no se borra ni se migra — deja de ser la de demostración, y eso
se dice.

## D3 y D4 — Qué contiene el corpus v2

Lo que hoy falta no es volumen: es **desacuerdo entre las reglas y la verdad**. Sin desacuerdo, la
matriz de confusión es decorativa y el barrido de umbrales no decide nada.

**Los falsos negativos son la parte que más enseña.** El arquetipo correcto no es «un fraude raro»:
es un fraude que las seis reglas *no pueden* ver por construcción — monto normal, país habitual,
hora habitual, comprador con historia, sin ráfaga. Una cuenta tomada de un comprador establecido
que compra algo de valor corriente. Ninguna regla local lo toca, y **un proveedor con efecto de red
sí lo vería**, porque ese comprador o ese dispositivo aparecieron en otro comercio. Es exactamente
el argumento que el README hace sobre lo que Salvo no es, demostrado con datos en vez de afirmado.

**Los falsos positivos son el costo del criterio.** Un cliente legítimo que viaja dispara
`foreign_country` y `cross_border_velocity`. Una compra legítima grande de un comprador sin historia
dispara `new_buyer_high_value` y `amount_anomaly`. Son los casos que una analista real revisa y
descarta, y son la razón por la que la tasa de falsos positivos es una métrica y no una anécdota.

El corpus tiene que además **hacer alcanzables las seis reglas y las tres bandas**, incluida
`unusual_hour`, que hoy es imposible: exige una franja de seis horas con ≤10 % de los pedidos del
comercio en treinta días, y la franja más rara de los tres comercios está en 13,8 %. Hace falta al
menos un comercio cuya actividad se concentre de verdad en horario comercial.

Números objetivo, para que el diseño sea verificable en vez de aspiracional: **precisión y recall
claramente por debajo de 1,00 y por encima de 0,5**, con al menos tres falsos negativos y tres
falsos positivos en la cohorte de holdout. El valor exacto no se fija acá —lo fija la construcción—
pero **F1 = 1,00 es un fallo de la etapa**, no un éxito.

## D5 — El motor emite campos tipados

Cada señal deja de llevar una frase inglesa y pasa a llevar sus campos: `ratio`, `median`, `scope`,
`historyCount`, `window`, `from`, `to`, `elapsedMinutes`, `share`, `bucket`, según la regla. Sube a
`e3-v2` e invalida los fingerprints a propósito.

Tres consecuencias que la etapa hereda de las anteriores y que hay que cobrar:

- **El extractor de `SignalFacts` se borra.** La Etapa 7 lo dejó escrito como semilla exactamente
  para este día; `ExplanationFacts` pasa a alimentarse de los campos, sin parsear nada.
- **La interfaz compone el texto de cada señal**, con los mismos rótulos que ya tiene `format.ts`.
  Deja de haber inglés dentro de una consola en castellano, que es lo que hoy se ve en la captura
  más importante del README.
- **La plantilla de explicación sube a `e7-v3`**, por la decisión 58: cambia el texto que produce.

## D6 — El portugués

Koin opera principalmente en Brasil y México. El castellano cubre México; el portugués es el que
falta, y es la señal más directa que este proyecto le puede dar a un empleador brasileño.

Con los campos tipados, traducir deja de ser cosmético: la frase se compone en la interfaz, así que
el portugués es un diccionario más y no una capa sobre prosa inglesa incrustada en un hash.

**Pregunta abierta para el usuario y para la revisión:** cómo se elige el idioma. No hay
autenticación ni preferencias de usuario. Las opciones son un valor de configuración del despliegue
—coherente con que la instancia sea de un comercio—, la cabecera `Accept-Language`, o un segmento en
la ruta. La primera es la que menos superficie agrega y la que mejor describe el producto: un
comercio brasileño despliega su consola en portugués. Recomiendo esa, y que el alcance sea **la
consola entera**, porque una consola medio traducida es peor que una sin traducir.

## D7 — Accesibilidad

Con un lector de pantalla real —VoiceOver en macOS—, no con un linter. Lo que hay que verificar es
el recorrido, no la ausencia de advertencias: que la cola se pueda navegar, que la severidad se
anuncie con palabras y no solo con color, que el formulario de veredicto diga qué se está por hacer
y que su casilla de reconocimiento se entienda, y que los avisos —divergencia, explicación
desactualizada, errores de importación— lleguen al lector cuando aparecen.

Lo que salga de ahí es una lista de correcciones concretas. La etapa no promete cumplir un nivel de
WCAG: promete que alguien recorrió la consola sin ver la pantalla y anotó qué no funcionó.

## D8 — Qué queda fuera, y por qué

- **`/orders`**: una ruta que lista pedidos sin alerta. Útil, y no cambia lo que el proyecto puede
  afirmar. Post-MVP.
- **Un proveedor simulado que se porte mal**, para que `NO_OP`, `SUPERSEDED` y `CONFLICTING` se vean
  en pantalla y no solo en los tests. Es la candidata más tentadora de las que quedan afuera: esos
  tres estados son de lo mejor del diseño de la Etapa 6 y hoy son invisibles. Pero es superficie
  nueva en la última etapa, y la etapa ya tiene un cambio de datos y uno de motor.
- **Anthropic.** Sigue siendo decisión aparte. La infraestructura está lista desde D11 de la Etapa 7
  y activarla no cambia ninguna afirmación del proyecto: cambia quién redacta, no si el texto se
  verifica.

## D9 — El repaso final

`E8A` entregó el inventario de cifras del corpus que viven fuera del README —el texto de `/import`,
las bandas del proveedor simulado y su test, los dos textos dorados, los tests del dashboard, una
línea del smoke, el §4.1 y la decisión 19 del Blueprint, las notas del Workboard y las evidencias
del Progress—. Esa lista es la lista de trabajo, no un `grep`.

Entra además: la geografía de los comercios en mercados plausibles, los códigos de error de fila que
hoy llegan en inglés desde la API (`describeRecordError`), «Se importaron 1 pedidos» sin
concordancia, las seis capturas regeneradas, el artículo para revisores actualizado, y las bases de
ensayo que quedaron en `backend/src/Salvo.Api/`.

## D10 — Partición

En este orden, y el orden es obligatorio:

1. **`E9A-SENALES-TIPADAS`** — `e3-v2`, campos tipados, la interfaz compone, el extractor de
   `SignalFacts` se borra, `e7-v3`. Corpus quieto.
2. **`E9B-FIXTURE`** — `demo-orders.v2.json` con los arquetipos de D3, el mensaje de conflicto que
   nombra la causa, y todas las cifras de tests que dependen del corpus. Motor quieto.
3. **`E9C-PORTUGUES-ACCESIBILIDAD`** — el diccionario, la elección de idioma, y las correcciones que
   salgan del recorrido con lector de pantalla.
4. **`E9D-CIERRE`** — el repaso final de documentos, las capturas regeneradas y el artículo.

## Preguntas abiertas para la revisión adversarial

1. El orden de D1, ¿es el correcto? ¿Hay algo en `e3-v2` que sea más fácil de verificar **después**
   de tener un corpus con desacuerdo?
2. Los arquetipos de D3, ¿son plausibles para alguien del rubro, o son lo que un ingeniero imagina
   que es el fraude? En particular el falso negativo: ¿una cuenta tomada se ve así?
3. ¿Qué cifra de F1 sería *sospechosamente buena*? Si el corpus nuevo da 0,95, ¿seguimos teniendo el
   mismo problema con otro número?
4. La elección de idioma por configuración, ¿alcanza? ¿Qué se rompe si dos personas del mismo
   comercio quieren idiomas distintos?
5. ¿Qué se rompe al borrar el extractor de `SignalFacts`? La Etapa 7 dijo que el resto queda intacto;
   ¿es cierto contra el código de hoy?
6. La base local del usuario queda incompatible. ¿Hay alguna forma de que el sistema lo diga antes
   de fallar, en vez de fallar y explicar?
