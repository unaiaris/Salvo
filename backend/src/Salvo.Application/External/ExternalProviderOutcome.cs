namespace Salvo.Application.External;

/// <summary>
/// What a provider adapter reports back, in the only taxonomy that matters to the state machine:
/// whether the request was sent, and whether what came back is final.
/// </summary>
/// <remarks>
/// The distinction the adapter is responsible for is <see cref="Unreachable"/> against
/// <see cref="Transient"/>. The first says the request never left; the second says it did and the
/// outcome is unknown. Only the first is safe to settle, because only the first guarantees the
/// provider has no evaluation of its own to tell us about later.
/// </remarks>
public enum ExternalProviderOutcome
{
    /// <summary>A final verdict: the provider approves.</summary>
    Approved = 1,

    /// <summary>A final verdict: the provider denies.</summary>
    Denied = 2,

    /// <summary>Accepted and not decided yet. The answer will arrive by callback or by a later probe.</summary>
    Pending = 3,

    /// <summary>Definitively refused — a validation error, not a failure. Settles in error.</summary>
    Rejected = 4,

    /// <summary>Never sent: connection refused, name resolution, no route. Settles in error.</summary>
    Unreachable = 5,

    /// <summary>Sent, outcome unknown: a timeout, a server error, an unreadable answer. Stays pending.</summary>
    Transient = 6,
}
