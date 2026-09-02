using System.Net;
using System.Net.Http.Json;
using Salvo.Application.Orders;

namespace Salvo.Api.IntegrationTests;

public sealed class OrderListingTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public OrderListingTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task OrdersArePagedChronologicallyAndValidateTheirBounds()
    {
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var firstPage = await ReadPageAsync(client, "/api/orders?page=1&pageSize=2");
        var secondPage = await ReadPageAsync(client, "/api/orders?page=2&pageSize=2");
        var defaultPage = await ReadPageAsync(client, "/api/orders");

        Assert.Equal(300, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(ListOrdersHandler.DefaultPageSize, defaultPage.PageSize);
        Assert.Equal(ListOrdersHandler.DefaultPageSize, defaultPage.Items.Count);
        Assert.Equal("ORD_000001", firstPage.Items[0].MerchantReferenceId);
        Assert.Empty(firstPage.Items.Select(item => item.Id)
            .Intersect(secondPage.Items.Select(item => item.Id)));
        Assert.True(firstPage.Items[^1].OccurredAt <= secondPage.Items[0].OccurredAt);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?pageSize=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?pageSize=201")).StatusCode);
    }

    [Theory]
    [InlineData("/api/orders?pageSize=200")]
    [InlineData("/api/orders?isFraudLabel=true")]
    [InlineData("/api/orders?include=isFraudLabel")]
    [InlineData("/api/orders?fields=isFraudLabel&expand=label")]
    [InlineData("/api/orders?page=1&pageSize=1&sort=isFraudLabel")]
    public async Task OrderListingNeverExposesGroundTruthLabels(string requestUri)
    {
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var response = await client.GetAsync(requestUri);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("fraud", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("label", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "isFraudLabel",
            typeof(OrderListItem).GetProperties().Select(property => property.Name),
            StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<ListOrdersResult> ReadPageAsync(HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<ListOrdersResult>(
            await response.Content.ReadFromJsonAsync<ListOrdersResult>());
    }
}
