using System.Buffers;
using System.Text;
using System.Text.Json;
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
    /// <para>
    /// The same shape as the differential test of the dashboard, and for the same reason: a
    /// reflection test over types could not see a join added inside an EF store. This one creates
    /// external evaluations in all four states and demands the same bytes back from the three
    /// surfaces that describe the local criterion — the dashboard, the feed ordered by score, and
    /// the quality metrics. Connect any of them to <c>external_evaluations</c> and it fails.
    /// </para>
    /// <para>
    /// One panel of the dashboard is exempt, and it is exempt by name: «denied by the provider
    /// without a local alert» exists precisely to report external verdicts, so it is compared
    /// separately and has to move. Excluding it by name rather than loosening the comparison is
    /// what keeps the assertion able to fail: any other field that started depending on the
    /// provider would still show up as different bytes.
    /// </para>
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

        Assert.Equal(WithoutExternalPanel(dashboardBefore), WithoutExternalPanel(dashboardAfter));
        Assert.Equal(feedBefore, feedAfter);
        Assert.Equal(metricsBefore, metricsAfter);

        // And the one panel that is meant to move did move. Without this the exclusion above would
        // be a way of not looking.
        Assert.Equal(0, ExternalPanelTotal(dashboardBefore));
        Assert.Equal(1, ExternalPanelTotal(dashboardAfter));
    }

    /// <summary>
    /// The same demand, one step further: not even a provider that settles the whole corpus by
    /// callback moves anything the local criterion reports.
    /// </summary>
    /// <remarks>
    /// The previous test creates the evaluations; this one delivers their callbacks, which is where
    /// a second write path could plausibly leak into the read surfaces — a receipt joined into the
    /// dashboard, an alert opened by an external verdict. Both would show up here as different bytes.
    /// </remarks>
    [Fact]
    public async Task DeliveringCallbacksForTheWholeCorpusChangesNothingAboutTheLocalCriterion()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        (await client.PostAsync("/api/demo-data/external-evaluations:request", null))
            .EnsureSuccessStatusCode();

        var dashboardBefore = await client.GetStringAsync("/api/dashboard");
        var feedBefore = await client.GetStringAsync("/api/alerts?sort=SCORE_DESC");
        var metricsBefore = await client.GetStringAsync("/api/evaluation-metrics");

        (await client.PostAsync("/api/demo-data/external-callbacks:deliver", null))
            .EnsureSuccessStatusCode();

        // Without this the test would also pass against a database where no callback was ever
        // delivered, which is the one situation in which changing nothing proves nothing.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            Assert.NotEmpty(await dbContext.CallbackReceipts.AsNoTracking().ToListAsync());
            Assert.Empty(await dbContext.ExternalEvaluations
                .AsNoTracking()
                .Where(evaluation => evaluation.Status == ExternalEvaluationStatus.Pending)
                .ToListAsync());
        }

        var dashboardAfter = await client.GetStringAsync("/api/dashboard");

        Assert.Equal(WithoutExternalPanel(dashboardBefore), WithoutExternalPanel(dashboardAfter));
        Assert.Equal(feedBefore, await client.GetStringAsync("/api/alerts?sort=SCORE_DESC"));
        Assert.Equal(metricsBefore, await client.GetStringAsync("/api/evaluation-metrics"));

        // The callbacks settle the pending band into verdicts, so the panel grows: nine of the
        // twenty-one pending evaluations come back denied.
        Assert.True(ExternalPanelTotal(dashboardAfter) > ExternalPanelTotal(dashboardBefore));
    }

    /// <summary>
    /// The dashboard without the one panel that reports external verdicts.
    /// </summary>
    private static string WithoutExternalPanel(string dashboard)
    {
        using var document = JsonDocument.Parse(dashboard);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.NameEquals("externalDenialsWithoutAlert"))
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static int ExternalPanelTotal(string dashboard)
    {
        using var document = JsonDocument.Parse(dashboard);

        return document.RootElement
            .GetProperty("externalDenialsWithoutAlert")
            .GetProperty("total")
            .GetInt32();
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
