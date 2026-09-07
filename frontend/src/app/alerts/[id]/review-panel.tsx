import { ALERT_STATUS, type AlertDetail, type Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
import { describeDivergence, type DivergenceNotice } from "./divergence";
import { ReviewForm } from "./review-form";
import { VerdictFocus } from "./verdict-focus";

/** El bloque que reemplaza al formulario, nombrado una sola vez para que el foco no cite un literal. */
const VERDICT_PANEL_ID = "recorded-verdict";

/**
 * The seam between the server and the browser.
 *
 * Everything the form needs is computed and worded here, on the server, and handed over as an id, a
 * boolean, a sentence and a language. The `AlertDetail` object stops at this function.
 *
 * <h3>Por qué el panel tiene una forma estable y no dos ramas sueltas</h3>
 *
 * Emitir un veredicto reemplaza el formulario por el veredicto registrado, y esa sustitución ocurre
 * bajo alguien que puede no estar viendo la pantalla. Dos cosas hacen falta para que se entere, y
 * ninguna funciona si el árbol se rehace entero.
 *
 * La primera es la **región viva**, el `<p role="status">` de acá abajo. Una región viva solo se
 * anuncia si **ya existía** cuando su contenido cambió: insertar el aviso y su región en el mismo
 * commit no dispara nada en ningún lector. Por eso el párrafo se renderiza siempre —vacío y oculto
 * mientras no haya nada que decir— y ocupa la misma posición en los dos estados, de modo que React
 * le cambia el texto en lugar de reemplazarlo. De paso es el mismo lugar donde vive el aviso de
 * divergencia, que antes estaba dentro de la sección del formulario: una sola región viva por panel,
 * que dice lo que corresponda en cada momento.
 *
 * La segunda es el **foco**, y la resuelve `VerdictFocus`.
 */
export function ReviewPanel({
  detail,
  language,
}: {
  readonly detail: AlertDetail;
  readonly language: Language;
}) {
  const f = formatting(language);
  const recorded = detail.status !== ALERT_STATUS.open;
  const notice: DivergenceNotice = recorded
    ? { kind: "none" }
    : describeDivergence(detail, language);
  const advisory = notice.kind === "advisory" ? notice.summary : "";

  return (
    <>
      <p
        role="status"
        className={
          advisory === ""
            ? "sr-only"
            : "rounded-md border border-slate-300 bg-slate-50 p-3 text-sm leading-6 text-slate-800"
        }
      >
        {recorded ? f.t.alertDetail.verdictAnnounced : advisory}
      </p>
      <VerdictFocus recorded={recorded} targetId={VERDICT_PANEL_ID} />
      {recorded ? (
        <RecordedVerdict detail={detail} language={language} />
      ) : (
        <section
          aria-labelledby="review-title"
          className="flex flex-col gap-4 rounded-lg border border-slate-200 bg-white p-5"
        >
          <div>
            <h2 id="review-title" className="text-lg font-semibold text-slate-900">
              {f.t.alertDetail.reviewTitle}
            </h2>
            <p className="mt-1 text-sm text-slate-600">{f.t.alertDetail.reviewLead}</p>
          </div>
          <ReviewForm
            alertId={detail.id}
            explanationId={detail.explanation?.id ?? ""}
            requiresAcknowledgement={notice.kind === "blocking"}
            divergenceSummary={notice.kind === "blocking" ? notice.summary : ""}
            language={language}
          />
        </section>
      )}
    </>
  );
}

function RecordedVerdict({
  detail,
  language,
}: {
  readonly detail: AlertDetail;
  readonly language: Language;
}) {
  const { review } = detail;
  const f = formatting(language);
  const { t } = f;

  return (
    <section
      id={VERDICT_PANEL_ID}
      /*
        Enfocable solo por código: `-1` lo saca del orden de tabulación y lo deja disponible para el
        `focus()` de `VerdictFocus`. Sin esto, `focus()` sobre una sección no hace nada.
      */
      tabIndex={-1}
      aria-labelledby="verdict-title"
      className="flex flex-col gap-3 rounded-lg border border-emerald-300 bg-emerald-50 p-5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-900"
    >
      <h2 id="verdict-title" className="text-lg font-semibold text-emerald-950">
        {t.alertDetail.verdictTitle(f.statusLabel(detail.status))}
      </h2>
      {review === null ? (
        <p className="text-sm leading-6 text-emerald-900">{t.alertDetail.verdictNoAudit}</p>
      ) : (
        <>
          <p className="text-sm leading-6 text-emerald-900">
            {t.alertDetail.verdictTransition(
              f.statusLabel(review.previousStatus),
              f.statusLabel(review.newStatus),
              f.formatInstant(review.reviewedAt),
            )}
          </p>
          {review.note === null ? (
            <p className="text-sm text-emerald-900">{t.alertDetail.verdictNoNote}</p>
          ) : (
            <div>
              <h3 className="text-sm font-semibold text-emerald-950">
                {t.alertDetail.verdictNoteTitle}
              </h3>
              <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-emerald-900">
                {review.note}
              </p>
            </div>
          )}
        </>
      )}
      <p className="text-xs text-emerald-800">{t.alertDetail.verdictTerminal}</p>
    </section>
  );
}
