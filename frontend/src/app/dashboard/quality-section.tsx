import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import type { EvaluationMetrics, MetricsFigures, ThresholdMetrics } from "@/lib/api/contract";
import type { ApiResult } from "@/lib/api/failures";
import { formatCount, formatInstant, formatPercent } from "@/lib/format";

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
 * The sentence that has to be on screen whenever the figures are, and that nothing can collapse or
 * dismiss.
 *
 * An F1 of 1,00 with no such caveat reads as an overfitted fixture and quietly discredits everything
 * around it. Stated, it reads as what it is: proof that the evaluation pipeline is honest — temporal
 * split, holdout with no retuning, arithmetic that checks out — not proof that the rules would
 * generalise to a corpus they were not built alongside. Enriching the fixture with harder cases is
 * scheduled work for stage 9; until then the caveat is what carries the truth.
 */
export const FIXTURE_CAVEAT =
  "La fixture demo fue construida para que las reglas recuperen sus propias etiquetas. Estas "
  + "métricas prueban el pipeline de evaluación —división temporal, holdout sin retuning, cálculo "
  + "correcto—, no la calidad del criterio de detección.";

export function QualitySection({ metrics }: { readonly metrics: ApiResult<EvaluationMetrics> }) {
  return (
    <section
      aria-labelledby="quality-title"
      className="flex flex-col gap-5 rounded-lg border border-slate-300 bg-slate-100 p-6"
    >
      <div className="flex flex-col gap-2">
        <h2 id="quality-title" className="text-lg font-semibold text-slate-900">
          Calidad del criterio
        </h2>
        <p className="max-w-3xl text-sm leading-6 text-slate-700">
          Medida contra las etiquetas del corpus de demostración. Ninguna cifra del resto de esta
          pantalla usa esas etiquetas.
        </p>
        {/*
          Not a dismissible banner and not an accordion: it is part of the figures, so it is rendered
          before them and cannot be closed.
        */}
        <p className="max-w-3xl rounded-md border-l-4 border-amber-500 bg-amber-50 p-4 text-sm leading-6 text-amber-950">
          {FIXTURE_CAVEAT}
        </p>
      </div>

      {metrics.ok ? (
        <QualityFigures metrics={metrics.value} />
      ) : (
        <FailureNotice failure={metrics.failure}>
          <p className="mt-3 text-sm">
            <Link
              href="/import"
              className="font-semibold underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-900"
            >
              Ejecutar scoring
            </Link>
          </p>
        </FailureNotice>
      )}
    </section>
  );
}

function QualityFigures({ metrics }: { readonly metrics: EvaluationMetrics }) {
  return (
    <div className="flex flex-col gap-6">
      <dl className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Figure term="Pedidos puntuados" value={formatCount(metrics.scoredOrders)} />
        <Figure term="Con etiqueta" value={formatCount(metrics.labeledOrders)} />
        <Figure
          term="Sin etiqueta"
          value={formatCount(metrics.unlabeledOrders)}
          hint="Se cuentan y se excluyen: un pedido importado nunca trae etiqueta."
        />
        <Figure term="Configuración de reglas" value={metrics.ruleConfigVersion} />
      </dl>

      <div className="grid gap-6 lg:grid-cols-2">
        <FiguresBlock
          title={`Holdout · ${formatCount(metrics.holdoutOrders)} pedidos`}
          description={`Umbral ${formatCount(metrics.selectedThreshold.threshold)}, elegido sobre la cohorte de calibración y aplicado acá sin retocar nada.`}
          figures={metrics.holdout}
        />
        <FiguresBlock
          title={`Calibración · ${formatCount(metrics.calibrationOrders)} pedidos`}
          description="La cohorte temprana, sobre la que se eligió el umbral. No es una medición independiente."
          figures={metrics.selectedThreshold.metrics}
        />
      </div>

      <ThresholdSweep
        sweep={metrics.calibrationSweep}
        selected={metrics.selectedThreshold.threshold}
      />

      <p className="text-xs text-slate-600">
        Corrida #{formatCount(metrics.scoringRunSequence)}, {formatInstant(metrics.scoringRunCompletedAt)}.
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
function ratio(value: number | null): string {
  return value === null ? "sin definir" : formatPercent(value);
}

function FiguresBlock({
  title,
  description,
  figures,
}: {
  readonly title: string;
  readonly description: string;
  readonly figures: MetricsFigures;
}) {
  const { matrix } = figures;

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5">
      <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
      <p className="mt-1 text-xs leading-5 text-slate-600">{description}</p>

      <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <Pair term="Precisión" value={ratio(figures.precision)} />
        <Pair term="Recall" value={ratio(figures.recall)} />
        <Pair term="F1" value={ratio(figures.f1)} />
        <Pair term="Tasa de falsos positivos" value={ratio(figures.falsePositiveRate)} />
      </dl>

      <table className="mt-4 w-full border-collapse text-sm">
        <caption className="pb-1 text-left text-xs text-slate-600">Matriz de confusión</caption>
        <tbody>
          <tr className="border-b border-slate-100">
            <th scope="row" className="py-1 text-left font-normal text-slate-700">
              Verdaderos positivos
            </th>
            <td className="py-1 text-right tabular-nums">{formatCount(matrix.truePositives)}</td>
            <th scope="row" className="py-1 pl-4 text-left font-normal text-slate-700">
              Falsos positivos
            </th>
            <td className="py-1 text-right tabular-nums">{formatCount(matrix.falsePositives)}</td>
          </tr>
          <tr>
            <th scope="row" className="py-1 text-left font-normal text-slate-700">
              Falsos negativos
            </th>
            <td className="py-1 text-right tabular-nums">{formatCount(matrix.falseNegatives)}</td>
            <th scope="row" className="py-1 pl-4 text-left font-normal text-slate-700">
              Verdaderos negativos
            </th>
            <td className="py-1 text-right tabular-nums">{formatCount(matrix.trueNegatives)}</td>
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
}: {
  readonly sweep: readonly ThresholdMetrics[];
  readonly selected: number;
}) {
  return (
    <details className="rounded-lg border border-slate-200 bg-white p-4">
      <summary className="cursor-pointer text-sm font-semibold text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900">
        Barrido de umbrales sobre la cohorte de calibración ({formatCount(sweep.length)} puntos)
      </summary>
      <p className="mt-2 text-xs leading-5 text-slate-600">
        Solo los umbrales donde la matriz de confusión cambia. El umbral{" "}
        {formatCount(selected)} es el que se eligió y el que se aplicó al holdout.
      </p>
      <div className="mt-3 overflow-x-auto">
        <table className="w-full min-w-[30rem] border-collapse text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-xs uppercase tracking-wide text-slate-600">
              <th scope="col" className="py-2 pr-4 font-semibold">
                Umbral
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                Precisión
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                Recall
              </th>
              <th scope="col" className="py-2 pr-4 text-right font-semibold">
                F1
              </th>
              <th scope="col" className="py-2 text-right font-semibold">
                Falsos positivos
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
                  {formatCount(point.threshold)}
                  {point.threshold === selected && " · elegido"}
                </th>
                <td className="py-1.5 pr-4 text-right tabular-nums">
                  {ratio(point.metrics.precision)}
                </td>
                <td className="py-1.5 pr-4 text-right tabular-nums">
                  {ratio(point.metrics.recall)}
                </td>
                <td className="py-1.5 pr-4 text-right tabular-nums">{ratio(point.metrics.f1)}</td>
                <td className="py-1.5 text-right tabular-nums">
                  {ratio(point.metrics.falsePositiveRate)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}
