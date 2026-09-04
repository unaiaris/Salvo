namespace Salvo.Domain.External;

/// <summary>
/// Stable textual names for the enumerations of an external evaluation. They are what the database
/// and the HTTP contract store, so both stay consistent by construction.
/// </summary>
public static class ExternalEvaluationWireNames
{
    public const string ExternalMock = "EXTERNAL_MOCK";
    public const string KoinSandbox = "KOIN_SANDBOX";

    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Denied = "DENIED";
    public const string Error = "ERROR";

    public const string Unreachable = "UNREACHABLE";
    public const string ProviderRejected = "PROVIDER_REJECTED";
    public const string Timeout = "TIMEOUT";
    public const string ProviderError = "PROVIDER_ERROR";
    public const string InvalidResponse = "INVALID_RESPONSE";

    public const string Sync = "SYNC";
    public const string Callback = "CALLBACK";
    public const string Reconciliation = "RECONCILIATION";

    public static string ToWire(ExternalProvider provider)
    {
        return provider switch
        {
            ExternalProvider.ExternalMock => ExternalMock,
            ExternalProvider.KoinSandbox => KoinSandbox,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported external provider."),
        };
    }

    public static ExternalProvider ParseProvider(string value)
    {
        return TryParseProvider(value, out var provider)
            ? provider
            : throw new ArgumentException($"Unsupported external provider '{value}'.", nameof(value));
    }

    public static bool TryParseProvider(string? value, out ExternalProvider provider)
    {
        switch (value)
        {
            case ExternalMock:
                provider = ExternalProvider.ExternalMock;
                return true;
            case KoinSandbox:
                provider = ExternalProvider.KoinSandbox;
                return true;
            default:
                provider = default;
                return false;
        }
    }

    public static string ToWire(ExternalEvaluationStatus status)
    {
        return status switch
        {
            ExternalEvaluationStatus.Pending => Pending,
            ExternalEvaluationStatus.Approved => Approved,
            ExternalEvaluationStatus.Denied => Denied,
            ExternalEvaluationStatus.Error => Error,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported external evaluation status."),
        };
    }

    public static ExternalEvaluationStatus ParseStatus(string value)
    {
        return value switch
        {
            Pending => ExternalEvaluationStatus.Pending,
            Approved => ExternalEvaluationStatus.Approved,
            Denied => ExternalEvaluationStatus.Denied,
            Error => ExternalEvaluationStatus.Error,
            _ => throw new ArgumentException($"Unsupported external evaluation status '{value}'.", nameof(value)),
        };
    }

    public static string ToWire(ExternalEvaluationErrorCode errorCode)
    {
        return errorCode switch
        {
            ExternalEvaluationErrorCode.Unreachable => Unreachable,
            ExternalEvaluationErrorCode.ProviderRejected => ProviderRejected,
            ExternalEvaluationErrorCode.Timeout => Timeout,
            ExternalEvaluationErrorCode.ProviderError => ProviderError,
            ExternalEvaluationErrorCode.InvalidResponse => InvalidResponse,
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Unsupported external error code."),
        };
    }

    public static ExternalEvaluationErrorCode ParseErrorCode(string value)
    {
        return value switch
        {
            Unreachable => ExternalEvaluationErrorCode.Unreachable,
            ProviderRejected => ExternalEvaluationErrorCode.ProviderRejected,
            Timeout => ExternalEvaluationErrorCode.Timeout,
            ProviderError => ExternalEvaluationErrorCode.ProviderError,
            InvalidResponse => ExternalEvaluationErrorCode.InvalidResponse,
            _ => throw new ArgumentException($"Unsupported external error code '{value}'.", nameof(value)),
        };
    }

    public static string ToWire(ExternalSettlementSource source)
    {
        return source switch
        {
            ExternalSettlementSource.Sync => Sync,
            ExternalSettlementSource.Callback => Callback,
            ExternalSettlementSource.Reconciliation => Reconciliation,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported settlement source."),
        };
    }

    public static ExternalSettlementSource ParseSettlementSource(string value)
    {
        return value switch
        {
            Sync => ExternalSettlementSource.Sync,
            Callback => ExternalSettlementSource.Callback,
            Reconciliation => ExternalSettlementSource.Reconciliation,
            _ => throw new ArgumentException($"Unsupported settlement source '{value}'.", nameof(value)),
        };
    }
}
