namespace Salvo.Application.Explanations;

/// <summary>
/// Timing policy for asking a provider for prose.
/// </summary>
/// <param name="RequestTimeout">
/// How long one call may take. Explicit at the port rather than inside an adapter, so a provider
/// that never grew a network still cannot hang a request, and the one that will grow one inherits
/// the budget instead of inventing it.
/// </param>
public sealed record ExplanationOptions(TimeSpan RequestTimeout)
{
    public static readonly ExplanationOptions Default = new(TimeSpan.FromSeconds(15));

    /// <summary>
    /// How long a reservation may sit before the next request takes it over.
    /// </summary>
    /// <remarks>
    /// Twice the timeout: past that, no call that respected the budget can still be running, so a
    /// row that is still pending belongs to a request that died. This is what stands in for a
    /// reconciliation sweep in this stage — without it a killed process leaves a pending row that
    /// the partial unique index defends forever and the evaluation can never be explained again.
    /// </remarks>
    public TimeSpan AbandonedAfter => RequestTimeout + RequestTimeout;
}
