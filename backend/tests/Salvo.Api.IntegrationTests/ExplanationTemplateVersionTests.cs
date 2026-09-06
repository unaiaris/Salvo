using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Explanations;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// What makes a wording fix reach an evaluation somebody already explained.
/// </summary>
/// <remarks>
/// <para>
/// A written explanation is never regenerated — the API refuses, because replacing a paragraph a
/// verdict may have been formed on is not regenerating it — so a template whose words changed can
/// only reach that evaluation by writing beside the old row rather than over it. The template
/// version is what makes the two rows different rows: it is part of the identity the store looks a
/// row up by, and part of the unique index that would otherwise collapse them into one.
/// </para>
/// <para>
/// This is therefore the test that gives the version bump its point. Without it the mechanism is an
/// assumption about EF and an index, and the symptom of getting it wrong is silent: the console
/// keeps showing the old paragraph and nothing fails.
/// </para>
/// </remarks>
public sealed class ExplanationTemplateVersionTests
{
    /// <summary>The words of `e7-v1`, before the polish of `E7C`.</summary>
    private const string PreviousWording =
        "El pedido obtuvo 90 puntos sobre un umbral de 60, y la severidad resultante es crítica. "
        + "Coincidieron 3 reglas.";

    private const string PreviousVersion = "e7-v1";

    [Fact]
    public async Task ANewTemplateVersionWritesBesideTheOldRowAndTheConsoleReadsTheNewOne()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=1");
        var alert = Assert.Single(alerts.Items);
        var detail = await AlertTestCorpus.GetAlertAsync(client, alert.Id);
        var evaluationId = detail.Snapshot.EvaluationId;

        // The state this test is about: an evaluation explained by a template that has since been
        // corrected. Inserted rather than produced, because the previous template no longer exists.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            await InsertReadyExplanationAsync(
                dbContext,
                alert.Id,
                evaluationId,
                PreviousVersion,
                PreviousWording,
                detail.AlertPolicyVersion);
        }

        var stale = await AlertTestCorpus.GetAlertAsync(client, alert.Id);
        Assert.NotNull(stale.Explanation);
        Assert.Equal(PreviousVersion, stale.Explanation.TemplateVersion);
        Assert.Equal(PreviousWording, stale.Explanation.Summary);

        // Asking again is what a wording fix relies on, and it is accepted here even though the
        // evaluation already reads as explained: the row the current template would own does not
        // exist yet.
        var written = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.True(written.Applied);
        Assert.Equal(ExplanationWireNames.Ready, written.Explanation.Status);
        Assert.Equal(DeterministicExplanationProvider.Version, written.Explanation.TemplateVersion);
        Assert.NotEqual(PreviousVersion, DeterministicExplanationProvider.Version);

        var current = await AlertTestCorpus.GetAlertAsync(client, alert.Id);
        Assert.NotNull(current.Explanation);
        Assert.Equal(DeterministicExplanationProvider.Version, current.Explanation.TemplateVersion);
        Assert.Equal(written.Explanation.Id, current.Explanation.Id);
        Assert.NotEqual(PreviousWording, current.Explanation.Summary);
        Assert.Contains("Se dispararon", current.Explanation.Summary!, StringComparison.Ordinal);

        // The version names the row; it is never a word of the paragraph. If it ever became one it
        // would have to be explained away on the grounding side, which is why the tokenizer strikes
        // these strings out before counting figures.
        Assert.DoesNotContain("e7-v", current.Explanation.Summary!, StringComparison.Ordinal);

        // Beside, not over: the old paragraph is still the record of what could be read while the
        // verdict was being formed.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            var stored = await dbContext.AlertExplanations
                .AsNoTracking()
                .Where(explanation => explanation.RiskEvaluationId == evaluationId)
                .OrderBy(explanation => explanation.RequestedAt)
                .ToListAsync();

            Assert.Equal(2, stored.Count);
            Assert.Equal(
                [PreviousVersion, DeterministicExplanationProvider.Version],
                stored.Select(explanation => explanation.TemplateVersion));
            Assert.Equal(PreviousWording, stored[0].Summary);
        }
    }

    private static async Task InsertReadyExplanationAsync(
        SalvoDbContext dbContext,
        Guid alertId,
        Guid evaluationId,
        string templateVersion,
        string summary,
        string alertPolicyVersion)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO alert_explanations (
                id, risk_evaluation_id, provider, template_version, alert_policy_version,
                requested_from_alert_id, status, summary, referenced_rules_json, failure_code,
                attempt_count, requested_at_utc, settled_at_utc, row_version)
            VALUES (
                $id, $evaluation, 'MOCK', $template, $policy,
                $alert, 'READY', $summary, '["amount_anomaly"]', NULL,
                1, '2026-09-05T00:00:00.000Z', '2026-09-05T00:00:01.000Z', 1);
            """;
        AddParameter(command, "$id", Guid.NewGuid().ToString());
        AddParameter(command, "$evaluation", evaluationId.ToString());
        AddParameter(command, "$template", templateVersion);
        AddParameter(command, "$policy", alertPolicyVersion);
        AddParameter(command, "$alert", alertId.ToString());
        AddParameter(command, "$summary", summary);

        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
