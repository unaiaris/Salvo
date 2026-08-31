namespace Salvo.Application.Orders.Importing;

public static class OrderImportFields
{
    public const string MerchantId = "merchantId";
    public const string MerchantReferenceId = "merchantReferenceId";
    public const string BuyerReferenceId = "buyerReferenceId";
    public const string OccurredAt = "occurredAt";
    public const string AmountCents = "amountCents";
    public const string CurrencyCode = "currencyCode";
    public const string CountryCode = "countryCode";
    public const string City = "city";
    public const string Channel = "channel";
    public const string DeviceSessionId = "deviceSessionId";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MerchantId,
        MerchantReferenceId,
        BuyerReferenceId,
        OccurredAt,
        AmountCents,
        CurrencyCode,
        CountryCode,
        City,
        Channel,
        DeviceSessionId,
    };

    public static readonly IReadOnlySet<string> Required = new HashSet<string>(StringComparer.Ordinal)
    {
        MerchantId,
        MerchantReferenceId,
        BuyerReferenceId,
        OccurredAt,
        AmountCents,
        CurrencyCode,
        CountryCode,
    };
}
