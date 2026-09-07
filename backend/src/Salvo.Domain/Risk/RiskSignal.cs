using System.Text.Json;
using System.Text.Json.Serialization;

namespace Salvo.Domain.Risk;

/// <summary>Whose median an amount was compared against.</summary>
[JsonConverter(typeof(AmountMedianScopeConverter))]
public enum AmountMedianScope
{
    Buyer = 1,
    Merchant = 2,
}

/// <summary>Writes the scope as the two words the canonical signal string uses.</summary>
public sealed class AmountMedianScopeConverter : JsonStringEnumConverter<AmountMedianScope>
{
    public AmountMedianScopeConverter()
        : base(JsonNamingPolicy.CamelCase)
    {
    }
}

/// <summary>
/// One rule that fired, stated as the fields it measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The shape is flat and nullable by rule, deliberately.</strong> Every signal has the same
/// shape on the wire whichever rule wrote it, with the fields that do not apply left out. A
/// polymorphic signal per rule would make the guard on the console branch before it can project, and
/// projecting is the one thing that guard is allowed to do.
/// </para>
/// <para>
/// <strong>The precision of each decimal is part of the identity of every evaluation, for ever.</strong>
/// The fingerprint hashes the canonical string as it stands, and a <c>decimal</c> carries the scale
/// of the division that produced it: <c>201111 / 8685</c> is not <c>23.2</c> until somebody rounds
/// it. So the rounding happens here, in the factory of each rule, and not in the caller and not in
/// the serializer: a signal cannot be built carrying more precision than its rule declares. The
/// scales are the ones the <c>e3-v1</c> prose already fixed — one decimal for a ratio and a share,
/// two for elapsed minutes — which is what makes <c>e3-v2</c> a change of representation and
/// nothing else.
/// </para>
/// <para>
/// <c>Detail</c> is the sentence <c>e3-v1</c> wrote. The engine does not write one any more, and it
/// is kept because the snapshot of an open alert is never rewritten (decision 33): a row stored
/// before this version still has its prose and still has to be readable.
/// </para>
/// </remarks>
public sealed record RiskSignal(string Rule, int Weight)
{
    /// <summary>Decimals of a ratio, as the <c>e3-v1</c> prose wrote it with <c>{0.0}</c>.</summary>
    public const int RatioDecimals = 1;

    /// <summary>Decimals of a share, as the <c>e3-v1</c> prose wrote it with <c>{0.0}</c>.</summary>
    public const int SharePercentDecimals = 1;

    /// <summary>Decimals of elapsed minutes, as <c>{0.##}</c> wrote them.</summary>
    public const int ElapsedMinutesDecimals = 2;

    /// <summary>The amount the rule judged, in cents.</summary>
    public long? AmountCents { get; init; }

    /// <summary>The currency that amount is denominated in.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>How many times the amount exceeds the median it was compared against.</summary>
    public decimal? Ratio { get; init; }

    /// <summary>
    /// Whose median that was, for the one rule that can compare against either.
    /// </summary>
    /// <remarks>
    /// <c>new_buyer_high_value</c> does not carry it: that rule fires precisely because the buyer
    /// has no history, so its median is the merchant's by definition and the field would be a
    /// constant in the fingerprint of every such signal.
    /// </remarks>
    public AmountMedianScope? Scope { get; init; }

    /// <summary>The median the amount was compared against, in cents.</summary>
    public long? MedianCents { get; init; }

    /// <summary>Prior orders the median was computed over.</summary>
    public int? HistoryCount { get; init; }

    /// <summary>The look-back of the comparison, in days.</summary>
    public int? WindowDays { get; init; }

    /// <summary>Orders in the burst, the current one included.</summary>
    public int? OrderCount { get; init; }

    /// <summary>The width of the burst window, in minutes.</summary>
    public int? WindowMinutes { get; init; }

    /// <summary>The burst size at which the rule fires.</summary>
    public int? Threshold { get; init; }

    /// <summary>The country of the earlier order that crossed a border.</summary>
    public string? FromCountry { get; init; }

    /// <summary>The country of this order, for the rule that compares two of them.</summary>
    public string? ToCountry { get; init; }

    /// <summary>Minutes between the two orders that crossed a border.</summary>
    public decimal? ElapsedMinutes { get; init; }

    /// <summary>First hour of the local bucket, inclusive.</summary>
    public int? BucketStartHour { get; init; }

    /// <summary>Last hour of the local bucket, exclusive.</summary>
    public int? BucketEndHour { get; init; }

    /// <summary>The business time zone those hours are local to.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Prior orders that fell in the same bucket, or in the habitual country.</summary>
    public int? ObservedCount { get; init; }

    /// <summary>Prior orders the observation was made over.</summary>
    public int? TotalCount { get; init; }

    /// <summary>The observation as a percentage of the total.</summary>
    public decimal? SharePercent { get; init; }

    /// <summary>The country of this order, for the rule that judges the country itself.</summary>
    public string? Country { get; init; }

    /// <summary>The country the merchant almost always sells to.</summary>
    public string? HabitualCountry { get; init; }

    /// <summary>
    /// The sentence <c>e3-v1</c> wrote, on a row stored by that version. Always absent from a
    /// signal this engine writes.
    /// </summary>
    public string? Detail { get; init; }

    public static RiskSignal AmountAnomaly(
        int weight,
        long amountCents,
        string currencyCode,
        decimal ratio,
        AmountMedianScope scope,
        long medianCents,
        int historyCount,
        int windowDays)
    {
        return new(RiskRuleNames.AmountAnomaly, weight)
        {
            AmountCents = amountCents,
            CurrencyCode = currencyCode,
            Ratio = Canonical(ratio, RatioDecimals),
            Scope = scope,
            MedianCents = medianCents,
            HistoryCount = historyCount,
            WindowDays = windowDays,
        };
    }

    public static RiskSignal Velocity(int weight, int orderCount, int windowMinutes, int threshold)
    {
        return new(RiskRuleNames.Velocity, weight)
        {
            OrderCount = orderCount,
            WindowMinutes = windowMinutes,
            Threshold = threshold,
        };
    }

    public static RiskSignal CrossBorderVelocity(
        int weight,
        string fromCountry,
        string toCountry,
        decimal elapsedMinutes)
    {
        return new(RiskRuleNames.CrossBorderVelocity, weight)
        {
            FromCountry = fromCountry,
            ToCountry = toCountry,
            ElapsedMinutes = Canonical(elapsedMinutes, ElapsedMinutesDecimals),
        };
    }

    public static RiskSignal UnusualHour(
        int weight,
        int bucketStartHour,
        int bucketEndHour,
        string timeZoneId,
        int observedCount,
        int totalCount,
        decimal sharePercent)
    {
        return new(RiskRuleNames.UnusualHour, weight)
        {
            BucketStartHour = bucketStartHour,
            BucketEndHour = bucketEndHour,
            TimeZoneId = timeZoneId,
            ObservedCount = observedCount,
            TotalCount = totalCount,
            SharePercent = Canonical(sharePercent, SharePercentDecimals),
        };
    }

    public static RiskSignal NewBuyerHighValue(
        int weight,
        long amountCents,
        string currencyCode,
        long medianCents,
        int historyCount,
        decimal ratio)
    {
        return new(RiskRuleNames.NewBuyerHighValue, weight)
        {
            AmountCents = amountCents,
            CurrencyCode = currencyCode,
            MedianCents = medianCents,
            HistoryCount = historyCount,
            Ratio = Canonical(ratio, RatioDecimals),
        };
    }

    public static RiskSignal ForeignCountry(
        int weight,
        string country,
        string habitualCountry,
        int observedCount,
        int totalCount,
        decimal sharePercent)
    {
        return new(RiskRuleNames.ForeignCountry, weight)
        {
            Country = country,
            HabitualCountry = habitualCountry,
            ObservedCount = observedCount,
            TotalCount = totalCount,
            SharePercent = Canonical(sharePercent, SharePercentDecimals),
        };
    }

    /// <summary>
    /// A measured number at the precision its field declares.
    /// </summary>
    /// <remarks>
    /// Away from zero, which is what the composite format strings of <c>e3-v1</c> did and what
    /// <see cref="decimal.Round(decimal, int)"/> does not: <c>{0.0}</c> takes <c>2.25</c> to
    /// <c>2.3</c> and the default rounding takes it to <c>2.2</c>. Copying the scale and forgetting
    /// the mode would move exactly the ties, which are the values nobody looks at.
    /// </remarks>
    private static decimal Canonical(decimal value, int decimals)
    {
        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}
