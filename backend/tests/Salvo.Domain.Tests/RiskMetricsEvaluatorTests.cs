using Salvo.Domain.Evaluation;

namespace Salvo.Domain.Tests;

public sealed class RiskMetricsEvaluatorTests
{
    [Fact]
    public void EvaluateBuildsTheExpectedMatrixAndMetrics()
    {
        var scores = new[]
        {
            Score(80, true, 0),
            Score(70, false, 1),
            Score(40, true, 2),
            Score(10, false, 3),
        };

        var metrics = RiskMetricsEvaluator.Evaluate(scores, 60);

        Assert.Equal(new ConfusionMatrix(1, 1, 1, 1), metrics.Matrix);
        Assert.Equal(0.5m, metrics.Precision);
        Assert.Equal(0.5m, metrics.Recall);
        Assert.Equal(0.5m, metrics.F1);
        Assert.Equal(0.5m, metrics.FalsePositiveRate);
        Assert.Equal(0.5m, metrics.FlagRate);
    }

    [Fact]
    public void UndefinedDenominatorsRemainNull()
    {
        var metrics = RiskMetricsEvaluator.Evaluate([], 60);

        Assert.Null(metrics.Precision);
        Assert.Null(metrics.Recall);
        Assert.Null(metrics.F1);
        Assert.Null(metrics.FalsePositiveRate);
        Assert.Null(metrics.FlagRate);
    }

    [Fact]
    public void SweepUsesInclusiveThresholdAndConservativeTieBreak()
    {
        var scores = new[]
        {
            Score(60, true, 0),
            Score(20, false, 1),
        };

        var sweep = RiskMetricsEvaluator.Sweep(scores);
        var best = RiskMetricsEvaluator.SelectBest(sweep);

        Assert.Equal(101, sweep.Count);
        Assert.Equal(60, best.Threshold);
        Assert.Equal(new ConfusionMatrix(1, 0, 0, 1), best.Metrics.Matrix);
    }

    [Fact]
    public void TemporalSplitKeepsTimestampCohortsTogether()
    {
        var start = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var scores = new[]
        {
            Score(0, false, 0, start),
            Score(0, false, 1, start),
            Score(0, false, 2, start.AddDays(1)),
            Score(0, false, 3, start.AddDays(2)),
        };

        var split = RiskMetricsEvaluator.SplitByTime(scores);

        Assert.Equal(3, split.Calibration.Count);
        Assert.Single(split.Holdout);
        Assert.All(split.Calibration, score => Assert.True(score.OccurredAt < start.AddDays(2)));
    }

    private static LabeledRiskScore Score(
        int score,
        bool label,
        int idSeed,
        DateTimeOffset? occurredAt = null)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(idSeed).CopyTo(bytes, 0);
        return new(
            new Guid(bytes),
            occurredAt ?? new DateTimeOffset(2026, 8, idSeed + 1, 0, 0, 0, TimeSpan.Zero),
            score,
            label);
    }
}
