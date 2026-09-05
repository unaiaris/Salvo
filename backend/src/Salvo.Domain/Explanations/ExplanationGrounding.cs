using System.Buffers;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Explanations;

/// <param name="FailureCode">Null exactly when the text passed every layer.</param>
/// <param name="Offender">
/// The token or rule name that failed, and nothing more. Never a fragment of the text: what is
/// needed to debug a rejection is which figure was not backed, not the sentence that carried it.
/// </param>
public sealed record GroundingVerdict(ExplanationFailureCode? FailureCode, string? Offender)
{
    public static readonly GroundingVerdict Grounded = new(null, null);

    public bool IsGrounded => FailureCode is null;
}

/// <summary>
/// The three layers that decide whether a generated summary may be persisted.
/// </summary>
/// <remarks>
/// <para>
/// The check is pure and lives here; the <em>call</em> lives in the use case, between the port and
/// the store. That placement is the whole design. Verifying inside an adapter would mean the
/// deterministic template passes because it is polite, and a future adapter passes because someone
/// remembered — instead of every provider passing because there is no route to the database that
/// skips this.
/// </para>
/// <para>
/// «Uses only the supplied signals» is a promise when it is written into a prompt and a property
/// when it is checked on the way out. This is the checking.
/// </para>
/// </remarks>
public static class ExplanationGrounding
{
    /// <summary>
    /// The character budget of a summary. Matches the database constraint, so text that would be
    /// refused by the schema is refused here first, with a code the interface can explain.
    /// </summary>
    public const int MaximumSummaryLength = 1200;

    /// <summary>
    /// Markup, code and link punctuation. A summary is prose an analyst reads inside a page this
    /// system renders; it has no business carrying its own formatting.
    /// </summary>
    private static readonly SearchValues<char> ForbiddenMarkup = SearchValues.Create("`*[]<>|#");

    private static readonly string[] LinkPrefixes = ["http://", "https://", "www."];

    public static GroundingVerdict Verify(
        string summary,
        IReadOnlyList<string> referencedRules,
        ExplanationInput input,
        ExplanationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(referencedRules);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(facts);

        var form = VerifyForm(summary);
        if (!form.IsGrounded)
        {
            return form;
        }

        var rules = VerifyRules(summary, referencedRules, input);

        return rules.IsGrounded ? VerifyNumbers(summary, facts) : rules;
    }

    /// <summary>
    /// Layer 3, applied first because it is the cheapest and because tokenizing an unbounded string
    /// to reject it afterwards would be work done for nothing.
    /// </summary>
    private static GroundingVerdict VerifyForm(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return new(ExplanationFailureCode.MalformedOutput, null);
        }

        if (summary.Length > MaximumSummaryLength)
        {
            return new(ExplanationFailureCode.TooLong, null);
        }

        var markup = summary.AsSpan().IndexOfAny(ForbiddenMarkup);
        if (markup >= 0)
        {
            return new(ExplanationFailureCode.MalformedOutput, summary[markup].ToString());
        }

        foreach (var prefix in LinkPrefixes)
        {
            if (summary.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return new(ExplanationFailureCode.MalformedOutput, prefix);
            }
        }

        return GroundingVerdict.Grounded;
    }

    /// <summary>
    /// Layer 1. Every rule the provider claims to have cited has to be one the evaluation actually
    /// raised, and so does every rule identifier that appears in the prose.
    /// </summary>
    private static GroundingVerdict VerifyRules(
        string summary,
        IReadOnlyList<string> referencedRules,
        ExplanationInput input)
    {
        var raised = new HashSet<string>(StringComparer.Ordinal);
        foreach (var signal in input.Signals)
        {
            raised.Add(signal.Rule);
        }

        foreach (var rule in referencedRules)
        {
            if (!raised.Contains(rule))
            {
                return new(ExplanationFailureCode.NotGroundedRule, rule);
            }
        }

        // A summary that names a rule the evaluation did not raise is claiming a reason that does
        // not exist, whether or not it also listed it as a citation.
        foreach (var rule in RiskRuleNames.CanonicalOrder)
        {
            if (!raised.Contains(rule) && summary.Contains(rule, StringComparison.Ordinal))
            {
                return new(ExplanationFailureCode.NotGroundedRule, rule);
            }
        }

        return GroundingVerdict.Grounded;
    }

    /// <summary>
    /// Layer 2. Every figure has to be backed by a fact of the evaluation.
    /// </summary>
    private static GroundingVerdict VerifyNumbers(string summary, ExplanationFacts facts)
    {
        foreach (var token in NumberTokenizer.Extract(summary))
        {
            if (!facts.IsGrounded(token))
            {
                return new(ExplanationFailureCode.NotGroundedNumber, token.Text);
            }
        }

        return GroundingVerdict.Grounded;
    }
}
