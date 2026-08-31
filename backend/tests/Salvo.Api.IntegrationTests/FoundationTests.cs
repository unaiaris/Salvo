using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Api;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class FoundationTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public FoundationTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpointReturnsExpectedContract()
    {
        using var client = await factory.CreateMigratedClientAsync();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.Equal("salvo-api", payload.Service);
    }

    [Fact]
    public async Task OpenApiPublishesE2EndpointsWithoutThePerOrderFraudLabel()
    {
        using var client = await factory.CreateMigratedClientAsync();

        var response = await client.GetAsync("/openapi/v1.json");
        var document = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/order-imports", document, StringComparison.Ordinal);
        Assert.Contains("/api/demo-data/seed", document, StringComparison.Ordinal);
        Assert.DoesNotContain("isFraudLabel", document, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitialMigrationCreatesExpectedTablesAndIndexes()
    {
        await factory.InitializeDatabaseAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var connection = dbContext.Database.GetDbConnection();

        var tables = await ReadFirstColumnAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;");
        Assert.Contains("orders", tables);
        Assert.Contains("order_evaluation_labels", tables);
        Assert.DoesNotContain("foundation_checkpoints", tables);

        var indexes = await ReadFirstColumnAsync(connection, "PRAGMA index_list('orders');", 1);
        Assert.Contains("ux_orders_merchant_reference", indexes);
        Assert.Contains("ix_orders_chronological", indexes);
        Assert.Contains("ix_orders_buyer_currency_history", indexes);
    }

    private static async Task<IReadOnlyList<string>> ReadFirstColumnAsync(
        DbConnection connection,
        string sql,
        int ordinal = 0)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(ordinal));
        }

        return values;
    }
}
