using System.Reflection;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class RiskSignalSerializerTests
{
    [Fact]
    public void SerializationFollowsTheRuleOrderTheEngineEmits()
    {
        var assessment = ScoreOrderTriggeringSeveralRules();

        var canonical = RiskSignalSerializer.Serialize(assessment.Signals);
        var fromReversedInput = RiskSignalSerializer.Serialize(
            assessment.Signals.Reverse().ToArray());

        Assert.True(assessment.Signals.Count >= 3);
        Assert.Equal(canonical, fromReversedInput);
        Assert.Equal(
            assessment.Signals.Select(signal => signal.Rule),
            ReadRuleOrder(canonical));

        // The engine order is not the alphabetical one; a canonicalization that sorted by name
        // would make a rescore of a stored row disagree with its own fingerprint.
        var alphabetical = assessment.Signals
            .Select(signal => signal.Rule)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEqual(alphabetical, ReadRuleOrder(canonical));
    }

    [Fact]
    public void CanonicalOrderCoversEveryDeterministicRuleExactlyOnce()
    {
        var declared = typeof(RiskRuleNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(
            declared.Order(StringComparer.Ordinal),
            RiskRuleNames.CanonicalOrder.Order(StringComparer.Ordinal));
        Assert.Equal(
            RiskRuleNames.CanonicalOrder.Count,
            RiskRuleNames.CanonicalOrder.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SerializationRejectsUnknownAndRepeatedRules()
    {
        Assert.Throws<ArgumentException>(() =>
            RiskSignalSerializer.Serialize([new("made_up_rule", 10) { Detail = "detail" }]));
        Assert.Throws<ArgumentException>(() =>
            RiskSignalSerializer.Serialize(
            [
                new(RiskRuleNames.Velocity, 30) { Detail = "first" },
                new(RiskRuleNames.Velocity, 30) { Detail = "second" },
            ]));
    }

    [Fact]
    public void SerializationIsStableAndIndependentOfTheAmbientCulture()
    {
        var signals = new RiskSignal[]
        {
            new(RiskRuleNames.AmountAnomaly, 40) { Detail = "300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days." },
            new(RiskRuleNames.ForeignCountry, 20) { Detail = "BR differs from habitual UY, observed in 9 of 10 prior merchant orders (90.0%)." },
        };
        var expected = "[{\"rule\":\"amount_anomaly\",\"weight\":40,\"detail\":\"300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days.\"},"
            + "{\"rule\":\"foreign_country\",\"weight\":20,\"detail\":\"BR differs from habitual UY, observed in 9 of 10 prior merchant orders (90.0%).\"}]";

        Assert.Equal("[]", RiskSignalSerializer.Serialize([]));
        Assert.Equal(expected, RiskSignalSerializer.Serialize(signals));
        Assert.All(
            CultureProbe.Run(["en-US", "es-UY", "de-DE"], () => RiskSignalSerializer.Serialize(signals)),
            actual => Assert.Equal(expected, actual));
    }

    /// <summary>
    /// The exact canonical text of one signal of every rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These strings are hashed into the fingerprint of every evaluation that raises the rule, so
    /// they are the identity of the corpus and not a formatting preference. Written out in full
    /// because that is the only way a change to them is visible in a diff: a helper that rebuilt the
    /// expectation from the same code being tested would agree with any mistake.
    /// </para>
    /// <para>
    /// Two things they pin that nothing else does. The field order is per rule and cannot be one
    /// global sequence — <c>amount_anomaly</c> states its ratio before the median and
    /// <c>new_buyer_high_value</c> states it after — and a decimal is written at the fixed width its
    /// field declares, which is not what rounding leaves behind: the ratio below is <c>4.0</c> and
    /// not <c>4</c>, and the elapsed minutes are <c>90.00</c> and not <c>90</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryRuleWritesItsDeclaredFieldsInItsDeclaredOrder()
    {
        Assert.Equal(
            """[{"rule":"amount_anomaly","weight":40,"amountCents":40000,"currencyCode":"BRL","ratio":4.0,"scope":"buyer","medianCents":10000,"historyCount":3,"windowDays":90}]""",
            RiskSignalSerializer.Serialize(
                [RiskSignal.AmountAnomaly(40, 40000, "BRL", 4m, AmountMedianScope.Buyer, 10000, 3, 90)]));

        Assert.Equal(
            """[{"rule":"velocity","weight":30,"orderCount":4,"windowMinutes":10,"threshold":4}]""",
            RiskSignalSerializer.Serialize([RiskSignal.Velocity(30, 4, 10, 4)]));

        Assert.Equal(
            """[{"rule":"cross_border_velocity","weight":40,"fromCountry":"BR","toCountry":"UY","elapsedMinutes":90.00}]""",
            RiskSignalSerializer.Serialize([RiskSignal.CrossBorderVelocity(40, "BR", "UY", 90m)]));

        Assert.Equal(
            """[{"rule":"unusual_hour","weight":10,"bucketStartHour":0,"bucketEndHour":6,"timeZoneId":"America/Montevideo","observedCount":1,"totalCount":25,"sharePercent":4.0}]""",
            RiskSignalSerializer.Serialize(
                [RiskSignal.UnusualHour(10, 0, 6, "America/Montevideo", 1, 25, 4m)]));

        Assert.Equal(
            """[{"rule":"new_buyer_high_value","weight":30,"amountCents":40000,"currencyCode":"BRL","medianCents":10000,"historyCount":3,"ratio":4.0}]""",
            RiskSignalSerializer.Serialize([RiskSignal.NewBuyerHighValue(30, 40000, "BRL", 10000, 3, 4m)]));

        Assert.Equal(
            """[{"rule":"foreign_country","weight":20,"country":"AR","habitualCountry":"BR","observedCount":3,"totalCount":3,"sharePercent":100.0}]""",
            RiskSignalSerializer.Serialize([RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m)]));
    }

    /// <summary>
    /// An <c>e3-v1</c> row still reads back and still writes itself the way it was stored.
    /// </summary>
    /// <remarks>
    /// It is the snapshot of every alert opened before this version, and by decision 33 a snapshot
    /// is never rewritten. A signal that carries prose carries no fields, so it writes the same
    /// three keys it always did — which is what keeps its fingerprint its own.
    /// </remarks>
    [Fact]
    public void AnE3V1SignalStillWritesTheThreeKeysItWasStoredWith()
    {
        const string stored =
            """[{"rule":"amount_anomaly","weight":40,"detail":"300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days."}]""";

        var read = Assert.Single(RiskSignalSerializer.Deserialize(stored));

        Assert.Equal("300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days.", read.Detail);
        Assert.Null(read.Ratio);
        Assert.Null(read.AmountCents);
        Assert.Equal(stored, RiskSignalSerializer.Serialize([read]));
    }

    /// <summary>
    /// An <c>e3-v2</c> row reads back into the same fields it was written from, decimals included.
    /// </summary>
    [Fact]
    public void AnE3V2SignalRoundTripsThroughItsCanonicalText()
    {
        RiskSignal[] signals =
        [
            RiskSignal.AmountAnomaly(40, 201111, "BRL", 201111m / 8685, AmountMedianScope.Merchant, 8685, 3, 90),
            RiskSignal.CrossBorderVelocity(40, "BR", "UY", 5m / 3),
            RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
        ];

        var canonical = RiskSignalSerializer.Serialize(signals);
        var read = RiskSignalSerializer.Deserialize(canonical);

        Assert.Equal(signals, read);
        Assert.Equal(canonical, RiskSignalSerializer.Serialize(read));
        Assert.Equal(AmountMedianScope.Merchant, read[0].Scope);
        Assert.Equal(23.2m, read[0].Ratio);
        Assert.Equal(1.67m, read[1].ElapsedMinutes);
        Assert.All(read, signal => Assert.Null(signal.Detail));
    }

    internal static LocalRiskAssessment ScoreOrderTriggeringSeveralRules()
    {
        var currentTime = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        var history = Enumerable.Range(1, 17)
            .Select(day => CreateOrder(
                $"ORD_MIX_DAY_{day:00}",
                new DateTimeOffset(2026, 8, 1, 15, 0, 0, TimeSpan.Zero).AddDays(-day),
                buyer: $"BUY_MIX_{day:00}"))
            .Concat(
            [
                CreateOrder("ORD_MIX_FAST_1", currentTime.AddMinutes(-9), buyer: "BUY_MIX_FAST"),
                CreateOrder("ORD_MIX_FAST_2", currentTime.AddMinutes(-5), buyer: "BUY_MIX_FAST"),
                CreateOrder("ORD_MIX_FAST_3", currentTime.AddMinutes(-1), buyer: "BUY_MIX_FAST"),
            ])
            .ToArray();
        var current = CreateOrder(
            "ORD_MIX_NOW",
            currentTime,
            amountCents: 300,
            country: "BR",
            buyer: "BUY_MIX_FAST");

        return Assert.Single(
            TemporalRiskEngine.Score([.. history, current], RuleConfig.E3V1),
            assessment => assessment.OrderId == current.Id);
    }

    internal static Order CreateOrder(
        string merchantReferenceId,
        DateTimeOffset occurredAt,
        long amountCents = 100,
        string country = "UY",
        string buyer = "BUY_DEFAULT",
        string merchant = "MER_RISK")
    {
        var result = Order.Create(new(
            Guid.NewGuid(),
            merchant,
            merchantReferenceId,
            buyer,
            occurredAt,
            amountCents,
            "UYU",
            country,
            null,
            null,
            null,
            occurredAt.AddMinutes(1)));
        return Assert.IsType<Order>(result.Order);
    }

    private static string[] ReadRuleOrder(string canonical)
    {
        return canonical
            .Split("{\"rule\":\"", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(fragment => fragment[..fragment.IndexOf('"', StringComparison.Ordinal)])
            .ToArray();
    }
}
