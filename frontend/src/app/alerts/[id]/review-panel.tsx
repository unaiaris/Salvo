import { ALERT_STATUS, type AlertDetail, type Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
import { describeDivergence } from "./divergence";
import { ReviewForm } from "./review-form";

/**
 * The seam between the server and the browser.
 *
 * Everything the form needs is computed and worded here, on the server, and handed over as an id, a
 * boolean, a sentence and a language. The `AlertDetail` object stops at this function.
 */
export function ReviewPanel({
  detail,
  language,
}: {
  readonly detail: AlertDetail;
  readonly language: Language;
}) {
  const f = formatting(language);

  if (detail.status !== ALERT_STATUS.open) {
    return <RecordedVerdict detail={detail} language={language} />;
  }

  const notice = describeDivergence(detail, language);

  return (
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
        explanationId={detail.explanation?.id ?? ""}
        requiresAcknowledgement={notice.kind === "blocking"}
        divergenceSummary={notice.kind === "blocking" ? notice.summary : ""}
        language={language}
      />
    </section>
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
      aria-labelledby="verdict-title"
      className="flex flex-col gap-3 rounded-lg border border-emerald-300 bg-emerald-50 p-5"
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
