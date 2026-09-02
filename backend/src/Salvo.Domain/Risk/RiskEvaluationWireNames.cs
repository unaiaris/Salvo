namespace Salvo.Domain.Risk;

/// <summary>
/// Stable textual names for the enumerations of a risk evaluation. The same names are hashed into
/// the fingerprint and stored in the database, so both stay consistent by construction.
/// </summary>
public static class RiskEvaluationWireNames
{
    public const string Local = "LOCAL";
    public const string ExternalMock = "EXTERNAL_MOCK";
    public const string KoinSandbox = "KOIN_SANDBOX";

    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Denied = "DENIED";
    public const string Error = "ERROR";

    public static string ToWire(RiskEvaluationSource source)
    {
        return source switch
        {
            RiskEvaluationSource.Local => Local,
            RiskEvaluationSource.ExternalMock => ExternalMock,
            RiskEvaluationSource.KoinSandbox => KoinSandbox,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported evaluation source."),
        };
    }

    public static RiskEvaluationSource ParseSource(string value)
    {
        return value switch
        {
            Local => RiskEvaluationSource.Local,
            ExternalMock => RiskEvaluationSource.ExternalMock,
            KoinSandbox => RiskEvaluationSource.KoinSandbox,
            _ => throw new ArgumentException($"Unsupported evaluation source '{value}'.", nameof(value)),
        };
    }

    public static string ToWire(RiskEvaluationStatus status)
    {
        return status switch
        {
            RiskEvaluationStatus.Pending => Pending,
            RiskEvaluationStatus.Approved => Approved,
            RiskEvaluationStatus.Denied => Denied,
            RiskEvaluationStatus.Error => Error,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported evaluation status."),
        };
    }

    public static RiskEvaluationStatus ParseStatus(string value)
    {
        return value switch
        {
            Pending => RiskEvaluationStatus.Pending,
            Approved => RiskEvaluationStatus.Approved,
            Denied => RiskEvaluationStatus.Denied,
            Error => RiskEvaluationStatus.Error,
            _ => throw new ArgumentException($"Unsupported evaluation status '{value}'.", nameof(value)),
        };
    }
}
