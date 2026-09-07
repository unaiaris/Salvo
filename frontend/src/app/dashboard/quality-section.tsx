import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import type {
  EvaluationMetrics,
  Language,
  MetricsFigures,
  ThresholdMetrics,
} from "@/lib/api/contract";
import type { ApiResult } from "@/lib/api/failures";
import { formatting, type Formatting } from "@/lib/format";

/**
 * The quality of the detection criterion — a different surface from the operational dashboard, and
 * deliberately so.
 *
 * Decision 37: outside a demo corpus, ground truth does not exist. A merchant knows what its analyst
 * decided and, months later, what ended in a chargeback; it does not know which orders really were
 * fraud. Everything above this section is built from the deterministic evaluations and the analyst's
 * verdicts, and reads no label by any path. This section reads labels, which is why it only exists
 * when the API declares the deployment a demo.
 */

/**
 * The caveat that has to be on screen whenever the figures are, and that nothing can collapse or
 * dismiss, lives in the dictionaries under `dashboard.qualityCaveat`.
 *
 * An F1 with no such caveat reads as an overfitted fixture and quietly discredits everything around
 * it. Stated, it reads as what it is: proof that the evaluation pipeline is honest — temporal
 * split, holdout with no retuning, arithmetic that checks out — not proof that the rules would
 * generalise to a corpus they were not built alongside.
 */
export function QualitySection({
  metrics,
  language,
}: {
  readonly metrics: ApiResult<EvaluationMetrics>;
  readonly language: Language;
}) {
  const f = formatting(language);

  return (
    <section
      aria-labelledby="quality-title"
      className="flex flex-col gap-5 rounded-lg border border-slate-300 bg-slate-100 p-6"
    >
      <div className="flex flex-col gap-2">
        <h2 id="quality-title" className="text-lg font-semibold text-slate-900">
          {f.t.dashboard.qualityTitle}
        </h2>
        <p className="max-w-3xl text-sm leading-6 text-slate-700">{f.t.dashboard.qualityLead}</p>
        {/*
          Not a dismissible banner and not an accordion: it is part of the figures, so it is rendered
          before them and cannot be closed.
        */}
        <p className="max-w-3xl rounded-md border-l-4 border-amber-500 bg-amber-50 p-4 text-sm leading-6 text-amber-950">
          {f.t.dashboard.qualityCaveat}
        </p>
      </div>

      {metrics.ok ? (
        <QualityFigures metrics={metrics.value} f={f} />
      ) : (
        <FailureNotice failure={metrics.failure} language={language}>
          <p className="mt-3 text-sm">
            <Link
              href="/import"
              className="font-semibold underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-900"
            >
              {f.t.common.runScoring}
            </Link>
          </p>
        </FailureNotice>
      )}
    </section>
  );
}

function QualityFigures({
  metrics,
  f,
}: {
  readonly metrics: EvaluationMetrics;
  readonly f: Formatting;
}) {
  const { t } = f;

  return (
    <div className="flex flex-col gap-6">
      <dl className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Figure term={t.dashboard.qualityScoredOrders} value={f.formatCount(metrics.scoredOrders)} />
        <Figure term={t.dashboard.qualityLabeled} value={f.formatCount(metrics.labeledOrders)} />
        <Figure
          term={t.dashboard.qualityUnlabeled}
          value={f.formatCount(metrics.unlabeledOrders)}
          hint={t.dashboard.qualityUnlabeledHint}
        />
        <Figure term={t.dashboard.qualityRuleConfig} value={metrics.ruleConfigVersion} />
      </dl>

      <div className="grid gap-6 lg:grid-cols-2">
        <FiguresBlock
          title={t.dashboard.qualityHoldoutTitle(f.formatCount(metrics.holdoutOrders))}
          description={t.dashboard.qualityHoldoutHint(
            f.formatCount(metrics.selectedThreshold.threshold),
          )}
          figures={metrics.holdout}
          f={f}
        />
        <FiguresBlock
          title={t.dashboard.qualityCalibrationTitle(f.formatCount(metrics.calibrationOrders))}
          description={t.dashboard.qualityCalibrationHint}
          figures={metrics.selectedThreshold.metrics}
          f={f}
        />
      </div>

      <ThresholdSweep
        sweep={metrics.calibrationSweep}
        selected={metrics.selectedThreshold.threshold}
        f={f}
      />

      <p className="text-xs text-slate-600">
        {t.dashboard.qualityRunFootnote(
          f.formatCount(metrics.scoringRunSequence),
          f.formatInstant(metrics.scoringRunCompletedAt),
        )}
      </p>
    </div>
  );
}

function Figure({
  term,
  value,
  hint,
}: {
  readonly term: string;
  readonly value: string;
  readonly hint?: string;
}) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-slate-600">{term}</dt>
      <dd className="mt-1 text-lg font-semibold tabular-nums text-slate-950">{value}</dd>
      {hint !== undefined && <p className="mt-1 text-xs leading-5 text-slate-600">{hint}</p>}
    </div>
  );
}

/** A ratio that may legitimately not exist: precision has no value when nothing was flagged. */
function ratio(value: number | null, f: Formatting): string {
  return value === null ? f.t.dashboard.qualityUndefined : f.formatPercent(value);
}

function FiguresBlock({
  title,
  description,
  figures,
  f,
}: {
  readonly title: string;
  readonly description: string;
  readonly figures: MetricsFigures;
  readonly f: Formatting;
}) {
  const { matrix } = figures;
  const { t } = f;

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5">
      <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
      <p className="mt-1 text-xs leading-5 text-slate-600">{description}</p>

      <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <Pair term={t.dashboard.qualityPrecision} value={ratio(figures.precision, f)} />
        <Pair term={t.dashboard.qualityRecall} value={ratio(figures.recall, f)} />
        <Pair term={t.dashboard.qualityF1} value={ratio(figures.f1, f)} />
        <Pair
          term={t.dashboard.qualityFalsePositiveRate}
          value={ratio(figures.falsePositiveRate, f)}
        />
      </dl>

      <table className="mt-4 w-full border-collapse text-sm">
        <caption className="pb-1 text-left text-xs text-slate-600">
          {t.dashboard.qualityMatrixCaption}
        </caption>
        <tbody>
          <tr className="border-b border-slate-100">
            <th scope="row" className="py-1 text-left font-normal text-slate-700">
              {t.dashboard.qualityTruePositives}
            </th>
            <td className="py-1 text-right tabular-nums">{f.formatCount(matrix.truePositives)}</td>
            <th scope="row" className="py-1 pl-4 text-left font-normal text-slate-700">
              {t.dashboard.qualityFalsePositives}
            </th>
            <td className="py-1 text-right tabular-nums">{f.formatCount(matrix.falsePositives)}</td>
          </tr>
          <tr>
            <th scope="row" className="py-1 text-left font-normal text-slate-700">
              {t.dashboard.qualityFalseNegatives}
            </th>
            <td className="py-1 text-right tabular-nums">{f.formatCount(matrix.falseNegatives)}</td>
            <th scope="row" className="py-1 pl-4 text-left font-normal text-slate-700">
              {t.dashboard.qualityTrueNegatives}
            </th>
            <td className="py-1 text-right tabular-nums">{f.formatCount(matrix.trueNegatives)}</td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}

function Pair({ term, value }: { readonly term: string; readonly value: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-600">{term}</dt>
      <dd className="font-semibold tabular-nums text-slate-950">{value}</dd>
    </div>
  );
}

/**
 * The threshold sweep, folded away.
 *
 * The API already collapses it to the thresholds where the confusion matrix actually changes, so
 * what is left is worth reading — but it is evidence for a decision that was already made, not
 * something an analyst acts on, so it opens on demand. `<details>` does that with no JavaScript and
 * therefore without turning this into a client component.
 */
function ThresholdSweep({
  sweep,
  selected,
  f,
}: {
  readonly sweep: readonly ThresholdMetrics[];
  readonly selected: number;
  readonly f: Formatting;
}) {
  const { t } = f;

  return (
    <details className="rounded-lg border border-slate-200 bg-white p-4">
      <summary className="cursor-pointer text-sm font-semibold text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900">
        {t.dashboard.qualitySweepSummary(f.formatCount(sweep.length))}
      </summary>
      <p className="mt-2 text-xs leading-5 text-slate-600">
        {t.dashboard.qualitySweepHint(f.formatCount(selected))}
      </p>
      <div className="mt-3 overflow-x-auto">
        <table className="w-full min-w-[30rem] border-collapse text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-xs uppercase tracking-wide text-slate-600">
              <th scope="col" className="py-2 pr-4 font-semibold">
                {t.dashboard.qualitySweepThreshold}
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                {t.dashboard.qualityPrecision}
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                {t.dashboard.qualityRecall}
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                {t.dashboard.qualityF1}
              </th>
              <th scope="col" className="py-2 text-right font-semibold">
                {t.dashboard.qualityFalsePositives}
              </th>
            </tr>
          </thead>
          <tbody>
            {sweep.map((point) => (
              <tr
                key={point.threshold}
                className={
                  point.threshold === selected
                    ? "border-b border-slate-100 bg-amber-50 font-semibold"
                    : "border-b border-slate-100"
                }
              >
                <th scope="row" className="py-1.5 pr-4 text-left font-normal text-slate-700">
                  {f.formatCount(point.threshold)}
                  {point.threshold === selected && t.dashboard.qualitySweepChosen}
                </th>
                <td className="py-1.5 pr-4 text-right tabular-nums">
                  {ratio(point.metrics.precision, f)}
                </td>
                <td className="py-1.5 pr-4 text-right tabular-nums">
                  {ratio(point.metrics.recall, f)}
                </td>
                <td className="py-1.5 pr-4 text-right tabular-nums">
                  {ratio(point.metrics.f1, f)}
                </td>
                <td className="py-1.5 text-right tabular-nums">
                  {ratio(point.metrics.falsePositiveRate, f)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}
