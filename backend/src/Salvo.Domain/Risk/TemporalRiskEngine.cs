using Salvo.Domain.Orders;

namespace Salvo.Domain.Risk;

public static class TemporalRiskEngine
{
    public static IReadOnlyList<LocalRiskAssessment> Score(
        IReadOnlyCollection<Order> orders,
        RuleConfig config)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(config);

        EnsureUniqueOrders(orders);

        var ordered = orders
            .OrderBy(order => order.OccurredAt)
            .ThenBy(order => order.MerchantId, StringComparer.Ordinal)
            .ThenBy(order => order.MerchantReferenceId, StringComparer.Ordinal)
            .ToArray();
        var state = new TemporalBaselineState();
        var assessments = new List<LocalRiskAssessment>(ordered.Length);

        foreach (var cohort in ordered.GroupBy(order => order.OccurredAt))
        {
            var cohortOrders = cohort.ToArray();
            foreach (var order in cohortOrders)
            {
                assessments.Add(ScoreOne(order, state.SnapshotFor(order), config));
            }

            state.Add(cohortOrders);
        }

        return assessments.AsReadOnly();
    }

    private static LocalRiskAssessment ScoreOne(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config)
    {
        var signals = new List<RiskSignal>(6);
        AddAmountAnomaly(order, baseline, config, signals);
        AddVelocity(order, baseline, config, signals);
        AddCrossBorderVelocity(order, baseline, config, signals);
        AddUnusualHour(order, baseline, config, signals);
        AddNewBuyerHighValue(order, baseline, config, signals);
        AddForeignCountry(order, baseline, config, signals);

        var score = Math.Min(config.ScoreCap, signals.Sum(signal => signal.Weight));
        return new(
            order.Id,
            order.OccurredAt,
            score,
            score >= config.FlagThreshold,
            signals.AsReadOnly());
    }

    private static void AddAmountAnomaly(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        var start = order.OccurredAt - config.AmountLookback;
        var buyerAmounts = baseline.BuyerOrders
            .Where(previous => previous.OccurredAt >= start && previous.CurrencyCode == order.CurrencyCode)
            .Select(previous => previous.AmountCents)
            .ToArray();
        var merchantAmounts = baseline.MerchantOrders
            .Where(previous => previous.OccurredAt >= start && previous.CurrencyCode == order.CurrencyCode)
            .Select(previous => previous.AmountCents)
            .ToArray();

        long median;
        int historyCount;
        AmountMedianScope scope;
        if (buyerAmounts.Length >= config.AmountBuyerMinimumHistory)
        {
            median = Median(buyerAmounts);
            historyCount = buyerAmounts.Length;
            scope = AmountMedianScope.Buyer;
        }
        else if (merchantAmounts.Length >= config.AmountMerchantMinimumHistory)
        {
            median = Median(merchantAmounts);
            historyCount = merchantAmounts.Length;
            scope = AmountMedianScope.Merchant;
        }
        else
        {
            return;
        }

        if (!config.AmountMultiplier.IsReachedBy(order.AmountCents, median))
        {
            return;
        }

        signals.Add(RiskSignal.AmountAnomaly(
            config.AmountAnomalyWeight,
            order.AmountCents,
            order.CurrencyCode,
            (decimal)order.AmountCents / median,
            scope,
            median,
            historyCount,
            config.AmountLookback.Days));
    }

    private static void AddVelocity(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        var start = order.OccurredAt - config.VelocityWindow;
        var previousCount = baseline.BuyerOrders.Count(previous => previous.OccurredAt >= start);
        if (previousCount < config.VelocityMinimumPriorOrders)
        {
            return;
        }

        signals.Add(RiskSignal.Velocity(
            config.VelocityWeight,
            previousCount + 1,
            (int)config.VelocityWindow.TotalMinutes,
            config.VelocityMinimumPriorOrders + 1));
    }

    private static void AddCrossBorderVelocity(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        var start = order.OccurredAt - config.CrossBorderWindow;
        var conflicting = baseline.BuyerOrders
            .Where(previous => previous.OccurredAt >= start && previous.CountryCode != order.CountryCode)
            .OrderByDescending(previous => previous.OccurredAt)
            .FirstOrDefault();
        if (conflicting is null)
        {
            return;
        }

        var elapsed = order.OccurredAt - conflicting.OccurredAt;
        signals.Add(RiskSignal.CrossBorderVelocity(
            config.CrossBorderVelocityWeight,
            conflicting.CountryCode,
            order.CountryCode,
            (decimal)elapsed.TotalMinutes));
    }

    private static void AddUnusualHour(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        var start = order.OccurredAt - config.UnusualHourLookback;
        var history = baseline.MerchantOrders
            .Where(previous => previous.OccurredAt >= start)
            .ToArray();
        if (history.Length < config.UnusualHourMinimumHistory)
        {
            return;
        }

        var currentBucket = GetLocalHourBucket(order.OccurredAt, config);
        var bucketCount = history.Count(previous => GetLocalHourBucket(previous.OccurredAt, config) == currentBucket);
        if (bucketCount * 100 > history.Length * config.UnusualHourMaximumSharePercent)
        {
            return;
        }

        var bucketStart = currentBucket * config.UnusualHourBucketHours;
        var bucketEnd = bucketStart + config.UnusualHourBucketHours;
        signals.Add(RiskSignal.UnusualHour(
            config.UnusualHourWeight,
            bucketStart,
            bucketEnd,
            config.BusinessTimeZone.Id,
            bucketCount,
            history.Length,
            (decimal)bucketCount * 100 / history.Length));
    }

    private static void AddNewBuyerHighValue(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        if (baseline.BuyerOrders.Count > 0)
        {
            return;
        }

        var start = order.OccurredAt - config.NewBuyerLookback;
        var merchantAmounts = baseline.MerchantOrders
            .Where(previous => previous.OccurredAt >= start && previous.CurrencyCode == order.CurrencyCode)
            .Select(previous => previous.AmountCents)
            .ToArray();
        if (merchantAmounts.Length < config.NewBuyerMinimumMerchantHistory)
        {
            return;
        }

        var median = Median(merchantAmounts);
        if (!config.NewBuyerAmountMultiplier.IsReachedBy(order.AmountCents, median))
        {
            return;
        }

        signals.Add(RiskSignal.NewBuyerHighValue(
            config.NewBuyerHighValueWeight,
            order.AmountCents,
            order.CurrencyCode,
            median,
            merchantAmounts.Length,
            (decimal)order.AmountCents / median));
    }

    private static void AddForeignCountry(
        Order order,
        TemporalBaselineSnapshot baseline,
        RuleConfig config,
        List<RiskSignal> signals)
    {
        var start = order.OccurredAt - config.CountryLookback;
        var history = baseline.MerchantOrders
            .Where(previous => previous.OccurredAt >= start)
            .ToArray();
        if (history.Length < config.CountryMinimumHistory)
        {
            return;
        }

        var habitual = history
            .GroupBy(previous => previous.CountryCode)
            .Select(group => new { Country = group.Key, Count = group.Count() })
            .OrderByDescending(candidate => candidate.Count)
            .ThenBy(candidate => candidate.Country, StringComparer.Ordinal)
            .First();
        if (habitual.Count * 100 < history.Length * config.HabitualCountryMinimumSharePercent
            || habitual.Country == order.CountryCode)
        {
            return;
        }

        signals.Add(RiskSignal.ForeignCountry(
            config.ForeignCountryWeight,
            order.CountryCode,
            habitual.Country,
            habitual.Count,
            history.Length,
            (decimal)habitual.Count * 100 / history.Length));
    }

    private static int GetLocalHourBucket(DateTimeOffset occurredAt, RuleConfig config)
    {
        var local = TimeZoneInfo.ConvertTime(occurredAt, config.BusinessTimeZone);
        return local.Hour / config.UnusualHourBucketHours;
    }

    private static long Median(IReadOnlyCollection<long> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        if (ordered.Length % 2 != 0)
        {
            return ordered[middle];
        }

        var lower = ordered[middle - 1];
        var upper = ordered[middle];
        return lower + ((upper - lower) / 2);
    }

    private static void EnsureUniqueOrders(IReadOnlyCollection<Order> orders)
    {
        if (orders.Select(order => order.Id).Distinct().Count() != orders.Count)
        {
            throw new ArgumentException("Orders must have unique technical identifiers.", nameof(orders));
        }

        if (orders.Select(order => order.Reference).Distinct().Count() != orders.Count)
        {
            throw new ArgumentException("Orders must have unique merchant references.", nameof(orders));
        }
    }

    private sealed class TemporalBaselineState
    {
        private readonly Dictionary<string, List<Order>> merchantOrders = new(StringComparer.Ordinal);
        private readonly Dictionary<(string MerchantId, string BuyerReferenceId), List<Order>> buyerOrders = new();

        public TemporalBaselineSnapshot SnapshotFor(Order order)
        {
            merchantOrders.TryGetValue(order.MerchantId, out var merchantHistory);
            buyerOrders.TryGetValue((order.MerchantId, order.BuyerReferenceId), out var buyerHistory);
            return new(merchantHistory ?? [], buyerHistory ?? []);
        }

        public void Add(IEnumerable<Order> orders)
        {
            foreach (var order in orders)
            {
                if (!merchantOrders.TryGetValue(order.MerchantId, out var merchantHistory))
                {
                    merchantHistory = [];
                    merchantOrders.Add(order.MerchantId, merchantHistory);
                }

                merchantHistory.Add(order);

                var buyerKey = (order.MerchantId, order.BuyerReferenceId);
                if (!buyerOrders.TryGetValue(buyerKey, out var buyerHistory))
                {
                    buyerHistory = [];
                    buyerOrders.Add(buyerKey, buyerHistory);
                }

                buyerHistory.Add(order);
            }
        }
    }

    private sealed record TemporalBaselineSnapshot(
        IReadOnlyList<Order> MerchantOrders,
        IReadOnlyList<Order> BuyerOrders);
}
