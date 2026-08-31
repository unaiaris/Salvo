using System.Net;
using System.Net.Http.Json;
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
        Assert.Equal(18, first.FraudLabelCount);
        Assert.Equal(0, second.InsertedOrders);
        Assert.Equal(0, second.InsertedLabels);
        Assert.Equal(300, second.DuplicateOrders);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var orders = await dbContext.Orders.AsNoTracking().ToListAsync();
        var labels = await dbContext.OrderEvaluationLabels.AsNoTracking().ToListAsync();

        Assert.Equal(300, orders.Count);
        Assert.Equal(300, labels.Count);
        Assert.Equal(18, labels.Count(label => label.IsFraudLabel));
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
