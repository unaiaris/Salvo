import type { AlertDetail } from "@/lib/api/contract";
import { formatInstant, statusLabel } from "@/lib/format";
import { ALERT_STATUS } from "@/lib/api/contract";
import { describeDivergence } from "./divergence";
import { ReviewForm } from "./review-form";

/**
 * The seam between the server and the browser.
 *
 * Everything the form needs is computed and worded here, on the server, and handed over as an id, a
 * boolean and a sentence. The `AlertDetail` object stops at this function.
 */
export function ReviewPanel({ detail }: { readonly detail: AlertDetail }) {
  if (detail.status !== ALERT_STATUS.open) {
    return <RecordedVerdict detail={detail} />;
  }

  const notice = describeDivergence(detail);

  return (
    <section
      aria-labelledby="review-title"
      className="flex flex-col gap-4 rounded-lg border border-slate-200 bg-white p-5"
    >
      <div>
        <h2 id="review-title" className="text-lg font-semibold text-slate-900">
          Emitir veredicto
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          El veredicto es definitivo: una alerta revisada no se reabre. Si el criterio cambia, hace
          falta una alerta nueva sobre el pedido.
        </p>
      </div>
      {notice.kind === "advisory" && (
        <p
          role="status"
          className="rounded-md border border-slate-300 bg-slate-50 p-3 text-sm leading-6 text-slate-800"
        >
          {notice.summary}
        </p>
      )}
      <ReviewForm
        alertId={detail.id}
        requiresAcknowledgement={notice.kind === "blocking"}
        divergenceSummary={notice.kind === "blocking" ? notice.summary : ""}
      />
    </section>
  );
}

function RecordedVerdict({ detail }: { readonly detail: AlertDetail }) {
  const { review } = detail;

  return (
    <section
      aria-labelledby="verdict-title"
      className="flex flex-col gap-3 rounded-lg border border-emerald-300 bg-emerald-50 p-5"
    >
      <h2 id="verdict-title" className="text-lg font-semibold text-emerald-950">
        Veredicto registrado: {statusLabel(detail.status)}
      </h2>
      {review === null ? (
        <p className="text-sm leading-6 text-emerald-900">
          La alerta está cerrada, pero no hay una entrada de auditoría asociada.
        </p>
      ) : (
        <>
          <p className="text-sm leading-6 text-emerald-900">
            Pasó de {statusLabel(review.previousStatus)} a {statusLabel(review.newStatus)} el{" "}
            {formatInstant(review.reviewedAt)}.
          </p>
          {review.note === null ? (
            <p className="text-sm text-emerald-900">La revisión se registró sin nota.</p>
          ) : (
            <div>
              <h3 className="text-sm font-semibold text-emerald-950">Nota registrada</h3>
              <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-emerald-900">
                {review.note}
              </p>
            </div>
          )}
        </>
      )}
      <p className="text-xs text-emerald-800">
        Un veredicto es terminal. Nada reabre una alerta revisada; una escalada crea una alerta nueva
        enlazada a esta.
      </p>
    </section>
  );
}
