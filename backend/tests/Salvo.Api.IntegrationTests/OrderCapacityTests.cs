using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Salvo.Application.Orders.Importing;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The ceiling on stored orders, which is the defence a rate limit cannot be.
/// </summary>
/// <remarks>
/// A rate limit bounds how often work is asked for. What has to be bounded here is how expensive
/// that work becomes, and the expensive one — a scoring run — costs what it costs because of how
/// many orders there are. An import accepts ten thousand records per file as many times as one
/// likes, so without this a visitor could make every later run slower for everybody with a handful
/// of requests no rate limit would find unusual.
/// </remarks>
public sealed class OrderCapacityTests
{
    [Fact]
    public async Task WithoutACeilingAnImportIsNeverRefusedForSize()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var response = await ImportAsync(client, "MER_NOCAP", 1, 8);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportOrdersResult>();
        Assert.Equal(8, result?.ImportedCount);
    }

    [Fact]
    public async Task AnImportThatWouldCrossTheCeilingIsRefusedWhole()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SharedInstance:MaxOrders"] = "5";
        using var client = await factory.CreateMigratedClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await ImportAsync(client, "MER_CAP", 1, 4)).StatusCode);

        // Four stored, three more asked for: the file is refused entire rather than filled to the
        // brim, because a silent partial import leaves the analyst not knowing what got in.
        var refused = await ImportAsync(client, "MER_CAP", 100, 3);
        var problem = await refused.Content.ReadFromJsonAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("ORDER_LIMIT_REACHED", problem?.Code);

        // And nothing of it was written: the next single order still fits.
        Assert.Equal(HttpStatusCode.OK, (await ImportAsync(client, "MER_CAP", 200, 1)).StatusCode);
    }

    /// <summary>
    /// The ceiling counts what would actually be stored, not the rows of the file.
    /// </summary>
    /// <remarks>
    /// Asking before parsing would have been cheaper and wrong: a ten thousand row file of orders
    /// this store already holds adds nothing, and refusing it would be refusing an import that
    /// cannot fill anything.
    /// </remarks>
    [Fact]
    public async Task ReimportingWhatIsAlreadyStoredDoesNotCountAgainstTheCeiling()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SharedInstance:MaxOrders"] = "4";
        using var client = await factory.CreateMigratedClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await ImportAsync(client, "MER_DUP", 1, 4)).StatusCode);

        var again = await ImportAsync(client, "MER_DUP", 1, 4);
        var result = await again.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(0, result?.ImportedCount);
        Assert.Equal(4, result?.DuplicateCount);
    }

    /// <summary>
    /// A ceiling that cannot be read stops the process instead of being ignored, because a public
    /// instance quietly running without one is the failure this setting exists to prevent.
    /// </summary>
    [Fact]
    public async Task AnUnreadableCeilingRefusesToStart()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SharedInstance:MaxOrders"] = "quinientos";

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await factory.CreateMigratedClientAsync());

        Assert.Contains("SharedInstance:MaxOrders", failure.Message, StringComparison.Ordinal);
    }

    private static Task<HttpResponseMessage> ImportAsync(
        HttpClient client,
        string merchantId,
        int firstReference,
        int count)
    {
        var csv = new StringBuilder(
            "merchantId,merchantReferenceId,buyerReferenceId,occurredAt,amountCents,currencyCode,countryCode\n");

        for (var offset = 0; offset < count; offset++)
        {
            var reference = (firstReference + offset).ToString("D6", CultureInfo.InvariantCulture);
            csv.Append(CultureInfo.InvariantCulture, $"{merchantId},ORD_{reference},BUY_{reference},");
            csv.Append(CultureInfo.InvariantCulture, $"2026-08-01T1{offset % 10}:00:00Z,10000,UYU,UY\n");
        }

        var content = new MultipartFormDataContent
        {
            { new StringContent(csv.ToString(), Encoding.UTF8, "text/csv"), "file", "orders.csv" },
            { new StringContent("CSV", Encoding.UTF8), "format" },
        };

        return client.PostAsync("/api/order-imports", content);
    }

    private sealed record ProblemResponse(string? Code);
}
