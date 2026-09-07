using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

/// <summary>
/// Two live rule configurations, and the resolution that keeps them apart.
/// </summary>
public sealed class RuleConfigTests
{
    [Fact]
    public void EveryKnownVersionResolvesToItself()
    {
        Assert.Equal(RuleConfig.E3V1, RuleConfig.ForVersion("e3-v1"));
        Assert.Equal(RuleConfig.E3V2, RuleConfig.ForVersion("e3-v2"));
        Assert.Equal(RuleConfig.Current, RuleConfig.ForVersion(RuleConfig.Current.Version));
        Assert.Equal(
            RuleConfig.Known.Select(config => config.Version),
            ["e3-v1", "e3-v2"]);
    }

    /// <summary>
    /// A version this build does not know is refused rather than quietly read as another one.
    /// </summary>
    /// <remarks>
    /// Reading a stored evaluation under the wrong configuration produces a paragraph that is
    /// correct about a different evaluation, which is worse than no paragraph. Falling back to the
    /// current version would do exactly that, silently.
    /// </remarks>
    [Theory]
    [InlineData("e3-v0")]
    [InlineData("e3-v3")]
    [InlineData("E3-V1")]
    public void AnUnknownVersionIsRefused(string version)
    {
        var refused = Assert.Throws<ArgumentException>(() => RuleConfig.ForVersion(version));

        Assert.Contains(version, refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>e3-v2</c> changed how a signal is written and nothing about how one is judged.
    /// </summary>
    /// <remarks>
    /// This is what makes the whole task a change of representation. If a threshold ever differs
    /// between the two, every evaluation stored under the older one has to be re-read under its own
    /// configuration for any statement about it to be true — which is the reason the readers resolve
    /// by row, and the reason this test states the equality rather than assuming it.
    /// </remarks>
    [Fact]
    public void TheTwoVersionsJudgeIdentically()
    {
        Assert.Equal(RuleConfig.E3V1.FlagThreshold, RuleConfig.E3V2.FlagThreshold);
        Assert.Equal(RuleConfig.E3V1.ScoreCap, RuleConfig.E3V2.ScoreCap);
        Assert.Equal(RuleConfig.E3V1.BusinessTimeZone, RuleConfig.E3V2.BusinessTimeZone);
        Assert.Equal(RuleConfig.E3V1.AmountAnomalyWeight, RuleConfig.E3V2.AmountAnomalyWeight);
        Assert.Equal(RuleConfig.E3V1.VelocityWeight, RuleConfig.E3V2.VelocityWeight);
        Assert.Equal(RuleConfig.E3V1.CrossBorderVelocityWeight, RuleConfig.E3V2.CrossBorderVelocityWeight);
        Assert.Equal(RuleConfig.E3V1.UnusualHourWeight, RuleConfig.E3V2.UnusualHourWeight);
        Assert.Equal(RuleConfig.E3V1.NewBuyerHighValueWeight, RuleConfig.E3V2.NewBuyerHighValueWeight);
        Assert.Equal(RuleConfig.E3V1.ForeignCountryWeight, RuleConfig.E3V2.ForeignCountryWeight);
        Assert.NotEqual(RuleConfig.E3V1.Version, RuleConfig.E3V2.Version);
    }
}
