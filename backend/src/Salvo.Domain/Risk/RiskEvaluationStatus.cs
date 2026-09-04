namespace Salvo.Domain.Risk;

/// <summary>
/// Outcome of a local risk evaluation. It is complete when it is created and resolves to
/// <see cref="Denied"/> when the deterministic rules flag the order, or <see cref="Approved"/> when
/// they do not.
/// </summary>
/// <remarks>
/// There is no pending or error state here, and there cannot be: a pure function of the corpus
/// always has an answer. Those states belong to <c>ExternalEvaluationStatus</c>.
/// </remarks>
public enum RiskEvaluationStatus
{
    Approved = 2,
    Denied = 3,
}
