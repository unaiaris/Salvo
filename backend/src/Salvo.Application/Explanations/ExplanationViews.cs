using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <summary>
/// The public projection of an explanation.
/// </summary>
/// <remarks>
/// A sub-object of its own beside the external evaluation, so that neither has to move when the
/// other changes. Fields are enumerated explicitly: nothing a future provider starts returning
/// reaches a client without a decision here first.
/// </remarks>
/// <param name="Summary">
/// Present exactly when <paramref name="Status"/> is ready. A failed explanation carries no text at
/// all — not a draft, not an excerpt — because the text that failed verification is never stored.
/// </param>
/// <param name="ReferencedRules">
/// The rules the provider says it leaned on, already verified to be rules the evaluation raised.
/// </param>
/// <param name="AttemptsExhausted">
/// Whether asking again is possible. It is the difference between a failure worth retrying and one
/// that is finished, which a reader cannot infer from the code alone.
/// </param>
/// <param name="IsOutdated">
/// Whether the evaluation this explains is still the current one. Computed on every read and never
/// stored, exactly like the band divergence of an alert: an evaluation that becomes current again
/// stops being outdated on its own, with nothing written.
/// </param>
/// <param name="WrittenByAnotherTemplate">
/// Whether the template that wrote this text is the one this deployment writes with today.
/// Computed on every read against the registered provider, and never stored, for the same reason
/// <paramref name="IsOutdated"/> is not: the answer changes when the provider changes, and a stored
/// copy would be a second opinion able to disagree with the one the console is about to act on.
/// <para>
/// It is inequality and not an ordering. Versions are opaque strings, so nothing here can tell a
/// later template from an earlier one, and after a rollback the stored row is the newer of the two.
/// The offer the console makes is the same either way — write this evaluation with the template
/// that is current — which is what the field is read for.
/// </para>
/// </param>
public sealed record AlertExplanationView(
    Guid Id,
    string Provider,
    string TemplateVersion,
    string? ProviderVersion,
    string Status,
    string? Summary,
    IReadOnlyList<string> ReferencedRules,
    string? FailureCode,
    int AttemptCount,
    bool AttemptsExhausted,
    bool IsOutdated,
    bool WrittenByAnotherTemplate,
    DateTimeOffset RequestedAt,
    DateTimeOffset? SettledAt);

/// <param name="Applied">
/// <see langword="false"/> when the request changed nothing because the row already answered it,
/// which is what makes a double click harmless.
/// </param>
public sealed record RequestExplanationResult(bool Applied, AlertExplanationView Explanation);

public static class ExplanationProjection
{
    /// <param name="currentTemplateVersion">
    /// What the registered provider writes with today, handed in rather than read from a constant:
    /// a copy of the version would keep answering for a provider that is no longer the one wired
    /// up, and the whole point of the field is to follow whoever is.
    /// </param>
    public static AlertExplanationView ToView(
        AlertExplanation explanation,
        bool isOutdated,
        string currentTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        return new(
            explanation.Id,
            ExplanationWireNames.ToWire(explanation.Provider),
            explanation.TemplateVersion,
            explanation.ProviderVersion,
            ExplanationWireNames.ToWire(explanation.Status),
            explanation.Summary,
            explanation.ReferencedRules(),
            explanation.FailureCode is { } code ? ExplanationWireNames.ToWire(code) : null,
            explanation.AttemptCount,
            explanation.AttemptsExhausted,
            isOutdated,
            !string.Equals(explanation.TemplateVersion, currentTemplateVersion, StringComparison.Ordinal),
            explanation.RequestedAt,
            explanation.SettledAt);
    }
}
