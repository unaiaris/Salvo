using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using Salvo.Application.Alerts;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Risk;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Synthetic corpora built so that exactly one order crosses the flag threshold, and so that a
/// retroactive import moves that order in a known direction. Every score below was derived from the
/// six rules of <c>RuleConfig e3-v1</c>; the assertions in the tests pin them down.
/// </summary>
internal static class AlertTestCorpus
{
    /// <summary>The instant of the order under test in every corpus.</summary>
    public static readonly DateTimeOffset Target = new(2026, 8, 20, 6, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Merchant history in Uruguay plus one earlier order of the buyer. The target order is a
    /// Brazilian purchase for three times the merchant median: amount anomaly plus foreign country,
    /// which is 60 and lands in the lowest band.
    /// </summary>
    public static string EscalationBase()
    {
        var orders = new List<TestOrder>();
        for (var index = 0; index < 10; index++)
        {
            orders.Add(new(
                "MER_ESC",
                $"ORD_ESC_HIST_{index}",
                $"BUY_ESC_OTH_{index}",
                Target.AddDays(-60 + index),
                100,
                "UY"));
        }

        orders.Add(new("MER_ESC", "ORD_ESC_PRIOR", "BUY_ESC", Target.AddDays(-30), 100, "UY"));
        orders.Add(new("MER_ESC", "ORD_ESC_TARGET", "BUY_ESC", Target, 300, "BR"));

        return ToJson(orders);
    }

    /// <summary>
    /// Three Argentinian purchases of the same buyer minutes before the target order. They add
    /// velocity and cross-border velocity, which takes the target from 60 to the capped 100 and from
    /// the lowest band to the highest.
    /// </summary>
    public static string EscalationBackfill()
    {
        return ToJson(
        [
            new("MER_ESC", "ORD_ESC_BF_1", "BUY_ESC", Target.AddMinutes(-9), 100, "AR"),
            new("MER_ESC", "ORD_ESC_BF_2", "BUY_ESC", Target.AddMinutes(-6), 100, "AR"),
            new("MER_ESC", "ORD_ESC_BF_3", "BUY_ESC", Target.AddMinutes(-3), 100, "AR"),
        ]);
    }

    /// <summary>
    /// Merchant history in Uruguay plus three Brazilian purchases of the buyer within ten minutes of
    /// the target order. Amount anomaly, velocity and foreign country give the target 90: the same
    /// highest band the backfill below will keep it in.
    /// </summary>
    public static string SameBandBase()
    {
        var orders = new List<TestOrder>();
        for (var index = 0; index < 10; index++)
        {
            orders.Add(new(
                "MER_BAND",
                $"ORD_BAND_HIST_{index}",
                $"BUY_BAND_OTH_{index}",
                Target.AddDays(-60 + index),
                100,
                "UY"));
        }

        orders.Add(new("MER_BAND", "ORD_BAND_BUYER_1", "BUY_BAND", Target.AddMinutes(-9), 100, "BR"));
        orders.Add(new("MER_BAND", "ORD_BAND_BUYER_2", "BUY_BAND", Target.AddMinutes(-6), 100, "BR"));
        orders.Add(new("MER_BAND", "ORD_BAND_BUYER_3", "BUY_BAND", Target.AddMinutes(-3), 100, "BR"));
        orders.Add(new("MER_BAND", "ORD_BAND_TARGET", "BUY_BAND", Target, 300, "BR"));

        return ToJson(orders);
    }

    /// <summary>
    /// Thirty ordinary merchant orders in the local midday bucket within the last thirty days. They
    /// give the unusual hour rule the history it needs, which adds ten points to the target and
    /// takes it from 90 to the capped 100 without leaving the highest band.
    /// </summary>
    public static string SameBandBackfill()
    {
        var start = new DateTimeOffset(2026, 7, 22, 15, 0, 0, TimeSpan.Zero);
        var orders = new List<TestOrder>();
        for (var index = 0; index < 30; index++)
        {
            orders.Add(new(
                "MER_BAND",
                $"ORD_BAND_FILL_{index}",
                $"BUY_BAND_FILL_{index}",
                start.AddDays(index / 2).AddHours(index % 2),
                100,
                "UY"));
        }

        return ToJson(orders);
    }

    /// <summary>
    /// A buyer with no history at all buying three times the merchant median. Amount anomaly plus
    /// new buyer high value is 70, the middle band.
    /// </summary>
    public static string DivergenceBase()
    {
        var orders = new List<TestOrder>();
        for (var index = 0; index < 3; index++)
        {
            orders.Add(new(
                "MER_DIV",
                $"ORD_DIV_HIST_{index}",
                $"BUY_DIV_OTH_{index}",
                Target.AddDays(-70 + index),
                100,
                "UY"));
        }

        orders.Add(new("MER_DIV", "ORD_DIV_TARGET", "BUY_DIV", Target, 300, "UY"));

        return ToJson(orders);
    }

    /// <summary>
    /// Three earlier purchases of the same buyer. The buyer is no longer new and the median of their
    /// own history is 150, so the target drops from 70 to 0: the snapshot of the open alert now
    /// describes a buyer that never existed.
    /// </summary>
    public static string DivergenceBackfill()
    {
        return ToJson(
        [
            new("MER_DIV", "ORD_DIV_BF_1", "BUY_DIV", Target.AddDays(-60), 150, "UY"),
            new("MER_DIV", "ORD_DIV_BF_2", "BUY_DIV", Target.AddDays(-50), 150, "UY"),
            new("MER_DIV", "ORD_DIV_BF_3", "BUY_DIV", Target.AddDays(-40), 150, "UY"),
        ]);
    }

    public static async Task ImportAsync(HttpClient client, string json)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(json, Encoding.UTF8, "application/json"), "file", "orders.json" },
            { new StringContent("JSON", Encoding.UTF8), "format" },
        };

        var response = await client.PostAsync("/api/order-imports", content);
        response.EnsureSuccessStatusCode();

        // A rejected record would silently shrink the corpus and make every score below wrong.
        var result = Assert.IsType<ImportOrdersResult>(
            await response.Content.ReadFromJsonAsync<ImportOrdersResult>());
        Assert.Equal(result.TotalRecords, result.ImportedCount);
    }

    public static async Task<ScoringRunSummary> RunScoringAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/risk-evaluations:run", null);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<ScoringRunSummary>(
            await response.Content.ReadFromJsonAsync<ScoringRunSummary>());
    }

    public static async Task<ListAlertsResult> ListAlertsAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/api/alerts{query}");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<ListAlertsResult>(
            await response.Content.ReadFromJsonAsync<ListAlertsResult>());
    }

    public static async Task<AlertDetail> GetAlertAsync(HttpClient client, Guid alertId)
    {
        var response = await client.GetAsync($"/api/alerts/{alertId}");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<AlertDetail>(await response.Content.ReadFromJsonAsync<AlertDetail>());
    }

    /// <param name="explanationId">
    /// Left empty by every existing caller on purpose: reviewing an alert nobody explained is the
    /// ordinary case, and it has to keep behaving exactly as it did.
    /// </param>
    public static Task<HttpResponseMessage> ReviewAsync(
        HttpClient client,
        Guid alertId,
        string newStatus,
        string? note = null,
        bool acknowledgedDivergence = false,
        Guid? explanationId = null)
    {
        return client.PostAsJsonAsync(
            $"/api/alerts/{alertId}/review",
            new ReviewAlertRequest(newStatus, note, acknowledgedDivergence, explanationId));
    }

    private static string ToJson(IReadOnlyList<TestOrder> orders)
    {
        var records = orders.Select(order => $$"""
              {
                "merchantId": "{{order.MerchantId}}",
                "merchantReferenceId": "{{order.Reference}}",
                "buyerReferenceId": "{{order.BuyerReferenceId}}",
                "occurredAt": "{{order.OccurredAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}}",
                "amountCents": {{order.AmountCents.ToString(CultureInfo.InvariantCulture)}},
                "currencyCode": "UYU",
                "countryCode": "{{order.CountryCode}}"
              }
            """);

        return $"[\n{string.Join(",\n", records)}\n]";
    }

    private sealed record TestOrder(
        string MerchantId,
        string Reference,
        string BuyerReferenceId,
        DateTimeOffset OccurredAt,
        long AmountCents,
        string CountryCode);
}
