using Salvo.Domain.Orders;

namespace Salvo.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void CreateNormalizesApprovedValuesAndUtcTimestamps()
    {
        var decomposedCity = "  Montevideo\u0301   Centro  ";
        var result = Order.Create(CreateValidDraft() with
        {
            MerchantId = " mer_shop-1 ",
            CurrencyCode = " uyu ",
            CountryCode = " uy ",
            City = decomposedCity,
            Channel = " mobile_app ",
            DeviceSessionId = " dev_session-1 ",
            OccurredAt = new DateTimeOffset(2026, 8, 1, 10, 30, 0, TimeSpan.FromHours(-3)),
        });

        Assert.True(result.IsSuccess);
        var order = Assert.IsType<Order>(result.Order);
        Assert.Equal("MER_SHOP-1", order.MerchantId);
        Assert.Equal("UYU", order.CurrencyCode);
        Assert.Equal("UY", order.CountryCode);
        Assert.Equal("Montevideó Centro", order.City);
        Assert.Equal(OrderChannel.MobileApp, order.Channel);
        Assert.Equal("DEV_SESSION-1", order.DeviceSessionId);
        Assert.Equal(TimeSpan.Zero, order.OccurredAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 13, 30, 0, TimeSpan.Zero), order.OccurredAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000_000_001)]
    public void CreateRejectsAmountsOutsideApprovedRange(long amountCents)
    {
        var result = Order.Create(CreateValidDraft() with { AmountCents = amountCents });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Field == "amountCents" && error.Code == "OUT_OF_RANGE");
    }

    [Fact]
    public void CreateRejectsMissingCurrencyAndInvalidIdentifiersWithoutFrameworkDependencies()
    {
        var result = Order.Create(CreateValidDraft() with
        {
            MerchantId = "merchant@example.test",
            BuyerReferenceId = "BUY_Á",
            CurrencyCode = null,
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Field == "merchantId");
        Assert.Contains(result.Errors, error => error.Field == "buyerReferenceId");
        Assert.Contains(result.Errors, error =>
            error.Field == "currencyCode" && error.Code == "REQUIRED");
    }

    [Fact]
    public void CreateRejectsAnIdentifierThatContainsOnlyItsPrefix()
    {
        var result = Order.Create(CreateValidDraft() with { MerchantId = "MER_" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Field == "merchantId" && error.Code == "INVALID_FORMAT");
    }

    [Fact]
    public void BusinessEqualityIgnoresTechnicalIdentityButNotImmutableFacts()
    {
        var first = Assert.IsType<Order>(Order.Create(CreateValidDraft()).Order);
        var sameFacts = Assert.IsType<Order>(Order.Create(CreateValidDraft() with
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero),
        }).Order);
        var changedAmount = Assert.IsType<Order>(Order.Create(CreateValidDraft() with
        {
            Id = Guid.NewGuid(),
            AmountCents = 50_001,
        }).Order);

        Assert.True(first.HasSameBusinessFactsAs(sameFacts));
        Assert.False(first.HasSameBusinessFactsAs(changedAmount));
    }

    private static OrderDraft CreateValidDraft()
    {
        return new(
            Guid.NewGuid(),
            "MER_SHOP",
            "ORD_0001",
            "BUY_0001",
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
            50_000,
            "UYU",
            "UY",
            "Montevideo",
            "WEB",
            null,
            new DateTimeOffset(2026, 8, 1, 12, 1, 0, TimeSpan.Zero));
    }
}
