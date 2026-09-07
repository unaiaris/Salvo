using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

/// <summary>
/// The tokenizer and the fact set, against the prose the engine really writes.
/// </summary>
/// <remarks>
/// Every detail quoted here was taken from a persisted evaluation, not invented for the test. The
/// point of the exercise is that the five false rejections the adversarial review found against
/// real rows do not happen: an amount in units, a thousands separator, a rounded percentage, an
/// instant in business time, and a true figure that lives in the configuration rather than in the
/// evaluation.
/// </remarks>
public sealed class ExplanationFactsTests
{
    /// <summary>The three signals of <c>ORD_000011</c>, score 90, in canonical order.</summary>
    private static readonly RiskSignal[] Signals =
    [
        new(RiskRuleNames.AmountAnomaly, 40) { Detail = "201111 BRL cents is 23.2x the merchant median 8685 over 3 prior orders in 90 days." },
        new(RiskRuleNames.NewBuyerHighValue, 30) { Detail = "The buyer has no prior merchant orders and 201111 BRL cents is 23.2x the merchant "
            + "median 8685 over 3 prior orders." },
        new(RiskRuleNames.ForeignCountry, 20) { Detail = "US differs from habitual BR, observed in 3 of 3 prior merchant orders (100.0%)." },
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

        Assert.Equal(201111L, amount.AmountCents);
        Assert.Equal("BRL", amount.CurrencyCode);
        Assert.Equal(23.2m, amount.Ratio);
        Assert.Equal(AmountMedianScope.Merchant, amount.Scope);
        Assert.Equal(8685L, amount.MedianCents);
        Assert.Equal(3, amount.HistoryCount);
        Assert.Equal(90, amount.WindowDays);

        Assert.Equal(201111L, newBuyer.AmountCents);
        Assert.Equal("BRL", newBuyer.CurrencyCode);
        Assert.Equal(23.2m, newBuyer.Ratio);
        Assert.Equal(8685L, newBuyer.MedianCents);
        Assert.Equal(3, newBuyer.HistoryCount);

        Assert.Equal("US", foreign.Country);
        Assert.Equal("BR", foreign.HabitualCountry);
        Assert.Equal(3, foreign.ObservedCount);
        Assert.Equal(3, foreign.TotalCount);
        Assert.Equal(100.0m, foreign.SharePercent);
    }

    /// <summary>
    /// The amount, the currency, the median and the time zone, which the patterns have always
    /// captured and the extractor used to throw away.
    /// </summary>
    /// <remarks>
    /// They are read here, one task before the engine emits them, for a reason that is the whole
    /// argument of <c>E9B</c>: the golden capture certifies <c>e3-v2</c> field by field against
    /// what this extractor read from real <c>e3-v1</c> prose. A column the extractor cannot read is
    /// a column that would enter the fingerprint with nothing having checked it. This is work that
    /// dies with the extractor, and it is the price of an oracle that covers the whole table.
    /// </remarks>
    [Fact]
    public void TheFieldsTheExtractorUsedToDiscardAreReadToo()
    {
        var unusualHour = SignalFacts.For(new(RiskRuleNames.UnusualHour, 10) { Detail = "Local bucket 00:00-06:00 in America/Montevideo appeared in 1 of 21 prior orders (4.8%)." });

        Assert.Equal("America/Montevideo", unusualHour.TimeZoneId);

        // A signal of every rule that names money says which money it is, so a sentence can state
        // the median in units without the verifier calling it invented.
        foreach (var signal in new[] { SignalFacts.For(Signals[0]), SignalFacts.For(Signals[1]) })
        {
            Assert.Equal(201111L, signal.AmountCents);
            Assert.Equal("BRL", signal.CurrencyCode);
            Assert.Equal(8685L, signal.MedianCents);
        }

        // The rules that name no money and no zone leave every one of those fields alone.
        var velocity = SignalFacts.For(new(RiskRuleNames.Velocity, 30) { Detail = "4 orders including the current order occurred within 10 minutes; threshold is 4." });

        Assert.Null(velocity.AmountCents);
        Assert.Null(velocity.CurrencyCode);
        Assert.Null(velocity.MedianCents);
        Assert.Null(velocity.TimeZoneId);
        Assert.Null(velocity.Country);
    }

    /// <summary>
    /// The other three rules, whose prose no alert of the demo corpus exercises.
    /// </summary>
    [Fact]
    public void TheRulesTheDemoCorpusNeverAlertsOnAreReadToo()
    {
        var velocity = SignalFacts.For(new(RiskRuleNames.Velocity, 30) { Detail = "4 orders including the current order occurred within 10 minutes; threshold is 4." });
        var crossBorder = SignalFacts.For(new(RiskRuleNames.CrossBorderVelocity, 40) { Detail = "Country changed from UY to ES within 2 minutes for the same merchant and buyer." });
        var unusualHour = SignalFacts.For(new(RiskRuleNames.UnusualHour, 10) { Detail = "Local bucket 00:00-06:00 in America/Montevideo appeared in 0 of 20 prior orders (0.0%)." });

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
    /// Prose that does not have the shape its rule writes is a declared failure, never a signal
    /// half understood.
    /// </summary>
    [Fact]
    public void ProseThatDoesNotMatchItsRuleIsRefused()
    {
        Assert.Throws<SignalDetailNotRecognizedException>(() =>
            SignalFacts.For(new(RiskRuleNames.AmountAnomaly, 40) { Detail = "the amount looked large" }));
    }

    /// <summary>
    /// The five false rejections the review verified against real rows, one assertion each.
    /// </summary>
    [Fact]
    public void TheFactSetBacksEveryWayACorrectSentenceWritesATrueNumber()
    {
        var facts = Facts();

        // The amount, in the units a sentence about money uses rather than in cents.
        Assert.True(Grounded(facts, "2.011,11"), "amount in units");
        Assert.True(Grounded(facts, "201111"), "amount in cents");

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
        Assert.True(Grounded(facts, "23,2"), "ratio");
        Assert.True(Grounded(facts, "8685"), "median");
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

    private static ExplanationInput Input()
    {
        return new(
            90,
            "CRITICAL",
            RuleConfig.E3V1.Version,
            "e4-v1",
            Signals,
            201111,
            "BRL",
            "US",
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
