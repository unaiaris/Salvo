namespace Salvo.Domain.Alerts;

/// <summary>
/// Raised when a review would move an alert out of a state that is already terminal, or into a
/// state that is not a verdict.
/// </summary>
public sealed class AlertTransitionException : Exception
{
    public AlertTransitionException(string message)
        : base(message)
    {
    }

    public AlertTransitionException()
        : base("The alert cannot make this transition.")
    {
    }

    public AlertTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
