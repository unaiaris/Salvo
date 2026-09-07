using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The check that decides whether a generated summary may be stored.
/// </summary>
/// <remarks>
/// Two halves that pull in opposite directions, and both have to hold. A summary carrying a figure
/// nobody computed is refused, and the refusal has to come from the use case rather than from the
/// goodwill of a provider. And a summary that is entirely correct has to be accepted — which is not
/// a given, because a naive check against «the supplied fields» rejects an amount written in units,
/// a rounded percentage and an instant in business time, all of which are true.
/// </remarks>
public sealed class ExplanationGroundingTests
{
    /// <summary>
    /// A provider that invents a figure produces a failed explanation and no text.
    /// </summary>
    /// <remarks>
    /// The invented figure is chosen at run time — the smallest positive integer the facts do not
    /// back, computed with the same rule the check applies — rather than written into the test. A
    /// hard-coded number stops being invented the day the corpus changes, and the test would then
    /// pass without asserting anything.
    /// <para>
    /// Falsification, recorded in the handoff: removing the call to
    /// <c>ExplanationGrounding.Verify</c> from <c>RequestExplanationHandler.GenerateAsync</c> turns
    /// this red. There is nothing to remove from the provider, which is the point: the provider used
    /// here validates nothing at all, and the refusal still happens.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AnInventedFigureIsRefusedAndNoTextIsStored()
    {
        var provider = new ExplanationTestCorpus.SwitchableProvider
        {
            Behaviour = ExplanationTestCorpus.ProviderBehaviour.InventANumber,
        };
        await using var factory = Factory(provider);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client, AlertTestCorpus.DivergenceBase());

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        // The attempt happened — the row exists and was written — and it did not produce a summary.
        Assert.True(result.Applied);
        Assert.Equal(ExplanationWireNames.Failed, result.Explanation.Status);
        Assert.Equal(ExplanationWireNames.NotGroundedNumber, result.Explanation.FailureCode);
        Assert.Null(result.Explanation.Summary);
        Assert.Empty(result.Explanation.ReferencedRules);
        Assert.Equal(1, provider.Calls);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.AlertExplanations.AsNoTracking().SingleAsync();

        Assert.Null(stored.Summary);
        Assert.Null(stored.ReferencedRulesJson);

        // The diagnosis names the figure and nothing else. The sentence that carried it is not
        // stored, not logged and not reachable from here.
        Assert.NotNull(stored.FailureDetail);
        Assert.DoesNotContain("mediana", stored.FailureDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("El monto", stored.FailureDetail, StringComparison.Ordinal);
        Assert.Matches(@"^\d+$", stored.FailureDetail);
    }

    [Fact]
    public async Task ARuleTheEvaluationNeverRaisedIsRefused()
    {
        var provider = new ExplanationTestCorpus.SwitchableProvider
        {
            Behaviour = ExplanationTestCorpus.ProviderBehaviour.CiteAnUnraisedRule,
        };
        await using var factory = Factory(provider);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client, AlertTestCorpus.DivergenceBase());

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Failed, result.Explanation.Status);
        Assert.Equal(ExplanationWireNames.NotGroundedRule, result.Explanation.FailureCode);
        Assert.Null(result.Explanation.Summary);
    }

    /// <summary>
    /// A provider may decline to answer, and a template may fall over. Neither is a server error and
    /// neither leaves the row open.
    /// </summary>
    [Fact]
    public async Task AProviderThatDeclinesToWriteIsRecordedRatherThanRaised()
    {
        await AssertSettlesWithAsync(
            ExplanationTestCorpus.ProviderBehaviour.Refuse,
            ExplanationWireNames.ProviderRefused);
    }

    [Fact]
    public async Task AProviderThatThrowsIsRecordedRatherThanRaised()
    {
        await AssertSettlesWithAsync(
            ExplanationTestCorpus.ProviderBehaviour.Throw,
            ExplanationWireNames.ProviderUnavailable);
    }

    private static async Task AssertSettlesWithAsync(
        ExplanationTestCorpus.ProviderBehaviour behaviour,
        string expectedCode)
    {
        var provider = new ExplanationTestCorpus.SwitchableProvider { Behaviour = behaviour };
        await using var factory = Factory(provider);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client, AlertTestCorpus.DivergenceBase());

        var response = await ExplanationTestCorpus.RequestAsync(client, alert.Id);
        var result = Assert.IsType<RequestExplanationResult>(
            await response.Content.ReadFromJsonAsync<RequestExplanationResult>());

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ExplanationWireNames.Failed, result.Explanation.Status);
        Assert.Equal(expectedCode, result.Explanation.FailureCode);
    }

    /// <summary>
    /// The deterministic template passes the very check a model would, on every alert of the demo
    /// corpus.
    /// </summary>
    /// <remarks>
    /// This is the half that catches a check which is too strict, and it is worth more than a
    /// hand-written example because the template writes an amount in units, a rounded percentage and
    /// an instant in business time on every single one of these. If any of the three were missing
    /// from the fact set, eighteen explanations would fail here rather than one.
    /// </remarks>
    [Fact]
    public async Task TheTemplatePassesItsOwnCheckOnEveryAlertOfTheDemoCorpus()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?status=OPEN&pageSize=200");
        Assert.Equal(23, alerts.TotalCount);

        foreach (var alert in alerts.Items)
        {
            var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

            Assert.True(
                result.Explanation.Status == ExplanationWireNames.Ready,
                $"Alert {alert.Id} failed with {result.Explanation.FailureCode}.");
            Assert.NotNull(result.Explanation.Summary);
            Assert.NotEmpty(result.Explanation.ReferencedRules);
        }
    }

    /// <summary>
    /// The three shapes of correct sentence a literal check would have refused, one alert each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Falsification, recorded in the handoff: dropping the amount in units from
    /// <c>ExplanationFacts</c> fails the first, dropping the rounding rule fails the second, and
    /// dropping the business-time instant fails the third.
    /// </para>
    /// <para>
    /// Between them the three scenarios raise all six rules, so this is also where the template is
    /// shown to have a sentence for each.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ThreeAlertsWithDifferentSignalsAllPass()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        // Amount anomaly and a buyer with no history: no percentage anywhere in this one.
        var newBuyer = await OneAlertAsync(client, AlertTestCorpus.DivergenceBase());
        var newBuyerText = await SummaryAsync(client, newBuyer.Id);

        // Amount anomaly, velocity, cross-border velocity and foreign country: weights that sum
        // past the cap, and a share to round.
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBase());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBackfill());
        await AlertTestCorpus.RunScoringAsync(client);
        var crossBorder = await AlertForAsync(client, "ORD_ESC_TARGET");
        var crossBorderText = await SummaryAsync(client, crossBorder.Id);

        // Amount anomaly, velocity, unusual hour and foreign country: the local bucket.
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBase());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBackfill());
        await AlertTestCorpus.RunScoringAsync(client);
        var unusualHour = await AlertForAsync(client, "ORD_BAND_TARGET");
        var unusualHourText = await SummaryAsync(client, unusualHour.Id);

        // The amount, in units, with the thousands point and decimal comma of the console. Three
        // hundred cents is «3,00»; a check against the stored 300 alone would refuse it.
        Assert.Contains("3,00", newBuyerText, StringComparison.Ordinal);
        Assert.Contains("no tenía pedidos previos", newBuyerText, StringComparison.Ordinal);
        Assert.DoesNotContain("%", newBuyerText, StringComparison.Ordinal);

        // A percentage the template rounded away from what the engine wrote, and the cap sentence.
        Assert.Contains("%", crossBorderText, StringComparison.Ordinal);
        Assert.Contains("El país cambió", crossBorderText, StringComparison.Ordinal);
        Assert.Contains("se limita a 100", crossBorderText, StringComparison.Ordinal);

        // The bucket, and the burst.
        Assert.Contains("La franja de", unusualHourText, StringComparison.Ordinal);
        Assert.Contains("pedidos del mismo comprador", unusualHourText, StringComparison.Ordinal);

        // Every corpus here places the order at 06:00 UTC, which is 03:00 in the business zone the
        // console and the rules both read. Writing the stored hour instead would put this sentence
        // on a different clock from the rest of the page.
        Assert.All(
            new[] { newBuyerText, crossBorderText, unusualHourText },
            text => Assert.Contains("a las 03:00, hora del comercio", text, StringComparison.Ordinal));
    }

    /// <summary>
    /// Every rule the engine can raise has a sentence, and the three scenarios above raise all six.
    /// </summary>
    [Fact]
    public async Task TheTemplateHasASentenceForEveryRule()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var cited = new HashSet<string>(StringComparer.Ordinal);

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBase());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBackfill());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBase());
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBackfill());
        await AlertTestCorpus.RunScoringAsync(client);

        foreach (var alert in (await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200")).Items)
        {
            var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

            Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);
            foreach (var rule in result.Explanation.ReferencedRules)
            {
                cited.Add(rule);
            }
        }

        Assert.Equal(RiskRuleNames.CanonicalOrder.Order(), cited.Order());
    }

    private static SalvoApiFactory Factory(IExplanationProvider provider)
    {
        return new()
        {
            ConfigureTestServices = services => services.AddScoped(_ => provider),
        };
    }

    private static async Task<AlertListItem> OneAlertAsync(HttpClient client, string corpus)
    {
        await AlertTestCorpus.ImportAsync(client, corpus);
        await AlertTestCorpus.RunScoringAsync(client);

        return Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
    }

    private static async Task<AlertListItem> AlertForAsync(HttpClient client, string reference)
    {
        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200");

        return Assert.Single(
            alerts.Items,
            alert => alert.MerchantReferenceId == reference);
    }

    private static async Task<string> SummaryAsync(HttpClient client, Guid alertId)
    {
        var result = await ExplanationTestCorpus.RequestOkAsync(client, alertId);

        Assert.True(
            result.Explanation.Status == ExplanationWireNames.Ready,
            $"Alert {alertId} failed with {result.Explanation.FailureCode}.");

        return result.Explanation.Summary!;
    }
}
