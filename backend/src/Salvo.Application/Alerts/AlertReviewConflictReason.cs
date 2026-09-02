namespace Salvo.Application.Alerts;

/// <summary>
/// Why a review was refused. Every reason maps to a conflict, never to a server error.
/// </summary>
public enum AlertReviewConflictReason
{
    /// <summary>The alert already carries a different verdict.</summary>
    AlreadyReviewedWithDifferentStatus = 1,

    /// <summary>The alert already carries the same verdict, recorded with a different note.</summary>
    AlreadyReviewedWithDifferentNote = 2,

    /// <summary>The current evaluation sits in another severity band and that was not acknowledged.</summary>
    DivergenceNotAcknowledged = 3,

    /// <summary>Another review won the race for the same alert.</summary>
    ConcurrentReview = 4,
}
