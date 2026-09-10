import "server-only";

import { StartingUpNotice } from "@/components/starting-up-notice";
import type { Language } from "@/lib/api/contract";
import { isSharedInstance } from "@/lib/api/deployment";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatting } from "@/lib/format";

/**
 * How a failure looks on screen: the console's own sentence first, the API's `detail` underneath as
 * secondary information. Never "Error 409", and never the raw `detail` as the only text.
 *
 * <h3>La excepción del arranque en frío, y por qué vive acá</h3>
 *
 * En la instancia pública compartida, un fallo de transporte no siempre es una avería: puede ser la
 * visita que despertó a la instancia, con la API todavía arrancando. Ahí lo que corresponde no es un
 * mensaje de error sino {@link StartingUpNotice}, y la bifurcación está en este componente y no en
 * cada página **por el mismo motivo por el que el cartel de instancia compartida vive en el
 * layout**: una pantalla que se agregue mañana la hereda sin que nadie se acuerde, y si no la
 * heredara nada se vería mal hasta que alguien abriera el link un lunes a la mañana.
 *
 * Los `children` se descartan en ese camino, y es deliberado: el único que los usa es la sección de
 * calidad del dashboard, y lo que ofrece es un enlace para correr el scoring. Invitar a correr una
 * corrida contra una API que todavía no está lista es peor que no ofrecer nada.
 */
export function FailureNotice({
  failure,
  language,
  children,
}: {
  readonly failure: ApiFailure;
  readonly language: Language;
  readonly children?: React.ReactNode;
}) {
  if (isStartingUp(failure)) {
    return <StartingUpNotice language={language} />;
  }

  const message = describeFailure(failure, language);
  const technicalDetail = failure.kind === "problem" ? failure.detail : null;

  return (
    <section
      role="alert"
      aria-labelledby="failure-title"
      className="rounded-lg border border-rose-200 bg-rose-50 p-5 text-rose-950"
    >
      <h2 id="failure-title" className="text-base font-semibold">
        {message.title}
      </h2>
      <p className="mt-2 text-sm leading-6">{message.body}</p>
      <p className="mt-2 text-sm font-medium leading-6">{message.recovery}</p>
      {technicalDetail !== null && (
        <p className="mt-3 border-t border-rose-200 pt-3 text-xs text-rose-800">
          {formatting(language).t.common.technicalDetail(technicalDetail)}
        </p>
      )}
      {children}
    </section>
  );
}

/**
 * Si lo que hay que mostrar es la espera del arranque y no una avería.
 *
 * Fuera de la instancia compartida devuelve siempre `false`, y ese es el punto: en desarrollo, y en
 * el escenario 3 de `scripts/smoke-ui.sh` —que apaga la API a propósito para comprobar que la
 * consola lo dice—, un `timeout` es exactamente lo que la pantalla tiene que reportar.
 */
function isStartingUp(failure: ApiFailure): boolean {
  return (failure.kind === "timeout" || failure.kind === "unreachable") && isSharedInstance();
}
