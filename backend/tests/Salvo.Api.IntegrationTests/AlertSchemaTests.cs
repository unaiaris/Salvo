using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class AlertSchemaTests
{
    [Fact]
    public async Task OnlyOneAlertPerOrderMayAwaitAVerdictAndOnlyOneAlertPerEvaluationMayExist()
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        var openOrder = await ReadIndexAsync(dbContext, "ux_alerts_open_order");
        var perEvaluation = await ReadIndexAsync(dbContext, "ux_alerts_risk_evaluation");
        var perAlert = await ReadIndexAsync(dbContext, "ux_alert_reviews_alert");

        // Partial, not total. A total index would also forbid the escalation that a retroactive
        // import legitimately produces on an order whose verdict is already recorded.
        Assert.Contains("CREATE UNIQUE INDEX", openOrder, StringComparison.Ordinal);
        Assert.Matches(@"WHERE\s+""?status""?\s*=\s*'OPEN'", openOrder);
        Assert.Contains("CREATE UNIQUE INDEX", perEvaluation, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", perEvaluation, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX", perAlert, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeverityIsDerivedAndThePolicyVersionIsStored()
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var columns = await ReadColumnNamesAsync(dbContext, "alerts");

        // Storing the derived value would let a future policy reclassify historical alerts;
        // storing the version of the function instead keeps every past verdict reproducible.
        Assert.DoesNotContain("severity", columns);
        Assert.Contains("alert_policy_version", columns);
        Assert.Contains("risk_score_snapshot", columns);
        Assert.Contains("supersedes_alert_id", columns);

        // Stage 7 owns explainability; its columns must not exist yet.
        Assert.DoesNotContain("explanation", columns);
        Assert.DoesNotContain("recommended_action", columns);
        Assert.DoesNotContain("explanation_status", columns);
    }

    [Fact]
    public async Task TheStatusOfAnAlertIsAConcurrencyTokenAndAStaleVerdictIsRefused()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        var alertId = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items).Id;

        await using var winnerScope = factory.Services.CreateAsyncScope();
        await using var loserScope = factory.Services.CreateAsyncScope();
        var winnerStore = winnerScope.ServiceProvider.GetRequiredService<IAlertStore>();
        var loserStore = loserScope.ServiceProvider.GetRequiredService<IAlertStore>();

        var property = winnerScope.ServiceProvider
            .GetRequiredService<SalvoDbContext>()
            .Model
            .FindEntityType(typeof(Alert))!
            .FindProperty(nameof(Alert.Status))!;

        // Both readers see the alert open, which is the check-then-act a transaction alone does not
        // protect: without the token both writes would succeed and one verdict would be lost.
        var winner = (await winnerStore.FindForReviewAsync(alertId, CancellationToken.None))!.Alert;
        var loser = (await loserStore.FindForReviewAsync(alertId, CancellationToken.None))!.Alert;

        var winnerReview = winner.Review(Guid.NewGuid(), AlertStatus.ConfirmedSafe, "safe", DateTimeOffset.UtcNow);
        await winnerStore.SaveReviewAsync(winner, winnerReview, CancellationToken.None);

        var loserReview = loser.Review(Guid.NewGuid(), AlertStatus.ReportedFraud, "fraud", DateTimeOffset.UtcNow);
        var exception = await Assert.ThrowsAsync<AlertReviewConflictException>(() =>
            loserStore.SaveReviewAsync(loser, loserReview, CancellationToken.None));

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(AlertReviewConflictReason.ConcurrentReview, exception.Reason);
        Assert.IsAssignableFrom<DbUpdateException>(exception.InnerException);
        Assert.Equal(AlertStatus.ConfirmedSafe, (await dbContext.Alerts.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(1, await dbContext.AlertReviews.CountAsync());
    }

    [Fact]
    public async Task AStaleVerdictLosesToTheConcurrencyTokenEvenWithoutTheAuditIndex()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        await using var winnerScope = factory.Services.CreateAsyncScope();
        await using var loserScope = factory.Services.CreateAsyncScope();
        var winnerContext = winnerScope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var loserContext = loserScope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        var winner = await winnerContext.Alerts.SingleAsync();
        var loser = await loserContext.Alerts.SingleAsync();

        // Only the alert row is written here, so the unique index on the audit table cannot be what
        // refuses the second write: the concurrency token has to carry the case on its own.
        winner.Review(Guid.NewGuid(), AlertStatus.ConfirmedSafe, null, DateTimeOffset.UtcNow);
        await winnerContext.SaveChangesAsync();

        loser.Review(Guid.NewGuid(), AlertStatus.ReportedFraud, null, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => loserContext.SaveChangesAsync());
        Assert.Equal(AlertStatus.ConfirmedSafe, (await winnerContext.Alerts.AsNoTracking().SingleAsync()).Status);
    }

    private static async Task<string> ReadIndexAsync(SalvoDbContext dbContext, string name)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = $name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }

    private static async Task<List<string>> ReadColumnNamesAsync(SalvoDbContext dbContext, string table)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{table}');";
        await using var reader = await command.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
