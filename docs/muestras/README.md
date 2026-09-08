# Muestras de importación

Cuatro archivos para probar `/import` sin inventarse datos. Son **cien por ciento sintéticos**: no
hay una sola persona, tarjeta ni dirección real, y los identificadores de comprador son pseudónimos.

Se suben desde la consola en `http://localhost:3000/import`, o por API:

```bash
curl -s -X POST -F "file=@docs/muestras/import-valido.csv" -F "format=CSV" \
  http://127.0.0.1:5100/api/order-imports
```

El campo `format` es obligatorio y vale `CSV` o `JSON`; sin él la API responde `415` con código
`UNSUPPORTED_FORMAT`. La consola lo manda por su cuenta.

| Archivo | Qué demuestra | Resultado |
| --- | --- | --- |
| `import-valido.csv` | El camino feliz, y que importar **no** puntúa | 5 importados, 0 rechazados |
| `import-con-errores.csv` | El rechazo por registro: el archivo no es todo o nada | 1 importado, 5 rechazados |
| `import-hora-inusual.json` | Formato JSON, y `unusual_hour` en un solo archivo, sin depender del corpus | 21 importados, 0 rechazados |
| `import-raiz-invalida.json` | El rechazo del **documento entero**, que es otra cosa | `400 INVALID_JSON_ROOT` |

## La importación no es todo o nada por archivo

Las filas válidas se escriben **todas juntas**, en una transacción; las rechazadas no se escriben
nunca. Un comercio que sube diez mil pedidos con doce filas rotas obtiene 9.988 adentro y la lista de
las doce para corregir, en vez de perder la importación entera.

Un documento mal formado es el otro caso: ahí no hay filas que salvar y se rechaza completo. Por eso
`import-raiz-invalida.json` devuelve un error de documento y no una lista de errores por fila.

## Qué rechaza `import-con-errores.csv`, exactamente

Estos códigos salen de correr la importación contra una base migrada con el corpus demo cargado, no
de leer el validador:

| Fila | Problema | Campo | Código |
| --- | --- | --- | --- |
| 2 | `merchantReferenceId` vacío | `merchantReferenceId` | `REQUIRED` |
| 3 | `occurredAt` dice «ayer a la tarde» | `occurredAt` | `INVALID_FORMAT` |
| 4 | `amountCents` es `-500` | `amountCents` | `INVALID_FORMAT` |
| 5 | `currencyCode` es `DOBLONES` | `currencyCode` | `UNSUPPORTED_VALUE` |
| 6 | `MER_BR_STORE / ORD_000011` ya existe en el corpus | `merchantReferenceId` | `REFERENCE_CONFLICT` |

La fila 1 sí entra. Y la 4 conviene mirarla dos veces: un monto negativo se rechaza como
`INVALID_FORMAT` y no como `OUT_OF_RANGE`, porque el parser exige un entero en base diez y el signo
menos lo detiene antes de que el dominio llegue a comprobar el rango. Las dos comprobaciones existen;
la primera gana.

## La referencia es compuesta

La unicidad es **`(merchantId, merchantReferenceId)`**, no `merchantReferenceId` solo:

```sql
CREATE UNIQUE INDEX "ux_orders_merchant_reference" ON "orders" ("merchant_id", "merchant_reference_id")
```

Dos comercios distintos pueden usar la misma numeración interna sin colisionar, que es el caso real
que esa decisión soporta. Por eso la fila 6 usa `MER_BR_STORE`: con cualquier otro comercio no habría
conflicto, sería un pedido nuevo y legítimo.

## `import-hora-inusual.json`: la sexta regla, en un solo archivo

Este archivo es anterior al corpus v2 y nació para alcanzar lo que el corpus v1 no alcanzaba. **Eso
ya no hace falta**: el corpus de demostración dispara hoy las seis reglas y produce las tres bandas.
`unusual_hour` salta dos veces sobre él, en `ORD_000160` y `ORD_000244`, los dos en la franja
00:00–06:00 con **cero** apariciones previas de esa franja en veinticinco y veintiséis pedidos del
comercio. La cifra que este documento repetía —«la franja más rara está en 16,7 %»— era del corpus
anterior; recalculada sobre el v2 da **0 %**, y la más rara que **no** llega a disparar da 12 %.

Lo que el archivo sigue teniendo de útil es que la regla se ve entera en una sola importación, sin
sembrar trescientos pedidos ni buscar una alerta en la cola. Crea un comercio nuevo,
`MER_UY_PHARMA`, con veinte pedidos de rutina —todos entre las 09:00 y las 11:30 locales, en
Uruguay, montos normales— y después uno solo, `ORD_959999`, a las **03:30 de la madrugada, desde
Argentina y por cuarenta veces la mediana**. Tras importarlo hay que ejecutar la corrida; el pedido
queda así:

| Regla | Peso | Qué midió la señal |
| --- | --- | --- |
| `amount_anomaly` | +40 | 39,6× la mediana del comercio sobre 20 pedidos previos |
| `foreign_country` | +20 | AR contra el habitual UY, en 20 de 20 pedidos previos |
| `unusual_hour` | +10 | Franja 00:00–06:00, hora de Montevideo, 0 de 20 previos |

Total **70**, que cae en la banda alta —60–69 media, 70–89 alta, 90–100 crítica—.
`new_buyer_high_value` **no** dispara, y eso es deliberado: `BUY_950001` ya tiene dos pedidos previos
en ese comercio. El motor guarda cada señal como campos con nombre, no como una frase, y la consola
compone el texto a partir de ellos.

## Valores admitidos que conviene no adivinar

- `currencyCode`: `UYU`, `BRL`, `USD`.
- `channel`: `WEB`, `MOBILE_APP`, `MARKETPLACE`. **No** `MOBILE`.
- `occurredAt`: ISO 8601 con desplazamiento explícito y precisión de milisegundos o menos.
- La raíz de un JSON **tiene que ser un array** de pedidos, con los mismos campos que el CSV.

El corpus de demostración de trescientos pedidos no se importa: vive como recurso embebido en
`backend/src/Salvo.Infrastructure/Seed/Fixtures/demo-orders.v2.json` y se carga con el botón «Cargar
corpus de demostración». Ese archivo tiene raíz de objeto y trae `isFraudLabel`, que el importador
**nunca** escribe: una importación no produce etiquetas de verdad de campo.

## Después de importar

Importar escribe pedidos y nada más. Para que aparezcan evaluaciones y alertas hay que **ejecutar la
corrida** desde la misma pantalla, o con
`curl -s -X POST http://127.0.0.1:5100/api/risk-evaluations:run`.

Los identificadores de estas muestras empiezan en `ORD_900001` y `ORD_950001` para no chocar con el
corpus demo, que llega a `ORD_000300`. La única colisión es deliberada: la fila 6 de
`import-con-errores.csv`.
