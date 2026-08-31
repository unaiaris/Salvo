namespace Salvo.Domain.Evaluation;

public static class RiskMetricsEvaluator
{
    public static EvaluationMetrics Evaluate(
        IReadOnlyCollection<LabeledRiskScore> scores,
        int threshold)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ValidateThreshold(threshold);

        var truePositives = 0;
        var falsePositives = 0;
        var falseNegatives = 0;
        var trueNegatives = 0;

        foreach (var score in scores)
        {
            var flagged = score.Score >= threshold;
            switch (flagged, score.IsFraudLabel)
            {
                case (true, true):
                    truePositives++;
                    break;
                case (true, false):
                    falsePositives++;
                    break;
                case (false, true):
                    falseNegatives++;
                    break;
                default:
                    trueNegatives++;
                    break;
            }
        }

        return EvaluationMetrics.From(new(
            truePositives,
            falsePositives,
            falseNegatives,
            trueNegatives));
    }

    public static IReadOnlyList<ThresholdEvaluation> Sweep(
        IReadOnlyCollection<LabeledRiskScore> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        return Enumerable.Range(0, 101)
            .Select(threshold => new ThresholdEvaluation(threshold, Evaluate(scores, threshold)))
            .ToArray();
    }

    public static ThresholdEvaluation SelectBest(
        IReadOnlyCollection<ThresholdEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        if (evaluations.Count == 0)
        {
            throw new ArgumentException("At least one threshold evaluation is required.", nameof(evaluations));
        }

        return evaluations
            .OrderByDescending(evaluation => evaluation.Metrics.F1 ?? decimal.MinValue)
            .ThenBy(evaluation => evaluation.Metrics.FalsePositiveRate ?? decimal.MaxValue)
            .ThenByDescending(evaluation => evaluation.Threshold)
            .First();
    }

    public static TemporalEvaluationSplit SplitByTime(
        IReadOnlyCollection<LabeledRiskScore> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var cohorts = scores
            .OrderBy(score => score.OccurredAt)
            .ThenBy(score => score.OrderId)
            .GroupBy(score => score.OccurredAt)
            .Select(group => group.ToArray())
            .ToArray();
        if (cohorts.Length < 2)
        {
            throw new ArgumentException(
                "Temporal calibration requires at least two distinct timestamp cohorts.",
                nameof(scores));
        }

        var calibrationCohortCount = Math.Clamp((cohorts.Length * 2) / 3, 1, cohorts.Length - 1);
        var calibration = cohorts
            .Take(calibrationCohortCount)
            .SelectMany(cohort => cohort)
            .ToArray();
        var holdout = cohorts
            .Skip(calibrationCohortCount)
            .SelectMany(cohort => cohort)
            .ToArray();

        return new(calibration, holdout);
    }

    private static void ValidateThreshold(int threshold)
    {
        if (threshold is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "threshold must be between 0 and 100.");
        }
    }
}
