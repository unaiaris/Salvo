using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Application.Dashboard;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class DashboardEndpointTests
{
    /// <summary>
    /// The invariant of the whole stage: the operational dashboard is not a function of the fraud
    /// label.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reflection test over constructor parameters cannot establish this. There is a second port
    /// that reads labels, a handler can consume another handler that reads them, and above all
    /// every EF store receives the whole <see cref="SalvoDbContext"/> and could join
    /// <c>order_evaluation_labels</c> without any type in the application layer showing it. This
    /// test does not care where the leak would be: it flips every label in the database and demands
    /// the same bytes back.
    /// </para>
    /// <para>
    /// The external evaluations are requested first, on purpose. The panel of orders a provider
    /// denied without a local alert joins three tables and is the newest place where a label could
    /// enter; leaving it empty would make the strongest assertion of the suite pass over a field
    /// that has nothing in it.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task TheDashboardIsIndependentOfGroundTruth()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        (await client.PostAsync("/api/demo-data/external-evaluations:request", null))
            .EnsureSuccessStatusCode();

        var before = await client.GetStringAsync("/api/dashboard");
        var flipped = await FlipEveryLabelAsync(factory);
        var after = await client.GetStringAsync("/api/dashboard");

        // Without this the test would also pass against an empty label table, which is the one
        // situation where flipping nothing proves nothing.
        Assert.Equal(300, flipped);
        Assert.Equal(before, after);

        // And the panel really does have rows to be independent of.
        using var document = JsonDocument.Parse(before);
        Assert.True(
            document.RootElement
                .GetProperty("externalDenialsWithoutAlert")
                .GetProperty("total")
                .GetInt32() > 0);
    }

    [Fact]
    public async Task TheDashboardContractNeverExposesGroundTruth()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var payload = await client.GetStringAsync("/api/dashboard");

        using var document = JsonDocument.Parse(payload);
        foreach (var name in ReadPropertyNames(document.RootElement))
        {
            Assert.DoesNotContain("fraudlabel", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("label", name, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Money at risk is reported one currency at a time. The demo corpus mixes three, and a single
    /// figure over them would be a number without a unit.
    /// </summary>
    /// <summary>
    /// The panel that makes an order without an alert visible at all.
    /// </summary>
    /// <remarks>
    /// The provider's verdict lives, everywhere else in the console, inside the detail of an alert,
    /// and an order the rules never flagged has no detail to open. Three of the seven archetypes of
    /// the stage 9 corpus are fraud the deterministic rules cannot see, and their references sit in
    /// the band the simulated provider denies precisely so that this panel has something true to
    /// show. Without it, "there is fraud a provider sees and we do not" is prose.
    /// </remarks>
    [Fact]
    public async Task TheExternalDenialsPanelShowsTheFraudTheRulesNeverFlagged()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var empty = (await GetDashboardAsync(client)).ExternalDenialsWithoutAlert;

        (await client.PostAsync("/api/demo-data/external-evaluations:request", null))
            .EnsureSuccessStatusCode();

        var panel = (await GetDashboardAsync(client)).ExternalDenialsWithoutAlert;
        var references = panel.Items.Select(item => item.MerchantReferenceId).ToArray();

        // Nobody asked the provider anything yet, so there is nothing to report and the panel says
        // so instead of being absent.
        Assert.Equal(0, empty.Total);
        Assert.Empty(empty.Items);

        Assert.Equal(43, panel.Total);
        Assert.Equal(panel.Total, panel.Listed);

        // The three archetypes the rules cannot see: the friendly fraud, the taken-over account
        // seen only in its device, and the card-testing burst.
        Assert.Contains("ORD_000275", references);
        Assert.Contains("ORD_000277", references);
        Assert.Contains("ORD_000075", references);

        // Every row really is an order without an alert, and the local score travels with it so the
        // disagreement is legible instead of implied.
        var alerted = (await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200")).Items
            .Select(alert => alert.MerchantReferenceId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(references, reference => Assert.DoesNotContain(reference, alerted));
        Assert.All(panel.Items, item =>
        {
            Assert.NotNull(item.LocalRiskScore);
            Assert.True(item.LocalRiskScore < 60);
        });

        // Newest first, so the corpus is read the way an analyst reads a queue.
        Assert.Equal(
            panel.Items.Select(item => item.OccurredAt).OrderByDescending(instant => instant),
            panel.Items.Select(item => item.OccurredAt));
    }

    [Fact]
    public async Task AmountAtRiskIsPerCurrencyAndCarriesNoTotal()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var dashboard = await GetDashboardAsync(client);
        var openAlerts = await AlertTestCorpus.ListAlertsAsync(client, "?status=OPEN&pageSize=200");
        var expected = openAlerts.Items
            .GroupBy(alert => alert.CurrencyCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DashboardAmountAtRiskView(
                group.Key,
                group.Sum(alert => alert.AmountCents),
                group.Count()))
            .ToArray();

        Assert.Equal(["BRL", "USD", "UYU"], dashboard.AmountAtRisk.Select(entry => entry.CurrencyCode));
        Assert.Equal(expected, dashboard.AmountAtRisk);
        Assert.Equal(
            [("BRL", 12), ("USD", 5), ("UYU", 6)],
            dashboard.AmountAtRisk.Select(entry => (entry.CurrencyCode, entry.AlertCount)));
        Assert.Equal(23, dashboard.OpenAlerts.Total);
        Assert.Equal(
            [("CRITICAL", 6), ("HIGH", 6), ("MEDIUM", 11)],
            dashboard.OpenAlerts.BySeverity.Select(entry => (entry.Severity, entry.AlertCount)));

        // Every entry carries a currency and nothing that aggregates across currencies: the shape
        // itself is what makes a cross-currency total unrepresentable.
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/dashboard"));
        foreach (var entry in document.RootElement.GetProperty("amountAtRisk").EnumerateArray())
        {
            Assert.Equal(
                ["currencyCode", "amountCents", "alertCount"],
                entry.EnumerateObject().Select(property => property.Name));
        }
    }

    /// <summary>
    /// An order escalated into a higher band carries two reported alerts and is still a single
    /// loss. Aggregating by alert would report its amount twice.
    /// </summary>
    [Fact]
    public async Task ReportedFraudCountsAnEscalatedOrderOnce()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBase());
        await AlertTestCorpus.RunScoringAsync(client);

        var first = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        Assert.Equal("MEDIUM", first.Severity);
        (await AlertTestCorpus.ReviewAsync(client, first.Id, "REPORTED_FRAUD", "confirmed"))
            .EnsureSuccessStatusCode();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBackfill());
        await AlertTestCorpus.RunScoringAsync(client);

        var escalated = Assert.Single(
            (await AlertTestCorpus.ListAlertsAsync(client, "?status=OPEN")).Items);
        Assert.Equal("CRITICAL", escalated.Severity);
        Assert.Equal(first.OrderId, escalated.OrderId);
        (await AlertTestCorpus.ReviewAsync(client, escalated.Id, "REPORTED_FRAUD", "escalated"))
            .EnsureSuccessStatusCode();

        var dashboard = await GetDashboardAsync(client);
        var reported = Assert.Single(dashboard.ReportedFraud);

        Assert.Equal(2, (await AlertTestCorpus.ListAlertsAsync(client, "?status=REPORTED_FRAUD")).TotalCount);
        Assert.Equal(1, reported.OrderCount);
        Assert.Equal(first.AmountCents, reported.AmountCents);
        Assert.Equal(first.CurrencyCode, reported.CurrencyCode);
        Assert.Empty(dashboard.AmountAtRisk);
    }

    /// <summary>
    /// Imported but not scored is a state of its own, and the dashboard has to say so rather than
    /// present the previous run as if it described the corpus on screen.
    /// </summary>
    [Fact]
    public async Task AnUnscoredCorpusIsDescribedInsteadOfBeingPresentedAsCurrent()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var empty = await GetDashboardAsync(client);
        Assert.Null(empty.ScoringRun);
        Assert.Equal(0, empty.OrdersPendingScoring);
        Assert.Null(empty.FlagRate);
        Assert.Empty(empty.RiskOverTime);
        Assert.Empty(empty.TopSignals);

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        var imported = await GetDashboardAsync(client);
        Assert.Null(imported.ScoringRun);
        Assert.Equal(4, imported.OrdersPendingScoring);

        var run = await AlertTestCorpus.RunScoringAsync(client);
        var scored = await GetDashboardAsync(client);
        Assert.Equal(run.Sequence, scored.ScoringRun?.Sequence);

        // Instants are persisted as millisecond-precision ISO 8601, so the run summary returned
        // straight from memory is finer-grained than anything read back from the database.
        Assert.Equal(
            run.CompletedAt.ToUniversalTime().ToString("O")[..23],
            scored.ScoringRun?.CompletedAt.ToUniversalTime().ToString("O")[..23]);
        Assert.Equal(4, scored.ScoringRun?.OrderCount);
        Assert.Equal(0, scored.OrdersPendingScoring);

        // One of the four orders is flagged, and the flag rate is read through the run.
        Assert.Equal(0.25m, scored.FlagRate);

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBackfill());
        var stale = await GetDashboardAsync(client);
        Assert.Equal(run.Sequence, stale.ScoringRun?.Sequence);
        Assert.Equal(3, stale.OrdersPendingScoring);
    }

    /// <summary>
    /// The temporal series is bucketed by when the purchases happened, in the time zone the rules
    /// themselves use, and the signals are counted over the queue an analyst actually faces.
    /// </summary>
    [Fact]
    public async Task RiskOverTimeBucketsBusinessWeeksAndSignalsCountOpenAlerts()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var dashboard = await GetDashboardAsync(client);

        Assert.NotEmpty(dashboard.RiskOverTime);
        Assert.All(dashboard.RiskOverTime, bucket => Assert.Equal(DayOfWeek.Monday, bucket.WeekStart.DayOfWeek));
        Assert.Equal(
            dashboard.RiskOverTime.Select(bucket => bucket.WeekStart).OrderBy(week => week),
            dashboard.RiskOverTime.Select(bucket => bucket.WeekStart));

        // The buckets are contiguous weeks and partition the run.
        for (var index = 1; index < dashboard.RiskOverTime.Count; index++)
        {
            Assert.Equal(
                dashboard.RiskOverTime[index - 1].WeekStart.AddDays(7),
                dashboard.RiskOverTime[index].WeekStart);
        }

        Assert.Equal(300, dashboard.RiskOverTime.Sum(bucket => bucket.OrderCount));
        Assert.Equal(
            dashboard.RiskOverTime.Sum(bucket => bucket.FlaggedCount),
            (int)Math.Round(dashboard.FlagRate!.Value * 300));

        // Signals are counted over the snapshots of the open alerts, not over every current
        // evaluation: the wider population is dominated by low-weight rules nobody has to act on.
        var openAlerts = await AlertTestCorpus.ListAlertsAsync(client, "?status=OPEN&pageSize=200");
        Assert.NotEmpty(dashboard.TopSignals);
        Assert.All(dashboard.TopSignals, signal => Assert.InRange(signal.AlertCount, 1, openAlerts.TotalCount));
        Assert.Equal(
            dashboard.TopSignals.Select(signal => signal.AlertCount).OrderByDescending(count => count),
            dashboard.TopSignals.Select(signal => signal.AlertCount));
    }

    private static async Task<DashboardResult> GetDashboardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<DashboardResult>(await response.Content.ReadFromJsonAsync<DashboardResult>());
    }

    /// <summary>
    /// Inverts every ground-truth label and returns how many rows changed.
    /// </summary>
    private static async Task<int> FlipEveryLabelAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE order_evaluation_labels SET is_fraud_label = 1 - is_fraud_label");
    }

    private static IEnumerable<string> ReadPropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;

                    foreach (var nested in ReadPropertyNames(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in ReadPropertyNames(item))
                    {
                        yield return nested;
                    }
                }

                break;
            default:
                break;
        }
    }
}
