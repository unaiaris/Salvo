namespace Salvo.Domain.Orders;

public sealed record OrderDraft(
    Guid Id,
    string? MerchantId,
    string? MerchantReferenceId,
    string? BuyerReferenceId,
    DateTimeOffset? OccurredAt,
    long? AmountCents,
    string? CurrencyCode,
    string? CountryCode,
    string? City,
    string? Channel,
    string? DeviceSessionId,
    DateTimeOffset? CreatedAt);
