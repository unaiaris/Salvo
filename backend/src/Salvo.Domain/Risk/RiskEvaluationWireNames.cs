namespace Salvo.Domain.Risk;

/// <summary>
/// Stable textual names for the enumerations of a risk evaluation. The same names are hashed into
/// the fingerprint and stored in the database, so both stay consistent by construction.
/// </summary>
public static class RiskEvaluationWireNames
{
    public const string Local = "LOCAL";

    public const string Approved = "APPROVED";
    public const string Denied = "DENIED";

    public static string ToWire(RiskEvaluationSource source)
    {
        return source switch
        {
            RiskEvaluationSource.Local => Local,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported evaluation source."),
        };
    }

    public static RiskEvaluationSource ParseSource(string value)
    {
        return value switch
        {
            Local => RiskEvaluationSource.Local,
            _ => throw new ArgumentException($"Unsupported evaluation source '{value}'.", nameof(value)),
        };
    }

    public static string ToWire(RiskEvaluationStatus status)
    {
        return status switch
        {
            RiskEvaluationStatus.Approved => Approved,
            RiskEvaluationStatus.Denied => Denied,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported evaluation status."),
        };
    }

    public static RiskEvaluationStatus ParseStatus(string value)
    {
        return value switch
        {
            Approved => RiskEvaluationStatus.Approved,
            Denied => RiskEvaluationStatus.Denied,
            _ => throw new ArgumentException($"Unsupported evaluation status '{value}'.", nameof(value)),
        };
    }
}
