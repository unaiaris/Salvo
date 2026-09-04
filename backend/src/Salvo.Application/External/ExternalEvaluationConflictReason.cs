namespace Salvo.Application.External;

public enum ExternalEvaluationConflictReason
{
    /// <summary>An evaluation of this order is still waiting for the provider.</summary>
    PendingEvaluationExists = 1,

    /// <summary>
    /// The current evaluation carries a verdict. Only an evaluation that failed may be replaced by
    /// a new request: asking again after a verdict would create a second evaluation on the provider
    /// side for a question already answered.
    /// </summary>
    AlreadySettled = 2,

    /// <summary>Another writer moved the row first.</summary>
    ConcurrentUpdate = 3,
}
