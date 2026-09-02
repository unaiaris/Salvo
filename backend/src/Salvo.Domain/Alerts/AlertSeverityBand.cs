namespace Salvo.Domain.Alerts;

/// <summary>
/// One closed score interval of an <see cref="AlertPolicy"/> and the severity it maps to.
/// </summary>
public sealed record AlertSeverityBand(int MinimumScore, int MaximumScore, AlertSeverity Severity)
{
    public bool Contains(int score) => score >= MinimumScore && score <= MaximumScore;
}
