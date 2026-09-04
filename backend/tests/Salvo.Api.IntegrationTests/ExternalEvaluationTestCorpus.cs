using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using Salvo.Application.External;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Orders whose reference picks the band of the mock provider on purpose, plus the calls the
/// external evaluation tests make.
/// </summary>
/// <remarks>
/// The mock reads the trailing digits of the reference modulo one hundred, so the last two digits
/// of the reference are what decides the outcome. Naming them here keeps every test from repeating
/// the arithmetic.
/// </remarks>
internal static class ExternalEvaluationTestCorpus
{
    public static readonly DateTimeOffset OccurredAt = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Remainder 1: the provider approves.</summary>
    public const string ApprovedReference = "ORD_EXT_000001";

    /// <summary>Remainder 80: the provider denies.</summary>
    public const string DeniedReference = "ORD_EXT_000080";

    /// <summary>Remainder 92: accepted and undecided; an even remainder settles as approved.</summary>
    public const string PendingReference = "ORD_EXT_000092";

    /// <summary>Remainder 93: accepted and undecided; an odd remainder settles as denied.</summary>
    public const string PendingThenDeniedReference = "ORD_EXT_000093";

    /// <summary>Remainder 97: the request never leaves. Settles in error, unreachable.</summary>
    public const string UnreachableReference = "ORD_EXT_000097";

    /// <summary>Remainder 98: refused outright. Settles in error, provider rejected.</summary>
    public const string RejectedReference = "ORD_EXT_000098";

    public const string Merchant = "MER_EXT";

    public static string Orders(params string[] references)
    {
        var records = references.Select((reference, index) => $$"""
              {
                "merchantId": "{{Merchant}}",
                "merchantReferenceId": "{{reference}}",
                "buyerReferenceId": "BUY_EXT_{{index.ToString(CultureInfo.InvariantCulture)}}",
                "occurredAt": "{{OccurredAt.AddMinutes(index).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}}",
                "amountCents": 100,
                "currencyCode": "UYU",
                "countryCode": "UY"
              }
            """);

        return $"[\n{string.Join(",\n", records)}\n]";
    }

    /// <summary>
    /// Imports the given references and returns their order identifiers, keyed by reference.
    /// </summary>
    public static async Task<Dictionary<string, Guid>> ImportAsync(
        HttpClient client,
        params string[] references)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(Orders(references), Encoding.UTF8, "application/json"), "file", "orders.json" },
            { new StringContent("JSON", Encoding.UTF8), "format" },
        };

        var response = await client.PostAsync("/api/order-imports", content);
        response.EnsureSuccessStatusCode();

        var imported = Assert.IsType<ImportOrdersResult>(
            await response.Content.ReadFromJsonAsync<ImportOrdersResult>());
        Assert.Equal(imported.TotalRecords, imported.ImportedCount);

        var orders = await client.GetFromJsonAsync<ListOrdersResult>("/api/orders?pageSize=100");
        Assert.NotNull(orders);

        return orders.Items
            .Where(item => references.Contains(item.MerchantReferenceId, StringComparer.Ordinal))
            .ToDictionary(item => item.MerchantReferenceId, item => item.Id, StringComparer.Ordinal);
    }

    public static Task<HttpResponseMessage> RequestAsync(
        HttpClient client,
        Guid orderId,
        bool requestNew = false,
        string? provider = null)
    {
        return client.PostAsJsonAsync(
            $"/api/orders/{orderId}/external-evaluations",
            new RequestExternalEvaluationRequest(provider, requestNew));
    }

    public static async Task<RequestExternalEvaluationResult> RequestOkAsync(
        HttpClient client,
        Guid orderId,
        bool requestNew = false)
    {
        using var response = await RequestAsync(client, orderId, requestNew);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<RequestExternalEvaluationResult>(
            await response.Content.ReadFromJsonAsync<RequestExternalEvaluationResult>());
    }

    public static async Task<OrderExternalEvaluationsResult> HistoryAsync(HttpClient client, Guid orderId)
    {
        var response = await client.GetAsync($"/api/orders/{orderId}/external-evaluations");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<OrderExternalEvaluationsResult>(
            await response.Content.ReadFromJsonAsync<OrderExternalEvaluationsResult>());
    }

    public static async Task<ReconciliationSummary> ReconcileAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/external-evaluations:reconcile", null);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<ReconciliationSummary>(
            await response.Content.ReadFromJsonAsync<ReconciliationSummary>());
    }

    public static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        return problem?["code"].ToString();
    }
}
