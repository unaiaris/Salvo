namespace Salvo.Domain.Risk;

public static class RiskRuleNames
{
    public const string AmountAnomaly = "amount_anomaly";
    public const string Velocity = "velocity";
    public const string CrossBorderVelocity = "cross_border_velocity";
    public const string UnusualHour = "unusual_hour";
    public const string NewBuyerHighValue = "new_buyer_high_value";
    public const string ForeignCountry = "foreign_country";

    /// <summary>
    /// The order in which <see cref="TemporalRiskEngine"/> emits signals. Canonical serialization
    /// follows this order, not an alphabetical one, so that rescoring a stored evaluation
    /// reproduces its own fingerprint.
    /// </summary>
    private static readonly string[] Canonical =
    [
        AmountAnomaly,
        Velocity,
        CrossBorderVelocity,
        UnusualHour,
        NewBuyerHighValue,
        ForeignCountry,
    ];

    public static IReadOnlyList<string> CanonicalOrder => Canonical;

    /// <summary>
    /// Position of <paramref name="rule"/> in the canonical order.
    /// </summary>
    /// <exception cref="ArgumentException">The rule is not a known deterministic rule.</exception>
    public static int CanonicalIndexOf(string rule)
    {
        var index = Array.IndexOf(Canonical, rule);
        if (index < 0)
        {
            throw new ArgumentException($"Unknown risk rule '{rule}'.", nameof(rule));
        }

        return index;
    }
}
