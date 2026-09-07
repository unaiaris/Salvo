using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;
using Salvo.Domain.Orders;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class DemoSeedTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public DemoSeedTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SeedIsFullyIdempotentAndContainsOnlySyntheticPseudonymousData()
    {
        using var client = await factory.CreateMigratedClientAsync();

        var firstResponse = await client.PostAsync("/api/demo-data/seed", null);
        var first = await firstResponse.Content.ReadFromJsonAsync<SeedDemoOrdersResult>();

        Guid[] firstOrderIds;
        (Guid OrderId, bool IsFraudLabel, DateTimeOffset CreatedAt)[] firstLabels;
        await using (var firstScope = factory.Services.CreateAsyncScope())
        {
            var firstContext = firstScope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            firstOrderIds = await firstContext.Orders
                .AsNoTracking()
                .OrderBy(order => order.Id)
                .Select(order => order.Id)
                .ToArrayAsync();
            firstLabels = (await firstContext.OrderEvaluationLabels
                    .AsNoTracking()
                    .OrderBy(label => label.OrderId)
                    .ToArrayAsync())
                .Select(label => (label.OrderId, label.IsFraudLabel, label.CreatedAt))
                .ToArray();
        }

        var secondResponse = await client.PostAsync("/api/demo-data/seed", null);
        var second = await secondResponse.Content.ReadFromJsonAsync<SeedDemoOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(300, first.InsertedOrders);
        Assert.Equal(300, first.InsertedLabels);
        Assert.Equal(28, first.FraudLabelCount);
        Assert.Equal(0, second.InsertedOrders);
        Assert.Equal(0, second.InsertedLabels);
        Assert.Equal(300, second.DuplicateOrders);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var orders = await dbContext.Orders.AsNoTracking().ToListAsync();
        var labels = await dbContext.OrderEvaluationLabels.AsNoTracking().ToListAsync();

        Assert.Equal(300, orders.Count);
        Assert.Equal(300, labels.Count);
        Assert.Equal(28, labels.Count(label => label.IsFraudLabel));
        Assert.Equal(300, orders.Select(order => order.Id).Distinct().Count());
        Assert.Equal(firstOrderIds, orders.OrderBy(order => order.Id).Select(order => order.Id));
        Assert.Equal(
            firstLabels,
            labels
                .OrderBy(label => label.OrderId)
                .Select(label => (label.OrderId, label.IsFraudLabel, label.CreatedAt)));
        Assert.All(orders, order =>
        {
            Assert.StartsWith("MER_", order.MerchantId, StringComparison.Ordinal);
            Assert.StartsWith("ORD_", order.MerchantReferenceId, StringComparison.Ordinal);
            Assert.StartsWith("BUY_", order.BuyerReferenceId, StringComparison.Ordinal);
            Assert.DoesNotContain('@', order.MerchantId + order.MerchantReferenceId + order.BuyerReferenceId);
        });
    }

    /// <summary>
    /// A database holding an earlier version of this corpus is named as such, before the load and
    /// during it, and nothing is written either way.
    /// </summary>
    /// <remarks>
    /// The two conflicts have to be told apart because the next step differs: an earlier corpus
    /// needs a new database — an order is immutable and both versions use the same merchant
    /// references — while imported orders that collide are somebody's file and the answer is to
    /// leave them alone. Telling them apart is not a guess: only the seed writes a ground-truth
    /// label, so a colliding order that carries one came from an earlier seed.
    /// </remarks>
    [Fact]
    public async Task AnEarlierVersionOfTheCorpusIsNamedAsSuchAndNothingIsWritten()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await WritePreviousCorpusOrderAsync(factory);

        var preview = await ReadPreviewAsync(client);
        var response = await client.PostAsync("/api/demo-data/seed", null);

        Assert.Equal("PREVIOUS_CORPUS", preview.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DEMO_DATA_PREVIOUS_CORPUS", await ReadCodeAsync(response));

        // Refusing is free because it happens before the first insert: the database is exactly as
        // it was, with the single order that was already in it.
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        Assert.Equal(1, await dbContext.Orders.CountAsync());
    }

    /// <summary>
    /// An imported order that collides is the other conflict, and it keeps the code it always had.
    /// </summary>
    [Fact]
    public async Task ImportedOrdersThatCollideAreADifferentConflict()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, CollidingImport());

        var preview = await ReadPreviewAsync(client);
        var response = await client.PostAsync("/api/demo-data/seed", null);

        Assert.Equal("IMPORTED_ORDERS", preview.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DEMO_DATA_CONFLICT", await ReadCodeAsync(response));
    }

    /// <summary>
    /// On a database that can take it, the preview says exactly what the load then does, and it
    /// says it without writing.
    /// </summary>
    [Fact]
    public async Task ThePreviewAgreesWithTheLoadAndWritesNothing()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var before = await ReadPreviewAsync(client);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            Assert.Equal(0, await dbContext.Orders.CountAsync());
        }

        var seeded = await client.PostAsync("/api/demo-data/seed", null);
        var result = await seeded.Content.ReadFromJsonAsync<SeedDemoOrdersResult>();
        var after = await ReadPreviewAsync(client);

        Assert.Null(before.Conflict);
        Assert.Equal(DemoDatasetShape.Current.Version, before.DatasetVersion);
        Assert.Equal(300, before.OrdersToInsert);
        Assert.Equal(0, before.DuplicateOrders);

        Assert.NotNull(result);
        Assert.Equal(before.OrdersToInsert, result.InsertedOrders);
        Assert.Equal(before.LabelsToInsert, result.InsertedLabels);

        // And once it is loaded the preview says so, which is what lets the console keep the
        // ordinary case silent.
        Assert.Null(after.Conflict);
        Assert.Equal(0, after.OrdersToInsert);
        Assert.Equal(300, after.DuplicateOrders);
    }

    /// <summary>
    /// One order with a reference of the fixture, other facts, and a ground-truth label: what an
    /// earlier version of the corpus leaves behind.
    /// </summary>
    private static async Task WritePreviousCorpusOrderAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var creation = Order.Create(new(
            Guid.NewGuid(),
            "MER_BR_STORE",
            "ORD_000011",
            "BUY_000999",
            new DateTimeOffset(2026, 5, 5, 5, 15, 0, TimeSpan.Zero),
            999,
            "BRL",
            "BR",
            "São Paulo",
            "WEB",
            null,
            new DateTimeOffset(2026, 5, 5, 5, 15, 0, TimeSpan.Zero)));
        var order = Assert.IsType<Order>(creation.Order);

        dbContext.Orders.Add(order);
        dbContext.OrderEvaluationLabels.Add(new(order.Id, true, order.CreatedAt));
        await dbContext.SaveChangesAsync();
    }

    /// <summary>An imported order on a reference of the fixture. Imports never carry a label.</summary>
    private static string CollidingImport()
    {
        return """
        [
          {
            "merchantId": "MER_BR_STORE",
            "merchantReferenceId": "ORD_000011",
            "buyerReferenceId": "BUY_000999",
            "occurredAt": "2026-05-05T05:15:00.000Z",
            "amountCents": 999,
            "currencyCode": "BRL",
            "countryCode": "BR"
          }
        ]
        """;
    }

    private static async Task<DemoSeedPreviewResult> ReadPreviewAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/demo-data/seed-preview");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<DemoSeedPreviewResult>(
            await response.Content.ReadFromJsonAsync<DemoSeedPreviewResult>());
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("code").GetString();
    }

    [Fact]
    public void PublicContractsAndOrderEntityNeverExposeEvaluationLabel()
    {
        var publicContractProperties = typeof(ImportOrdersResult)
            .GetProperties()
            .Select(property => property.Name)
            .Concat(typeof(ImportRecordError).GetProperties().Select(property => property.Name))
            .Concat(typeof(Order).GetProperties().Select(property => property.Name));

        Assert.DoesNotContain(
            publicContractProperties,
            property => property.Contains("FraudLabel", StringComparison.OrdinalIgnoreCase));

        var fixtureProperties = typeof(DemoOrderRecord).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(fixtureProperties, property =>
            property.Contains("Email", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Phone", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Address", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Name", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Pan", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Cvv", StringComparison.OrdinalIgnoreCase));
    }
}
