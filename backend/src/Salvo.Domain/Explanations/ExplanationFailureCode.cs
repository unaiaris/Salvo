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

    /// <summary>
    /// The evaluation states its signals the way <c>e3-v1</c> did, as sentences rather than as
    /// fields, so no fact of it can be built and nothing about it could be verified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is the only member that describes this system rather than a provider, and it exists
    /// because the nearest of the other nine was a lie. Until <c>E9C1</c> this case was stored as
    /// <see cref="ProviderUnavailable"/>, which reads «the provider failed before answering» and
    /// sends an analyst to debug a provider that was never called: the attempt is refused before
    /// anything is asked of anybody.
    /// </para>
    /// <para>
    /// Unreachable on a freshly seeded database, which holds no <c>e3-v1</c> row. It is reachable
    /// on one that crossed the version change, where the snapshot of an alert is never rewritten
    /// (decision 33) and therefore keeps its sentences for as long as the alert exists.
    /// </para>
    /// </remarks>
    LegacySignalFormat = 10,
}
