import {
  EXTERNAL_STATUS,
  type AlertDetail,
  type AlertEvaluation,
  type AlertExternalEvaluation,
  type Language,
} from "@/lib/api/contract";
import { formatting, type Formatting } from "@/lib/format";
import { ExternalActions } from "./external-actions";

/**
 * The provider's opinion, as a third block beside the snapshot and the current evaluation.
 *
 * The two blocks that were here already describe two moments of the same criterion. This one
 * describes a different criterion altogether, so it never blends with them: no combined verdict, and
 * no arithmetic between the two scores. "Externo 45 contra local 60" means nothing — they are
 * different scales of different systems — so what is contrasted is the two verdicts, each labelled
 * with where it came from.
 */
export function ExternalEvaluationBlock({
  detail,
  triggerEnabled,
  language,
}: {
  readonly detail: AlertDetail;
  readonly triggerEnabled: boolean;
  readonly language: Language;
}) {
  const external = detail.externalEvaluation;
  const f = formatting(language);

  return (
    <section
      aria-labelledby="external-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-violet-300 bg-violet-50/40 p-5"
    >
      <div>
        <h2 id="external-title" className="text-lg font-semibold text-slate-900">
          {f.t.alertDetail.externalTitle}
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {external === null
            ? f.t.alertDetail.externalNever
            : f.t.alertDetail.externalProvider(f.providerLabel(external.provider))}
        </p>
      </div>

      {external !== null && <ExternalState external={external} f={f} />}
      {external !== null && (
        <Divergence external={external} current={detail.currentEvaluation} f={f} />
      )}
      {external?.hasContradictoryCallback === true && <Contradiction f={f} />}

      <ExternalActions
        orderId={detail.orderId}
        alertId={detail.id}
        externalEvaluationId={external === null ? "" : external.id}
        canRequest={external === null}
        canDeliver={triggerEnabled && external?.status === EXTERNAL_STATUS.pending}
        language={language}
      />
    </section>
  );
}

function ExternalState({
  external,
  f,
}: {
  readonly external: AlertExternalEvaluation;
  readonly f: Formatting;
}) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-lg font-semibold text-slate-900">
        {f.externalStatusLabel(external.status)}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        {f.t.alertDetail.externalRequestedAt(f.formatInstant(external.requestedAt))}
        {external.settledAt !== null && external.settledBy !== null
          ? f.t.alertDetail.externalSettledAt(
              f.formatInstant(external.settledAt),
              f.externalSourceLabel(external.settledBy),
            )
          : "."}
      </p>
      {external.score !== null && (
        <p className="text-xs leading-5 text-slate-600">
          {f.t.alertDetail.externalScore}{" "}
          <span className="tabular-nums">{external.score}</span>
          {f.t.alertDetail.externalScoreHint}
        </p>
      )}
      {external.errorCode !== null && (
        <p className="text-sm leading-6 text-slate-800">
          {f.externalErrorLabel(external.errorCode)}.
        </p>
      )}
      {external.lastErrorCode !== null && external.status === EXTERNAL_STATUS.pending && (
        <p className="text-sm leading-6 text-amber-900">
          {f.t.alertDetail.externalLastError(
            f.externalErrorLabel(external.lastErrorCode).toLowerCase(),
          )}
        </p>
      )}
    </div>
  );
}

/**
 * The two opinions side by side when they disagree, each with its provenance and neither combined
 * into a single answer.
 */
function Divergence({
  external,
  current,
  f,
}: {
  readonly external: AlertExternalEvaluation;
  readonly current: AlertEvaluation | null;
  readonly f: Formatting;
}) {
  if (current === null) {
    return null;
  }

  const externalVerdict = verdictOf(external.status);
  if (externalVerdict === null || externalVerdict === current.isFlagged) {
    return null;
  }

  return (
    <div
      role="note"
      className="rounded-md border border-violet-400 bg-white p-3 text-sm leading-6 text-slate-800"
    >
      <p className="font-semibold text-slate-900">{f.t.alertDetail.externalDisagreementTitle}</p>
      <ul className="mt-2 flex flex-col gap-1">
        <li>
          <span className="font-medium">{f.t.alertDetail.externalLocalLabel}</span>{" "}
          {current.isFlagged
            ? f.t.alertDetail.externalLocalFlagged
            : f.t.alertDetail.externalLocalNotFlagged}{" "}
          {f.t.alertDetail.externalLocalHint}
        </li>
        <li>
          <span className="font-medium">{f.t.alertDetail.externalProviderLabel}</span>{" "}
          {externalVerdict
            ? f.t.alertDetail.externalProviderDenied
            : f.t.alertDetail.externalProviderApproved}{" "}
          {external.settledBy === null
            ? ""
            : f.t.alertDetail.externalArrived(f.externalSourceLabel(external.settledBy))}
        </li>
      </ul>
      <p className="mt-2">{f.t.alertDetail.externalDisagreementHint}</p>
    </div>
  );
}

function Contradiction({ f }: { readonly f: Formatting }) {
  return (
    <p
      role="status"
      className="rounded-md border border-amber-400 bg-amber-50 p-3 text-sm leading-6 text-amber-950"
    >
      {f.t.alertDetail.externalContradiction}
    </p>
  );
}

/**
 * The external verdict as the same boolean the local one uses: denied is "this looks like fraud".
 * `PENDING` and `ERROR` are not verdicts and have nothing to disagree with.
 */
function verdictOf(status: string): boolean | null {
  if (status === EXTERNAL_STATUS.denied) {
    return true;
  }

  return status === EXTERNAL_STATUS.approved ? false : null;
}