namespace Salvo.Application.External;

/// <summary>
/// How an evaluation is asked about again.
/// </summary>
/// <param name="ExternalEvaluationId">
/// The identifier the provider assigned, when there is one.
/// </param>
/// <param name="ReferenceId">
/// The stable reference of the order, always present because it is written during the reservation.
/// Looking up by identifier alone cannot reconcile the evaluation whose identifier never arrived,
/// which is precisely the case reconciliation exists to resolve.
/// </param>
public sealed record ExternalEvaluationLookup(string? ExternalEvaluationId, string ReferenceId);
