using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The language is a property of the deployment, and it is part of what identifies an explanation.
/// </summary>
/// <remarks>
/// Everything here runs over a database file rather than the shared in-memory connection, because
/// the question every one of these tests asks is what a <em>second</em> host finds when it stands
/// over the rows a first one wrote. That is the shape of the real change — a deployment restarted
/// with another <c>SALVO_LANGUAGE</c> — and an in-memory database dies with its host.
/// </remarks>
public sealed class ExplanationLanguageTests : IDisposable
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"salvo-language-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    /// <summary>
    /// <strong>The central check of this task.</strong> An evaluation explained in Spanish, read
    /// again by a deployment that now writes Portuguese, is explained again — beside the first row
    /// and without touching it.
    /// </summary>
    /// <remarks>
    /// Without the language inside <c>ux_alert_explanations_identity</c> the second deployment
    /// looks the row up, finds the Spanish one, reports «already explained» and never writes a word
    /// of Portuguese. That is the defect of <c>E7D</c> with a different column in the same place,
    /// and it is what this asserts is gone. It is also the one assertion that separates this work
    /// from a search and replace.
    /// </remarks>
    [Fact]
    public async Task ChangingTheLanguageWritesANewRowAndLeavesTheSpanishOneUntouched()
    {
        Guid alertId;
        string spanishSummary;

        await using (var spanish = await StartAsync(ExplanationWireNames.Spanish))
        {
            using var client = await spanish.CreateMigratedClientAsync();
            alertId = await OpenExplainedAlertAsync(client);
            spanishSummary = await SummaryOfAsync(client, alertId);

            Assert.Contains("El pedido obtuvo", spanishSummary, StringComparison.Ordinal);
        }

        await using var portuguese = await StartAsync(ExplanationWireNames.Portuguese);
        using var second = portuguese.CreateClient();

        var result = await ExplanationTestCorpus.RequestOkAsync(second, alertId);

        // It really wrote something rather than handing back what was there.
        Assert.True(result.Applied);
        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);
        Assert.NotNull(result.Explanation.Summary);
        Assert.Contains("O pedido obteve", result.Explanation.Summary, StringComparison.Ordinal);
        Assert.NotEqual(spanishSummary, result.Explanation.Summary);

        // Two rows over one evaluation, and the Spanish one is exactly as it was written.
        await using var scope = portuguese.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.AlertExplanations.AsNoTracking().ToListAsync();

        Assert.Equal(2, stored.Count);

        var kept = Assert.Single(stored, row => row.Language == ExplanationLanguage.Spanish);
        var written = Assert.Single(stored, row => row.Language == ExplanationLanguage.Portuguese);

        Assert.Equal(spanishSummary, kept.Summary);
        Assert.Equal(result.Explanation.Summary, written.Summary);
        Assert.NotEqual(kept.Id, written.Id);

        // Same identity in everything else, which is what makes the language the column that
        // decided this.
        Assert.Equal(kept.RiskEvaluationId, written.RiskEvaluationId);
        Assert.Equal(kept.TemplateVersion, written.TemplateVersion);
        Assert.Equal(kept.AlertPolicyVersion, written.AlertPolicyVersion);
    }

    /// <summary>
    /// And the console reads the row of its own language, which is the other half and the half a
    /// green write path hides.
    /// </summary>
    /// <remarks>
    /// The write above could be perfect and the alert detail still show the Spanish paragraph for
    /// ever, because the read that feeds the console groups explanations by evaluation. The right
    /// row would exist and nobody would ever see it — the most expensive failure of this change,
    /// since everything else is green while it happens.
    /// </remarks>
    [Fact]
    public async Task TheConsoleReadsTheExplanationOfItsOwnLanguage()
    {
        Guid alertId;

        await using (var spanish = await StartAsync(ExplanationWireNames.Spanish))
        {
            using var client = await spanish.CreateMigratedClientAsync();
            alertId = await OpenExplainedAlertAsync(client);
        }

        await using var portuguese = await StartAsync(ExplanationWireNames.Portuguese);
        using var second = portuguese.CreateClient();

        // Before anybody asks for it, the detail carries no explanation at all: the only row that
        // exists is in the other language, and it is not this deployment's answer.
        var before = await AlertTestCorpus.GetAlertAsync(second, alertId);

        Assert.Null(before.Explanation);

        await ExplanationTestCorpus.RequestOkAsync(second, alertId);

        var after = await AlertTestCorpus.GetAlertAsync(second, alertId);

        Assert.NotNull(after.Explanation);
        Assert.NotNull(after.Explanation.Summary);
        Assert.Contains("O pedido obteve", after.Explanation.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("El pedido obtuvo", after.Explanation.Summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Two live reservations over one evaluation, one per language, coexist.
    /// </summary>
    /// <remarks>
    /// The partial unique index over pending rows is the one that serializes two requests while
    /// nothing has been asked of anybody yet. Without the language in it, the second deployment's
    /// reservation collides with the first deployment's instead of being written: the lookup by
    /// identity does not find it, so the insert is attempted, and what an analyst sees is a
    /// database error rather than a paragraph. Reserved directly, because a handler always settles.
    /// </remarks>
    [Fact]
    public async Task TwoPendingReservationsInDifferentLanguagesDoNotCollide()
    {
        await using var factory = await StartAsync(ExplanationWireNames.Spanish);
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenAlertAsync(client);
        var detail = await AlertTestCorpus.GetAlertAsync(client, alertId);

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExplanationStore>();

        foreach (var language in new[] { ExplanationLanguage.Spanish, ExplanationLanguage.Portuguese })
        {
            await store.ReserveAsync(
                AlertExplanation.Reserve(
                    Guid.NewGuid(),
                    detail.Snapshot.EvaluationId,
                    ExplanationProvider.Mock,
                    "e7-v2",
                    detail.AlertPolicyVersion,
                    language,
                    alertId,
                    DateTimeOffset.UtcNow),
                CancellationToken.None);
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var pending = await dbContext.AlertExplanations
            .AsNoTracking()
            .Where(row => row.Status == ExplanationStatus.Pending)
            .ToListAsync();

        Assert.Equal(2, pending.Count);
        Assert.Equal(
            [ExplanationLanguage.Spanish, ExplanationLanguage.Portuguese],
            pending.Select(row => row.Language).Order().ToArray());
    }

    /// <summary>
    /// A second reservation in the <em>same</em> language still collides. The index was narrowed by
    /// one column, not disabled.
    /// </summary>
    [Fact]
    public async Task TwoPendingReservationsInTheSameLanguageStillCollide()
    {
        await using var factory = await StartAsync(ExplanationWireNames.Spanish);
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenAlertAsync(client);
        var detail = await AlertTestCorpus.GetAlertAsync(client, alertId);

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExplanationStore>();

        AlertExplanation Reserve() => AlertExplanation.Reserve(
            Guid.NewGuid(),
            detail.Snapshot.EvaluationId,
            ExplanationProvider.Mock,
            "e7-v2",
            detail.AlertPolicyVersion,
            ExplanationLanguage.Spanish,
            alertId,
            DateTimeOffset.UtcNow);

        await store.ReserveAsync(Reserve(), CancellationToken.None);

        await Assert.ThrowsAsync<ExplanationConflictException>(
            () => store.ReserveAsync(Reserve(), CancellationToken.None));
    }

    /// <summary>The language this deployment writes is what the capabilities endpoint publishes.</summary>
    [Theory]
    [InlineData(ExplanationWireNames.Spanish)]
    [InlineData(ExplanationWireNames.Portuguese)]
    public async Task CapabilitiesPublishTheConfiguredLanguage(string configured)
    {
        await using var factory = await StartAsync(configured);
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = Assert.IsType<CapabilitiesResponse>(
            await (await client.GetAsync("/api/system/capabilities"))
                .Content.ReadFromJsonAsync<CapabilitiesResponse>());

        Assert.Equal(configured, capabilities.Language);
    }

    /// <summary>Unset means Spanish, and the console the demonstration shows is unchanged.</summary>
    [Fact]
    public async Task WithoutTheVariableTheDeploymentIsSpanish()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = Assert.IsType<CapabilitiesResponse>(
            await (await client.GetAsync("/api/system/capabilities"))
                .Content.ReadFromJsonAsync<CapabilitiesResponse>());

        Assert.Equal(ExplanationWireNames.Spanish, capabilities.Language);
    }

    /// <summary>
    /// A language this build cannot write stops the process, with a message that names the value it
    /// got and the values it could have had.
    /// </summary>
    /// <remarks>
    /// The shape of <c>AI_PROVIDER</c> and <c>KOIN_MODE</c>, and for the same reason: falling back
    /// to Spanish in silence would let a deployment that meant Portuguese serve Spanish and report
    /// nothing — and then write rows stamped <c>es</c> that the corrected deployment cannot reuse.
    /// </remarks>
    [Theory]
    [InlineData("fr")]
    [InlineData("es-UY")]
    [InlineData("ES")]
    public async Task AnUnknownLanguageStopsTheApiFromStarting(string configured)
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SALVO_LANGUAGE"] = configured;

        var refused = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains($"'{configured}'", refused.Message, StringComparison.Ordinal);
        Assert.Contains("es, pt", refused.Message, StringComparison.Ordinal);

        await Task.CompletedTask;
    }

    private async Task<SalvoApiFactory> StartAsync(string language)
    {
        var factory = SalvoApiFactory.OverDatabaseFile(databasePath);
        factory.Settings["SALVO_LANGUAGE"] = language;

        await Task.CompletedTask;

        return factory;
    }

    /// <summary>One alert over the small corpus the alert tests use, with nothing explained yet.</summary>
    private static async Task<Guid> OpenAlertAsync(HttpClient client)
    {
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        return Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items).Id;
    }

    private static async Task<Guid> OpenExplainedAlertAsync(HttpClient client)
    {
        var alertId = await OpenAlertAsync(client);
        var result = await ExplanationTestCorpus.RequestOkAsync(client, alertId);

        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);

        return alertId;
    }

    private static async Task<string> SummaryOfAsync(HttpClient client, Guid alertId)
    {
        var detail = await AlertTestCorpus.GetAlertAsync(client, alertId);

        Assert.NotNull(detail.Explanation);
        Assert.NotNull(detail.Explanation.Summary);

        return detail.Explanation.Summary;
    }
}
