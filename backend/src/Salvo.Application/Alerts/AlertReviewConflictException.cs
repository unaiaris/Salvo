namespace Salvo.Application.Alerts;

/// <summary>
/// Raised when a review cannot be applied. Infrastructure translates provider-specific concurrency
/// and uniqueness failures into this type so that no persistence detail crosses the boundary.
/// </summary>
public sealed class AlertReviewConflictException : Exception
{
    public AlertReviewConflictException(AlertReviewConflictReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public AlertReviewConflictException(AlertReviewConflictReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public AlertReviewConflictException()
        : base("The review conflicts with the persisted state of the alert.")
    {
        Reason = AlertReviewConflictReason.ConcurrentReview;
    }

    public AlertReviewConflictException(string message)
        : base(message)
    {
        Reason = AlertReviewConflictReason.ConcurrentReview;
    }

    public AlertReviewConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = AlertReviewConflictReason.ConcurrentReview;
    }

    public AlertReviewConflictReason Reason { get; }
}
