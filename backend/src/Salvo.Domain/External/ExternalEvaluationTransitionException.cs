namespace Salvo.Domain.External;

/// <summary>
/// A transition the state machine of an external evaluation does not allow.
/// </summary>
public sealed class ExternalEvaluationTransitionException : InvalidOperationException
{
    public ExternalEvaluationTransitionException()
        : base("The external evaluation cannot make that transition.")
    {
    }

    public ExternalEvaluationTransitionException(string message)
        : base(message)
    {
    }

    public ExternalEvaluationTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
