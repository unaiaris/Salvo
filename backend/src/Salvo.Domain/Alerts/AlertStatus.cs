namespace Salvo.Domain.Alerts;

/// <summary>
/// Review state of an alert. <see cref="ConfirmedSafe"/> and <see cref="ReportedFraud"/> are
/// terminal: a verdict is a judgement that is recorded once and never reopened.
/// </summary>
public enum AlertStatus
{
    Open = 1,
    ConfirmedSafe = 2,
    ReportedFraud = 3,
}
