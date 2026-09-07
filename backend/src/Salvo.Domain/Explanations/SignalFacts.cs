using System.Globalization;
using System.Text.RegularExpressions;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Explanations;

/// <summary>
/// One signal of an evaluation, read into typed fields.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The engine writes these fields now, and this type mostly copies them.</strong> That was
/// the point of putting the seam here in stage 7: the template that writes the summary and
/// <see cref="ExplanationFacts"/> that grounds it did not have to change when the engine stopped
/// writing sentences. What is left of the extractor reads an <c>e3-v1</c> row, and an <c>e3-v1</c>
/// row is the snapshot of every alert opened before this version — never rewritten, by decision 33.
/// </para>
/// <para>
/// <see cref="Numbers"/> is what the grounding facts consume, and with fields it is
/// <strong>enumerated by hand</strong> rather than tokenized. That enumeration is load-bearing and
/// its absence would be silent: the deterministic template writes only figures that reach the fact
/// set by another route, so every golden text would stay green while a real model writing a true
/// figure — the median, say — started being refused as invented. It is the stage 7 defect with the
/// sign reversed, and the test that walks every numeric field of every rule is what stands in its
/// way.
/// </para>
/// </remarks>
public sealed partial record SignalFacts
{
    public required string Rule { get; init; }

    public required int Weight { get; init; }

    /// <summary>Every figure the engine put in this signal's prose.</summary>
    public required IReadOnlyList<NumberToken> Numbers { get; init; }

    /// <summary>The amount the rule judged, in cents.</summary>
    public long? AmountCents { get; init; }

    /// <summary>The currency that amount is denominated in.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>How many times the amount exceeds the median it was compared against.</summary>
    public decimal? Ratio { get; init; }

    /// <summary>
    /// Whose median that was, for the rule that can compare against either.
    /// </summary>
    /// <remarks>
    /// Only <c>amount_anomaly</c> carries it. <c>new_buyer_high_value</c> fires precisely because
    /// the buyer has no history at all, so its median is the merchant's by definition and a field
    /// saying so would be a constant on the wire and in the fingerprint of every such signal.
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

    /// <summary>The business time zone those hours are local to.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Prior orders that fell in the same bucket, or in the habitual country.</summary>
    public int? ObservedCount { get; init; }

    /// <summary>Prior orders the observation was made over.</summary>
    public int? TotalCount { get; init; }

    /// <summary>The observation as a percentage of the total.</summary>
    public decimal? SharePercent { get; init; }

    /// <summary>The country of the order, when the rule judged the country itself.</summary>
    public string? Country { get; init; }

    /// <summary>The country the merchant almost always sells to.</summary>
    public string? HabitualCountry { get; init; }

    /// <summary>
    /// Reads one signal, whichever version wrote it.
    /// </summary>
    /// <exception cref="SignalDetailNotRecognizedException">
    /// The signal is <c>e3-v1</c> prose that does not have the shape its rule writes.
    /// </exception>
    public static SignalFacts For(RiskSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        return signal.Detail is { } prose ? Parse(signal, prose) : FromFields(signal);
    }

    public static IReadOnlyList<SignalFacts> ForAll(IReadOnlyList<RiskSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var read = new SignalFacts[signals.Count];
        for (var index = 0; index < signals.Count; index++)
        {
            read[index] = For(signals[index]);
        }

        return read;
    }

    /// <summary>
    /// An <c>e3-v2</c> signal, which already is its fields.
    /// </summary>
    private static SignalFacts FromFields(RiskSignal signal)
    {
        return new()
        {
            Rule = signal.Rule,
            Weight = signal.Weight,
            Numbers = NumbersOf(signal),
            AmountCents = signal.AmountCents,
            CurrencyCode = signal.CurrencyCode,
            Ratio = signal.Ratio,
            Scope = signal.Scope,
            MedianCents = signal.MedianCents,
            HistoryCount = signal.HistoryCount,
            WindowDays = signal.WindowDays,
            OrderCount = signal.OrderCount,
            WindowMinutes = signal.WindowMinutes,
            Threshold = signal.Threshold,
            FromCountry = signal.FromCountry,
            ToCountry = signal.ToCountry,
            ElapsedMinutes = signal.ElapsedMinutes,
            BucketStartHour = signal.BucketStartHour,
            BucketEndHour = signal.BucketEndHour,
            TimeZoneId = signal.TimeZoneId,
            ObservedCount = signal.ObservedCount,
            TotalCount = signal.TotalCount,
            SharePercent = signal.SharePercent,
            Country = signal.Country,
            HabitualCountry = signal.HabitualCountry,
        };
    }

    /// <summary>
    /// Every figure this signal states, enumerated field by field.
    /// </summary>
    /// <remarks>
    /// <strong>Every numeric field of the table belongs here, and a field left out fails silently.</strong>
    /// A figure that is not in this list is a figure the grounding check will call invented the day a
    /// provider that is not the template writes it. The amounts contribute their reading in units as
    /// well as in cents, because nobody writing a sentence about money states the cents: without it
    /// «la mediana fue 86,85 BRL» is a true figure that gets refused.
    /// </remarks>
    private static List<NumberToken> NumbersOf(RiskSignal signal)
    {
        var numbers = new List<NumberToken>();

        Money(numbers, signal.AmountCents);
        Money(numbers, signal.MedianCents);

        Whole(numbers, signal.HistoryCount);
        Whole(numbers, signal.WindowDays);
        Whole(numbers, signal.OrderCount);
        Whole(numbers, signal.WindowMinutes);
        Whole(numbers, signal.Threshold);
        Whole(numbers, signal.BucketStartHour);
        Whole(numbers, signal.BucketEndHour);
        Whole(numbers, signal.ObservedCount);
        Whole(numbers, signal.TotalCount);

        Measured(numbers, signal.Ratio, RiskSignal.RatioDecimals);
        Measured(numbers, signal.ElapsedMinutes, RiskSignal.ElapsedMinutesDecimals);
        Measured(numbers, signal.SharePercent, RiskSignal.SharePercentDecimals);

        return numbers;
    }

    /// <summary>A count, which reads one way only.</summary>
    private static void Whole(List<NumberToken> numbers, int? value)
    {
        if (value is { } present)
        {
            numbers.Add(Token(present, 0));
        }
    }

    /// <summary>
    /// A measured number, at exactly the precision its field declares.
    /// </summary>
    /// <remarks>
    /// One reading and no coarser ones. A sentence that rounds a ratio further is already accepted,
    /// because <see cref="ExplanationFacts.IsGrounded(NumberReading)"/> backs a reading of
    /// <c>d</c> decimals with any fact that becomes it once written with <c>d</c> decimals. Adding
    /// them here would put the whole part of every ratio into the set as a fact in its own right —
    /// <c>3</c>, from a ratio of <c>3.4</c> — which is a number nobody measured.
    /// </remarks>
    private static void Measured(List<NumberToken> numbers, decimal? value, int decimals)
    {
        if (value is { } present)
        {
            numbers.Add(Token(present, decimals));
        }
    }

    /// <summary>
    /// An amount, in the cents it is stored as and in the units anybody writes it in.
    /// </summary>
    private static void Money(List<NumberToken> numbers, long? cents)
    {
        if (cents is not { } present)
        {
            return;
        }

        var readings = new List<NumberReading> { new(present, 0) };
        var units = present / 100m;
        for (var written = 0; written <= NumberTokenizer.MaximumRoundedDecimals; written++)
        {
            readings.Add(new(Math.Round(units, written, MidpointRounding.AwayFromZero), written));
        }

        numbers.Add(new(present.ToString(CultureInfo.InvariantCulture), readings));
    }

    private static NumberToken Token(decimal value, int decimals)
    {
        return new(value.ToString(CultureInfo.InvariantCulture), [new(value, decimals)]);
    }

    /// <summary>
    /// Reads an <c>e3-v1</c> signal, whose fields live inside the sentence the engine wrote.
    /// </summary>
    private static SignalFacts Parse(RiskSignal signal, string prose)
    {
        var facts = new SignalFacts
        {
            Rule = signal.Rule,
            Weight = signal.Weight,
            Numbers = NumberTokenizer.Extract(prose),
        };

        return signal.Rule switch
        {
            RiskRuleNames.AmountAnomaly => ReadAmountAnomaly(facts, prose),
            RiskRuleNames.Velocity => ReadVelocity(facts, prose),
            RiskRuleNames.CrossBorderVelocity => ReadCrossBorderVelocity(facts, prose),
            RiskRuleNames.UnusualHour => ReadUnusualHour(facts, prose),
            RiskRuleNames.NewBuyerHighValue => ReadNewBuyerHighValue(facts, prose),
            RiskRuleNames.ForeignCountry => ReadForeignCountry(facts, prose),
            _ => throw SignalDetailNotRecognizedException.ForRule(signal.Rule),
        };
    }

    private static SignalFacts ReadAmountAnomaly(SignalFacts facts, string detail)
    {
        var match = Require(AmountAnomalyPattern(), detail, facts.Rule);

        return facts with
        {
            AmountCents = Amount(match, "amount"),
            CurrencyCode = match.Groups["currency"].Value,
            Ratio = Number(match, "ratio"),
            Scope = string.Equals(match.Groups["scope"].Value, "buyer", StringComparison.Ordinal)
                ? AmountMedianScope.Buyer
                : AmountMedianScope.Merchant,
            MedianCents = Amount(match, "median"),
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
            TimeZoneId = match.Groups["zone"].Value,
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
            AmountCents = Amount(match, "amount"),
            CurrencyCode = match.Groups["currency"].Value,
            MedianCents = Amount(match, "median"),
            HistoryCount = Count(match, "history"),
            Ratio = Number(match, "ratio"),
        };
    }

    private static SignalFacts ReadForeignCountry(SignalFacts facts, string detail)
    {
        var match = Require(ForeignCountryPattern(), detail, facts.Rule);

        return facts with
        {
            Country = match.Groups["country"].Value,
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

    private static long Amount(Match match, string group)
    {
        return long.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
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
