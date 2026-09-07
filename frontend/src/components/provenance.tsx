import type { Language, ScoringRun } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * Every screen that shows data says which scoring run it is showing.
 *
 * The run is what makes an evaluation current, and it is not the same thing as the instant the
 * evaluation was computed: a run that finds an unchanged fingerprint reuses the stored row and
 * leaves its timestamp alone. Labelling the current state with `evaluatedAt` would present a
 * weeks-old moment as if it were now.
 */
export function scoringRunLabel(run: ScoringRun, language: Language): string {
  const f = formatting(language);

  return f.t.provenance.run(String(run.sequence), f.formatInstant(run.completedAt));
}

export function Provenance({
  run,
  language,
}: {
  readonly run: ScoringRun | null;
  readonly language: Language;
}) {
  const { t } = formatting(language);

  if (run === null) {
    return <p className="text-sm text-slate-600">{t.provenance.noRun}</p>;
  }

  return (
    <p className="text-sm text-slate-600">{t.provenance.since(scoringRunLabel(run, language))}</p>
  );
}
