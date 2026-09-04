namespace Salvo.Application.External;

/// <summary>
/// Timing policy for talking to an external provider. §5.3 of the Blueprint requires a stated
/// policy for timeout, retry, backoff and reconciliation before a real sandbox is enabled; this
/// type and the state machine around it are that policy.
/// </summary>
/// <param name="RequestTimeout">
/// How long a single call may take. Every external call has an explicit timeout, and a timeout is
/// never treated as a verdict.
/// </param>
/// <param name="ReconciliationMinimumAge">
/// How old a pending evaluation must be before a sweep looks at it. Zero by default: with a mock
/// that answers instantly, any positive threshold would make the demo find nothing for minutes.
/// </param>
public sealed record ExternalEvaluationOptions(
    TimeSpan RequestTimeout,
    TimeSpan ReconciliationMinimumAge)
{
    public static readonly ExternalEvaluationOptions Default = new(TimeSpan.FromSeconds(10), TimeSpan.Zero);
}
