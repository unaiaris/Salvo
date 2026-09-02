namespace Salvo.Domain.Risk;

/// <summary>
/// Outcome of a risk evaluation. A local evaluation is complete when it is created and resolves to
/// <see cref="Denied"/> when the deterministic rules flag the order, or <see cref="Approved"/> when
/// they do not. <see cref="Pending"/> and <see cref="Error"/> belong to the external lifecycle of a
/// later stage.
/// </summary>
public enum RiskEvaluationStatus
{
    Pending = 1,
    Approved = 2,
    Denied = 3,
    Error = 4,
}
