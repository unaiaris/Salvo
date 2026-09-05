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
        Guid? explanationId,
        DateTimeOffset reviewedAt)
    {
        Id = id;
        AlertId = alertId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Note = note;
        ExplanationId = explanationId;
        ReviewedAt = reviewedAt;
    }

    public Guid Id { get; private set; }

    public Guid AlertId { get; private set; }

    public AlertStatus PreviousStatus { get; private set; }

    public AlertStatus NewStatus { get; private set; }

    public string? Note { get; private set; }

    /// <summary>
    /// The explanation the reviewer had in front of them, when there was one.
    /// </summary>
    /// <remarks>
    /// An explanation is kept after it goes out of date precisely because it is the record of what
    /// could have been read when the verdict was formed — and without this column that record did
    /// not exist: a review issued while the explanation was still pending and one issued with it
    /// written were indistinguishable afterwards, for good. This is the review noting what it had
    /// in front of it, not generated prose reaching a decision: nothing here is written by a
    /// provider, and the identifier changes nothing about the verdict.
    /// </remarks>
    public Guid? ExplanationId { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }

    internal static AlertReview Record(
        Guid id,
        Guid alertId,
        AlertStatus previousStatus,
        AlertStatus newStatus,
        string? note,
        Guid? explanationId,
        DateTimeOffset reviewedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (explanationId == Guid.Empty)
        {
            throw new ArgumentException(
                "explanationId must be null or a non-empty GUID.",
                nameof(explanationId));
        }

        return new(
            id,
            alertId,
            previousStatus,
            newStatus,
            note,
            explanationId,
            reviewedAt.ToUniversalTime());
    }
}
