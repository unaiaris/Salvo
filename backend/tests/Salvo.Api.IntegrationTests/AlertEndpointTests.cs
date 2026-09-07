using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Salvo.Application.Alerts;

namespace Salvo.Api.IntegrationTests;

public sealed class AlertEndpointTests
{
    [Fact]
    public async Task ListingFiltersByStatusAndBySeverityAndPagesDeterministically()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var all = await AlertTestCorpus.ListAlertsAsync(client);
        var medium = await AlertTestCorpus.ListAlertsAsync(client, "?severity=MEDIUM");
        var high = await AlertTestCorpus.ListAlertsAsync(client, "?severity=HIGH");
        var critical = await AlertTestCorpus.ListAlertsAsync(client, "?severity=CRITICAL&pageSize=200");

        Assert.Equal(23, all.TotalCount);
        Assert.Equal(11, medium.TotalCount);
        Assert.Equal(6, high.TotalCount);
        Assert.Equal(6, critical.TotalCount);
        Assert.All(high.Items, alert => Assert.Equal("HIGH", alert.Severity));
        Assert.All(medium.Items, alert => Assert.Equal("MEDIUM", alert.Severity));
        Assert.All(critical.Items, alert => Assert.Equal("CRITICAL", alert.Severity));

        (await AlertTestCorpus.ReviewAsync(client, all.Items[0].Id, "CONFIRMED_SAFE", "known buyer"))
            .EnsureSuccessStatusCode();

        var open = await AlertTestCorpus.ListAlertsAsync(client, "?status=OPEN&pageSize=200");
        var reviewed = await AlertTestCorpus.ListAlertsAsync(client, "?status=CONFIRMED_SAFE");

        Assert.Equal(22, open.TotalCount);
        Assert.Equal(all.Items[0].Id, Assert.Single(reviewed.Items).Id);

        var firstPage = await AlertTestCorpus.ListAlertsAsync(client, "?page=1&pageSize=5");
        var secondPage = await AlertTestCorpus.ListAlertsAsync(client, "?page=2&pageSize=5");

        Assert.Equal(23, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(5, secondPage.Items.Count);
        Assert.Empty(firstPage.Items.Select(alert => alert.Id)
            .Intersect(secondPage.Items.Select(alert => alert.Id)));
    }

    [Fact]
    public async Task InvalidQueriesAndUnknownIdentifiersAreRejectedWithoutAServerError()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        var missing = Guid.NewGuid();

        var badStatus = await client.GetAsync("/api/alerts?status=CLOSED");
        var badSeverity = await client.GetAsync("/api/alerts?severity=LOW");
        var badPage = await client.GetAsync("/api/alerts?page=0");
        var badPageSize = await client.GetAsync("/api/alerts?pageSize=1000");
        var missingDetail = await client.GetAsync($"/api/alerts/{missing}");
        var missingReview = await AlertTestCorpus.ReviewAsync(client, missing, "CONFIRMED_SAFE");

        var alertId = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items).Id;
        var reopen = await AlertTestCorpus.ReviewAsync(client, alertId, "OPEN");
        var nonsense = await AlertTestCorpus.ReviewAsync(client, alertId, "MAYBE");

        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badSeverity.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPageSize.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingReview.StatusCode);

        // A verdict is terminal by design, so there is no request that reopens an alert.
        Assert.Equal(HttpStatusCode.BadRequest, reopen.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nonsense.StatusCode);
    }

    [Fact]
    public async Task TheAlertContractNeverExposesGroundTruth()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);
        var alertId = (await AlertTestCorpus.ListAlertsAsync(client)).Items[0].Id;

        var listing = await client.GetStringAsync("/api/alerts?pageSize=200");
        var withUnknownParameter = await client.GetStringAsync("/api/alerts?pageSize=200&isFraudLabel=true");
        var detail = await client.GetStringAsync($"/api/alerts/{alertId}");

        // The demo corpus does carry labels; nothing on this path may read them, and an unknown
        // query parameter must not open a side door either.
        foreach (var payload in new[] { listing, withUnknownParameter, detail })
        {
            using var document = JsonDocument.Parse(payload);
            foreach (var name in ReadPropertyNames(document.RootElement))
            {
                Assert.DoesNotContain("fraudlabel", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("label", name, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.Equal(listing, withUnknownParameter);
    }

    [Fact]
    public async Task TheDetailCarriesTheSnapshotSignalsAndTheOrderContext()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        var alertId = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items).Id;

        var detail = Assert.IsType<AlertDetail>(
            await (await client.GetAsync($"/api/alerts/{alertId}")).Content.ReadFromJsonAsync<AlertDetail>());

        Assert.Equal("ORD_DIV_TARGET", detail.Order.MerchantReferenceId);
        Assert.Equal("BUY_DIV", detail.Order.BuyerReferenceId);
        Assert.Equal(300, detail.Order.AmountCents);
        Assert.Equal("e4-v1", detail.AlertPolicyVersion);
        Assert.Equal("HIGH", detail.Severity);
        Assert.Equal(70, detail.Snapshot.Score);
        Assert.Equal(70, detail.Snapshot.Signals.Sum(signal => signal.Weight));
        Assert.All(detail.Snapshot.Signals, signal => Assert.NotEmpty(signal.Detail));
        Assert.Equal(detail.Snapshot.EvaluationId, detail.CurrentEvaluation?.EvaluationId);
        Assert.Null(detail.Review);
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
