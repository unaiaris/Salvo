# Capturas del recorrido

Se regeneran con un comando:

```bash
./scripts/capturas.sh
```

Levanta la API y la consola sobre una **base temporal propia** —nunca `salvo.db`—, prepara por API el
estado que cada toma promete, y fotografía con Playwright a 1280 px de ancho y factor de escala 2.
Escribe siempre estos seis nombres y **sobrescribe**: no borra nada.

Antes de cada disparo el script afirma que en la pantalla está lo que la captura promete. Si una
espera vence, falla nombrando la toma en vez de dejar un PNG que miente. Una captura es una
afirmación sobre el producto, y una que se saca sin comprobar nada fotografía igual de bien una
página de error.

## Qué cambia entre regeneraciones

Ninguna captura y ningún paso del guion cita una alerta por su URL ni por su posición en la cola. Se
nombran pedidos, y por eso:

- **El orden dentro de cada banda cambia.** Todas las alertas de una corrida comparten instante de
  creación y, con scores de solo 60 y 90, el desempate lo decide un `Guid.NewGuid()`. `ORD_000011`
  aparece en la banda crítica, pero no siempre en el mismo renglón.
- **El identificador de cada alerta cambia**, por lo mismo. La URL de una captura de hoy no existe
  mañana.
- **Los instantes en pantalla cambian.** «Abierta el…», «Vigente desde la corrida #1…», «Redactada
  … el …» son de la corrida que sacó las capturas, no del corpus.

Lo que **no** cambia es todo lo demás: el corpus es el mismo, el motor es determinista y el
proveedor simulado decide por los dígitos de la referencia. Los scores, las señales, los montos y
los veredictos de una regeneración son los de la anterior.

Conviene regenerarlas cuando cambia la interfaz, no en cada tarea: son un par de megabytes por
regeneración en la historia de Git.

## Las seis, con su texto alternativo

| Archivo | Qué muestra | Texto alternativo |
| --- | --- | --- |
| `01-cola.png` | `/alerts`: 18 alertas abiertas, de mayor a menor score vigente, con la severidad del snapshot y la vigente en columnas separadas | Cola de alertas de Salvo con dieciocho alertas abiertas, ordenadas por score, mostrando pedido, severidad, score del snapshot, score vigente, monto y fecha |
| `02-detalle.png` | El detalle completo de la alerta de `ORD_000011`: pedido, snapshot congelado, evaluación vigente, evaluación externa con su divergencia, explicación y el formulario de veredicto | Detalle de la alerta del pedido ORD_000011, con sus tres señales de riesgo, la opinión del proveedor externo, la explicación redactada y el formulario para emitir el veredicto |
| `03-explicacion.png` | El bloque de explicación, con el pie que dice quién la escribió y que se verificó antes de guardarse | Bloque de explicación de una alerta: un párrafo que describe la evaluación con sus cifras, y debajo la nota de que lo redactó una plantilla determinista y no un modelo, verificado contra la evaluación antes de guardarse |
| `04-dashboard.png` | `/dashboard` entero: alertas por banda, monto en riesgo por moneda, tasa de marcado, señales principales, el gráfico semanal en SVG y «Calidad del criterio» con su advertencia | Dashboard de Salvo con dieciocho alertas abiertas, monto en riesgo separado por moneda, gráfico semanal de pedidos y denegados, y la sección de calidad del criterio con la advertencia de que las métricas prueban el pipeline y no la detección |
| `05-import.png` | `/import` después de enviar `docs/muestras/import-con-errores.csv`: un pedido importado y cinco rechazados, cada uno con su registro, línea, campo y motivo | Pantalla de importación tras enviar un archivo con errores: un pedido importado, cinco registros rechazados listados uno por uno con su línea, su campo y el motivo del rechazo |
| `06-divergencia.png` | La divergencia **de criterio**: el motor local marcó el pedido y el proveedor lo aprobó, sin combinarlos ni comparar sus scores | Bloque de evaluación externa mostrando que los dos criterios no coinciden: el motor local marcó el pedido por encima del umbral y el proveedor externo lo aprobó, con la aclaración de que no se combinan en un veredicto único |

La toma 5 es la única que necesita un navegador que interactúe: el resultado de una importación vive
en el estado del cliente y no hay tabla de importaciones, así que sin enviar el formulario de verdad
no hay ninguna pantalla que fotografiar. Componerla de otro modo sería fabricarla.
