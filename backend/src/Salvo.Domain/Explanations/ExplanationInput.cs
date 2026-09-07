using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Explanations;

/// <summary>
/// Everything a provider is told about an evaluation, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The rule this type exists to enforce: <strong>no text that the engine did not write reaches a
/// language model</strong>. The signals are prose, but prose this system composed from its own
/// rules; every other field here is a number, an instant or a validated enumeration.
/// </para>
/// <para>
/// What is deliberately absent, and why:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>city</c> — the one free-text field of the import contract, eighty characters of arbitrary
/// Unicode.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>buyerReferenceId</c>, <c>merchantReferenceId</c>, <c>merchantId</c>,
/// <c>deviceSessionId</c> — normalised to upper-case ASCII, which is not the same as safe: an
/// instruction fits comfortably in sixty-four characters of that alphabet.
/// </description>
/// </item>
/// <item>
/// <description>
/// The review note — up to two thousand characters written by a person.
/// </description>
/// </item>
/// <item>
/// <description>
/// The external verdict and its error code — the provider's opinion may be quoted in the console
/// beside this one, never used as the ground of it.
/// </description>
/// </item>
/// </list>
/// <para>
/// The antifraud port is deliberately less strict — <c>ExternalEvaluationInput</c> does send the
/// buyer reference — because its consumer scores a transaction rather than reading instructions.
/// </para>
/// <para>
/// <c>Language</c> is here and not a property of the provider so that writing stays a function of
/// its input: the same input produces the same paragraph on any deployment, which is what makes a
/// golden text worth pinning. It is a validated enumeration of two members, so it carries no text
/// a provider could read as anything but a choice between two vocabularies.
/// </para>
/// </remarks>
public sealed record ExplanationInput(
    int Score,
    string Severity,
    string RuleConfigVersion,
    string AlertPolicyVersion,
    ExplanationLanguage Language,
    IReadOnlyList<RiskSignal> Signals,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    OrderChannel? Channel,
    DateTimeOffset OccurredAt);
