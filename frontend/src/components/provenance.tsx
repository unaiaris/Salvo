import type { ScoringRun } from "@/lib/api/contract";
import { formatInstant } from "@/lib/format";

/**
 * Every screen that shows data says which scoring run it is showing.
 *
 * The run is what makes an evaluation current, and it is not the same thing as the instant the
 * evaluation was computed: a run that finds an unchanged fingerprint reuses the stored row and
 * leaves its timestamp alone. Labelling the current state with `evaluatedAt` would present a
 * weeks-old moment as if it were now.
 */
export function scoringRunLabel(run: ScoringRun): string {
  return `corrida #${String(run.sequence)}, ${formatInstant(run.completedAt)}`;
}

export function Provenance({ run }: { readonly run: ScoringRun | null }) {
  if (run === null) {
    return (
      <p className="text-sm text-slate-600">
        El corpus todavía no se puntuó: no hay ninguna corrida de scoring.
      </p>
    );
  }

  return <p className="text-sm text-slate-600">Vigente desde la {scoringRunLabel(run)}.</p>;
}
