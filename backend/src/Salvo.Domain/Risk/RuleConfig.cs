namespace Salvo.Domain.Risk;

public sealed class RuleConfig
{
    private RuleConfig()
    {
        Version = "e3-v1";
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

    public static RuleConfig E3V1 { get; } = new();

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
