namespace Salvo.Domain.Explanations;

/// <summary>
/// Who wrote an explanation.
/// </summary>
/// <remarks>
/// Part of the identity of the row: the same evaluation explained by a template and by a model are
/// two different explanations, not one rewritten. <see cref="Anthropic"/> existed from stage 7 so
/// that adding the adapter in stage 11 was a registration rather than a migration, and it was.
/// </remarks>
public enum ExplanationProvider
{
    /// <summary>The deterministic template. Composes prose from the signals; no network, no model.</summary>
    Mock = 1,

    /// <summary>
    /// A model of Anthropic, through <c>AI_PROVIDER=anthropic</c> with a key and a model. It writes
    /// what the template writes — a draft — and the same verifier decides whether it is kept. Never
    /// on the shared public instance.
    /// </summary>
    Anthropic = 2,
}
