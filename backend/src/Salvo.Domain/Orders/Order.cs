using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Salvo.Domain.Orders;

public sealed partial class Order
{
    public const long MaximumAmountCents = 1_000_000_000_000;
    public const int MaximumIdentifierLength = 64;
    public const int MaximumCityLength = 80;

    private static readonly string[] SupportedCurrencies = ["UYU", "BRL", "USD"];

    private Order()
    {
        MerchantId = string.Empty;
        MerchantReferenceId = string.Empty;
        BuyerReferenceId = string.Empty;
        CurrencyCode = string.Empty;
        CountryCode = string.Empty;
    }

    private Order(
        Guid id,
        string merchantId,
        string merchantReferenceId,
        string buyerReferenceId,
        DateTimeOffset occurredAt,
        long amountCents,
        string currencyCode,
        string countryCode,
        string? city,
        OrderChannel? channel,
        string? deviceSessionId,
        DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        MerchantReferenceId = merchantReferenceId;
        BuyerReferenceId = buyerReferenceId;
        OccurredAt = occurredAt;
        AmountCents = amountCents;
        CurrencyCode = currencyCode;
        CountryCode = countryCode;
        City = city;
        Channel = channel;
        DeviceSessionId = deviceSessionId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string MerchantId { get; private set; }

    public string MerchantReferenceId { get; private set; }

    public string BuyerReferenceId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public long AmountCents { get; private set; }

    public string CurrencyCode { get; private set; }

    public string CountryCode { get; private set; }

    public string? City { get; private set; }

    public OrderChannel? Channel { get; private set; }

    public string? DeviceSessionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public OrderReference Reference => new(MerchantId, MerchantReferenceId);

    public static OrderCreationResult Create(OrderDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = new List<OrderValidationError>();
        var merchantId = NormalizeRequiredIdentifier(
            draft.MerchantId,
            "merchantId",
            "MER_",
            errors);
        var merchantReferenceId = NormalizeRequiredIdentifier(
            draft.MerchantReferenceId,
            "merchantReferenceId",
            "ORD_",
            errors);
        var buyerReferenceId = NormalizeRequiredIdentifier(
            draft.BuyerReferenceId,
            "buyerReferenceId",
            "BUY_",
            errors);
        var deviceSessionId = NormalizeOptionalIdentifier(
            draft.DeviceSessionId,
            "deviceSessionId",
            "DEV_",
            errors);
        var currencyCode = NormalizeCurrency(draft.CurrencyCode, errors);
        var countryCode = NormalizeCountry(draft.CountryCode, errors);
        var city = NormalizeCity(draft.City, errors);
        var channel = NormalizeChannel(draft.Channel, errors);

        if (draft.Id == Guid.Empty)
        {
            errors.Add(new("id", "INVALID_FORMAT", "id must be a non-empty GUID."));
        }

        if (draft.OccurredAt is null)
        {
            errors.Add(new("occurredAt", "REQUIRED", "occurredAt is required."));
        }

        if (draft.CreatedAt is null)
        {
            errors.Add(new("createdAt", "REQUIRED", "createdAt is required."));
        }

        if (draft.AmountCents is null)
        {
            errors.Add(new("amountCents", "REQUIRED", "amountCents is required."));
        }
        else if (draft.AmountCents is < 1 or > MaximumAmountCents)
        {
            errors.Add(new(
                "amountCents",
                "OUT_OF_RANGE",
                $"amountCents must be between 1 and {MaximumAmountCents.ToString(CultureInfo.InvariantCulture)}."));
        }

        if (errors.Count > 0)
        {
            return new(null, errors.AsReadOnly());
        }

        var order = new Order(
            draft.Id,
            merchantId ?? string.Empty,
            merchantReferenceId ?? string.Empty,
            buyerReferenceId ?? string.Empty,
            draft.OccurredAt?.ToUniversalTime() ?? DateTimeOffset.UnixEpoch,
            draft.AmountCents ?? 1,
            currencyCode ?? string.Empty,
            countryCode ?? string.Empty,
            city,
            channel,
            deviceSessionId,
            draft.CreatedAt?.ToUniversalTime() ?? DateTimeOffset.UnixEpoch);

        return new(order, Array.Empty<OrderValidationError>());
    }

    public bool HasSameBusinessFactsAs(Order other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Reference == other.Reference
            && BuyerReferenceId == other.BuyerReferenceId
            && OccurredAt == other.OccurredAt
            && AmountCents == other.AmountCents
            && CurrencyCode == other.CurrencyCode
            && CountryCode == other.CountryCode
            && City == other.City
            && Channel == other.Channel
            && DeviceSessionId == other.DeviceSessionId;
    }

    private static string? NormalizeRequiredIdentifier(
        string? value,
        string field,
        string prefix,
        List<OrderValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new(field, "REQUIRED", $"{field} is required."));
            return null;
        }

        return NormalizeIdentifier(value, field, prefix, errors);
    }

    private static string? NormalizeOptionalIdentifier(
        string? value,
        string field,
        string prefix,
        List<OrderValidationError> errors)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeIdentifier(value, field, prefix, errors);
    }

    private static string NormalizeIdentifier(
        string value,
        string field,
        string prefix,
        List<OrderValidationError> errors)
    {
        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaximumIdentifierLength)
        {
            errors.Add(new(
                field,
                "OUT_OF_RANGE",
                $"{field} must not exceed {MaximumIdentifierLength.ToString(CultureInfo.InvariantCulture)} characters."));
        }

        if (normalized.Length <= prefix.Length
            || !normalized.StartsWith(prefix, StringComparison.Ordinal)
            || !IdentifierPattern().IsMatch(normalized))
        {
            errors.Add(new(
                field,
                "INVALID_FORMAT",
                $"{field} must start with {prefix} and contain only ASCII letters, digits, underscores, or hyphens."));
        }

        return normalized;
    }

    private static string? NormalizeCurrency(
        string? value,
        List<OrderValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new("currencyCode", "REQUIRED", "currencyCode is required."));
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!SupportedCurrencies.Contains(normalized, StringComparer.Ordinal))
        {
            errors.Add(new(
                "currencyCode",
                "UNSUPPORTED_VALUE",
                "currencyCode must be one of UYU, BRL, or USD."));
        }

        return normalized;
    }

    private static string? NormalizeCountry(
        string? value,
        List<OrderValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new("countryCode", "REQUIRED", "countryCode is required."));
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!IsoCountryCodes.Contains(normalized))
        {
            errors.Add(new(
                "countryCode",
                "UNSUPPORTED_VALUE",
                $"countryCode must belong to the ISO 3166-1 alpha-2 snapshot dated {IsoCountryCodes.SnapshotDate}."));
        }

        return normalized;
    }

    private static string? NormalizeCity(
        string? value,
        List<OrderValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Any(char.IsControl))
        {
            errors.Add(new("city", "INVALID_FORMAT", "city must not contain control characters."));
        }

        var normalized = CollapseWhitespacePattern()
            .Replace(value.Trim(), " ")
            .Normalize(NormalizationForm.FormC);

        if (normalized.Length > MaximumCityLength)
        {
            errors.Add(new(
                "city",
                "OUT_OF_RANGE",
                $"city must not exceed {MaximumCityLength.ToString(CultureInfo.InvariantCulture)} characters."));
        }

        return normalized;
    }

    private static OrderChannel? NormalizeChannel(
        string? value,
        List<OrderValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "WEB" => OrderChannel.Web,
            "MOBILE_APP" => OrderChannel.MobileApp,
            "MARKETPLACE" => OrderChannel.Marketplace,
            _ => AddUnsupportedChannel(errors),
        };
    }

    private static OrderChannel? AddUnsupportedChannel(List<OrderValidationError> errors)
    {
        errors.Add(new(
            "channel",
            "UNSUPPORTED_VALUE",
            "channel must be WEB, MOBILE_APP, or MARKETPLACE."));
        return null;
    }

    [GeneratedRegex("^[A-Z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex CollapseWhitespacePattern();
}
