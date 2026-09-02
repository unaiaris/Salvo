namespace Salvo.Domain.Alerts;

/// <summary>
/// The audit record of a verdict. Written in the same transaction as the alert it reviews.
/// </summary>
/// <remarks>
/// Without authentication the decision cannot be attributed to a person, so the record carries no
/// reviewer identity: inventing one would fabricate an audit trail. Adding it is part of the
/// post-MVP authentication work.
/// </remarks>
public sealed class AlertReview
{
    private AlertReview()
    {
    }

    private AlertReview(
        Guid id,
        Guid alertId,
        AlertStatus previousStatus,
        AlertStatus newStatus,
        string? note,
        DateTimeOffset reviewedAt)
    {
        Id = id;
        AlertId = alertId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Note = note;
        ReviewedAt = reviewedAt;
    }

    public Guid Id { get; private set; }

    public Guid AlertId { get; private set; }

    public AlertStatus PreviousStatus { get; private set; }

    public AlertStatus NewStatus { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }

    internal static AlertReview Record(
        Guid id,
        Guid alertId,
        AlertStatus previousStatus,
        AlertStatus newStatus,
        string? note,
        DateTimeOffset reviewedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        return new(id, alertId, previousStatus, newStatus, note, reviewedAt.ToUniversalTime());
    }
}
