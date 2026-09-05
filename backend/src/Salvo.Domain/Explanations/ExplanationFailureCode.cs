namespace Salvo.Domain.Explanations;

/// <summary>
/// Closed catalogue of the reasons an explanation ends in
/// <see cref="ExplanationStatus.Failed"/>.
/// </summary>
/// <remarks>
/// <para>
/// Complete from the first day on purpose. A real provider fails in ways a template never can — it
/// times out, it declines to answer, it returns something unreadable — and discovering that after
/// the fact would mean migrating this column. <see cref="ProviderRefused"/> in particular exists
/// because a model can finish without text: that is a failure with a code, not an exception.
/// </para>
/// <para>
/// No member ever carries the text that was rejected. The catalogue is a fixed set of names, so
/// nothing a provider writes can reach the database through it.
/// </para>
/// </remarks>
public enum ExplanationFailureCode
{
    /// <summary>The provider could not be reached or threw before answering.</summary>
    ProviderUnavailable = 1,

    /// <summary>The call exceeded the explicit timeout of the port.</summary>
    ProviderTimeout = 2,

    /// <summary>The provider answered without text. A model may decline; a template may not.</summary>
    ProviderRefused = 3,

    /// <summary>The answer was structurally unusable: empty, or carrying markup or links.</summary>
    MalformedOutput = 4,

    /// <summary>A figure in the text is backed by no fact of the evaluation.</summary>
    NotGroundedNumber = 5,

    /// <summary>A rule was cited that the evaluation did not raise.</summary>
    NotGroundedRule = 6,

    /// <summary>The text exceeded the declared character budget.</summary>
    TooLong = 7,

    /// <summary>The caller went away while the provider was answering.</summary>
    Cancelled = 8,

    /// <summary>
    /// The attempt budget is spent. The row keeps this code so that a reader can tell an
    /// explanation worth retrying from one that is not.
    /// </summary>
    AttemptLimitReached = 9,
}
