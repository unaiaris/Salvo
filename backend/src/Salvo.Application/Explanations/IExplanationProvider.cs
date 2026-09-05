using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <summary>
/// What a provider hands back.
/// </summary>
/// <remarks>
/// Structured at the port rather than at the adapter, so that the shape does not depend on who
/// answers. A model will be asked for exactly this as a JSON schema; the template fills it in
/// directly. Persisting <paramref name="ReferencedRules"/> is what makes the citation auditable
/// instead of implied, and what lets the console highlight the signals a summary leaned on.
/// </remarks>
/// <param name="Summary">
/// The prose. <see langword="null"/> means the provider declined to write anything, which a model
/// may legitimately do and which is a failure with a code rather than an exception.
/// </param>
/// <param name="ProviderVersion">The concrete model, when there is one.</param>
public sealed record ExplanationDraft(
    string? Summary,
    IReadOnlyList<string> ReferencedRules,
    string? ProviderVersion = null,
    int? InputTokens = null,
    int? OutputTokens = null);

/// <summary>
/// The port every writer of explanations is reached through.
/// </summary>
/// <remarks>
/// No client library type and no vendor error crosses it, and no implementation of it is trusted:
/// whatever comes back is verified against the evaluation before it can be stored.
/// </remarks>
public interface IExplanationProvider
{
    /// <summary>Who this is, as it will be recorded in the identity of the row.</summary>
    ExplanationProvider Provider { get; }

    /// <summary>
    /// Which template or prompt this writes with. Part of the identity too: the same evaluation
    /// explained by a later template is a different explanation.
    /// </summary>
    string TemplateVersion { get; }

    Task<ExplanationDraft> ExplainAsync(ExplanationInput input, CancellationToken cancellationToken);
}
