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
    DateTimeOffset RequestedAt,
    DateTimeOffset? SettledAt);

/// <param name="Applied">
/// <see langword="false"/> when the request changed nothing because the row already answered it,
/// which is what makes a double click harmless.
/// </param>
public sealed record RequestExplanationResult(bool Applied, AlertExplanationView Explanation);

public static class ExplanationProjection
{
    public static AlertExplanationView ToView(AlertExplanation explanation, bool isOutdated)
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
            explanation.RequestedAt,
            explanation.SettledAt);
    }
}
