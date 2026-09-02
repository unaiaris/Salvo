using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class AlertPolicyTests
{
    [Theory]
    [InlineData(60, AlertSeverity.Medium)]
    [InlineData(69, AlertSeverity.Medium)]
    [InlineData(70, AlertSeverity.High)]
    [InlineData(89, AlertSeverity.High)]
    [InlineData(90, AlertSeverity.Critical)]
    [InlineData(100, AlertSeverity.Critical)]
    public void ApprovedPolicyBandsTheAlertableScoreRange(int score, AlertSeverity expected)
    {
        Assert.Equal(expected, AlertPolicy.E4V1.SeverityFor(score));
    }

    [Fact]
    public void ApprovedPolicyIsValidAgainstTheApprovedRuleConfiguration()
    {
        AlertPolicy.E4V1.Validate(RuleConfig.E3V1);

        Assert.Equal("e4-v1", AlertPolicy.E4V1.Version);
        Assert.Same(AlertPolicy.E4V1, AlertPolicy.ForVersion("e4-v1"));
    }

    [Fact]
    public void AScoreBelowTheAlertingFloorHasNoBand()
    {
        Assert.Null(AlertPolicy.E4V1.SeverityForOrNull(59));
        Assert.Throws<ArgumentOutOfRangeException>(() => AlertPolicy.E4V1.SeverityFor(59));
    }

    [Fact]
    public void ValidationFailsWhenTheLowestBandDoesNotStartAtTheFlagThreshold()
    {
        // The situation the check exists for: a later rule configuration lowers the threshold and a
        // score of 55 would be flagged without belonging to any band.
        var policy = new AlertPolicy(
            "e4-drifted",
            [
                new(50, 69, AlertSeverity.Medium),
                new(70, 89, AlertSeverity.High),
                new(90, 100, AlertSeverity.Critical),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => policy.Validate(RuleConfig.E3V1));

        Assert.Contains("50", exception.Message, StringComparison.Ordinal);
        Assert.Contains("60", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationFailsOnGapsOverlapsAndAMissingCeiling()
    {
        var withGap = new AlertPolicy(
            "e4-gap",
            [
                new(60, 69, AlertSeverity.Medium),
                new(71, 89, AlertSeverity.High),
                new(90, 100, AlertSeverity.Critical),
            ]);
        var withOverlap = new AlertPolicy(
            "e4-overlap",
            [
                new(60, 70, AlertSeverity.Medium),
                new(70, 89, AlertSeverity.High),
                new(90, 100, AlertSeverity.Critical),
            ]);
        var short_ = new AlertPolicy(
            "e4-short",
            [
                new(60, 69, AlertSeverity.Medium),
                new(70, 89, AlertSeverity.High),
            ]);
        var unordered = new AlertPolicy(
            "e4-unordered",
            [
                new(60, 69, AlertSeverity.Critical),
                new(70, 89, AlertSeverity.High),
                new(90, 100, AlertSeverity.Medium),
            ]);

        Assert.Throws<InvalidOperationException>(() => withGap.Validate(RuleConfig.E3V1));
        Assert.Throws<InvalidOperationException>(() => withOverlap.Validate(RuleConfig.E3V1));
        Assert.Throws<InvalidOperationException>(() => short_.Validate(RuleConfig.E3V1));
        Assert.Throws<InvalidOperationException>(() => unordered.Validate(RuleConfig.E3V1));
        Assert.Throws<InvalidOperationException>(() =>
            new AlertPolicy("e4-empty", []).Validate(RuleConfig.E3V1));
    }

    [Fact]
    public void AnUnknownPolicyVersionCannotBeResolved()
    {
        Assert.Throws<ArgumentException>(() => AlertPolicy.ForVersion("e4-v2"));
    }
}
