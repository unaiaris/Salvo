using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Application.Explanations;
using Salvo.Application.Risk;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// An alert opened before <c>e3-v2</c>, on a database that crossed the version change.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot of an alert is never rewritten (decision 33), so an evaluation stored as English
/// sentences stays that way for as long as the alert exists. This is the part of the change that
/// cannot be tidied away, and the answer is to say what happens rather than to hope nobody looks:
/// the console shows the sentence exactly as the engine wrote it, and asking for an explanation of
/// that row ends in a failure with a code instead of an unhandled exception.
/// </para>
/// <para>
/// The prose is read out of the capture the engine produced, not written here. A hand-made sentence
/// would be prose no evaluation ever carried, which is the difference between testing the case and
/// testing a guess about it. On a freshly seeded database — the one a reviewer runs — none of this
/// exists at all.
/// </para>
/// </remarks>
public sealed class LegacyEvaluationTests
{
    [Fact]
    public async Task AnE3V1SnapshotIsStillReadableAndItsExplanationFailsWithACode()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());

        var prose = CapturedProse();
        var alertId = await OpenLegacyAlertAsync(factory, prose);

        // The console reads it. The sentence is shown as written and no field is invented for it.
        var detail = Assert.IsType<AlertDetail>(
            await (await client.GetAsync($"/api/alerts/{alertId}")).Content.ReadFromJsonAsync<AlertDetail>());
        var signal = Assert.Single(detail.Snapshot.Signals);

        Assert.Equal("amount_anomaly", signal.Rule);
        Assert.Equal(prose, signal.Detail);
        Assert.Null(signal.AmountCents);
        Assert.Null(signal.Ratio);
        Assert.Null(signal.MedianCents);

        // And asking for an explanation of it is answered, not thrown.
        var response = await ExplanationTestCorpus.RequestAsync(client, alertId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = Assert.IsType<RequestExplanationResult>(
            await response.Content.ReadFromJsonAsync<RequestExplanationResult>());

        Assert.Equal("FAILED", result.Explanation.Status);
        Assert.NotNull(result.Explanation.FailureCode);
        Assert.Null(result.Explanation.Summary);

        // The row is closed rather than left reserved, so the evaluation is not jammed for ever.
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.AlertExplanations.AsNoTracking().SingleAsync();

        Assert.Equal("FAILED", stored.Status.ToString().ToUpperInvariant());
        Assert.Null(stored.Summary);
    }

    /// <summary>
    /// The configuration used to explain a row is the configuration <em>that row</em> was written
    /// under, and never a constant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It cannot be seen by comparing paragraphs, because `e3-v1` and `e3-v2` carry the same
    /// thresholds — which is exactly why a mistake here would stay invisible until the day they do
    /// not, and why this test reaches for the one case that is observable now: an evaluation stamped
    /// with a version this build has never heard of. Resolving by row refuses it. A hard-coded
    /// configuration would explain it happily, under thresholds nobody can claim applied to it.
    /// </para>
    /// <para>
    /// It is unreachable through the API — every version that reaches the database is one this build
    /// wrote — so the row is planted directly.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AnEvaluationOfAnUnknownVersionIsRefusedInsteadOfExplainedUnderAnotherOne()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());

        var alertId = await OpenLegacyAlertAsync(factory, CapturedProse(), version: "e3-v0");

        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RequestExplanationHandler>();

        var refused = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(alertId, regenerate: false, CancellationToken.None));

        Assert.Contains("e3-v0", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Persists one `e3-v1` evaluation and the alert it opened, the way a scoring run of that
    /// version would have left them.
    /// </summary>
    private static async Task<Guid> OpenLegacyAlertAsync(
        SalvoApiFactory factory,
        string prose,
        string version = "e3-v1")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();
        var orderId = await dbContext.Orders
            .Where(order => order.MerchantReferenceId == "ORD_DIV_TARGET")
            .Select(order => order.Id)
            .SingleAsync();

        var at = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var runId = Guid.NewGuid();
        var evaluation = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            version,
            new(orderId, at, 70, true, [new(RiskRuleNames.AmountAnomaly, 70) { Detail = prose }]),
            at);
        var alert = Alert.Open(
            Guid.NewGuid(),
            orderId,
            evaluation.Id,
            70,
            evaluation.SignalsJson!,
            AlertPolicy.E4V1.Version,
            supersedesAlertId: null,
            at);

        await store.SaveRunAsync(
            ScoringRun.Complete(runId, 1, version, at, at, 1, 1, 0, 1, 0, 0),
            [evaluation],
            [RunEvaluation.Create(runId, orderId, evaluation.Id)],
            [alert],
            CancellationToken.None);

        return alert.Id;
    }

    /// <summary>One sentence the engine really wrote, taken from the frozen capture.</summary>
    private static string CapturedProse([CallerFilePath] string thisFile = "")
    {
        var path = Path.Combine(Path.GetDirectoryName(thisFile)!, "Goldens", "signal-facts.v2.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        foreach (var entry in document.RootElement.EnumerateArray())
        {
            foreach (var signal in entry.GetProperty("signals").EnumerateArray())
            {
                if (signal.GetProperty("rule").GetString() == RiskRuleNames.AmountAnomaly)
                {
                    return signal.GetProperty("detail").GetString()!;
                }
            }
        }

        throw new InvalidOperationException("The capture carries no amount_anomaly signal.");
    }
}
