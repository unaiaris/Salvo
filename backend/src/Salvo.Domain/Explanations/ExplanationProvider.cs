namespace Salvo.Domain.Explanations;

/// <summary>
/// Who wrote an explanation.
/// </summary>
/// <remarks>
/// Part of the identity of the row: the same evaluation explained by a template and by a model are
/// two different explanations, not one rewritten. <see cref="Anthropic"/> has no adapter in this
/// build and exists so that adding one is a registration rather than a migration.
/// </remarks>
public enum ExplanationProvider
{
    /// <summary>The deterministic template. Composes prose from the signals; no network, no model.</summary>
    Mock = 1,

    /// <summary>Reserved. No adapter exists yet, and asking for one refuses to start.</summary>
    Anthropic = 2,
}
