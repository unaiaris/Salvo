using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Orders.Importing;
using Salvo.Domain.Orders;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class OrderPersistenceTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public OrderPersistenceTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SqliteFiltersAndOrdersCanonicalUtcTimestampsInDatabase()
    {
        await factory.InitializeDatabaseAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            dbContext.Orders.AddRange(
                CreateOrder("MER_TIME_B", "ORD_TIME_2", new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.FromHours(-3))),
                CreateOrder("MER_TIME_A", "ORD_TIME_3", new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero)),
                CreateOrder("MER_TIME_A", "ORD_TIME_1", new DateTimeOffset(2026, 8, 5, 11, 59, 59, TimeSpan.Zero)));
            await dbContext.SaveChangesAsync();
        }

        await using var queryScope = factory.Services.CreateAsyncScope();
        var queryContext = queryScope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var cutoff = new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        var orders = await queryContext.Orders
            .Where(order => order.OccurredAt >= cutoff)
            .OrderBy(order => order.OccurredAt)
            .ThenBy(order => order.MerchantId)
            .ThenBy(order => order.MerchantReferenceId)
            .ToListAsync();

        Assert.Equal(["ORD_TIME_3", "ORD_TIME_2"], orders.Select(order => order.MerchantReferenceId));
        Assert.All(orders, order => Assert.Equal(TimeSpan.Zero, order.OccurredAt.Offset));

        var rawTimestamps = await queryContext.Database
            .SqlQueryRaw<string>("SELECT occurred_at_utc AS Value FROM orders WHERE merchant_id LIKE 'MER_TIME_%'")
            .ToListAsync();
        Assert.All(rawTimestamps, timestamp =>
        {
            Assert.Equal(24, timestamp.Length);
            Assert.EndsWith("Z", timestamp, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task SameReferenceCanExistAcrossMerchantsButNotWithinOneMerchant()
    {
        await factory.InitializeDatabaseAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        dbContext.Orders.AddRange(
            CreateOrder("MER_KEY_A", "ORD_SHARED", new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero)),
            CreateOrder("MER_KEY_B", "ORD_SHARED", new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();

        dbContext.Orders.Add(
            CreateOrder("MER_KEY_A", "ORD_SHARED", new DateTimeOffset(2026, 8, 6, 11, 0, 0, TimeSpan.Zero)));
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task TechnicalFailureRollsBackAllValidRowsInTheImport()
    {
        await factory.InitializeDatabaseAsync();
        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            await setupContext.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER fail_synthetic_import
                BEFORE INSERT ON orders
                WHEN NEW.merchant_reference_id = 'ORD_ATOMIC_FAIL'
                BEGIN
                    SELECT RAISE(ABORT, 'synthetic technical failure');
                END;
                """);
        }

        const string json = """
            [
              {"merchantId":"MER_ATOMIC","merchantReferenceId":"ORD_ATOMIC_OK","buyerReferenceId":"BUY_ATOMIC_1","occurredAt":"2026-08-07T10:00:00Z","amountCents":100,"currencyCode":"USD","countryCode":"US"},
              {"merchantId":"MER_ATOMIC","merchantReferenceId":"ORD_ATOMIC_FAIL","buyerReferenceId":"BUY_ATOMIC_2","occurredAt":"2026-08-07T11:00:00Z","amountCents":200,"currencyCode":"USD","countryCode":"US"}
            ]
            """;

        await using (var importScope = factory.Services.CreateAsyncScope())
        {
            var handler = importScope.ServiceProvider.GetRequiredService<ImportOrdersHandler>();
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                handler.HandleAsync(stream, OrderImportFormat.Json, CancellationToken.None));
        }

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        Assert.Equal(0, await verifyContext.Orders.CountAsync(order => order.MerchantId == "MER_ATOMIC"));
    }

    private static Order CreateOrder(
        string merchantId,
        string merchantReferenceId,
        DateTimeOffset occurredAt)
    {
        var result = Order.Create(new(
            Guid.NewGuid(),
            merchantId,
            merchantReferenceId,
            "BUY_PERSISTENCE",
            occurredAt,
            100,
            "USD",
            "US",
            null,
            null,
            null,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)));
        return Assert.IsType<Order>(result.Order);
    }
}
