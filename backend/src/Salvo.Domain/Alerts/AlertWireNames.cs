namespace Salvo.Domain.Alerts;

/// <summary>
/// Stable textual names for the enumerations of an alert. The same names are stored in the database
/// and returned over HTTP, so persistence and contract cannot drift apart.
/// </summary>
public static class AlertWireNames
{
    public const string Open = "OPEN";
    public const string ConfirmedSafe = "CONFIRMED_SAFE";
    public const string ReportedFraud = "REPORTED_FRAUD";

    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";

    public static string ToWire(AlertStatus status)
    {
        return status switch
        {
            AlertStatus.Open => Open,
            AlertStatus.ConfirmedSafe => ConfirmedSafe,
            AlertStatus.ReportedFraud => ReportedFraud,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported alert status."),
        };
    }

    public static AlertStatus ParseStatus(string value)
    {
        return value switch
        {
            Open => AlertStatus.Open,
            ConfirmedSafe => AlertStatus.ConfirmedSafe,
            ReportedFraud => AlertStatus.ReportedFraud,
            _ => throw new ArgumentException($"Unsupported alert status '{value}'.", nameof(value)),
        };
    }

    public static bool TryParseStatus(string? value, out AlertStatus status)
    {
        switch (value)
        {
            case Open:
                status = AlertStatus.Open;
                return true;
            case ConfirmedSafe:
                status = AlertStatus.ConfirmedSafe;
                return true;
            case ReportedFraud:
                status = AlertStatus.ReportedFraud;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static string ToWire(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Medium => Medium,
            AlertSeverity.High => High,
            AlertSeverity.Critical => Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported alert severity."),
        };
    }

    public static bool TryParseSeverity(string? value, out AlertSeverity severity)
    {
        switch (value)
        {
            case Medium:
                severity = AlertSeverity.Medium;
                return true;
            case High:
                severity = AlertSeverity.High;
                return true;
            case Critical:
                severity = AlertSeverity.Critical;
                return true;
            default:
                severity = default;
                return false;
        }
    }
}
