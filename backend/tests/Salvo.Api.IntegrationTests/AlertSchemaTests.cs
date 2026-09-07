using System.Data.Common;
using Microsoft.Data.Sqlite;
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

        // Explainability lives in its own table. These columns staying absent is what stage 7
        // decided rather than what it postponed: an explanation has a lifecycle that can fail and
        // be retried, and this snapshot is frozen and its status is a concurrency token.
        Assert.DoesNotContain("explanation", columns);
        Assert.DoesNotContain("recommended_action", columns);
        Assert.DoesNotContain("explanation_status", columns);
    }

    /// <summary>
    /// The explanation has a table of its own, keyed by the evaluation and not by the alert.
    /// </summary>
    /// <remarks>
    /// Both indexes matter and they are not the same guarantee. The total one is the identity: one
    /// answer per evaluation, provider, template and policy, which it can afford to be because a
    /// retry happens on that same row. The partial one is the reservation: only one provider is
    /// being asked at a time, which is what makes two concurrent requests cost one call.
    /// </remarks>
    [Fact]
    public async Task TheExplanationHasItsOwnTableKeyedByTheEvaluation()
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        var columns = await ReadColumnNamesAsync(dbContext, "alert_explanations");
        var identity = await ReadIndexAsync(dbContext, "ux_alert_explanations_identity");
        var pending = await ReadIndexAsync(dbContext, "ux_alert_explanations_pending_evaluation");

        Assert.Contains("risk_evaluation_id", columns);
        Assert.Contains("requested_from_alert_id", columns);
        Assert.Contains("attempt_count", columns);
        Assert.Contains("row_version", columns);

        // The columns a real provider needs, put here now so that adding one is a registration
        // rather than a migration.
        Assert.Contains("provider_version", columns);
        Assert.Contains("input_tokens", columns);
        Assert.Contains("output_tokens", columns);

        Assert.Contains("CREATE UNIQUE INDEX", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", identity, StringComparison.Ordinal);
        Assert.Contains("risk_evaluation_id", identity, StringComparison.Ordinal);
        Assert.Contains("template_version", identity, StringComparison.Ordinal);
        Assert.Contains("alert_policy_version", identity, StringComparison.Ordinal);

        Assert.Contains("CREATE UNIQUE INDEX", pending, StringComparison.Ordinal);
        Assert.Matches(@"WHERE\s+""?status""?\s*=\s*'PENDING'", pending);

        // The alert is where the request came from, never part of what identifies the answer.
        Assert.DoesNotContain("requested_from_alert_id", identity, StringComparison.Ordinal);

        // And the review records what the reviewer was reading.
        Assert.Contains("explanation_id", await ReadColumnNamesAsync(dbContext, "alert_reviews"));
    }

    /// <summary>
    /// Which template wrote the text is stored; whether that template is still the current one is
    /// not.
    /// </summary>
    /// <remarks>
    /// The same shape as the band divergence of an alert and as the outdated flag beside it: the
    /// answer depends on something outside the row — here, which provider this deployment has
    /// registered — so a stored copy would go stale the moment that changes, and it would go stale
    /// silently, which is the failure mode this whole task exists to undo. Computing it on every
    /// read costs a string comparison and can never disagree with the writer.
    /// </remarks>
    [Fact]
    public async Task WhetherTheCurrentTemplateWroteTheTextIsComputedAndNeverStored()
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var columns = await ReadColumnNamesAsync(dbContext, "alert_explanations");

        // The fact, which identifies the row and is part of its unique index.
        Assert.Contains("template_version", columns);

        // The comparison, which is not a fact about the row at all.
        Assert.DoesNotContain(
            columns,
            column => column.Contains("another_template", StringComparison.Ordinal)
                || column.Contains("older_template", StringComparison.Ordinal)
                || column.Contains("current_template", StringComparison.Ordinal)
                || column.Contains("outdated", StringComparison.Ordinal));
    }

    /// <summary>
    /// «A rejected summary is never stored» is a property of the database, not a promise of the
    /// handler.
    /// </summary>
    /// <remarks>
    /// The test writes straight to SQLite, going around every line of application code, because
    /// that is the only way to find out whether the guarantee survives a handler that has a bug in
    /// it. The second insert is the control: the same row without the summary is accepted, so what
    /// the first one proves is the constraint and not some unrelated invalidity.
    /// </remarks>
    [Fact]
    public async Task TheDatabaseRefusesAFailedExplanationThatCarriesText()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        var alert = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        var detail = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        var withText = await TryInsertExplanationAsync(
            dbContext,
            alert.Id,
            detail.Snapshot.EvaluationId,
            summary: "un resumen que la validación rechazó");
        var withoutText = await TryInsertExplanationAsync(
            dbContext,
            alert.Id,
            detail.Snapshot.EvaluationId,
            summary: null);

        Assert.NotNull(withText);
        Assert.Contains("ck_alert_explanations_ready", withText, StringComparison.Ordinal);
        Assert.Null(withoutText);
    }

    /// <summary>
    /// Inserts a failed explanation directly. Returns the SQLite message, or <c>null</c> when the
    /// row was accepted.
    /// </summary>
    private static async Task<string?> TryInsertExplanationAsync(
        SalvoDbContext dbContext,
        Guid alertId,
        Guid evaluationId,
        string? summary)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO alert_explanations (
                id, risk_evaluation_id, provider, template_version, alert_policy_version,
                language, requested_from_alert_id, status, summary, referenced_rules_json,
                failure_code, attempt_count, requested_at_utc, settled_at_utc, row_version)
            VALUES (
                $id, $evaluation, 'MOCK', 'e7-v1', 'e4-v1',
                'es', $alert, 'FAILED', $summary, NULL,
                'NOT_GROUNDED_NUMBER', 1, '2026-09-05T00:00:00.000Z', '2026-09-05T00:00:01.000Z', 1);
            """;
        AddParameter(command, "$id", Guid.NewGuid().ToString());
        AddParameter(command, "$evaluation", evaluationId.ToString());
        AddParameter(command, "$alert", alertId.ToString());
        AddParameter(command, "$summary", summary);

        try
        {
            await command.ExecuteNonQueryAsync();

            return null;
        }
        catch (SqliteException exception)
        {
            return exception.Message;
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
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

        var winnerReview = winner.Review(Guid.NewGuid(), AlertStatus.ConfirmedSafe, "safe", null, DateTimeOffset.UtcNow);
        await winnerStore.SaveReviewAsync(winner, winnerReview, CancellationToken.None);

        var loserReview = loser.Review(Guid.NewGuid(), AlertStatus.ReportedFraud, "fraud", null, DateTimeOffset.UtcNow);
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
        winner.Review(Guid.NewGuid(), AlertStatus.ConfirmedSafe, null, null, DateTimeOffset.UtcNow);
        await winnerContext.SaveChangesAsync();

        loser.Review(Guid.NewGuid(), AlertStatus.ReportedFraud, null, null, DateTimeOffset.UtcNow);

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
