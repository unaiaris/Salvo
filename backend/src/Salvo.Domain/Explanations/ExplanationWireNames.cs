namespace Salvo.Domain.Explanations;

/// <summary>
/// Stable textual names for the enumerations of an explanation. The same names are stored in the
/// database and returned over HTTP, so persistence and contract cannot drift apart.
/// </summary>
public static class ExplanationWireNames
{
    public const string Mock = "MOCK";
    public const string Anthropic = "ANTHROPIC";

    public const string Pending = "PENDING";
    public const string Ready = "READY";
    public const string Failed = "FAILED";

    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string ProviderTimeout = "PROVIDER_TIMEOUT";
    public const string ProviderRefused = "PROVIDER_REFUSED";
    public const string MalformedOutput = "MALFORMED_OUTPUT";
    public const string NotGroundedNumber = "NOT_GROUNDED_NUMBER";
    public const string NotGroundedRule = "NOT_GROUNDED_RULE";
    public const string TooLong = "TOO_LONG";
    public const string Cancelled = "CANCELLED";
    public const string AttemptLimitReached = "ATTEMPT_LIMIT_REACHED";

    public static string ToWire(ExplanationProvider provider)
    {
        return provider switch
        {
            ExplanationProvider.Mock => Mock,
            ExplanationProvider.Anthropic => Anthropic,
            _ => throw new ArgumentOutOfRangeException(
                nameof(provider),
                provider,
                "Unsupported explanation provider."),
        };
    }

    public static ExplanationProvider ParseProvider(string value)
    {
        return value switch
        {
            Mock => ExplanationProvider.Mock,
            Anthropic => ExplanationProvider.Anthropic,
            _ => throw new ArgumentException(
                $"Unsupported explanation provider '{value}'.",
                nameof(value)),
        };
    }

    public static string ToWire(ExplanationStatus status)
    {
        return status switch
        {
            ExplanationStatus.Pending => Pending,
            ExplanationStatus.Ready => Ready,
            ExplanationStatus.Failed => Failed,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported explanation status."),
        };
    }

    public static ExplanationStatus ParseStatus(string value)
    {
        return value switch
        {
            Pending => ExplanationStatus.Pending,
            Ready => ExplanationStatus.Ready,
            Failed => ExplanationStatus.Failed,
            _ => throw new ArgumentException(
                $"Unsupported explanation status '{value}'.",
                nameof(value)),
        };
    }

    public static string ToWire(ExplanationFailureCode code)
    {
        return code switch
        {
            ExplanationFailureCode.ProviderUnavailable => ProviderUnavailable,
            ExplanationFailureCode.ProviderTimeout => ProviderTimeout,
            ExplanationFailureCode.ProviderRefused => ProviderRefused,
            ExplanationFailureCode.MalformedOutput => MalformedOutput,
            ExplanationFailureCode.NotGroundedNumber => NotGroundedNumber,
            ExplanationFailureCode.NotGroundedRule => NotGroundedRule,
            ExplanationFailureCode.TooLong => TooLong,
            ExplanationFailureCode.Cancelled => Cancelled,
            ExplanationFailureCode.AttemptLimitReached => AttemptLimitReached,
            _ => throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "Unsupported explanation failure code."),
        };
    }

    public static ExplanationFailureCode ParseFailureCode(string value)
    {
        return value switch
        {
            ProviderUnavailable => ExplanationFailureCode.ProviderUnavailable,
            ProviderTimeout => ExplanationFailureCode.ProviderTimeout,
            ProviderRefused => ExplanationFailureCode.ProviderRefused,
            MalformedOutput => ExplanationFailureCode.MalformedOutput,
            NotGroundedNumber => ExplanationFailureCode.NotGroundedNumber,
            NotGroundedRule => ExplanationFailureCode.NotGroundedRule,
            TooLong => ExplanationFailureCode.TooLong,
            Cancelled => ExplanationFailureCode.Cancelled,
            AttemptLimitReached => ExplanationFailureCode.AttemptLimitReached,
            _ => throw new ArgumentException(
                $"Unsupported explanation failure code '{value}'.",
                nameof(value)),
        };
    }
}
