using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

/// <param name="AcknowledgedDivergence">
/// Whether the reviewer confirmed having seen that the current evaluation sits in another severity
/// band than the snapshot the alert was opened with.
/// </param>
public sealed record ReviewAlertCommand(
    AlertStatus NewStatus,
    string? Note,
    bool AcknowledgedDivergence);
