# Salvo — Diseño de la Etapa 8: el argumento del proyecto

> Estado: propuesta v1, pendiente de revisión adversarial y de aprobación del usuario
> Fecha: 2026-09-06
> Base: `main` tras el cierre de la Etapa 7
> Fuente de alcance: `DesignAgent/Salvo-Blueprint.md` §11 «Etapa 8», acotada con el usuario el
> 2026-09-06

## Qué está en juego

Esta etapa no agrega funcionalidad. Produce lo que alguien lee **antes** de mirar el código, y en
un proyecto cuyo propósito declarado es demostrar criterio para un puesto, eso lo vuelve la etapa
de mayor palanca y la más fácil de hacer mal.

El modo de fallar es conocido y ya nos pasó tres veces en dos días: **afirmaciones de hecho que
nadie contrastó contra el código**. Un README es un documento hecho casi enteramente de esas
afirmaciones. Por eso la verificación de esta etapa no es «que compile»: es que cada cifra y cada
afirmación se abran contra el archivo que las sostiene.

## Resumen de decisiones

| # | Decisión | Origen |
| --- | --- | --- |
| D1 | El README **argumenta**; las instrucciones de arranque ocupan lo mínimo | Propósito del proyecto |
| D2 | Toda cifra del corpus vive en **un solo bloque marcado** | Secuencia con la Etapa 9 |
| D3 | Los diagramas son **Mermaid versionado**, no imágenes | Mantenibilidad |
| D4 | Cuatro diagramas, cada uno con un trabajo distinto | — |
| D5 | Las capturas tienen **lista de tomas fija** y se sacan del recorrido real | Reproducibilidad |
| D6 | El guion de demo dura **diez minutos** y sirve para la entrevista | Uso real |
| D7 | Cada afirmación de hecho **se abre contra el archivo** antes de escribirla | Los tres fallos previos |
| D8 | Lo que la Etapa 9 va a invalidar **no se escribe con detalle** ahora | Evitar trabajo rehecho |
| D9 | Partición en `E8A` y `E8B` | — |

---

## D1 — El README argumenta

El README actual describe el proyecto y explica cómo levantarlo. Eso no es lo que hace falta: hoy
está además desactualizado —dice «cinco etapas» y da las 6 a 8 por pendientes—, lo que por sí solo
demuestra el problema.

La estructura propuesta, en este orden:

1. **Qué es y qué no.** Salvo es el lado del comercio, no un motor antifraude. Es lo primero porque
   la confusión más cara es leerlo como un intento de competir con un proveedor.
2. **El recorrido en una imagen**, con el diagrama de flujo.
3. **Las tres fuentes de verdad que nunca se mezclan**, con el diagrama del modelo.
4. **Cinco decisiones con su porqué.** No un catálogo de las 58: las cinco que un revisor del rubro
   discutiría. Cada una en tres o cuatro oraciones, con enlace a la bitácora.
5. **Cómo se verifica.** Diferenciales, tests de falsación, el guion de recorrido. Es la sección que
   más distingue al proyecto y hoy no existe.
6. **Límites declarados.** Copiados del artículo para revisores, que ya los tiene bien.
7. **Cómo correrlo.** Corto: dos procesos y el orden de las pantallas.
8. **Mapa de documentación.** La tabla que ya existe, revisada.

Lo que **no** va: un catálogo de features, badges, ni una sección de «tecnologías usadas» que repita
lo que el `Directory.Packages.props` ya dice.

## D2 — Las cifras del corpus, en un bloque marcado

La Etapa 9 reconstruye la fixture: cambian los pedidos, las alertas, los scores, las métricas y las
divergencias. Todo número que dependa del corpus queda obsoleto ese día.

Por eso van **todos juntos**, en una sección delimitada por comentarios HTML —`<!-- corpus:inicio -->`
y `<!-- corpus:fin -->`— con una nota que diga que el bloque se regenera. Fuera de ese bloque, el
README no menciona ninguna cifra del corpus. Las cifras estructurales —seis reglas, sus pesos, el
umbral 60, las tres bandas— **no** son del corpus y pueden ir en el cuerpo.

Se registra como decisión: **un documento que cita datos declara dónde los cita.**

## D3 y D4 — Cuatro diagramas Mermaid

Mermaid, versionado en Markdown, porque GitHub lo renderiza y porque una imagen se desactualiza en
silencio mientras que un diagrama en texto entra en el diff. El Blueprint ya usa uno.

| Diagrama | Qué tiene que dejar claro |
| --- | --- |
| **Las tres fuentes de verdad** | Que el score local, la evaluación externa y el veredicto humano son tablas distintas con ciclos de vida distintos, y que la explicación cuelga de la evaluación y no de la alerta |
| **El recorrido de un pedido** | Los ocho pasos, y sobre todo que la corrida de scoring es un paso explícito: sin ella no hay nada |
| **La máquina de estados de la evaluación externa** | Que un fallo posterior al envío no cierra la evaluación, y qué la cierra |
| **El ciclo de vida de una explicación** | Reserva, verificación sobre la salida, y que un texto rechazado no se guarda |

Ninguno debe repetir lo que el texto ya dice: un diagrama que se puede leer en una lista no vale su
espacio.

## D5 — Las capturas

Lista de tomas fija, del recorrido real con el corpus demo:

1. `/alerts` con la cola ordenada.
2. `/alerts/[id]` completo: pedido, snapshot, evaluación vigente, evaluación externa, explicación y
   panel de veredicto.
3. El bloque de explicación en detalle, que es la pantalla que más distingue al proyecto.
4. `/dashboard`.
5. `/import` con el resultado de un archivo que tiene filas rechazadas.
6. El aviso de divergencia de banda, si se puede provocar.

**Pregunta abierta para el usuario:** las saca él a mano, o se automatizan con el navegador de
Chrome conectado a su máquina, que sí alcanza `localhost:3000`. La segunda opción las vuelve
reproducibles y es la que recomiendo, pero depende de que la extensión esté disponible.

Las capturas van a `docs/capturas/`, referenciadas desde el README, y quedan anotadas como
regenerables en la Etapa 9.

## D6 — El guion de demo

Un documento de diez minutos con tiempos por bloque, pensado para que el usuario lo use **en la
entrevista**, no solo para grabar un video. Estructura propuesta:

| Minuto | Bloque |
| --- | --- |
| 0–1 | Qué es y qué no: el lado del comercio |
| 1–3 | Importar, correr el scoring, ver la cola |
| 3–5 | Una alerta: las señales con sus números, no «riesgo alto» |
| 5–7 | La explicación y por qué está verificada |
| 7–8 | La segunda opinión del proveedor y la divergencia |
| 8–9 | El veredicto terminal y su auditoría |
| 9–10 | Límites declarados y qué haría distinto con datos reales |

Cada bloque lleva **la frase que hay que decir** y **el clic que hay que dar**. El último bloque no
es relleno: cerrar declarando límites es lo que separa una demo de una venta.

## D7 — Cada afirmación se abre contra el archivo

Es el criterio de aceptación central de la etapa, y viene de tres fallos consecutivos: rutas citadas
de memoria en un brief, un pedido de ejemplo sacado de una captura, y un tablero de estado que no
releí al cerrarlo.

Concretamente, el brief exige que la entrega incluya una **tabla de verificación**: cada afirmación
de hecho del README —cifras, nombres de archivo, comandos, versiones, cantidades de test— con el
archivo y la línea que la sostiene, o el comando que la produce. Una afirmación sin fuente se
elimina o se convierte en una pregunta.

## D8 — Lo que la Etapa 9 va a invalidar no se detalla ahora

- Las métricas del criterio se mencionan como límite declarado —hoy F1 vale 1,00 porque la fixture
  recupera sus propias etiquetas— sin desarrollar un análisis que va a cambiar entero.
- No se escribe una sección de internacionalización ni de accesibilidad.
- El README dice, en una línea, qué trae la Etapa 9. Un proyecto que declara lo que le falta se lee
  mejor que uno que finge estar terminado.

## D9 — Partición

**E8A-README-DIAGRAMAS.** El README reescrito, los cuatro diagramas Mermaid, la tabla de
verificación de afirmaciones, y la actualización de `Salvo-Overview.md` y `Salvo-MOC.md` si quedaron
desfasados. Sin capturas.

**E8B-DEMO-CAPTURAS.** El guion de demo, la lista de tomas ejecutada, y el enlace de las capturas
desde el README. Depende de `E8A` integrada.

Ninguna de las dos toca código de producción. `scripts/` solo si el guion de demo necesita un paso
que hoy no existe — y si lo necesita, es hallazgo, no tarea.

## Cambios de estado canónico que exige este diseño

1. **Blueprint §11**: ya aplicado — Etapa 8 acotada y Etapa 9 creada.
2. **Bitácora**: la decisión de D2, sobre dónde viven las cifras citadas.
3. **`Salvo-Progress.md`** y **`Coordination/Workboard.md`**: ya aplicados.

## Preguntas abiertas para la revisión adversarial

1. ¿La estructura de D1 sostiene el argumento, o hay una sección que sobra y otra que falta? En
   particular: ¿«Cómo se verifica» merece el lugar que le doy, o es un detalle de ingeniería que a
   un revisor de producto no le importa?
2. El bloque marcado de D2, ¿alcanza? ¿Hay cifras dependientes del corpus que se van a colar fuera
   del bloque sin que nadie lo note —en los diagramas, en el guion, en los pies de las capturas—?
3. ¿Los cuatro diagramas son los correctos? ¿Alguno repite lo que el texto ya dice, y cuál falta?
4. ¿Diez minutos es el largo correcto para el guion, y el reparto por bloques es el que un revisor
   del rubro querría?
5. La tabla de verificación de D7, ¿es exigible en la práctica o se va a volver una formalidad que
   se llena sin abrir los archivos? ¿Hay una forma mejor de obligar al anclaje?
6. ¿Qué afirmación del README actual es **falsa hoy** y yo no estoy viendo? El documento dice «cinco
   etapas» y da por pendientes las 6 a 8; puede haber más.
