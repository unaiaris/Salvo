using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class ScoringRunTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompletedRunNormalizesTimestampsAndKeepsItsCounters()
    {
        var run = ScoringRun.Complete(
            Guid.NewGuid(),
            7,
            "e3-v1",
            new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.FromHours(-3)),
            StartedAt.AddSeconds(4),
            300,
            12,
            288,
            4,
            2,
            1);

        Assert.Equal(TimeSpan.Zero, run.StartedAt.Offset);
        Assert.Equal(StartedAt, run.StartedAt);
        Assert.Equal(7, run.Sequence);
        Assert.Equal(300, run.OrderCount);
        Assert.Equal(12, run.EvaluationsCreated);
        Assert.Equal(288, run.EvaluationsReused);
        Assert.Equal(4, run.AlertsCreated);
        Assert.Equal(2, run.AlertsSkippedOpen);
        Assert.Equal(1, run.AlertsSkippedReviewed);
    }

    [Fact]
    public void RunRejectsCountersThatDoNotAccountForEveryScoredOrder()
    {
        Assert.Throws<ArgumentException>(() => ScoringRun.Complete(
            Guid.NewGuid(),
            1,
            "e3-v1",
            StartedAt,
            StartedAt,
            300,
            10,
            10,
            0,
            0,
            0));
    }

    [Fact]
    public void RunRejectsMoreAlertOutcomesThanScoredOrders()
    {
        Assert.Throws<ArgumentException>(() => ScoringRun.Complete(
            Guid.NewGuid(),
            1,
            "e3-v1",
            StartedAt,
            StartedAt,
            2,
            2,
            0,
            2,
            1,
            0));
    }

    [Fact]
    public void RunRejectsCompletionBeforeItStarted()
    {
        Assert.Throws<ArgumentException>(() => ScoringRun.Complete(
            Guid.NewGuid(),
            1,
            "e3-v1",
            StartedAt,
            StartedAt.AddSeconds(-1),
            0,
            0,
            0,
            0,
            0,
            0));
    }

    [Fact]
    public void RunAndReferenceRejectEmptyIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => ScoringRun.Complete(
            Guid.Empty,
            1,
            "e3-v1",
            StartedAt,
            StartedAt,
            0,
            0,
            0,
            0,
            0,
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoringRun.Complete(
            Guid.NewGuid(),
            0,
            "e3-v1",
            StartedAt,
            StartedAt,
            0,
            0,
            0,
            0,
            0,
            0));
        Assert.Throws<ArgumentException>(() =>
            RunEvaluation.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() =>
            RunEvaluation.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() =>
            RunEvaluation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }
}
