namespace Salvo.Domain.Explanations;

/// <summary>
/// A signal whose <c>detail</c> does not have the shape the engine of its rule configuration
/// writes.
/// </summary>
/// <remarks>
/// Declared rather than silent, and that is the whole point. The extractor of
/// <see cref="SignalFacts"/> reads prose the engine composed; if that prose ever changes without
/// the rule configuration version changing with it, an explanation built on a half-understood
/// signal would be worse than no explanation at all. The caller turns this into a failed
/// explanation with a code, never into a summary.
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
