using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Risk;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class ScoringRunPersistenceTests
{
    [Fact]
    public async Task RepeatedRunOverTheSameCorpusAppendsNothingAndReferencesTheSameEvaluations()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var first = await RunScoringAsync(client);
        var second = await RunScoringAsync(client);

        Assert.Equal("e3-v1", first.RuleConfigVersion);
        Assert.Equal(300, first.OrderCount);
        Assert.Equal(300, first.EvaluationsCreated);
        Assert.Equal(0, first.EvaluationsReused);
        Assert.Equal(1, first.Sequence);

        Assert.Equal(300, second.OrderCount);
        Assert.Equal(0, second.EvaluationsCreated);
        Assert.Equal(300, second.EvaluationsReused);
        Assert.Equal(2, second.Sequence);
        Assert.NotEqual(first.RunId, second.RunId);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.Equal(300, await dbContext.RiskEvaluations.CountAsync());
        Assert.Equal(2, await dbContext.ScoringRuns.CountAsync());
        Assert.Equal(600, await dbContext.RunEvaluations.CountAsync());

        var firstLinks = await ReadLinksAsync(dbContext, first.RunId);
        var secondLinks = await ReadLinksAsync(dbContext, second.RunId);
        Assert.Equal(300, firstLinks.Count);
        Assert.Equal(firstLinks, secondLinks);
    }

    [Fact]
    public async Task DemoCorpusProducesTheGoldenFingerprintsOfTheApprovedRuleConfiguration()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        await RunScoringAsync(client);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var rows = await dbContext.RiskEvaluations
            .AsNoTracking()
            .Join(
                dbContext.Orders.AsNoTracking(),
                evaluation => evaluation.OrderId,
                order => order.Id,
                (evaluation, order) => new
                {
                    order.MerchantReferenceId,
                    evaluation.Score,
                    evaluation.Status,
                    evaluation.EvaluationFingerprint,
                })
            .ToListAsync();

        Assert.Equal(300, rows.Count);
        Assert.Equal(300, rows.Select(row => row.EvaluationFingerprint).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            new Dictionary<int, int>
            {
                [0] = 266,
                [20] = 16,
                [60] = 13,
                [90] = 5,
            },
            rows.GroupBy(row => row.Score!.Value).ToDictionary(group => group.Key, group => group.Count()));
        Assert.Equal(18, rows.Count(row => row.Status == RiskEvaluationStatus.Denied));

        var manifest = string.Join(
            "\n",
            rows
                .OrderBy(row => row.MerchantReferenceId, StringComparer.Ordinal)
                .Select(row => $"{row.MerchantReferenceId}:{row.Score}:{row.EvaluationFingerprint}"));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));

        // Golden values. They change only when the corpus, the rule configuration or the wording of
        // a signal detail changes: a redaction edit that did not bump the rule config version breaks
        // this test loudly instead of silently appending 300 "new" evaluations on the next run.
        Assert.Equal(
            "0d7470c324b0ca1fb3e6e8e10fc7f6a3c736314758dd5538238ec90da764edb0",
            rows.Single(row => row.MerchantReferenceId == "ORD_000001").EvaluationFingerprint);
        Assert.Equal("a13e26d1744ea734a1d0be745775533f82b5d06e997bdc27233c7595a032bae0", digest);
    }

    [Fact]
    public async Task AScoreThatReturnsToAnEarlierValueKeepsTheCurrentEvaluationCorrect()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        // The buyer has two prior orders, which is below the minimum history of every rule.
        await ImportAsync(client, ReboundOrders(("ORD_RB_BASE_1", -10, 100), ("ORD_RB_BASE_2", -9, 100)));
        await ImportAsync(client, ReboundOrders(("ORD_RB_TARGET", 0, 900)));
        var first = await RunScoringAsync(client);

        // A third retroactive order completes the buyer history: 900 is nine times the median 100.
        await ImportAsync(client, ReboundOrders(("ORD_RB_BACKFILL_1", -8, 100)));
        var second = await RunScoringAsync(client);

        // Three more retroactive orders raise the median to 350, so the anomaly disappears and the
        // score returns to its original value with its original fingerprint.
        await ImportAsync(client, ReboundOrders(
            ("ORD_RB_BACKFILL_2", -7, 600),
            ("ORD_RB_BACKFILL_3", -6, 600),
            ("ORD_RB_BACKFILL_4", -5, 600)));
        var third = await RunScoringAsync(client);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var targetId = await dbContext.Orders
            .Where(order => order.MerchantReferenceId == "ORD_RB_TARGET")
            .Select(order => order.Id)
            .SingleAsync();
        var evaluations = await dbContext.RiskEvaluations
            .AsNoTracking()
            .Where(evaluation => evaluation.OrderId == targetId)
            .OrderBy(evaluation => evaluation.CreatedAt)
            .ToListAsync();

        Assert.Equal([0, 40], evaluations.Select(evaluation => evaluation.Score!.Value).Order());

        // Run 1 scores the three orders that exist. Run 2 appends the backfilled order and the new
        // evaluation of the target at 40. Run 3 appends only the three orders it introduced: the
        // target falls back to the evaluation of run 1 instead of producing a fourth row.
        Assert.Equal((1L, 3, 3, 0), (first.Sequence, first.OrderCount, first.EvaluationsCreated, first.EvaluationsReused));
        Assert.Equal((2L, 4, 2, 2), (second.Sequence, second.OrderCount, second.EvaluationsCreated, second.EvaluationsReused));
        Assert.Equal((3L, 7, 3, 4), (third.Sequence, third.OrderCount, third.EvaluationsCreated, third.EvaluationsReused));

        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();
        var current = await store.GetCurrentEvaluationsAsync(CancellationToken.None);
        var currentTarget = current[targetId];

        // "Current" is what the last run referenced, not the most recently inserted row: the score
        // bounced back to zero and the stale evaluation of 40 must not survive as the current one.
        Assert.Equal(0, currentTarget.Score);
        Assert.Equal(evaluations[0].Id, currentTarget.Id);
        Assert.Equal(RiskEvaluationStatus.Approved, currentTarget.Status);
        Assert.Equal("[]", currentTarget.SignalsJson);
    }

    [Fact]
    public async Task ScoringRunSucceedsForImportedOrdersThatHaveNoGroundTruthLabel()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await ImportAsync(client, ReboundOrders(("ORD_UNLABELLED_1", 0, 5000)));

        var response = await client.PostAsync("/api/risk-evaluations:run", null);
        var summary = await response.Content.ReadFromJsonAsync<ScoringRunSummary>();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal(1, summary.OrderCount);
        Assert.Equal(1, summary.EvaluationsCreated);
        Assert.Equal(0, await dbContext.OrderEvaluationLabels.CountAsync());
        Assert.Equal(1, await dbContext.RiskEvaluations.CountAsync());
    }

    [Fact]
    public async Task AFailedWriteLeavesNeitherRunNorEvaluationsNorReferences()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await ImportAsync(client, ReboundOrders(("ORD_ATOMIC_1", 0, 1000)));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var orderId = await dbContext.Orders.Select(order => order.Id).SingleAsync();
        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();

        var runId = Guid.NewGuid();
        var evaluation = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            "e3-v1",
            new(orderId, DateTimeOffset.UnixEpoch, 0, false, []),
            DateTimeOffset.UnixEpoch);
        var run = ScoringRun.Complete(
            runId,
            1,
            "e3-v1",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            1,
            1,
            0);

        // The reference points at an evaluation the run never appended, so the write fails after
        // the run and the evaluation were already part of the same unit of work.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => store.SaveRunAsync(
            run,
            [evaluation],
            [RunEvaluation.Create(runId, orderId, Guid.NewGuid())],
            CancellationToken.None));

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.Equal(0, await verificationContext.ScoringRuns.CountAsync());
        Assert.Equal(0, await verificationContext.RiskEvaluations.CountAsync());
        Assert.Equal(0, await verificationContext.RunEvaluations.CountAsync());
    }

    [Fact]
    public async Task TheFingerprintIndexIsUniqueOnlyForLocallyProducedEvaluations()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await ImportAsync(client, ReboundOrders(("ORD_INDEX_1", 0, 1000)));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'ux_risk_evaluations_local_fingerprint';";
        var definition = Assert.IsType<string>(await command.ExecuteScalarAsync());

        // A partial index. Constraining every source would leave the mutable lifecycle of an
        // external evaluation without a way to finish a pending state.
        Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
        Assert.Contains("'LOCAL'", definition, StringComparison.Ordinal);
        Assert.Matches(@"WHERE\s+""?source""?\s*=\s*'LOCAL'", definition);

        var orderId = await dbContext.Orders.Select(order => order.Id).SingleAsync();
        var assessment = new LocalRiskAssessment(orderId, DateTimeOffset.UnixEpoch, 0, false, []);
        var first = RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", assessment, DateTimeOffset.UnixEpoch);
        var duplicate = RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", assessment, DateTimeOffset.UnixEpoch);
        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();
        var runId = Guid.NewGuid();

        Assert.Equal(first.EvaluationFingerprint, duplicate.EvaluationFingerprint);
        await Assert.ThrowsAsync<ScoringRunConflictException>(() => store.SaveRunAsync(
            ScoringRun.Complete(runId, 1, "e3-v1", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 1, 1, 0),
            [first, duplicate],
            [RunEvaluation.Create(runId, orderId, first.Id)],
            CancellationToken.None));
    }

    [Fact]
    public async Task TwoRunsThatClaimTheSamePositionCollideInsteadOfBecomingAmbiguous()
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();

        await store.SaveRunAsync(CreateEmptyRun(1), [], [], CancellationToken.None);

        await using var conflictingScope = factory.Services.CreateAsyncScope();
        var conflictingStore = conflictingScope.ServiceProvider.GetRequiredService<IScoringRunStore>();

        await Assert.ThrowsAsync<ScoringRunConflictException>(() =>
            conflictingStore.SaveRunAsync(CreateEmptyRun(1), [], [], CancellationToken.None));
        Assert.Equal(1, await store.GetLastRunSequenceAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ARunConflictIsReportedAsConflictAndNotAsServerError()
    {
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services =>
                services.AddScoped<IScoringRunStore, ConflictingScoringRunStore>(),
        };
        using var client = await factory.CreateMigratedClientAsync();

        var response = await client.PostAsync("/api/risk-evaluations:run", null);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("SCORING_RUN_CONFLICT", problem["code"].ToString());
    }

    private static ScoringRun CreateEmptyRun(long sequence)
    {
        return ScoringRun.Complete(
            Guid.NewGuid(),
            sequence,
            "e3-v1",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            0,
            0,
            0);
    }

    private static async Task<ScoringRunSummary> RunScoringAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/risk-evaluations:run", null);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<ScoringRunSummary>(
            await response.Content.ReadFromJsonAsync<ScoringRunSummary>());
    }

    private static async Task<List<Guid>> ReadLinksAsync(SalvoDbContext dbContext, Guid runId)
    {
        return await dbContext.RunEvaluations
            .AsNoTracking()
            .Where(link => link.RunId == runId)
            .OrderBy(link => link.OrderId)
            .Select(link => link.EvaluationId)
            .ToListAsync();
    }

    private static string ReboundOrders(params (string Reference, int DayOffset, long AmountCents)[] orders)
    {
        var records = orders.Select(order => $$"""
              {
                "merchantId": "MER_REBOUND",
                "merchantReferenceId": "{{order.Reference}}",
                "buyerReferenceId": "BUY_REBOUND",
                "occurredAt": "{{new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero).AddDays(order.DayOffset):yyyy-MM-ddTHH:mm:ss}}Z",
                "amountCents": {{order.AmountCents}},
                "currencyCode": "UYU",
                "countryCode": "UY"
              }
            """);

        return $"[\n{string.Join(",\n", records)}\n]";
    }

    private static async Task ImportAsync(HttpClient client, string json)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(json, Encoding.UTF8, "application/json"), "file", "orders.json" },
            { new StringContent("JSON", Encoding.UTF8), "format" },
        };

        var response = await client.PostAsync("/api/order-imports", content);
        response.EnsureSuccessStatusCode();
    }

    private sealed class ConflictingScoringRunStore : IScoringRunStore
    {
        public Task<long> GetLastRunSequenceAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(41L);
        }

        public Task<IReadOnlyDictionary<string, Guid>> GetLocalEvaluationIdsByFingerprintAsync(
            IReadOnlyCollection<string> fingerprints,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        }

        public Task SaveRunAsync(
            ScoringRun run,
            IReadOnlyCollection<RiskEvaluation> evaluationsToAppend,
            IReadOnlyCollection<RunEvaluation> runEvaluations,
            CancellationToken cancellationToken)
        {
            throw new ScoringRunConflictException();
        }

        public Task<IReadOnlyDictionary<Guid, RiskEvaluation>> GetCurrentEvaluationsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, RiskEvaluation>>(
                new Dictionary<Guid, RiskEvaluation>());
        }
    }
}
