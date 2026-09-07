import { NoSeverityBadge, SeverityBadge } from "@/components/severity-badge";
import { scoringRunLabel } from "@/components/provenance";
import type {
  AlertEvaluation,
  AlertSignal,
  AlertSnapshot,
  Language,
  ScoringRun,
} from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * The snapshot and the current evaluation, as two blocks that never blend.
 *
 * They describe different moments and each one carries its own provenance. Mixing the signals of one
 * with the score of the other is the mistake this layout exists to make impossible.
 */

function SignalList({
  signals,
  language,
}: {
  readonly signals: readonly AlertSignal[];
  readonly language: Language;
}) {
  const f = formatting(language);

  if (signals.length === 0) {
    return <p className="text-sm text-slate-600">{f.t.alertDetail.noSignals}</p>;
  }

  return (
    <ul className="flex flex-col gap-2">
      {signals.map((signal) => (
        <li key={signal.rule} className="rounded-md border border-slate-200 bg-slate-50 p-3">
          <p className="flex items-baseline justify-between gap-3 text-sm font-medium text-slate-900">
            <span>{f.ruleLabel(signal.rule)}</span>
            <span className="tabular-nums text-slate-700">+{signal.weight}</span>
          </p>
          <p className="mt-1 text-xs leading-5 text-slate-600">{f.signalSentence(signal)}</p>
        </li>
      ))}
    </ul>
  );
}

export function SnapshotBlock({
  snapshot,
  createdAt,
  language,
}: {
  readonly snapshot: AlertSnapshot;
  readonly createdAt: string;
  readonly language: Language;
}) {
  const f = formatting(language);

  return (
    <section
      aria-labelledby="snapshot-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-slate-300 bg-white p-5"
    >
      <div>
        <h2 id="snapshot-title" className="text-lg font-semibold text-slate-900">
          {f.t.alertDetail.snapshotTitle}
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {f.t.alertDetail.snapshotHint(f.formatInstant(createdAt))}
        </p>
      </div>
      <p className="flex items-center gap-3">
        <SeverityBadge severity={snapshot.severity} language={language} />
        <span className="text-2xl font-semibold tabular-nums text-slate-900">{snapshot.score}</span>
      </p>
      <SignalList signals={snapshot.signals} language={language} />
    </section>
  );
}

export function CurrentEvaluationBlock({
  evaluation,
  currentRun,
  language,
}: {
  readonly evaluation: AlertEvaluation | null;
  readonly currentRun: ScoringRun | null;
  readonly language: Language;
}) {
  const f = formatting(language);
  const { t } = f;

  return (
    <section
      aria-labelledby="current-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-sky-300 bg-sky-50/40 p-5"
    >
      <div>
        <h2 id="current-title" className="text-lg font-semibold text-slate-900">
          {t.alertDetail.currentTitle}
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {currentRun === null
            ? t.alertDetail.currentNoRun
            : t.provenance.since(scoringRunLabel(currentRun, language))}
        </p>
      </div>
      {evaluation === null ? (
        <p className="text-sm leading-6 text-slate-700">{t.alertDetail.currentAbsent}</p>
      ) : (
        <>
          <p className="flex items-center gap-3">
            {evaluation.severity === null ? (
              <NoSeverityBadge language={language} />
            ) : (
              <SeverityBadge severity={evaluation.severity} language={language} />
            )}
            <span className="text-2xl font-semibold tabular-nums text-slate-900">
              {evaluation.score}
            </span>
            <span className="text-sm text-slate-600">
              {evaluation.isFlagged
                ? t.alertDetail.currentFlagged
                : t.alertDetail.currentBelowThreshold}
            </span>
          </p>
          <p className="text-xs text-slate-600">
            {t.alertDetail.currentEvaluatedAt(f.formatInstant(evaluation.evaluatedAt))}
          </p>
          <SignalList signals={evaluation.signals} language={language} />
        </>
      )}
    </section>
  );
}
