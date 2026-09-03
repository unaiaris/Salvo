import { NoSeverityBadge, SeverityBadge } from "@/components/severity-badge";
import { scoringRunLabel } from "@/components/provenance";
import type { AlertEvaluation, AlertSignal, AlertSnapshot, ScoringRun } from "@/lib/api/contract";
import { formatInstant, ruleLabel } from "@/lib/format";

/**
 * The snapshot and the current evaluation, as two blocks that never blend.
 *
 * They describe different moments and each one carries its own provenance. Mixing the signals of one
 * with the score of the other is the mistake this layout exists to make impossible.
 */

function SignalList({ signals }: { readonly signals: readonly AlertSignal[] }) {
  if (signals.length === 0) {
    return <p className="text-sm text-slate-600">Esta evaluación no disparó ninguna regla.</p>;
  }

  return (
    <ul className="flex flex-col gap-2">
      {signals.map((signal) => (
        <li key={signal.rule} className="rounded-md border border-slate-200 bg-slate-50 p-3">
          <p className="flex items-baseline justify-between gap-3 text-sm font-medium text-slate-900">
            <span>{ruleLabel(signal.rule)}</span>
            <span className="tabular-nums text-slate-700">+{signal.weight}</span>
          </p>
          <p className="mt-1 text-xs leading-5 text-slate-600">{signal.detail}</p>
        </li>
      ))}
    </ul>
  );
}

export function SnapshotBlock({
  snapshot,
  createdAt,
}: {
  readonly snapshot: AlertSnapshot;
  readonly createdAt: string;
}) {
  return (
    <section
      aria-labelledby="snapshot-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-slate-300 bg-white p-5"
    >
      <div>
        <h2 id="snapshot-title" className="text-lg font-semibold text-slate-900">
          Snapshot que abrió la alerta
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          Congelado el {formatInstant(createdAt)}. No se reescribe nunca: es la premisa sobre la que
          se forma el veredicto.
        </p>
      </div>
      <p className="flex items-center gap-3">
        <SeverityBadge severity={snapshot.severity} />
        <span className="text-2xl font-semibold tabular-nums text-slate-900">{snapshot.score}</span>
      </p>
      <SignalList signals={snapshot.signals} />
    </section>
  );
}

export function CurrentEvaluationBlock({
  evaluation,
  currentRun,
}: {
  readonly evaluation: AlertEvaluation | null;
  readonly currentRun: ScoringRun | null;
}) {
  return (
    <section
      aria-labelledby="current-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-sky-300 bg-sky-50/40 p-5"
    >
      <div>
        <h2 id="current-title" className="text-lg font-semibold text-slate-900">
          Evaluación vigente
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {currentRun === null
            ? "El corpus no tiene ninguna corrida de scoring."
            : `Vigente desde la ${scoringRunLabel(currentRun)}.`}
        </p>
      </div>
      {evaluation === null ? (
        <p className="text-sm leading-6 text-slate-700">
          No hay evaluación vigente para este pedido. No es un score de cero: la corrida vigente no
          dejó ninguna evaluación asociada a este pedido.
        </p>
      ) : (
        <>
          <p className="flex items-center gap-3">
            {evaluation.severity === null ? (
              <NoSeverityBadge />
            ) : (
              <SeverityBadge severity={evaluation.severity} />
            )}
            <span className="text-2xl font-semibold tabular-nums text-slate-900">
              {evaluation.score}
            </span>
            <span className="text-sm text-slate-600">
              {evaluation.isFlagged ? "Marcada por el motor" : "Por debajo del umbral"}
            </span>
          </p>
          <p className="text-xs text-slate-600">
            Calculada por primera vez el {formatInstant(evaluation.evaluatedAt)}. Una corrida
            posterior que no encuentra cambios reutiliza esta misma evaluación y conserva su fecha,
            así que este instante no es el de la corrida vigente.
          </p>
          <SignalList signals={evaluation.signals} />
        </>
      )}
    </section>
  );
}
