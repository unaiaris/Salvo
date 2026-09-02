using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

/// <summary>
/// The persisted alert history of one order, reduced to what the creation predicate needs.
/// </summary>
/// <param name="HasOpenAlert">Whether an alert of the order is still awaiting a verdict.</param>
/// <param name="LatestAlertId">
/// The most recently opened alert of the order, or <see langword="null"/> when it has none.
/// </param>
/// <param name="LatestSeverity">
/// The severity of that alert. Because an alert is only ever opened on an escalation, this is also
/// the highest severity the order has reached.
/// </param>
public sealed record OrderAlertState(
    bool HasOpenAlert,
    Guid? LatestAlertId,
    AlertSeverity? LatestSeverity);
