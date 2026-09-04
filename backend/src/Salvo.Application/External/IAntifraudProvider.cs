using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The port every external antifraud provider is reached through. No type of an HTTP client, and no
/// error of a specific vendor, crosses it.
/// </summary>
public interface IAntifraudProvider
{
    ExternalProvider Provider { get; }

    Task<ExternalEvaluationResult> EvaluateAsync(
        ExternalEvaluationInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asks again about an evaluation already sent. Takes the whole lookup, not just an identifier,
    /// because the pending row that most needs reconciling is the one whose identifier never came.
    /// </summary>
    Task<ExternalEvaluationResult> GetStatusAsync(
        ExternalEvaluationLookup lookup,
        CancellationToken cancellationToken);
}
