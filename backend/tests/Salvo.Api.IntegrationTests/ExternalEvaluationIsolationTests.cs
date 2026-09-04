using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Infrastructure.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class ExternalEvaluationIsolationTests
{
    /// <summary>
    /// The invariant of this stage: the external provider is another opinion, and it changes
    /// nothing about the local one.
    /// </summary>
    /// <remarks>
    /// The same shape as the differential test of the dashboard, and for the same reason: a
    /// reflection test over types could not see a join added inside an EF store. This one creates
    /// external evaluations in all four states and demands the same bytes back from the three
    /// surfaces that describe the local criterion — the dashboard, the feed ordered by score, and
    /// the quality metrics. Connect any of them to <c>external_evaluations</c> and it fails.
    /// </remarks>
    [Fact]
    public async Task ExternalEvaluationsChangeNothingAboutTheLocalCriterion()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var dashboardBefore = await client.GetStringAsync("/api/dashboard");
        var feedBefore = await client.GetStringAsync("/api/alerts?sort=SCORE_DESC");
        var metricsBefore = await client.GetStringAsync("/api/evaluation-metrics");

        var states = await EvaluateEveryStateAsync(factory, client);

        var dashboardAfter = await client.GetStringAsync("/api/dashboard");
        var feedAfter = await client.GetStringAsync("/api/alerts?sort=SCORE_DESC");
        var metricsAfter = await client.GetStringAsync("/api/evaluation-metrics");

        // Without this the test would also pass against a database where nothing was created, which
        // is the one situation in which changing nothing proves nothing.
        Assert.Equal(
            [
                ExternalEvaluationStatus.Pending,
                ExternalEvaluationStatus.Approved,
                ExternalEvaluationStatus.Denied,
                ExternalEvaluationStatus.Error,
            ],
            states.Order());

        Assert.Equal(dashboardBefore, dashboardAfter);
        Assert.Equal(feedBefore, feedAfter);
        Assert.Equal(metricsBefore, metricsAfter);
    }

    /// <summary>
    /// A provider that fails must not be able to stop the local engine, which is the whole reason
    /// the two are separate.
    /// </summary>
    [Fact]
    public async Task AFailingProviderChangesNeitherTheScoringRunNorTheFingerprints()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var before = await AlertTestCorpus.RunScoringAsync(client);
        var fingerprintsBefore = await ReadFingerprintsAsync(factory);

        await using var broken = new SalvoApiFactory();
        using var brokenClient = await broken.CreateMigratedClientAsync();

        // The same corpus, scored by an API whose provider throws at every call.
        await using var brokenFactory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddSingleton<IAntifraudProvider, BrokenProvider>(),
        };
        using var scoringClient = await brokenFactory.CreateMigratedClientAsync();
        (await scoringClient.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        var after = await AlertTestCorpus.RunScoringAsync(scoringClient);
        var fingerprintsAfter = await ReadFingerprintsAsync(brokenFactory);

        Assert.Equal(before.OrderCount, after.OrderCount);
        Assert.Equal(before.AlertsCreated, after.AlertsCreated);
        Assert.Equal(fingerprintsBefore, fingerprintsAfter);
    }

    /// <summary>
    /// The distribution the mock produces over the demo corpus, pinned the way the golden
    /// fingerprints are.
    /// </summary>
    /// <remarks>
    /// The references of the fixture run from <c>ORD_000001</c> to <c>ORD_000300</c>, so every
    /// remainder appears exactly three times and the declared bands turn into these four numbers. A
    /// change to the fixture or to the bands moves them, and that is a decision someone has to make
    /// on purpose rather than discover during a demo.
    /// </remarks>
    [Fact]
    public async Task TheMockDistributesTheDemoCorpusIntoItsDeclaredBands()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var references = await dbContext.Orders
            .AsNoTracking()
            .Select(order => order.MerchantId + "|" + order.MerchantReferenceId)
            .ToListAsync();

        var provider = new MockAntifraudProvider(MockAntifraudProviderOptions.Default, TimeProvider.System);
        var outcomes = new List<ExternalProviderOutcome>();
        foreach (var reference in references)
        {
            outcomes.Add((await provider.EvaluateAsync(
                new(Guid.NewGuid(), reference, "MER", "ORD", "BUY", DateTimeOffset.UnixEpoch, 100, "UYU", "UY"),
                CancellationToken.None)).Outcome);
        }

        Assert.Equal(300, outcomes.Count);
        Assert.Equal(225, outcomes.Count(outcome => outcome == ExternalProviderOutcome.Approved));
        Assert.Equal(45, outcomes.Count(outcome => outcome == ExternalProviderOutcome.Denied));
        Assert.Equal(21, outcomes.Count(outcome => outcome == ExternalProviderOutcome.Pending));
        Assert.Equal(
            9,
            outcomes.Count(outcome =>
                outcome is ExternalProviderOutcome.Unreachable or ExternalProviderOutcome.Rejected));
    }

    /// <summary>
    /// Asking for a provider that does not exist has to stop the process, not quietly fall back to
    /// the mock. A deployment that believes it is talking to Koin while it is talking to a function
    /// of an order reference is the worst possible outcome of this stage.
    /// </summary>
    [Fact]
    public async Task SandboxModeRefusesToStart()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["KOIN_MODE"] = "sandbox";

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("KOIN_MODE=sandbox is not supported", exception.Message, StringComparison.Ordinal);
        Assert.Contains("5.3", exception.Message, StringComparison.Ordinal);

        await using var explicitMock = new SalvoApiFactory();
        explicitMock.Settings["KOIN_MODE"] = "mock";
        using var client = await explicitMock.CreateMigratedClientAsync();

        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            (await client.GetAsync("/health")).StatusCode);
    }

    /// <summary>
    /// Puts one external evaluation of the demo corpus into each of the four states, and reports
    /// which states were actually reached.
    /// </summary>
    private static async Task<List<ExternalEvaluationStatus>> EvaluateEveryStateAsync(
        SalvoApiFactory factory,
        HttpClient client)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        // References of the demo corpus chosen by their remainder: approved, denied, pending and
        // the two that settle in error.
        var wanted = new[] { "ORD_000001", "ORD_000080", "ORD_000092", "ORD_000097" };
        var orderIds = await dbContext.Orders
            .AsNoTracking()
            .Where(order => wanted.Contains(order.MerchantReferenceId))
            .Select(order => order.Id)
            .ToListAsync();

        Assert.Equal(wanted.Length, orderIds.Count);

        var states = new List<ExternalEvaluationStatus>();
        foreach (var orderId in orderIds)
        {
            var result = await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId);
            states.Add(ExternalEvaluationWireNames.ParseStatus(result.Evaluation.Status));
        }

        return states;
    }

    private static async Task<List<string>> ReadFingerprintsAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.RiskEvaluations
            .AsNoTracking()
            .Select(evaluation => evaluation.EvaluationFingerprint!)
            .OrderBy(fingerprint => fingerprint)
            .ToListAsync();
    }

    private sealed class BrokenProvider : IAntifraudProvider
    {
        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The provider is down.");
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The provider is down.");
        }
    }
}
