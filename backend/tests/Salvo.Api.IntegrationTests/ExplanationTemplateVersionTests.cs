using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Explanations;
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

        // What the console needs in order to offer anything at all. Without it the page reads a
        // written explanation, offers nothing, and the corrected wording never arrives.
        Assert.True(stale.Explanation.WrittenByAnotherTemplate);

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
        Assert.False(current.Explanation.WrittenByAnotherTemplate);
        Assert.False(written.Explanation.WrittenByAnotherTemplate);
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

            // The old row, field by field rather than by its text alone: it is what the review
            // record points at, so «not rewritten» has to mean the whole row and not just the
            // paragraph.
            Assert.Equal(PreviousWording, stored[0].Summary);
            Assert.Equal(ExplanationStatus.Ready, stored[0].Status);
            Assert.Equal(1, stored[0].AttemptCount);
            Assert.Equal(stale.Explanation.Id, stored[0].Id);
            Assert.Equal(stale.Explanation.RequestedAt, stored[0].RequestedAt);
            Assert.Equal(stale.Explanation.SettledAt, stored[0].SettledAt);
        }
    }

    /// <summary>
    /// The comparison follows whichever provider is registered, rather than a copy of one version.
    /// </summary>
    /// <remarks>
    /// This is the test a duplicated constant fails. The provider wired up here writes with
    /// <c>e7-v1</c>, which is not what
    /// <see cref="DeterministicExplanationProvider.Version"/> says; a projection that compared
    /// against that constant would report the row it just wrote as written by another template, and
    /// the console would offer to rewrite an explanation that is already the current one — for
    /// ever, because every rewrite would produce the same row.
    /// </remarks>
    [Fact]
    public async Task TheComparisonFollowsTheRegisteredProviderAndNotAConstant()
    {
        await using var factory = new SalvoApiFactory();
        var provider = new ExplanationTestCorpus.SwitchableProvider();
        factory.ConfigureTestServices = services =>
            services.AddScoped<IExplanationProvider>(_ => provider);

        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=1");
        var alert = Assert.Single(alerts.Items);

        var written = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        Assert.Equal(provider.TemplateVersion, written.Explanation.TemplateVersion);
        Assert.NotEqual(DeterministicExplanationProvider.Version, provider.TemplateVersion);

        // Written by the provider this deployment has, so there is nothing newer to offer.
        Assert.False(written.Explanation.WrittenByAnotherTemplate);

        var detail = await AlertTestCorpus.GetAlertAsync(client, alert.Id);
        Assert.NotNull(detail.Explanation);
        Assert.False(detail.Explanation.WrittenByAnotherTemplate);
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
                language, requested_from_alert_id, status, summary, referenced_rules_json,
                failure_code, attempt_count, requested_at_utc, settled_at_utc, row_version)
            VALUES (
                $id, $evaluation, 'MOCK', $template, $policy,
                'es', $alert, 'READY', $summary, '["amount_anomaly"]',
                NULL, 1, '2026-09-05T00:00:00.000Z', '2026-09-05T00:00:01.000Z', 1);
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
