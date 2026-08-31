using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class OrderChannelConverter()
    : ValueConverter<OrderChannel, string>(
        value => ToProvider(value),
        value => FromProvider(value))
{
    private static string ToProvider(OrderChannel value)
    {
        return value switch
        {
            OrderChannel.Web => "WEB",
            OrderChannel.MobileApp => "MOBILE_APP",
            OrderChannel.Marketplace => "MARKETPLACE",
            _ => throw new InvalidOperationException("Unsupported order channel."),
        };
    }

    private static OrderChannel FromProvider(string value)
    {
        return value switch
        {
            "WEB" => OrderChannel.Web,
            "MOBILE_APP" => OrderChannel.MobileApp,
            "MARKETPLACE" => OrderChannel.Marketplace,
            _ => throw new InvalidOperationException("Unsupported persisted order channel."),
        };
    }
}
