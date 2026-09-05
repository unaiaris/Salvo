namespace Salvo.Domain.Explanations;

/// <summary>
/// Lifecycle of an explanation.
/// </summary>
/// <remarks>
/// <see cref="Failed"/> is deliberately <em>not</em> terminal. Regenerating transitions the same
/// row back to <see cref="Pending"/>, which is what lets the total unique index over the identity
/// coexist with a retry: a second row for the same evaluation would violate it, and a lifecycle
/// that could not retry would leave a failed explanation stuck forever.
/// </remarks>
public enum ExplanationStatus
{
    Pending = 1,
    Ready = 2,
    Failed = 3,
}
