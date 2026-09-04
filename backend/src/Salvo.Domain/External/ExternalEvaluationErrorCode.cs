namespace Salvo.Domain.External;

/// <summary>
/// Closed catalogue of failure reasons. The raw message of a provider never reaches this type, so
/// nothing a remote system writes can end up persisted or shown.
/// </summary>
/// <remarks>
/// <see cref="Unreachable"/> and <see cref="ProviderRejected"/> are the only two that settle an
/// evaluation: the request was never sent, or the provider refused it definitively. The remaining
/// three describe a request that was sent and whose outcome is unknown, which leaves the evaluation
/// pending.
/// </remarks>
public enum ExternalEvaluationErrorCode
{
    Unreachable = 1,
    ProviderRejected = 2,
    Timeout = 3,
    ProviderError = 4,
    InvalidResponse = 5,
}
