# Salvo — Recorrido con VoiceOver

> Para: Unai Arismendes · Escrito por: el coordinador · Fecha: 2026-09-07
> Duración estimada: 40 a 60 minutos
> No hace falta saber nada de accesibilidad para correrlo.

## Qué es esto y qué no es

Esto **no** comprueba que la consola cumpla una norma. Comprueba una sola cosa, que es la que
importa y la que ningún linter puede contestar: **si alguien que no ve la pantalla puede usar
Salvo.** Lo que salga es una lista de hallazgos; `E9C2` corrige los que vos elijas.

Un hallazgo no es «esto se escucha raro». Un hallazgo es **algo que no pudiste hacer, o algo que
hiciste sin haber entendido qué estabas haciendo.** Si dudás, anotalo igual: descartar es barato.

## Preparación

```bash
cd ~/Proyectos/Salvo && ./scripts/demo.sh
```

Eso levanta la API y la consola sobre una base nueva con el corpus sembrado y la corrida hecha.
Anotá el puerto que imprime.

**Además vas a necesitar el estado que preparó la fase 1**, y viene con sus pasos escritos en el
handoff: al menos una alerta **con explicación escrita** y, si se pudo producir, una **con aviso de
divergencia**. Una base recién sembrada no tiene ninguna de las dos, y sin ellas los pasos 11 y 12
no verifican nada. Si el handoff dice que la divergencia no se pudo producir, saltá el paso 11 y
anotalo como no verificado: eso es información honesta, no una falta tuya. Abrí la consola en Chrome — **no en Brave**, que es tu navegador de uso
personal y ya lo dejamos afuera para las capturas.

**VoiceOver se prende y se apaga con `Cmd + F5`.** La primera vez te va a ofrecer un tutorial: podés
saltearlo. Bajá el volumen antes, que arranca fuerte.

## Las seis teclas que necesitás

`VO` quiere decir **`Control + Option` apretadas juntas**. Es el modificador de VoiceOver.

| Tecla | Qué hace |
| --- | --- |
| `Cmd + F5` | Prende y apaga VoiceOver |
| `VO + A` | Lee desde donde está el cursor hacia abajo. Se corta con `Control` |
| `VO + →` / `VO + ←` | Mueve el cursor un elemento a la derecha o a la izquierda |
| `Tab` | Salta al siguiente control **operable**: enlace, botón, campo |
| `VO + U` | Abre el rotor. Con `→` y `←` cambiás de lista: encabezados, enlaces, controles. `Esc` cierra |
| `VO + Espacio` | Activa lo que está bajo el cursor, como un clic |

Si te perdés: `VO + Shift + Fn + ←` vuelve el cursor al principio de la página —un MacBook no
tiene tecla `Inicio`, la hace `Fn + ←`—. Si se traba, `Cmd + F5` dos veces.

## Cómo anotar

Un renglón por hallazgo, con este formato. No hace falta más:

```
[pantalla] [qué hiciste] → [qué escuchaste] → [qué esperabas]
```

Ejemplo real de cómo se ve uno bueno:

```
/alerts  Tab hasta la primera fila → "enlace, ORD_000011" → no dijo la severidad ni el score,
         así que no sé cuál abrir sin abrirlas todas
```

---

## Pantalla 1 — La entrada (`/`)

1. Abrí `/` y apretá `VO + A`. Dejalo leer entero.
   - **Deberías escuchar**, en algún orden razonable: «Salvo», «Consola antifraude», el título largo
     que empieza con «Salvo concentra el riesgo antifraude», y el enlace «Ir a la cola de alertas».
   - **Es hallazgo si**: nunca dice que hay una navegación, o el aviso «Datos sintéticos · sin
     autenticación · uso local» no se lee. Ese aviso importa: es lo que le dice a alguien que esto
     no son datos reales.

2. `VO + U`, movete hasta la lista de **encabezados** con `→`.
   - **Deberías escuchar** un encabezado: el título de la página. Que haya uno solo acá **es
     correcto** —la cabecera no lleva encabezado y la navegación se anuncia por su etiqueta—, así
     que no lo anotes.
   - **Es hallazgo si**: la lista está vacía, o el encabezado que hay no describe la página.

3. `Esc`, después `Tab` cuatro o cinco veces.
   - **Deberías escuchar**, en este orden: el enlace de marca «Salvo», después «Alertas»,
     «Importación», «Dashboard», y por último «Ir a la cola de alertas».
   - **Es hallazgo si**: algún `Tab` cae en algo que no se anuncia, o si el foco se va a la barra
     del navegador antes de recorrer la página.

## Pantalla 2 — La cola de alertas (`/alerts`)

Es la pantalla que una analista usa todo el día. Si esta no funciona, no funciona nada.

4. Entrá y apretá `VO + A`.
   - **Deberías escuchar** «Cola de alertas» y, poco después, cuántas alertas abiertas hay.
   - **Es hallazgo si**: la cantidad no se dice, o se dice antes que el título y no se entiende a
     qué se refiere.

5. `VO + U` → lista de **encabezados**. ¿Está «Cola de alertas»?

6. Ahora la tabla. Movete con `VO + →` hasta entrar en ella.
   - **Deberías escuchar** que es una tabla, y su descripción: «Alertas abiertas, de mayor a menor
     score local de la evaluación vigente».
   - **Es hallazgo si**: entra en la tabla sin decir que es una tabla, o no dice cuántas filas tiene.

7. Recorré **una fila entera** con `VO + →`, celda por celda.
   - **Deberías escuchar**, para cada celda, **el nombre de la columna y después el valor**: por
     ejemplo «Severidad del snapshot, CRÍTICA» y «Score vigente, 90».
   - **Es hallazgo si**: dice el valor suelto, sin el nombre de la columna. Las columnas son seis
     —Pedido, Severidad del snapshot, Score del snapshot, Score vigente, Monto y Ocurrió—, así que
     «90» sin más no significa nada: hay dos columnas de score.
   - **Es hallazgo si**: la severidad se anuncia solo como un color o no se anuncia. Tiene que decir
     una palabra: CRÍTICA, ALTA, MEDIA.

8. `Tab` hasta la primera fila operable y activá con `Return`.
   - **Es hallazgo si**: no queda claro qué pedido estás abriendo antes de abrirlo.

## Pantalla 3 — El detalle de una alerta (`/alerts/[id]`)

La pantalla más densa del producto. Tomate el tiempo acá.

9. `VO + U` → **encabezados**. Anotá los que escuchás.
   - **Deberías escuchar** algo como: el pedido, la evaluación del snapshot, la evaluación vigente,
     la evaluación externa, la explicación, y «Emitir veredicto».
   - **Es hallazgo si**: falta alguno, o dos secciones distintas tienen el mismo nombre. Con el mismo
     nombre no se puede saltar de una a otra.

10. `Esc` y `VO + A` desde arriba. Escuchá **el bloque de señales**.
    - **Deberías escuchar**, por cada señal, su nombre —«Monto atípico», «Ráfaga de pedidos», «Hora
      inusual»— seguido del peso y de la frase completa que la explica.
    - **Es hallazgo si**: el peso se lee como «más cuarenta» sin decir de qué, o la frase se corta.

11. **El aviso de divergencia.** Abrí la alerta que el handoff de la fase 1 señale como la que lo
    tiene, y escuchá la frase entera: empieza con «La evaluación del pedido cambió» o «La alerta se
    abrió en».
    - **Es hallazgo si**: se lee como texto suelto en el medio de otra cosa, sin nada que indique
      que es un aviso. Esa frase cambia lo que la analista está por hacer.
    - **Si el handoff dice que no se pudo producir, saltá este paso y anotá «divergencia: no
      verificada».** No intentes provocarla importando: el corpus termina el 28 de agosto y la única
      fila válida de la muestra es del 29, para un comprador sin historia en ese comercio, así que
      no cambia ningún baseline y ninguna evaluación existente se mueve.

12. El bloque de **explicación**. `Tab` hasta el botón que la pide y activalo.
    - **Deberías escuchar** que algo pasó, y después el párrafo redactado, **sin tener que ir a
      buscarlo**.
    - **Es hallazgo si**: la explicación aparece y VoiceOver no dice nada. Es el mismo modo de falla
      del paso 15 y en la misma pantalla.
    - Los avisos de «desactualizada» y «escrita por otra plantilla» **no se pueden provocar sobre
      una base nueva**: hacen falta dos versiones vivas. Quedan **no verificados** y así se anota.

13. Ahora el **formulario de veredicto**. `Tab` hasta él.
    - **Deberías escuchar** «Veredicto» como nombre del grupo, y después las dos opciones:
      «Confirmar segura» con su aclaración «El pedido no es fraude», y «Reportar fraude» con la suya.
    - **Es hallazgo si**: las opciones se anuncian sin su aclaración. La diferencia entre las dos es
      todo el producto.
    - **Es hallazgo si**: en ningún momento se anuncia que **el veredicto es definitivo y la alerta
      no se reabre**. Eso está escrito en la pantalla; la pregunta es si llega al oído antes del
      botón, no después.

14. Elegí «Confirmar segura», `Tab` hasta el campo de nota, escribí cualquier cosa, y `Tab` hasta el
    botón de enviar. **Antes de apretar**, `VO + A`.
    - **Es hallazgo si**: no podés reconstruir de oído qué estás por hacer y sobre qué pedido.

15. Enviá.
    - **Deberías escuchar** el resultado **sin tener que ir a buscarlo**.
    - **Es hallazgo si**: la página cambia y VoiceOver no dice nada, o el foco queda en un botón que
      ya no sirve. Este es el hallazgo más probable de todo el recorrido y el más importante.

## Pantalla 4 — El dashboard (`/dashboard`)

16. `VO + U` → **encabezados**. Deberían estar los paneles: el de calidad, el de señales más
    frecuentes, el de denegados por el proveedor.
17. Entrá al panel de **denegados por el proveedor sin alerta local** y recorré su tabla como en el
    paso 7.
    - **Es hallazgo si**: la tabla no dice qué es. Ese panel es el argumento más importante del
      proyecto y sin contexto son números sueltos.
18. Escuchá las cifras de calidad —precisión, recall, F1, la matriz—.
    - **Es hallazgo si**: un número se lee sin decir de qué es. «Cero coma seis tres dos» solo no es
      nada.

## Pantalla 5 — Importación, y el aviso que aparece (`/import`)

19. `VO + A` desde arriba.
    - **Deberías escuchar** «Importación y scoring» y el estado del corpus.
20. Importá `docs/muestras/import-con-errores.csv`, que trae filas rechazadas a propósito.
    - **Deberías escuchar**, al terminar, cuántos registros se rechazaron **sin ir a buscarlo**.
    - **Es hallazgo si**: la pantalla se llena de errores y VoiceOver sigue callado.
21. Recorré la lista de rechazos con `VO + →`.
    - **Deberías escuchar**, por cada uno, el número de registro, la línea, el campo y el motivo en
      castellano. Por ejemplo: «Registro 6, línea 7 · campo `merchantReferenceId` · La referencia ya
      existe con otros datos», seguido del **detalle técnico de la API en inglés**.
    - **Ese fragmento en inglés al final es deliberado** y está documentado: es el mensaje crudo de
      la API, que se conserva a propósito. **No lo anotes como hallazgo.**
    - **Es hallazgo si**: el motivo suena a código —`REFERENCE_CONFLICT` leído tal cual— en vez de a
      frase, o si no se dice de qué registro se trata.
22. Ejecutá una corrida de scoring desde esta pantalla.
    - **Es hallazgo si**: no se anuncia que terminó, ni qué cambió.

---

## Cuando termines

Apagá VoiceOver con `Cmd + F5` y pasame la lista tal como te quedó, sin ordenar ni limpiar. Yo la
convierto en hallazgos con severidad y decidimos juntos cuáles entran en `E9C2`.

**Si no encontrás nada en alguna pantalla, decilo.** «La pantalla 4 no me dio ningún hallazgo» es
información: quiere decir que el dashboard está bien y que no hace falta tocarlo.
