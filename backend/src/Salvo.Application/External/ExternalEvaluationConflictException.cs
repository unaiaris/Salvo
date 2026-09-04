namespace Salvo.Application.External;

public sealed class ExternalEvaluationConflictException : Exception
{
    public ExternalEvaluationConflictException()
        : this(ExternalEvaluationConflictReason.ConcurrentUpdate, "The external evaluation could not be written.")
    {
    }

    public ExternalEvaluationConflictException(string message)
        : this(ExternalEvaluationConflictReason.ConcurrentUpdate, message)
    {
    }

    public ExternalEvaluationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = ExternalEvaluationConflictReason.ConcurrentUpdate;
    }

    public ExternalEvaluationConflictException(
        ExternalEvaluationConflictReason reason,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public ExternalEvaluationConflictReason Reason { get; }
}
