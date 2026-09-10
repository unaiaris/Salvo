import type { Language } from "@/lib/api/contract";
import { COLD_START_SECONDS } from "@/lib/api/deployment";
import { formatting } from "@/lib/format";

/**
 * Lo que ve quien llega a la instancia compartida mientras la API todavía está arrancando.
 *
 * <h3>Por qué esto no es un `FailureNotice` de otro color</h3>
 *
 * Porque no falló nada. La instancia se apaga sola cuando pasa un rato sin visitas —es el plan
 * gratuito, y dormirse es parte del diseño— y volver a levantarla lleva lo que lleva arrancar dos
 * procesos con 0,1 de un núcleo. Decirle a alguien «la API tardó demasiado en responder» cuando lo
 * que pasa es que su visita despertó la instancia es describir un comportamiento normal con el
 * vocabulario de una avería.
 *
 * La diferencia se nota en el marcado y no solo en el texto: esto es un `role="status"` con
 * `aria-live="polite"`, no un `role="alert"`. Un lector de pantalla anuncia una alerta
 * interrumpiendo lo que esté diciendo, y esto no merece interrumpir a nadie.
 *
 * <h3>Y por qué solo en la instancia compartida</h3>
 *
 * Fuera de ella —en desarrollo, y en el escenario 3 de `scripts/smoke-ui.sh`, que apaga la API a
 * propósito— una API que no contesta **es** una avería, y el mensaje de siempre es el correcto.
 * Quien corre la consola en su máquina necesita que se lo digan; quien abre un link no.
 *
 * <h3>El reintento, y por qué no es un `meta refresh`</h3>
 *
 * El camino común no llega hasta acá: `fetchWithColdStartGrace` reintenta del lado del servidor
 * mientras la API termina de arrancar, así que la visita normal recibe la página con los datos
 * puestos y nunca ve esta pantalla. Esta es la salida del caso en que ese presupuesto se agota.
 *
 * Llegado ese punto el reintento es un botón, y es un formulario sin `action` —que reemite la misma
 * petición GET— para que funcione sin una línea de JavaScript de cliente, que es lo que
 * `boundary.test.ts` exige de todo lo que el dashboard pinta.
 *
 * **Un `<meta http-equiv="refresh">` habría sido más cómodo y se descartó, con la comprobación
 * hecha.** `axe-core` lo marca como violación de `meta-refresh` (WCAG 2.2.1) cuando se lo corre
 * sobre el documento. El detalle incómodo es que la comprobación de este proyecto corre sobre el
 * contenedor, y React iza los `<meta>` al `<head>`: el test **habría pasado en verde** sin ver
 * nunca la violación. Aprovechar eso es exactamente lo que `src/test/axe.ts` llama una comprobación
 * silenciosamente incompleta, así que la opción se descartó por lo que es y no por lo que el test
 * alcanza a ver.
 */
export function StartingUpNotice({ language }: { readonly language: Language }) {
  const { t } = formatting(language);
  const notice = t.startingUp;

  return (
    <section
      role="status"
      aria-live="polite"
      aria-labelledby="starting-up-title"
      className="rounded-lg border border-sky-200 bg-sky-50 p-5 text-sky-950"
    >
      <h2 id="starting-up-title" className="text-base font-semibold">
        {notice.title}
      </h2>
      <p className="mt-2 text-sm leading-6">{notice.body(String(COLD_START_SECONDS))}</p>
      <p className="mt-2 text-sm font-medium leading-6">{notice.reassurance}</p>
      {/*
        Sin `action`: un formulario que no la declara reemite un GET contra la URL actual, así que
        el botón sirve en cualquiera de las rutas de datos sin que ninguna tenga que decirle dónde
        está. Y sin `method`, que por omisión ya es GET: una recarga no muta nada, y mandarla como
        POST la haría contar contra el cubo de mutaciones del limitador de tasa.
      */}
      <form className="mt-4">
        <button
          type="submit"
          className="rounded-md border border-sky-300 bg-white px-3 py-2 text-sm font-medium text-sky-900 hover:bg-sky-100"
        >
          {notice.retryNow}
        </button>
      </form>
    </section>
  );
}
