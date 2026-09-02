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
            RiskSignalSerializer.Serialize([new("made_up_rule", 10, "detail")]));
        Assert.Throws<ArgumentException>(() =>
            RiskSignalSerializer.Serialize(
            [
                new(RiskRuleNames.Velocity, 30, "first"),
                new(RiskRuleNames.Velocity, 30, "second"),
            ]));
    }

    [Fact]
    public void SerializationIsStableAndIndependentOfTheAmbientCulture()
    {
        var signals = new RiskSignal[]
        {
            new(RiskRuleNames.AmountAnomaly, 40, "300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days."),
            new(RiskRuleNames.ForeignCountry, 20, "BR differs from habitual UY, observed in 9 of 10 prior merchant orders (90.0%)."),
        };
        var expected = "[{\"rule\":\"amount_anomaly\",\"weight\":40,\"detail\":\"300 UYU cents is 3.0x the buyer median 100 over 3 prior orders in 90 days.\"},"
            + "{\"rule\":\"foreign_country\",\"weight\":20,\"detail\":\"BR differs from habitual UY, observed in 9 of 10 prior merchant orders (90.0%).\"}]";

        Assert.Equal("[]", RiskSignalSerializer.Serialize([]));
        Assert.Equal(expected, RiskSignalSerializer.Serialize(signals));
        Assert.All(
            CultureProbe.Run(["en-US", "es-UY", "de-DE"], () => RiskSignalSerializer.Serialize(signals)),
            actual => Assert.Equal(expected, actual));
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
