namespace Salvo.Application.Explanations;

/// <summary>
/// Why a request for a new explanation was refused. Every reason is a conflict with what is
/// already there, never a server fault.
/// </summary>
public enum ExplanationConflictReason
{
    /// <summary>A provider is answering right now, and a second call would pay twice.</summary>
    PendingInFlight = 1,

    /// <summary>The explanation is already written. Regenerating it is not replacing it.</summary>
    AlreadyReady = 2,

    /// <summary>The attempt budget is spent, which with a paid provider is the point of having one.</summary>
    AttemptsExhausted = 3,

    /// <summary>Another writer moved the row first.</summary>
    ConcurrentUpdate = 4,
}

public sealed class ExplanationConflictException : Exception
{
    public ExplanationConflictException()
        : this(ExplanationConflictReason.ConcurrentUpdate, "The explanation could not be written.")
    {
    }

    public ExplanationConflictException(string message)
        : this(ExplanationConflictReason.ConcurrentUpdate, message)
    {
    }

    public ExplanationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = ExplanationConflictReason.ConcurrentUpdate;
    }

    public ExplanationConflictException(
        ExplanationConflictReason reason,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public ExplanationConflictReason Reason { get; }
}
