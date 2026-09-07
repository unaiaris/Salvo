using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

/// <summary>
/// The tokenizer and the fact set, against the prose the engine really writes.
/// </summary>
/// <remarks>
/// Every figure quoted here was taken from a persisted evaluation, not invented for the test. The
/// point of the exercise is that the five false rejections the adversarial review found against
/// real rows do not happen: an amount in units, a thousands separator, a rounded percentage, an
/// instant in business time, and a true figure that lives in the configuration rather than in the
/// evaluation.
/// </remarks>
public sealed class ExplanationFactsTests
{
    /// <summary>
    /// The three signals of <c>ORD_000011</c>, score 90, in canonical order, as <c>e3-v2</c> writes
    /// them and with the values the engine measured over the demo corpus.
    /// </summary>
    private static readonly RiskSignal[] Signals =
    [
        RiskSignal.AmountAnomaly(40, 50786, "BRL", 50786m / 14937, AmountMedianScope.Merchant, 14937, 3, 90),
        RiskSignal.NewBuyerHighValue(30, 50786, "BRL", 14937, 3, 50786m / 14937),
        RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
    ];

    /// <summary>
    /// 2026-05-11 09:36 UTC is 06:36 in Montevideo, so the two zones disagree about the hour.
    /// </summary>
    private static readonly DateTimeOffset OccurredAt = new(2026, 5, 11, 9, 36, 0, TimeSpan.Zero);

    [Theory]
    // A single mark followed by anything but three digits is a decimal mark, which is how the
    // engine writes a ratio and a share.
    [InlineData("56.0", 56.0)]
    [InlineData("88.3", 88.3)]
    [InlineData("100.0", 100.0)]
    // The console writes money the other way round, and both marks appear at once.
    [InlineData("8.900,00", 8900)]
    [InlineData("2.011,11", 2011.11)]
    [InlineData("1.279", 1279)]
    [InlineData("890000", 890000)]
    // A leading zero is not a separator problem.
    [InlineData("06", 6)]
    public void TokensAreReadIntoEveryDefensibleValue(string token, double expected)
    {
        var extracted = Assert.Single(NumberTokenizer.Extract(token));

        Assert.Contains(extracted.Readings, reading => reading.Value == (decimal)expected);
    }

    /// <summary>
    /// Both readings, so neither spelling of an ambiguous token is rejected out of hand.
    /// </summary>
    [Fact]
    public void AThousandsSizedGroupIsAmbiguousAndYieldsBothReadings()
    {
        var token = Assert.Single(NumberTokenizer.Extract("1.279"));

        Assert.Equal([1.279m, 1279m], token.Readings.Select(reading => reading.Value).Order());
    }

    /// <summary>
    /// Colons are not separators, so a bucket is four figures rather than one unreadable one.
    /// </summary>
    [Fact]
    public void ClockRangesTokenizeIntoTheirParts()
    {
        var tokens = NumberTokenizer.Extract("Local bucket 00:00-06:00");

        Assert.Equal(["00", "00", "06", "00"], tokens.Select(token => token.Text));
    }

    /// <summary>
    /// Version identifiers would otherwise donate a 3 and a 1 to every fact set.
    /// </summary>
    [Fact]
    public void VersionIdentifiersDonateNoDigits()
    {
        Assert.Empty(NumberTokenizer.Extract("configuración e3-v1 y política e4-v1"));
    }

    [Fact]
    public void EverySignalTheEngineWritesIsReadIntoTypedFields()
    {
        var amount = SignalFacts.For(Signals[0]);
        var newBuyer = SignalFacts.For(Signals[1]);
        var foreign = SignalFacts.For(Signals[2]);

        Assert.Equal(50786L, amount.AmountCents);
        Assert.Equal("BRL", amount.CurrencyCode);
        Assert.Equal(3.4m, amount.Ratio);
        Assert.Equal(AmountMedianScope.Merchant, amount.Scope);
        Assert.Equal(14937L, amount.MedianCents);
        Assert.Equal(3, amount.HistoryCount);
        Assert.Equal(90, amount.WindowDays);

        Assert.Equal(50786L, newBuyer.AmountCents);
        Assert.Equal("BRL", newBuyer.CurrencyCode);
        Assert.Equal(3.4m, newBuyer.Ratio);
        Assert.Equal(14937L, newBuyer.MedianCents);
        Assert.Equal(3, newBuyer.HistoryCount);
        Assert.Null(newBuyer.Scope);

        Assert.Equal("AR", foreign.Country);
        Assert.Equal("BR", foreign.HabitualCountry);
        Assert.Equal(3, foreign.ObservedCount);
        Assert.Equal(3, foreign.TotalCount);
        Assert.Equal(100.0m, foreign.SharePercent);
    }

    /// <summary>
    /// The three rules the demo corpus raises least often, read into fields like the rest.
    /// </summary>
    [Fact]
    public void TheRulesTheDemoCorpusAlertsOnLeastAreReadToo()
    {
        var velocity = SignalFacts.For(RiskSignal.Velocity(30, 4, 10, 4));
        var crossBorder = SignalFacts.For(RiskSignal.CrossBorderVelocity(40, "UY", "ES", 2m));
        var unusualHour = SignalFacts.For(
            RiskSignal.UnusualHour(10, 0, 6, "America/Montevideo", 0, 20, 0m));

        Assert.Equal(4, velocity.OrderCount);
        Assert.Equal(10, velocity.WindowMinutes);
        Assert.Equal(4, velocity.Threshold);

        Assert.Equal("UY", crossBorder.FromCountry);
        Assert.Equal("ES", crossBorder.ToCountry);
        Assert.Equal(2m, crossBorder.ElapsedMinutes);

        Assert.Equal(0, unusualHour.BucketStartHour);
        Assert.Equal(6, unusualHour.BucketEndHour);
        Assert.Equal("America/Montevideo", unusualHour.TimeZoneId);
        Assert.Equal(0, unusualHour.ObservedCount);
        Assert.Equal(20, unusualHour.TotalCount);
    }

    /// <summary>
    /// A signal that states itself as <c>e3-v1</c> prose is a declared failure, never a signal half
    /// understood.
    /// </summary>
    /// <remarks>
    /// Nothing reads sentences any more. The row is the snapshot of an alert opened before the
    /// engine changed and it is never rewritten, so the case is permanent: it is still shown on
    /// screen exactly as it was written, and only the grounding facts a summary would be checked
    /// against cannot be built from it. The caller turns this into an explanation that failed with
    /// a code rather than into an unhandled exception.
    /// </remarks>
    [Fact]
    public void AnE3V1SignalIsRefusedRatherThanHalfUnderstood()
    {
        var stored = new RiskSignal(RiskRuleNames.AmountAnomaly, 40)
        {
            Detail = "50786 BRL cents is 3.4x the merchant median 14937 over 3 prior orders in 90 days.",
        };

        var refused = Assert.Throws<SignalDetailNotRecognizedException>(() => SignalFacts.For(stored));

        Assert.Contains(RiskRuleNames.AmountAnomaly, refused.Message, StringComparison.Ordinal);
        // The sentence itself is never quoted back: a rejection names the rule, not the text.
        Assert.DoesNotContain("50786", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The five false rejections the review verified against real rows, one assertion each.
    /// </summary>
    [Fact]
    public void TheFactSetBacksEveryWayACorrectSentenceWritesATrueNumber()
    {
        var facts = Facts();

        // The amount, in the units a sentence about money uses rather than in cents.
        Assert.True(Grounded(facts, "507,86"), "amount in units");
        Assert.True(Grounded(facts, "50786"), "amount in cents");

        // A percentage the provider rounded.
        Assert.True(Grounded(facts, "100"), "rounded share");

        // The hour in business time, which is not the hour the engine stored.
        Assert.True(Grounded(facts, "6"), "business hour");
        Assert.True(Grounded(facts, "9"), "utc hour");
        Assert.True(Grounded(facts, "36"), "minute");

        // Figures that are true of the evaluation without appearing in any of its fields.
        Assert.True(Grounded(facts, "60"), "flag threshold");
        Assert.True(Grounded(facts, "100"), "score cap");
        Assert.True(Grounded(facts, "3"), "how many rules matched");
        Assert.True(Grounded(facts, "90"), "sum of the weights before the cap");

        // And what the engine itself wrote.
        Assert.True(Grounded(facts, "3,4"), "ratio");
        Assert.True(Grounded(facts, "14937"), "median");
        Assert.True(Grounded(facts, "149,37"), "median in units");
    }

    [Fact]
    public void AFabricatedFigureIsNotBacked()
    {
        var facts = Facts();

        Assert.False(Grounded(facts, "48"));
        Assert.False(facts.IsGrounded(SmallestUngrounded(facts)));
    }

    /// <summary>
    /// The summary of a provider that invents a figure is refused, and the rejection names the
    /// figure rather than quoting the sentence.
    /// </summary>
    [Fact]
    public void GroundingRejectsAnInventedFigureAndNamesOnlyTheToken()
    {
        var input = Input();
        var facts = Facts();
        var invented = SmallestUngrounded(facts);

        var verdict = ExplanationGrounding.Verify(
            $"El monto es {invented} veces la mediana del comercio.",
            [RiskRuleNames.AmountAnomaly],
            input,
            facts);

        Assert.Equal(ExplanationFailureCode.NotGroundedNumber, verdict.FailureCode);
        Assert.Equal(invented.ToString(System.Globalization.CultureInfo.InvariantCulture), verdict.Offender);
        Assert.DoesNotContain("mediana", verdict.Offender, StringComparison.Ordinal);
    }

    [Fact]
    public void GroundingRejectsARuleTheEvaluationNeverRaised()
    {
        var verdict = ExplanationGrounding.Verify(
            "El pedido llegó en una hora inusual.",
            [RiskRuleNames.UnusualHour],
            Input(),
            Facts());

        Assert.Equal(ExplanationFailureCode.NotGroundedRule, verdict.FailureCode);
        Assert.Equal(RiskRuleNames.UnusualHour, verdict.Offender);
    }

    [Fact]
    public void GroundingRejectsMarkupAndLinks()
    {
        var markup = ExplanationGrounding.Verify("Ver **el monto**.", [], Input(), Facts());
        var link = ExplanationGrounding.Verify("Ver https://ejemplo.uy", [], Input(), Facts());
        var tooLong = ExplanationGrounding.Verify(
            new string('a', ExplanationGrounding.MaximumSummaryLength + 1),
            [],
            Input(),
            Facts());

        Assert.Equal(ExplanationFailureCode.MalformedOutput, markup.FailureCode);
        Assert.Equal(ExplanationFailureCode.MalformedOutput, link.FailureCode);
        Assert.Equal(ExplanationFailureCode.TooLong, tooLong.FailureCode);
    }

    /// <summary>
    /// The whole fact set is a function of the evaluation, so building it twice gives the same
    /// answer on any machine.
    /// </summary>
    [Fact]
    public void TheFactSetIsStableAcrossCultures()
    {
        var expected = string.Join(
            ";",
            Facts().Values.Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        var observed = CultureProbe.Run(
            ["en-US", "es-UY", "de-DE"],
            () => string.Join(
                ";",
                Facts().Values.Select(value =>
                    value.ToString(System.Globalization.CultureInfo.InvariantCulture))));

        Assert.All(observed, actual => Assert.Equal(expected, actual));
    }

    /// <summary>
    /// Every numeric field of every rule reaches the fact set, one assertion per field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the test that stands in for a signal that cannot go red.</strong> The fact
    /// set used to be filled by tokenizing the sentence the engine wrote, so it contained whatever
    /// the engine had said, whether or not this system had a name for it. With fields it is
    /// enumerated by hand, and a field left out of that enumeration fails silently: the
    /// deterministic template writes only figures that reach the set by another route, so every
    /// golden text would stay green while a real model writing a true figure started being refused
    /// as invented. It is the stage 7 defect with the sign reversed.
    /// </para>
    /// <para>
    /// The median is the case that made this worth writing. It lived only inside the prose, the
    /// tokenizer picked it up for free, and no golden text mentions it — so losing it would have
    /// cost nothing until the day a model wrote «la mediana fue 149,37 BRL» and was told it made
    /// the number up.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryNumericFieldOfEveryRuleIsAFact()
    {
        RiskSignal[] signals =
        [
            RiskSignal.AmountAnomaly(40, 201111, "BRL", 201111m / 8685, AmountMedianScope.Merchant, 8685, 3, 90),
            RiskSignal.Velocity(30, 4, 10, 4),
            RiskSignal.CrossBorderVelocity(40, "BR", "UY", 5m / 3),
            RiskSignal.UnusualHour(10, 0, 6, "America/Montevideo", 1, 25, 4m),
            RiskSignal.NewBuyerHighValue(30, 201111, "BRL", 8685, 3, 201111m / 8685),
            RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
        ];
        var input = Input() with { Signals = signals };
        var facts = ExplanationFacts.For(input, SignalFacts.ForAll(signals), RuleConfig.Current);

        (string Field, decimal Value)[] expected =
        [
            ("amount_anomaly.amountCents", 201111m),
            ("amount_anomaly.ratio", 23.2m),
            ("amount_anomaly.medianCents", 8685m),
            ("amount_anomaly.historyCount", 3m),
            ("amount_anomaly.windowDays", 90m),
            ("velocity.orderCount", 4m),
            ("velocity.windowMinutes", 10m),
            ("velocity.threshold", 4m),
            ("cross_border_velocity.elapsedMinutes", 1.67m),
            ("unusual_hour.bucketStartHour", 0m),
            ("unusual_hour.bucketEndHour", 6m),
            ("unusual_hour.observedCount", 1m),
            ("unusual_hour.totalCount", 25m),
            ("unusual_hour.sharePercent", 4m),
            ("new_buyer_high_value.amountCents", 201111m),
            ("new_buyer_high_value.medianCents", 8685m),
            ("new_buyer_high_value.historyCount", 3m),
            ("new_buyer_high_value.ratio", 23.2m),
            ("foreign_country.observedCount", 3m),
            ("foreign_country.totalCount", 3m),
            ("foreign_country.sharePercent", 100m),
        ];

        Assert.All(expected, entry =>
            Assert.True(
                facts.IsGrounded(new NumberReading(entry.Value, Decimals(entry.Value))),
                $"{entry.Field} = {entry.Value} is not a fact."));

        // The amounts in the units anybody writes them in, which is the reading the prose never
        // contained: the engine stated cents and a sentence about money never does.
        Assert.True(facts.IsGrounded(new NumberReading(2011.11m, 2)), "the amount in units");
        Assert.True(facts.IsGrounded(new NumberReading(86.85m, 2)), "the median in units");
        Assert.True(Grounded(facts, "86,85"), "the median as a sentence writes it");
    }

    /// <summary>
    /// The set is not simply everything: a figure nobody measured is still refused.
    /// </summary>
    [Fact]
    public void EnumeratingTheFieldsDoesNotGroundAFigureNobodyMeasured()
    {
        RiskSignal[] signals =
        [
            RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
        ];
        var input = Input() with { Signals = signals };
        var facts = ExplanationFacts.For(input, SignalFacts.ForAll(signals), RuleConfig.Current);

        Assert.False(facts.IsGrounded(SmallestUngrounded(facts)));
    }

    private static int Decimals(decimal value)
    {
        return (decimal.GetBits(value)[3] >> 16) & 0xFF;
    }

    private static ExplanationInput Input()
    {
        return new(
            90,
            "CRITICAL",
            RuleConfig.E3V1.Version,
            "e4-v1",
            Signals,
            50786,
            "BRL",
            "AR",
            null,
            OccurredAt);
    }

    private static ExplanationFacts Facts()
    {
        var input = Input();

        return ExplanationFacts.For(input, SignalFacts.ForAll(input.Signals), RuleConfig.E3V1);
    }

    private static bool Grounded(ExplanationFacts facts, string token)
    {
        return facts.IsGrounded(Assert.Single(NumberTokenizer.Extract(token)));
    }

    /// <summary>
    /// The smallest positive integer no fact backs. A falsification test uses it so that the
    /// invented figure stays invented when the corpus changes.
    /// </summary>
    private static int SmallestUngrounded(ExplanationFacts facts)
    {
        var candidate = 1;
        while (facts.IsGrounded(candidate))
        {
            candidate++;
        }

        return candidate;
    }
}
