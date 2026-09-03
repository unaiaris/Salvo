using Salvo.Application.Risk;
using Salvo.Domain.Evaluation;

namespace Salvo.Application.Metrics;

/// <summary>
/// Measures the deterministic criterion against the ground truth of a demo corpus, over the
/// evaluations the current scoring run made current.
/// </summary>
/// <remarks>
/// This is deliberately not <see cref="EvaluateLocalRiskHandler"/>, which rescores the live corpus
/// and requires a label for every order it scores. Reading the persisted run instead keeps this
/// surface and the operational dashboard describing the same state, and treating a missing label as
/// a fact to report rather than as a failure keeps an ordinary import from turning the page into a
/// server error.
/// </remarks>
public sealed class GetEvaluationMetricsHandler(
    IEvaluationMetricsReader reader,
    IEvaluationLabelReader labelReader)
{
    /// <exception cref="EvaluationMetricsUnavailableException">
    /// No run exists, or the labelled orders do not span two distinct instants.
    /// </exception>
    public async Task<EvaluationMetricsResult> HandleAsync(CancellationToken cancellationToken)
    {
        var run = await reader.GetCurrentRunAsync(cancellationToken)
            ?? throw new EvaluationMetricsUnavailableException(
                "The corpus has not been scored yet. Run the scoring before asking for quality metrics.");

        var scored = (await reader.GetScoredOrdersAsync(run.Id, cancellationToken))
            .Where(row => row.Score is not null)
            .ToArray();
        var labels = await labelReader.GetByOrderIdsAsync(
            scored.Select(row => row.OrderId).ToArray(),
            cancellationToken);

        var labeled = scored
            .Where(row => labels.ContainsKey(row.OrderId))
            .Select(row => new LabeledRiskScore(
                row.OrderId,
                row.OccurredAt,
                row.Score!.Value,
                labels[row.OrderId].IsFraudLabel))
            .ToArray();

        // SplitByTime throws below two cohorts. Checking here is what turns "the corpus has no
        // ground truth to measure against" into an answer the interface can render.
        if (labeled.Select(score => score.OccurredAt).Distinct().Count() < 2)
        {
            throw new EvaluationMetricsUnavailableException(
                $"Quality metrics need labelled orders on at least two distinct instants; run "
                + $"{run.Sequence} covers {labeled.Length} labelled of {scored.Length} scored orders.");
        }

        var split = RiskMetricsEvaluator.SplitByTime(labeled);
        var sweep = RiskMetricsEvaluator.Sweep(split.Calibration);
        var selected = RiskMetricsEvaluator.SelectBest(sweep);
        var holdout = RiskMetricsEvaluator.Evaluate(split.Holdout, selected.Threshold);

        return new(
            run.Sequence,
            run.CompletedAt,
            run.RuleConfigVersion,
            scored.Length,
            labeled.Length,
            scored.Length - labeled.Length,
            split.Calibration.Count,
            split.Holdout.Count,
            Collapse(sweep),
            ToView(selected),
            ToView(holdout));
    }

    /// <summary>
    /// Keeps one row per distinct confusion matrix: the highest threshold that still produces it.
    /// </summary>
    /// <remarks>
    /// The highest rather than the lowest, because <see cref="RiskMetricsEvaluator.SelectBest"/>
    /// breaks ties towards the higher threshold. Collapsing towards the same end is what guarantees
    /// that the selected threshold is one of the rows the client receives.
    /// </remarks>
    private static ThresholdMetricsView[] Collapse(IReadOnlyList<ThresholdEvaluation> sweep)
    {
        var collapsed = new List<ThresholdMetricsView>();
        for (var index = 0; index < sweep.Count; index++)
        {
            if (index + 1 < sweep.Count
                && sweep[index + 1].Metrics.Matrix == sweep[index].Metrics.Matrix)
            {
                continue;
            }

            collapsed.Add(ToView(sweep[index]));
        }

        return [.. collapsed];
    }

    private static ThresholdMetricsView ToView(ThresholdEvaluation evaluation)
    {
        return new(evaluation.Threshold, ToView(evaluation.Metrics));
    }

    private static EvaluationMetricsView ToView(EvaluationMetrics metrics)
    {
        return new(
            new(
                metrics.Matrix.TruePositives,
                metrics.Matrix.FalsePositives,
                metrics.Matrix.FalseNegatives,
                metrics.Matrix.TrueNegatives),
            metrics.Precision,
            metrics.Recall,
            metrics.F1,
            metrics.FalsePositiveRate,
            metrics.FlagRate);
    }
}
