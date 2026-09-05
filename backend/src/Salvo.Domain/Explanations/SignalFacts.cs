using System.Globalization;
using System.Text.RegularExpressions;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Explanations;

/// <summary>Whose median an amount was compared against.</summary>
public enum AmountMedianScope
{
    Buyer = 1,
    Merchant = 2,
}

/// <summary>
/// One signal of an evaluation, read into typed fields.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This type is the seed of <c>e3-v2</c>, and the extractor below is the part that is meant
/// to die.</strong> The engine currently states each signal as an English sentence and the
/// fingerprint hashes that sentence, so the numbers a Spanish summary needs live inside prose. The
/// Workboard already carries the stage 8 candidate where the engine emits these fields directly and
/// the interface composes the text. When that happens, <see cref="Parse"/> is deleted and
/// <see cref="SignalFacts"/> is built from what the engine hands over. Everything downstream — the
/// template that writes the summary and <see cref="ExplanationFacts"/> that grounds it — keeps
/// working untouched, which is the reason for putting the seam here rather than scattering regular
/// expressions through a provider.
/// </para>
/// <para>
/// <see cref="Numbers"/> is what the grounding facts consume, and it comes from the one tokenizer
/// rather than from the typed fields. A number the engine wrote is a number a correct sentence may
/// repeat, whether or not this type gave it a name.
/// </para>
/// </remarks>
public sealed partial record SignalFacts
{
    public required string Rule { get; init; }

    public required int Weight { get; init; }

    /// <summary>Every figure the engine put in this signal's prose.</summary>
    public required IReadOnlyList<NumberToken> Numbers { get; init; }

    /// <summary>How many times the amount exceeds the median it was compared against.</summary>
    public decimal? Ratio { get; init; }

    /// <summary>Whose median that was.</summary>
    public AmountMedianScope? Scope { get; init; }

    /// <summary>Prior orders the median was computed over.</summary>
    public int? HistoryCount { get; init; }

    /// <summary>The look-back of the comparison, in days.</summary>
    public int? WindowDays { get; init; }

    /// <summary>Orders in the burst, the current one included.</summary>
    public int? OrderCount { get; init; }

    /// <summary>The burst size at which the rule fires.</summary>
    public int? Threshold { get; init; }

    /// <summary>The width of the burst window, in minutes.</summary>
    public int? WindowMinutes { get; init; }

    /// <summary>Minutes between the two orders that crossed a border.</summary>
    public decimal? ElapsedMinutes { get; init; }

    public string? FromCountry { get; init; }

    public string? ToCountry { get; init; }

    /// <summary>First hour of the local bucket, inclusive.</summary>
    public int? BucketStartHour { get; init; }

    /// <summary>Last hour of the local bucket, exclusive.</summary>
    public int? BucketEndHour { get; init; }

    /// <summary>Prior orders that fell in the same bucket, or in the habitual country.</summary>
    public int? ObservedCount { get; init; }

    /// <summary>Prior orders the observation was made over.</summary>
    public int? TotalCount { get; init; }

    /// <summary>The observation as a percentage of the total.</summary>
    public decimal? SharePercent { get; init; }

    /// <summary>The country the merchant almost always sells to.</summary>
    public string? HabitualCountry { get; init; }

    /// <summary>
    /// Reads a signal into typed fields.
    /// </summary>
    /// <exception cref="SignalDetailNotRecognizedException">
    /// The prose does not have the shape its rule writes.
    /// </exception>
    public static SignalFacts Parse(RiskSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        var numbers = NumberTokenizer.Extract(signal.Detail);
        var facts = new SignalFacts
        {
            Rule = signal.Rule,
            Weight = signal.Weight,
            Numbers = numbers,
        };

        return signal.Rule switch
        {
            RiskRuleNames.AmountAnomaly => ReadAmountAnomaly(facts, signal.Detail),
            RiskRuleNames.Velocity => ReadVelocity(facts, signal.Detail),
            RiskRuleNames.CrossBorderVelocity => ReadCrossBorderVelocity(facts, signal.Detail),
            RiskRuleNames.UnusualHour => ReadUnusualHour(facts, signal.Detail),
            RiskRuleNames.NewBuyerHighValue => ReadNewBuyerHighValue(facts, signal.Detail),
            RiskRuleNames.ForeignCountry => ReadForeignCountry(facts, signal.Detail),
            _ => throw SignalDetailNotRecognizedException.ForRule(signal.Rule),
        };
    }

    public static IReadOnlyList<SignalFacts> ParseAll(IReadOnlyList<RiskSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var parsed = new SignalFacts[signals.Count];
        for (var index = 0; index < signals.Count; index++)
        {
            parsed[index] = Parse(signals[index]);
        }

        return parsed;
    }

    private static SignalFacts ReadAmountAnomaly(SignalFacts facts, string detail)
    {
        var match = Require(AmountAnomalyPattern(), detail, facts.Rule);

        return facts with
        {
            Ratio = Number(match, "ratio"),
            Scope = string.Equals(match.Groups["scope"].Value, "buyer", StringComparison.Ordinal)
                ? AmountMedianScope.Buyer
                : AmountMedianScope.Merchant,
            HistoryCount = Count(match, "history"),
            WindowDays = Count(match, "window"),
        };
    }

    private static SignalFacts ReadVelocity(SignalFacts facts, string detail)
    {
        var match = Require(VelocityPattern(), detail, facts.Rule);

        return facts with
        {
            OrderCount = Count(match, "orders"),
            WindowMinutes = Count(match, "minutes"),
            Threshold = Count(match, "threshold"),
        };
    }

    private static SignalFacts ReadCrossBorderVelocity(SignalFacts facts, string detail)
    {
        var match = Require(CrossBorderVelocityPattern(), detail, facts.Rule);

        return facts with
        {
            FromCountry = match.Groups["from"].Value,
            ToCountry = match.Groups["to"].Value,
            ElapsedMinutes = Number(match, "elapsed"),
        };
    }

    private static SignalFacts ReadUnusualHour(SignalFacts facts, string detail)
    {
        var match = Require(UnusualHourPattern(), detail, facts.Rule);

        return facts with
        {
            BucketStartHour = Count(match, "start"),
            BucketEndHour = Count(match, "end"),
            ObservedCount = Count(match, "observed"),
            TotalCount = Count(match, "total"),
            SharePercent = Number(match, "share"),
        };
    }

    private static SignalFacts ReadNewBuyerHighValue(SignalFacts facts, string detail)
    {
        var match = Require(NewBuyerHighValuePattern(), detail, facts.Rule);

        return facts with
        {
            Ratio = Number(match, "ratio"),
            Scope = AmountMedianScope.Merchant,
            HistoryCount = Count(match, "history"),
        };
    }

    private static SignalFacts ReadForeignCountry(SignalFacts facts, string detail)
    {
        var match = Require(ForeignCountryPattern(), detail, facts.Rule);

        return facts with
        {
            ToCountry = match.Groups["country"].Value,
            HabitualCountry = match.Groups["habitual"].Value,
            ObservedCount = Count(match, "observed"),
            TotalCount = Count(match, "total"),
            SharePercent = Number(match, "share"),
        };
    }

    private static Match Require(Regex pattern, string detail, string rule)
    {
        var match = pattern.Match(detail);

        return match.Success ? match : throw SignalDetailNotRecognizedException.ForRule(rule);
    }

    private static decimal Number(Match match, string group)
    {
        return decimal.Parse(
            match.Groups[group].Value,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture);
    }

    private static int Count(Match match, string group)
    {
        return int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(
        @"^(?<amount>\d+) (?<currency>[A-Z]{3}) cents is (?<ratio>[\d.]+)x the (?<scope>buyer|merchant) median (?<median>\d+) over (?<history>\d+) prior orders in (?<window>\d+) days\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AmountAnomalyPattern();

    [GeneratedRegex(
        @"^(?<orders>\d+) orders including the current order occurred within (?<minutes>\d+) minutes; threshold is (?<threshold>\d+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex VelocityPattern();

    [GeneratedRegex(
        @"^Country changed from (?<from>[A-Z]{2}) to (?<to>[A-Z]{2}) within (?<elapsed>[\d.]+) minutes for the same merchant and buyer\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CrossBorderVelocityPattern();

    [GeneratedRegex(
        @"^Local bucket (?<start>\d{2}):00-(?<end>\d{2}):00 in (?<zone>\S+) appeared in (?<observed>\d+) of (?<total>\d+) prior orders \((?<share>[\d.]+)%\)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex UnusualHourPattern();

    [GeneratedRegex(
        @"^The buyer has no prior merchant orders and (?<amount>\d+) (?<currency>[A-Z]{3}) cents is (?<ratio>[\d.]+)x the merchant median (?<median>\d+) over (?<history>\d+) prior orders\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex NewBuyerHighValuePattern();

    [GeneratedRegex(
        @"^(?<country>[A-Z]{2}) differs from habitual (?<habitual>[A-Z]{2}), observed in (?<observed>\d+) of (?<total>\d+) prior merchant orders \((?<share>[\d.]+)%\)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ForeignCountryPattern();
}
