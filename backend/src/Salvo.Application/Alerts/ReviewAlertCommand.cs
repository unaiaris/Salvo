using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

/// <param name="AcknowledgedDivergence">
/// Whether the reviewer confirmed having seen that the current evaluation sits in another severity
/// band than the snapshot the alert was opened with.
/// </param>
/// <param name="ExplanationId">
/// The explanation the reviewer was reading, when there was one. Optional in every sense: a verdict
/// never requires an explanation to exist, and omitting it changes nothing about the review.
/// </param>
public sealed record ReviewAlertCommand(
    AlertStatus NewStatus,
    string? Note,
    bool AcknowledgedDivergence,
    Guid? ExplanationId = null);
