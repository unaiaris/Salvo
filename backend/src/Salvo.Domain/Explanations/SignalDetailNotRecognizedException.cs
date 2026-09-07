namespace Salvo.Domain.Explanations;

/// <summary>
/// A signal whose <c>detail</c> does not have the shape the engine of its rule configuration
/// writes.
/// </summary>
/// <remarks>
/// Declared rather than silent, and that is the whole point. What raises it now is an
/// <c>e3-v1</c> row, whose signals are English sentences that nothing here reads any more: the
/// snapshot of an alert opened before the engine emitted fields, never rewritten by decision 33.
/// An explanation built on a half-understood signal would be worse than no explanation at all, so
/// the caller turns this into a failed explanation with a code, never into a summary and never into
/// an unhandled exception.
/// </remarks>
public sealed class SignalDetailNotRecognizedException : Exception
{
    public SignalDetailNotRecognizedException()
        : base("The signal detail does not match the shape its rule is known to write.")
    {
    }

    public SignalDetailNotRecognizedException(string message)
        : base(message)
    {
    }

    public SignalDetailNotRecognizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static SignalDetailNotRecognizedException ForRule(string rule)
    {
        return new($"The detail of rule '{rule}' does not match the shape the engine writes for it.");
    }
}
