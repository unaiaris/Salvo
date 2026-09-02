namespace Salvo.Application.Orders;

public sealed record ListOrdersResult(
    IReadOnlyList<OrderListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>
/// The public projection of an order. The fields are enumerated explicitly so that nothing outside
/// this list can reach a client; ground-truth labels live in a separate entity and are never read
/// by this path.
/// </summary>
public sealed record OrderListItem(
    Guid Id,
    string MerchantId,
    string MerchantReferenceId,
    string BuyerReferenceId,
    DateTimeOffset OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    string? City,
    string? Channel,
    string? DeviceSessionId,
    DateTimeOffset CreatedAt);
