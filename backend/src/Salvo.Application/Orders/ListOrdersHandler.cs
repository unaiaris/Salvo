using Salvo.Domain.Orders;

namespace Salvo.Application.Orders;

public sealed class ListOrdersHandler(IOrderPageReader reader)
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 200;

    public async Task<ListOrdersResult> HandleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaximumPageSize);

        var result = await reader.GetPageAsync(page, pageSize, cancellationToken);

        return new(
            result.Items.Select(ToListItem).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    private static OrderListItem ToListItem(Order order)
    {
        return new(
            order.Id,
            order.MerchantId,
            order.MerchantReferenceId,
            order.BuyerReferenceId,
            order.OccurredAt,
            order.AmountCents,
            order.CurrencyCode,
            order.CountryCode,
            order.City,
            ToWire(order.Channel),
            order.DeviceSessionId,
            order.CreatedAt);
    }

    private static string? ToWire(OrderChannel? channel)
    {
        return channel switch
        {
            null => null,
            OrderChannel.Web => "WEB",
            OrderChannel.MobileApp => "MOBILE_APP",
            OrderChannel.Marketplace => "MARKETPLACE",
            _ => throw new InvalidOperationException("Unsupported order channel."),
        };
    }
}
