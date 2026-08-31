namespace Salvo.Application.Orders.Seed;

public sealed record DemoOrderDocument(
    string Version,
    string CreatedAt,
    IReadOnlyList<DemoOrderRecord> Orders);

public sealed record DemoOrderRecord(
    string MerchantId,
    string MerchantReferenceId,
    string BuyerReferenceId,
    string OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    string? City,
    string? Channel,
    string? DeviceSessionId,
    bool IsFraudLabel);
