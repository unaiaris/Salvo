using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Risk;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class RiskEvaluationTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public RiskEvaluationTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task DemoFixtureProducesReproducibleTemporalEvaluationWithoutPersistenceEffects()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var seedResponse = await client.PostAsync("/api/demo-data/seed", null);
        seedResponse.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var tableNamesBefore = await ReadTableNamesAsync(dbContext);
        var orderCountBefore = await dbContext.Orders.CountAsync();
        var labelCountBefore = await dbContext.OrderEvaluationLabels.CountAsync();
        var handler = scope.ServiceProvider.GetRequiredService<EvaluateLocalRiskHandler>();

        var first = await handler.HandleAsync(CancellationToken.None);
        var second = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal("e3-v1", first.ConfigVersion);
        Assert.Equal(300, first.Assessments.Count);
        Assert.Equal(
            new Dictionary<int, int>
            {
                [0] = 266,
                [20] = 16,
                [60] = 13,
                [90] = 5,
            },
            first.Assessments
                .GroupBy(assessment => assessment.Score)
                .ToDictionary(group => group.Key, group => group.Count()));
        Assert.Equal(60, first.SelectedThreshold.Threshold);
        Assert.Equal(new ConfusionMatrix(12, 0, 0, 188), first.SelectedThreshold.Metrics.Matrix);
        Assert.Equal(new ConfusionMatrix(6, 0, 0, 94), first.HoldoutMetrics.Matrix);
        Assert.Equal(1m, first.HoldoutMetrics.Precision);
        Assert.Equal(1m, first.HoldoutMetrics.Recall);
        Assert.Equal(1m, first.HoldoutMetrics.F1);
        Assert.Equal(0m, first.HoldoutMetrics.FalsePositiveRate);
        Assert.All(first.Assessments.SelectMany(assessment => assessment.Signals), signal =>
        {
            Assert.DoesNotContain("BUY_", signal.Detail, StringComparison.Ordinal);
            Assert.DoesNotContain("DEV_", signal.Detail, StringComparison.Ordinal);
            Assert.DoesNotContain("FraudLabel", signal.Detail, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(
            first.Assessments.Select(Projection),
            second.Assessments.Select(Projection));

        Assert.Equal(orderCountBefore, await dbContext.Orders.CountAsync());
        Assert.Equal(labelCountBefore, await dbContext.OrderEvaluationLabels.CountAsync());
        Assert.Equal(tableNamesBefore, await ReadTableNamesAsync(dbContext));

        // Scoring for metrics is a read-only path: persisting an evaluation and opening an alert
        // are the job of a scoring run, and nothing here may do either.
        Assert.Equal(0, await dbContext.RiskEvaluations.CountAsync());
        Assert.Equal(0, await dbContext.ScoringRuns.CountAsync());
        Assert.Equal(0, await dbContext.RunEvaluations.CountAsync());
        Assert.Equal(0, await dbContext.Alerts.CountAsync());
        Assert.Equal(0, await dbContext.AlertReviews.CountAsync());
    }

    [Fact]
    public async Task EvaluationRejectsIncompleteGroundTruthAfterScoring()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var seedResponse = await client.PostAsync("/api/demo-data/seed", null);
        seedResponse.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var orderReader = scope.ServiceProvider.GetRequiredService<IRiskOrderReader>();
        var handler = new EvaluateLocalRiskHandler(orderReader, new EmptyLabelReader());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(CancellationToken.None));

        Assert.Equal(
            "Evaluation requires exactly one ground-truth label for every scored order.",
            exception.Message);
    }

    private static async Task<string[]> ReadTableNamesAsync(SalvoDbContext dbContext)
    {
        return (await dbContext.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' ORDER BY name;")
                .ToArrayAsync())
            .Where(name => !name.StartsWith("sqlite_", StringComparison.Ordinal))
            .ToArray();
    }

    private static (Guid OrderId, int Score, bool IsFlagged, string Signals) Projection(
        LocalRiskAssessment assessment)
    {
        return (
            assessment.OrderId,
            assessment.Score,
            assessment.IsFlagged,
            string.Join("|", assessment.Signals.Select(signal => $"{signal.Rule}:{signal.Weight}:{signal.Detail}")));
    }

    private sealed class EmptyLabelReader : IEvaluationLabelReader
    {
        public Task<IReadOnlyDictionary<Guid, OrderEvaluationLabel>> GetByOrderIdsAsync(
            IReadOnlyCollection<Guid> orderIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, OrderEvaluationLabel>>(
                new Dictionary<Guid, OrderEvaluationLabel>());
        }
    }
}
