namespace Salvo.Domain.Explanations;

/// <summary>
/// A transition the lifecycle of an explanation does not allow.
/// </summary>
public sealed class ExplanationTransitionException : InvalidOperationException
{
    public ExplanationTransitionException()
        : base("The explanation cannot make that transition.")
    {
    }

    public ExplanationTransitionException(string message)
        : base(message)
    {
    }

    public ExplanationTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
