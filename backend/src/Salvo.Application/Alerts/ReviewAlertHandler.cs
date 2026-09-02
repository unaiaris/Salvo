using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

/// <summary>
/// Records the verdict of an analyst.
/// </summary>
/// <remarks>
/// The in-memory check that an alert is still open does not protect anything on its own: two
/// requests can both read <see cref="AlertStatus.Open"/> and both commit. The status of an alert is
/// a concurrency token, so the loser of that race is refused by the store instead of overwriting
/// the verdict of the winner.
/// </remarks>
public sealed class ReviewAlertHandler(
    IAlertStore store,
    IAlertIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Applies the verdict, or returns <see langword="null"/> when no alert carries that identifier.
    /// </summary>
    /// <exception cref="AlertReviewConflictException">
    /// The alert already carries a different verdict, the same verdict with a different note, or the
    /// current evaluation diverges from the snapshot and the reviewer did not acknowledge it.
    /// </exception>
    /// <exception cref="AlertTransitionException">
    /// <paramref name="command"/> asks for a status that is not a verdict.
    /// </exception>
    public async Task<AlertReviewResult?> HandleAsync(
        Guid alertId,
        ReviewAlertCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = await store.FindForReviewAsync(alertId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        if (command.NewStatus is not (AlertStatus.ConfirmedSafe or AlertStatus.ReportedFraud))
        {
            throw new AlertTransitionException(
                $"'{AlertWireNames.ToWire(command.NewStatus)}' is not a verdict; an alert can only be closed.");
        }

        var alert = context.Alert;
        var note = Normalize(command.Note);

        if (alert.Status != AlertStatus.Open)
        {
            return RepeatOf(context, command.NewStatus, note);
        }

        var divergence = AlertProjection.ToDivergence(alert, context.CurrentEvaluation);
        if (divergence.HasBandDivergence && !command.AcknowledgedDivergence)
        {
            throw new AlertReviewConflictException(
                AlertReviewConflictReason.DivergenceNotAcknowledged,
                $"The current evaluation of the order scores {divergence.CurrentScore} "
                + $"({divergence.CurrentSeverity ?? "no band"}) while the alert was opened at "
                + $"{divergence.SnapshotScore} ({divergence.SnapshotSeverity}). Acknowledge the "
                + "divergence to review it anyway.");
        }

        var review = alert.Review(
            idGenerator.Create(),
            command.NewStatus,
            note,
            timeProvider.GetUtcNow());
        await store.SaveReviewAsync(alert, review, cancellationToken);

        return new(true, AlertProjection.ToDetail(context with { Review = review }));
    }

    /// <summary>
    /// Resolves a request against an alert that is already reviewed. Repeating the very same verdict
    /// is a no-op; anything else is a conflict, because without a reviewer identity a second,
    /// differing decision has nowhere to be recorded and must not be discarded in silence.
    /// </summary>
    private static AlertReviewResult RepeatOf(AlertContext context, AlertStatus newStatus, string? note)
    {
        var alert = context.Alert;

        if (alert.Status != newStatus)
        {
            throw new AlertReviewConflictException(
                AlertReviewConflictReason.AlreadyReviewedWithDifferentStatus,
                $"Alert {alert.Id} was already reviewed as '{AlertWireNames.ToWire(alert.Status)}' "
                + $"and a verdict is terminal.");
        }

        if (!string.Equals(Normalize(context.Review?.Note), note, StringComparison.Ordinal))
        {
            throw new AlertReviewConflictException(
                AlertReviewConflictReason.AlreadyReviewedWithDifferentNote,
                $"Alert {alert.Id} was already reviewed as "
                + $"'{AlertWireNames.ToWire(alert.Status)}' with a different note.");
        }

        return new(false, AlertProjection.ToDetail(context));
    }

    private static string? Normalize(string? note)
    {
        var trimmed = note?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
