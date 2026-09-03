import type { AlertDetail } from "@/lib/api/contract";
import { severityLabel } from "@/lib/format";

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

export function describeDivergence(detail: AlertDetail): DivergenceNotice {
  const { divergence, snapshot, currentEvaluation } = detail;

  if (divergence.hasBandDivergence) {
    return { kind: "blocking", summary: bandSummary(detail) };
  }

  if (currentEvaluation === null || currentEvaluation.evaluationId === snapshot.evaluationId) {
    return { kind: "none" };
  }

  return {
    kind: "advisory",
    summary:
      `La evaluación del pedido cambió (${String(snapshot.score)} → ` +
      `${String(currentEvaluation.score)}) sin cambiar de banda. Las señales vigentes están en el ` +
      "bloque «Evaluación vigente».",
  };
}

function bandSummary(detail: AlertDetail): string {
  const { divergence } = detail;
  const from = `${severityLabel(divergence.snapshotSeverity)} con score ${String(divergence.snapshotScore)}`;

  if (divergence.currentScore === null) {
    return (
      `La alerta se abrió en ${from}, pero el pedido ya no tiene evaluación vigente, así que no hay ` +
      "nada con qué comparar el snapshot."
    );
  }

  const to =
    divergence.currentSeverity === null
      ? `score ${String(divergence.currentScore)}, por debajo del umbral de alerta y sin banda`
      : `${severityLabel(divergence.currentSeverity)} con score ${String(divergence.currentScore)}`;

  return `La alerta se abrió en ${from}. La evaluación vigente está en ${to}.`;
}
