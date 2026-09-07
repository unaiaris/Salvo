namespace Salvo.Domain.Risk;

/// <summary>
/// The thresholds, windows and weights the engine judges by, and the version that names them.
/// </summary>
/// <remarks>
/// <para>
/// <c>e3-v2</c> keeps every number of <c>e3-v1</c> and changes only how a signal is written: prose
/// became fields. The two configurations therefore judge identically and differ solely in the
/// version each evaluation is stamped with, which is exactly what makes the change one of
/// representation.
/// </para>
/// <para>
/// Two live versions is why <see cref="ForVersion"/> exists. Reading a stored row means resolving
/// the configuration <em>that row</em> was written under: explaining an <c>e3-v1</c> evaluation
/// with the thresholds of another version would produce a paragraph that is correct about the
/// wrong evaluation. The thresholds happen to be equal today, which is precisely why a mistake here
/// would be invisible until the day they are not.
/// </para>
/// </remarks>
public sealed class RuleConfig
{
    private RuleConfig(string version)
    {
        Version = version;
        ScoreCap = 100;
        FlagThreshold = 60;
        BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo");

        AmountLookback = TimeSpan.FromDays(90);
        AmountBuyerMinimumHistory = 3;
        AmountMerchantMinimumHistory = 3;
        AmountMultiplier = new(3, 1);
        AmountAnomalyWeight = 40;

        VelocityWindow = TimeSpan.FromMinutes(10);
        VelocityMinimumPriorOrders = 3;
        VelocityWeight = 30;

        CrossBorderWindow = TimeSpan.FromHours(2);
        CrossBorderVelocityWeight = 40;

        UnusualHourLookback = TimeSpan.FromDays(30);
        UnusualHourMinimumHistory = 20;
        UnusualHourBucketHours = 6;
        UnusualHourMaximumSharePercent = 10;
        UnusualHourWeight = 10;

        NewBuyerLookback = TimeSpan.FromDays(90);
        NewBuyerMinimumMerchantHistory = 3;
        NewBuyerAmountMultiplier = new(5, 2);
        NewBuyerHighValueWeight = 30;

        CountryLookback = TimeSpan.FromDays(90);
        CountryMinimumHistory = 3;
        HabitualCountryMinimumSharePercent = 60;
        ForeignCountryWeight = 20;

        Validate();
    }

    /// <summary>The version that wrote each signal as an English sentence.</summary>
    public static RuleConfig E3V1 { get; } = new("e3-v1");

    /// <summary>The version that writes each signal as its fields.</summary>
    public static RuleConfig E3V2 { get; } = new("e3-v2");

    /// <summary>What the engine writes with now. Every new evaluation is stamped with it.</summary>
    public static RuleConfig Current => E3V2;

    /// <summary>Every configuration this build knows how to read a stored evaluation under.</summary>
    public static IReadOnlyList<RuleConfig> Known { get; } = [E3V1, E3V2];

    /// <summary>
    /// The configuration a stored evaluation was written under.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// No configuration of this build carries that version, so nothing can be said about the row
    /// with any authority.
    /// </exception>
    public static RuleConfig ForVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        foreach (var candidate in Known)
        {
            if (string.Equals(candidate.Version, version, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        throw new ArgumentException($"Unknown rule configuration version '{version}'.", nameof(version));
    }

    public string Version { get; }

    public int ScoreCap { get; }

    public int FlagThreshold { get; }

    public TimeZoneInfo BusinessTimeZone { get; }

    public TimeSpan AmountLookback { get; }

    public int AmountBuyerMinimumHistory { get; }

    public int AmountMerchantMinimumHistory { get; }

    public RationalMultiplier AmountMultiplier { get; }

    public int AmountAnomalyWeight { get; }

    public TimeSpan VelocityWindow { get; }

    public int VelocityMinimumPriorOrders { get; }

    public int VelocityWeight { get; }

    public TimeSpan CrossBorderWindow { get; }

    public int CrossBorderVelocityWeight { get; }

    public TimeSpan UnusualHourLookback { get; }

    public int UnusualHourMinimumHistory { get; }

    public int UnusualHourBucketHours { get; }

    public int UnusualHourMaximumSharePercent { get; }

    public int UnusualHourWeight { get; }

    public TimeSpan NewBuyerLookback { get; }

    public int NewBuyerMinimumMerchantHistory { get; }

    public RationalMultiplier NewBuyerAmountMultiplier { get; }

    public int NewBuyerHighValueWeight { get; }

    public TimeSpan CountryLookback { get; }

    public int CountryMinimumHistory { get; }

    public int HabitualCountryMinimumSharePercent { get; }

    public int ForeignCountryWeight { get; }

    private void Validate()
    {
        if (ScoreCap != 100 || FlagThreshold is < 0 or > 100)
        {
            throw new InvalidOperationException("The score cap and flag threshold are invalid.");
        }

        if (UnusualHourBucketHours <= 0 || 24 % UnusualHourBucketHours != 0)
        {
            throw new InvalidOperationException("The unusual-hour bucket must divide a day exactly.");
        }

        if (UnusualHourMaximumSharePercent is < 0 or > 100
            || HabitualCountryMinimumSharePercent is < 1 or > 100)
        {
            throw new InvalidOperationException("Configured percentages must be between zero and one hundred.");
        }

        var weights = new[]
        {
            AmountAnomalyWeight,
            VelocityWeight,
            CrossBorderVelocityWeight,
            UnusualHourWeight,
            NewBuyerHighValueWeight,
            ForeignCountryWeight,
        };
        if (weights.Any(weight => weight is < 0 or > 100))
        {
            throw new InvalidOperationException("Rule weights must be between zero and one hundred.");
        }
    }
}
