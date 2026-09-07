using Salvo.Domain.Explanations;
using Salvo.Domain.Orders;

namespace Salvo.Application.Explanations;

/// <summary>
/// The alert an explanation was asked from, reduced to what explaining it needs.
/// </summary>
/// <remarks>
/// <para>
/// This projection is the first of the two lines that keep untrusted text away from a language
/// model. The city, the buyer, merchant and device references and the review note are not omitted
/// from the prompt later on — they never leave persistence in the first place, so no handler can
/// pass along what it never received. The second line is
/// <see cref="Salvo.Domain.Explanations.ExplanationInput"/>, whose shape a test asserts directly.
/// </para>
/// <para>
/// <paramref name="CurrentRiskEvaluationId"/> is here so that a reader can be told the explanation
/// describes an evaluation that is no longer current, which is computed and never stored.
/// </para>
/// </remarks>
public sealed record ExplanationTarget(
    Guid AlertId,
    Guid RiskEvaluationId,
    Guid? CurrentRiskEvaluationId,
    string AlertPolicyVersion,
    int Score,
    string RuleConfigVersion,
    string SignalsJson,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    OrderChannel? Channel,
    DateTimeOffset OccurredAt);

public interface IExplanationStore
{
    /// <summary>
    /// The alert, the evaluation its snapshot froze and the evaluation that is current now, or
    /// <see langword="null"/> when no alert carries that identifier.
    /// </summary>
    Task<ExplanationTarget?> FindTargetAsync(Guid alertId, CancellationToken cancellationToken);

    /// <summary>
    /// The explanation carrying this identity, tracked, or <see langword="null"/> when nobody has
    /// asked for one. Absence is an ordinary state and never an error: storing a row to mean «not
    /// requested» would be a row per evaluation for a question nobody asked.
    /// </summary>
    /// <param name="language">
    /// The language of the deployment, and the fifth column of the identity. It is what makes a
    /// deployment that changed language find that this evaluation has no text yet in the language
    /// it now writes, rather than find the other one and take it for the answer.
    /// </param>
    Task<AlertExplanation?> FindAsync(
        Guid riskEvaluationId,
        ExplanationProvider provider,
        string templateVersion,
        string alertPolicyVersion,
        ExplanationLanguage language,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the reservation. This runs <em>before</em> the provider is called, so the partial
    /// unique index decides which of two concurrent requests proceeds while nothing has been asked
    /// of anybody yet.
    /// </summary>
    /// <exception cref="ExplanationConflictException">
    /// A concurrent request reserved the same evaluation first.
    /// </exception>
    Task ReserveAsync(AlertExplanation explanation, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a transition on a row that already exists.
    /// </summary>
    /// <exception cref="ExplanationConflictException">
    /// Another writer moved the row after it was read.
    /// </exception>
    Task SaveAsync(AlertExplanation explanation, CancellationToken cancellationToken);
}
