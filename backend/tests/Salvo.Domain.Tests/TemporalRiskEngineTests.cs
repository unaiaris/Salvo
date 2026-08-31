using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class TemporalRiskEngineTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SameTimestampOrdersAreIsolatedAndInputOrderDoesNotMatter()
    {
        var orders = new[]
        {
            CreateOrder("ORD_PRIOR_1", Epoch.AddMinutes(-9), buyer: "BUY_SHARED"),
            CreateOrder("ORD_PRIOR_2", Epoch.AddMinutes(-4), buyer: "BUY_SHARED"),
            CreateOrder("ORD_CURRENT_B", Epoch, buyer: "BUY_SHARED"),
            CreateOrder("ORD_CURRENT_A", Epoch, buyer: "BUY_SHARED"),
        };

        var forward = TemporalRiskEngine.Score(orders, RuleConfig.E3V1);
        var reverse = TemporalRiskEngine.Score(orders.Reverse().ToArray(), RuleConfig.E3V1);

        Assert.Equal(
            forward.Select(Projection),
            reverse.Select(Projection));
        Assert.All(
            forward.Where(assessment => assessment.OccurredAt == Epoch),
            assessment => Assert.DoesNotContain(
                assessment.Signals,
                signal => signal.Rule == RiskRuleNames.Velocity));
    }

    [Fact]
    public void AddingFutureOrdersCannotChangeEarlierAssessments()
    {
        var prefix = new[]
        {
            CreateOrder("ORD_PREFIX_1", Epoch.AddDays(-3), amountCents: 100),
            CreateOrder("ORD_PREFIX_2", Epoch.AddDays(-2), amountCents: 100),
            CreateOrder("ORD_PREFIX_3", Epoch.AddDays(-1), amountCents: 100),
            CreateOrder("ORD_PREFIX_4", Epoch, amountCents: 300),
        };
        var withFuture = prefix.Append(
            CreateOrder("ORD_FUTURE", Epoch.AddDays(1), amountCents: 1_000_000)).ToArray();

        var first = TemporalRiskEngine.Score(prefix, RuleConfig.E3V1);
        var second = TemporalRiskEngine.Score(withFuture, RuleConfig.E3V1)
            .Where(assessment => prefix.Any(order => order.Id == assessment.OrderId));

        Assert.Equal(first.Select(Projection), second.Select(Projection));
    }

    [Fact]
    public void AmountAnomalyUsesInclusiveMultiplierAndRespectsColdStart()
    {
        var history = new[]
        {
            CreateOrder("ORD_AMOUNT_1", Epoch.AddDays(-3), amountCents: 90, buyer: "BUY_TARGET"),
            CreateOrder("ORD_AMOUNT_2", Epoch.AddDays(-2), amountCents: 100, buyer: "BUY_OTHER_1"),
            CreateOrder("ORD_AMOUNT_3", Epoch.AddDays(-1), amountCents: 110, buyer: "BUY_OTHER_2"),
        };

        var atBoundary = ScoreCurrent(history, CreateOrder(
            "ORD_AMOUNT_BOUNDARY",
            Epoch,
            amountCents: 300,
            buyer: "BUY_TARGET"));
        var belowBoundary = ScoreCurrent(history, CreateOrder(
            "ORD_AMOUNT_BELOW",
            Epoch,
            amountCents: 299,
            buyer: "BUY_TARGET"));
        var coldStart = ScoreCurrent(history.Take(2), CreateOrder(
            "ORD_AMOUNT_COLD",
            Epoch,
            amountCents: 300,
            buyer: "BUY_TARGET"));

        Assert.Contains(atBoundary.Signals, signal => signal.Rule == RiskRuleNames.AmountAnomaly);
        Assert.DoesNotContain(belowBoundary.Signals, signal => signal.Rule == RiskRuleNames.AmountAnomaly);
        Assert.DoesNotContain(coldStart.Signals, signal => signal.Rule == RiskRuleNames.AmountAnomaly);
    }

    [Fact]
    public void AmountBaselinePrefersBuyerHistoryAndNeverMixesCurrencies()
    {
        var history = new[]
        {
            CreateOrder("ORD_SCOPE_B1", Epoch.AddDays(-6), amountCents: 200, buyer: "BUY_SCOPED"),
            CreateOrder("ORD_SCOPE_B2", Epoch.AddDays(-5), amountCents: 200, buyer: "BUY_SCOPED"),
            CreateOrder("ORD_SCOPE_B3", Epoch.AddDays(-4), amountCents: 200, buyer: "BUY_SCOPED"),
            CreateOrder("ORD_SCOPE_M1", Epoch.AddDays(-3), amountCents: 100, buyer: "BUY_OTHER_1"),
            CreateOrder("ORD_SCOPE_M2", Epoch.AddDays(-2), amountCents: 100, buyer: "BUY_OTHER_2"),
            CreateOrder("ORD_SCOPE_M3", Epoch.AddDays(-1), amountCents: 100, buyer: "BUY_OTHER_3"),
        };
        var buyerBaseline = ScoreCurrent(history, CreateOrder(
            "ORD_SCOPE_NOW",
            Epoch,
            amountCents: 450,
            buyer: "BUY_SCOPED"));
        var otherCurrencyOnly = ScoreCurrent(
            history.Select((order, index) => CreateOrder(
                $"ORD_SCOPE_USD_{index}",
                order.OccurredAt,
                amountCents: 100,
                buyer: order.BuyerReferenceId,
                currency: "USD")),
            CreateOrder("ORD_SCOPE_UYU", Epoch, amountCents: 1_000, buyer: "BUY_SCOPED"));

        Assert.DoesNotContain(buyerBaseline.Signals, signal => signal.Rule == RiskRuleNames.AmountAnomaly);
        Assert.DoesNotContain(otherCurrencyOnly.Signals, signal => signal.Rule == RiskRuleNames.AmountAnomaly);
    }

    [Fact]
    public void VelocityIncludesTheExactWindowBoundary()
    {
        var history = new[]
        {
            CreateOrder("ORD_VELOCITY_1", Epoch.AddMinutes(-10), buyer: "BUY_FAST"),
            CreateOrder("ORD_VELOCITY_2", Epoch.AddMinutes(-5), buyer: "BUY_FAST"),
            CreateOrder("ORD_VELOCITY_3", Epoch.AddMinutes(-1), buyer: "BUY_FAST"),
        };
        var current = CreateOrder("ORD_VELOCITY_NOW", Epoch, buyer: "BUY_FAST");
        var positive = ScoreCurrent(history, current);
        var outside = history.ToArray();
        outside[0] = CreateOrder("ORD_VELOCITY_OLD", Epoch.AddMinutes(-10).AddTicks(-1), buyer: "BUY_FAST");
        var negative = ScoreCurrent(outside, current);

        Assert.Contains(positive.Signals, signal => signal.Rule == RiskRuleNames.Velocity);
        Assert.DoesNotContain(negative.Signals, signal => signal.Rule == RiskRuleNames.Velocity);
    }

    [Fact]
    public void CrossBorderVelocityIsScopedToMerchantAndBuyer()
    {
        var positive = ScoreCurrent(
            [CreateOrder("ORD_BORDER_1", Epoch.AddHours(-2), country: "UY", buyer: "BUY_TRAVEL")],
            CreateOrder("ORD_BORDER_NOW", Epoch, country: "BR", buyer: "BUY_TRAVEL"));
        var otherBuyer = ScoreCurrent(
            [CreateOrder("ORD_BORDER_2", Epoch.AddMinutes(-30), country: "UY", buyer: "BUY_OTHER")],
            CreateOrder("ORD_BORDER_OTHER", Epoch, country: "BR", buyer: "BUY_TRAVEL"));
        var outside = ScoreCurrent(
            [CreateOrder("ORD_BORDER_3", Epoch.AddHours(-2).AddTicks(-1), country: "UY", buyer: "BUY_TRAVEL")],
            CreateOrder("ORD_BORDER_OLD", Epoch, country: "BR", buyer: "BUY_TRAVEL"));

        Assert.Contains(positive.Signals, signal => signal.Rule == RiskRuleNames.CrossBorderVelocity);
        Assert.DoesNotContain(otherBuyer.Signals, signal => signal.Rule == RiskRuleNames.CrossBorderVelocity);
        Assert.DoesNotContain(outside.Signals, signal => signal.Rule == RiskRuleNames.CrossBorderVelocity);
    }

    [Fact]
    public void UnusualHourRequiresEnoughMerchantHistoryAndARareLocalBucket()
    {
        var history = Enumerable.Range(1, 20)
            .Select(day => CreateOrder(
                $"ORD_HOUR_{day:00}",
                new DateTimeOffset(2026, 7, day, 15, 0, 0, TimeSpan.Zero),
                buyer: $"BUY_HOUR_{day:00}"))
            .ToArray();
        var unusual = ScoreCurrent(history, CreateOrder(
            "ORD_HOUR_RARE",
            new DateTimeOffset(2026, 7, 21, 5, 0, 0, TimeSpan.Zero),
            buyer: "BUY_HOUR_RARE"));
        var usual = ScoreCurrent(history, CreateOrder(
            "ORD_HOUR_USUAL",
            new DateTimeOffset(2026, 7, 21, 15, 0, 0, TimeSpan.Zero),
            buyer: "BUY_HOUR_USUAL"));
        var coldStart = ScoreCurrent(history.Take(19), CreateOrder(
            "ORD_HOUR_COLD",
            new DateTimeOffset(2026, 7, 21, 5, 0, 0, TimeSpan.Zero),
            buyer: "BUY_HOUR_COLD"));

        Assert.Contains(unusual.Signals, signal => signal.Rule == RiskRuleNames.UnusualHour);
        Assert.DoesNotContain(usual.Signals, signal => signal.Rule == RiskRuleNames.UnusualHour);
        Assert.DoesNotContain(coldStart.Signals, signal => signal.Rule == RiskRuleNames.UnusualHour);
    }

    [Fact]
    public void NewBuyerHighValueRequiresBothNoveltyAndHighAmount()
    {
        var history = new[]
        {
            CreateOrder("ORD_NEW_1", Epoch.AddDays(-3), amountCents: 100, buyer: "BUY_KNOWN"),
            CreateOrder("ORD_NEW_2", Epoch.AddDays(-2), amountCents: 100, buyer: "BUY_OTHER_1"),
            CreateOrder("ORD_NEW_3", Epoch.AddDays(-1), amountCents: 100, buyer: "BUY_OTHER_2"),
        };
        var newAndHigh = ScoreCurrent(history, CreateOrder(
            "ORD_NEW_HIGH",
            Epoch,
            amountCents: 250,
            buyer: "BUY_NEW"));
        var knownAndHigh = ScoreCurrent(history, CreateOrder(
            "ORD_KNOWN_HIGH",
            Epoch,
            amountCents: 250,
            buyer: "BUY_KNOWN"));
        var newAndNormal = ScoreCurrent(history, CreateOrder(
            "ORD_NEW_NORMAL",
            Epoch,
            amountCents: 249,
            buyer: "BUY_NEW"));
        var coldStart = ScoreCurrent(history.Take(2), CreateOrder(
            "ORD_NEW_COLD",
            Epoch,
            amountCents: 1_000,
            buyer: "BUY_NEW"));

        Assert.Contains(newAndHigh.Signals, signal => signal.Rule == RiskRuleNames.NewBuyerHighValue);
        Assert.DoesNotContain(knownAndHigh.Signals, signal => signal.Rule == RiskRuleNames.NewBuyerHighValue);
        Assert.DoesNotContain(newAndNormal.Signals, signal => signal.Rule == RiskRuleNames.NewBuyerHighValue);
        Assert.DoesNotContain(coldStart.Signals, signal => signal.Rule == RiskRuleNames.NewBuyerHighValue);
    }

    [Fact]
    public void ForeignCountryRequiresADominantHabitualCountry()
    {
        var dominant = new[]
        {
            CreateOrder("ORD_COUNTRY_1", Epoch.AddDays(-3), country: "UY"),
            CreateOrder("ORD_COUNTRY_2", Epoch.AddDays(-2), country: "UY"),
            CreateOrder("ORD_COUNTRY_3", Epoch.AddDays(-1), country: "UY"),
        };
        var weak = new[]
        {
            CreateOrder("ORD_COUNTRY_4", Epoch.AddDays(-4), country: "UY"),
            CreateOrder("ORD_COUNTRY_5", Epoch.AddDays(-3), country: "UY"),
            CreateOrder("ORD_COUNTRY_6", Epoch.AddDays(-2), country: "BR"),
            CreateOrder("ORD_COUNTRY_7", Epoch.AddDays(-1), country: "BR"),
        };

        var positive = ScoreCurrent(dominant, CreateOrder("ORD_COUNTRY_NOW", Epoch, country: "BR"));
        var noDominance = ScoreCurrent(weak, CreateOrder("ORD_COUNTRY_WEAK", Epoch, country: "AR"));
        var coldStart = ScoreCurrent(
            dominant.Take(2),
            CreateOrder("ORD_COUNTRY_COLD", Epoch, country: "BR"));

        Assert.Contains(positive.Signals, signal => signal.Rule == RiskRuleNames.ForeignCountry);
        Assert.DoesNotContain(noDominance.Signals, signal => signal.Rule == RiskRuleNames.ForeignCountry);
        Assert.DoesNotContain(coldStart.Signals, signal => signal.Rule == RiskRuleNames.ForeignCountry);
    }

    [Fact]
    public void ScoreIsCappedAndSignalsRemainInCanonicalOrder()
    {
        var currentTime = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        var history = Enumerable.Range(1, 17)
            .Select(day => CreateOrder(
                $"ORD_CAP_DAY_{day:00}",
                new DateTimeOffset(2026, 8, 1, 15, 0, 0, TimeSpan.Zero).AddDays(-day),
                amountCents: 100,
                country: "UY",
                buyer: $"BUY_CAP_{day:00}"))
            .Concat(
            [
                CreateOrder("ORD_CAP_FAST_1", currentTime.AddMinutes(-9), amountCents: 100, country: "UY", buyer: "BUY_CAP_FAST"),
                CreateOrder("ORD_CAP_FAST_2", currentTime.AddMinutes(-5), amountCents: 100, country: "UY", buyer: "BUY_CAP_FAST"),
                CreateOrder("ORD_CAP_FAST_3", currentTime.AddMinutes(-1), amountCents: 100, country: "UY", buyer: "BUY_CAP_FAST"),
            ])
            .ToArray();
        var current = CreateOrder(
            "ORD_CAP_NOW",
            currentTime,
            amountCents: 300,
            country: "BR",
            buyer: "BUY_CAP_FAST");

        var assessment = ScoreCurrent(history, current);

        Assert.Equal(100, assessment.Score);
        Assert.True(assessment.IsFlagged);
        Assert.Equal(
            [
                RiskRuleNames.AmountAnomaly,
                RiskRuleNames.Velocity,
                RiskRuleNames.CrossBorderVelocity,
                RiskRuleNames.UnusualHour,
                RiskRuleNames.ForeignCountry,
            ],
            assessment.Signals.Select(signal => signal.Rule));
    }

    [Fact]
    public void RuleConfigExposesTheApprovedImmutableVersion()
    {
        var config = RuleConfig.E3V1;

        Assert.Equal("e3-v1", config.Version);
        Assert.Equal(60, config.FlagThreshold);
        Assert.Equal("America/Montevideo", config.BusinessTimeZone.Id);
        Assert.Equal(new RationalMultiplier(3, 1), config.AmountMultiplier);
        Assert.Equal(new RationalMultiplier(5, 2), config.NewBuyerAmountMultiplier);
    }

    private static LocalRiskAssessment ScoreCurrent(
        IEnumerable<Order> history,
        Order current)
    {
        return Assert.Single(
            TemporalRiskEngine.Score(history.Append(current).ToArray(), RuleConfig.E3V1),
            assessment => assessment.OrderId == current.Id);
    }

    private static Order CreateOrder(
        string merchantReferenceId,
        DateTimeOffset occurredAt,
        long amountCents = 100,
        string country = "UY",
        string buyer = "BUY_DEFAULT",
        string merchant = "MER_RISK",
        string currency = "UYU")
    {
        var result = Order.Create(new(
            Guid.NewGuid(),
            merchant,
            merchantReferenceId,
            buyer,
            occurredAt,
            amountCents,
            currency,
            country,
            null,
            null,
            null,
            occurredAt.AddMinutes(1)));
        return Assert.IsType<Order>(result.Order);
    }

    private static (Guid OrderId, DateTimeOffset OccurredAt, int Score, bool IsFlagged, string Signals) Projection(
        LocalRiskAssessment assessment)
    {
        var signals = string.Join(
            "|",
            assessment.Signals.Select(signal => $"{signal.Rule}:{signal.Weight}:{signal.Detail}"));
        return (assessment.OrderId, assessment.OccurredAt, assessment.Score, assessment.IsFlagged, signals);
    }
}
