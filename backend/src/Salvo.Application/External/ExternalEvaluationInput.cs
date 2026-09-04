namespace Salvo.Application.External;

/// <summary>
/// What an external provider is told about an order. Every identifier is pseudonymous and no field
/// carries payment data: the provider gets what it needs to form an opinion and nothing else.
/// </summary>
public sealed record ExternalEvaluationInput(
    Guid OrderId,
    string ReferenceId,
    string MerchantId,
    string MerchantReferenceId,
    string BuyerReferenceId,
    DateTimeOffset OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode);
