import type { AlertDetail, AlertSignal, Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * How far the corpus has moved from the premise the alert was opened on, and how loudly the console
 * has to say it.
 *
 * Three cases, not two. A band change is what the API itself refuses to accept a verdict without —
 * it is blocking here for the same reason. An evaluation that changed *inside* the band is not
 * refused by the API, so the console does not refuse it either: being stricter than the contract
 * would invent a `200` no other client could ever reach. But the signals underneath did change, and
 * saying nothing would leave the analyst reading a snapshot that no longer describes the order.
 */
export type DivergenceNotice =
  | { readonly kind: "none" }
  | { readonly kind: "blocking"; readonly summary: string }
  | { readonly kind: "advisory"; readonly summary: string };

export function describeDivergence(detail: AlertDetail, language: Language): DivergenceNotice {
  const { divergence, snapshot, currentEvaluation } = detail;
  const { t } = formatting(language);

  if (divergence.hasBandDivergence) {
    return { kind: "blocking", summary: bandSummary(detail, language) };
  }

  if (currentEvaluation === null || !hasMoved(snapshot, currentEvaluation)) {
    return { kind: "none" };
  }

  return {
    kind: "advisory",
    summary: t.alertDetail.divergenceAdvisory(
      String(snapshot.score),
      String(currentEvaluation.score),
    ),
  };
}

/**
 * Whether the current evaluation says something different about the risk than the snapshot does.
 *
 * It compares the score and the rules with their weights, and deliberately **not** the `detail` of
 * each signal. The identifier is not compared either, and that is the whole point of this function:
 * an evaluation is a new row whenever the rule configuration version changes, so comparing
 * identifiers made every alert in the database announce «la evaluación cambió (60 → 60)» the day
 * the engine was versioned — a change of wording reported as a change of judgement, on every alert
 * at once, which is the fastest way to teach an analyst to ignore the notice.
 *
 * What the notice exists for is a corpus that moved under the alert: a retroactive import that
 * completed a buyer's history and changed the score, or made a rule fire or stop firing. Those all
 * show up here. A rule configuration that only rewrites how a signal is worded does not, and should
 * not.
 */
function hasMoved(
  snapshot: { readonly score: number; readonly signals: readonly AlertSignal[] },
  current: { readonly score: number; readonly signals: readonly AlertSignal[] },
): boolean {
  if (snapshot.score !== current.score) {
    return true;
  }

  if (snapshot.signals.length !== current.signals.length) {
    return true;
  }

  return snapshot.signals.some(
    (signal, index) =>
      signal.rule !== current.signals[index]?.rule
      || signal.weight !== current.signals[index]?.weight,
  );
}

function bandSummary(detail: AlertDetail, language: Language): string {
  const { divergence } = detail;
  const f = formatting(language);
  const { t } = f;
  const from = t.alertDetail.divergenceOpenedAt(
    f.severityLabel(divergence.snapshotSeverity),
    String(divergence.snapshotScore),
  );

  if (divergence.currentScore === null) {
    return t.alertDetail.divergenceNoCurrent(from);
  }

  const to =
    divergence.currentSeverity === null
      ? t.alertDetail.divergenceBandless(String(divergence.currentScore))
      : t.alertDetail.divergenceOpenedAt(
          f.severityLabel(divergence.currentSeverity),
          String(divergence.currentScore),
        );

  return t.alertDetail.divergenceBand(from, to);
}
