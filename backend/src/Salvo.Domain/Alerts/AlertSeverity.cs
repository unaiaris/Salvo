namespace Salvo.Domain.Alerts;

/// <summary>
/// Severity band of an alert. The numeric values are ordered so that an escalation is a plain
/// comparison; they are never persisted, because severity is derived from the risk score.
/// </summary>
public enum AlertSeverity
{
    Medium = 1,
    High = 2,
    Critical = 3,
}
