using Salvo.Domain.Alerts;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Application.Explanations;

/// <summary>
/// Turns what persistence returned into what a provider is allowed to see.
/// </summary>
/// <remarks>
/// The whole of the boundary rule is this one mapping, which is why it is a named type rather than
/// a few lines inside the handler: a test serializes its output and asserts what is absent, and
/// that test fails the day somebody adds a field here.
/// </remarks>
internal static class ExplanationInputFactory
{
    public static ExplanationInput For(ExplanationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var severity = AlertPolicy.ForVersion(target.AlertPolicyVersion).SeverityFor(target.Score);

        return new(
            target.Score,
            AlertWireNames.ToWire(severity),
            target.RuleConfigVersion,
            target.AlertPolicyVersion,
            RiskSignalSerializer.Deserialize(target.SignalsJson),
            target.AmountCents,
            target.CurrencyCode,
            target.CountryCode,
            target.Channel,
            target.OccurredAt);
    }
}
