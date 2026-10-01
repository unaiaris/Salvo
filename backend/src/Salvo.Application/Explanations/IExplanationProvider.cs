using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <summary>
/// The text a provider wrote, and the rules it says the text leans on.
/// </summary>
/// <remarks>
/// Structured at the port rather than at the adapter, so that the shape does not depend on who
/// answers. A model is asked for exactly this as a JSON schema; the template fills it in directly.
/// Persisting <paramref name="ReferencedRules"/> is what makes the citation auditable instead of
/// implied, and what lets the console highlight the signals a summary leaned on.
/// </remarks>
/// <param name="Summary">
/// The prose, unverified. It exists only inside a <see cref="ExplanationProviderOutcome.Drafted"/>
/// result, so a provider that declined to write has no way to hand over half a draft.
/// </param>
public sealed record ExplanationDraft(string Summary, IReadOnlyList<string> ReferencedRules);

/// <summary>
/// What became of asking a provider, said by the provider and closed.
/// </summary>
/// <remarks>
/// Four outcomes and no more, because these are the four things a provider can know about its own
/// answer. Whether the text is <em>grounded</em> is not one of them: that is the verifier's to say,
/// after the port and before the store. And a timeout, a caller that went away or a provider that
/// threw is not one of them either: <c>ProviderCall</c> classifies those, once, for every kind of
/// provider.
/// </remarks>
public enum ExplanationProviderOutcome
{
    /// <summary>There is a draft. The verifier decides whether it is kept.</summary>
    Drafted = 1,

    /// <summary>The provider declined to write. A model may; a template may not.</summary>
    Refused = 2,

    /// <summary>There was an answer and it is not a draft: cut short, unreadable, or ended oddly.</summary>
    Malformed = 3,

    /// <summary>The provider could not answer: an error status, or a network that failed.</summary>
    Unavailable = 4,
}

/// <summary>
/// What a provider hands back: an outcome, the draft when there is one, and what the attempt cost.
/// </summary>
/// <remarks>
/// <para>
/// <strong>By value, and never by exception.</strong> <c>ProviderCall</c>, shared with the antifraud
/// provider, turns anything a provider throws into «faulted» without letting the exception cross —
/// its own documentation says so — so an adapter that signalled a cut text by throwing would see it
/// recorded as «the provider was unavailable», which is false. The antifraud port reached the same
/// shape for the same reason (decision 73).
/// </para>
/// <para>
/// <strong>No failure code is carried here, and that is the point of the type.</strong> The code is
/// fixed by <see cref="ExplanationExchange"/> from the outcome, so an adapter cannot name a code that
/// belongs to the verifier, to the lifecycle or to the system — <c>NOT_GROUNDED_NUMBER</c>,
/// <c>TOO_LONG</c>, <c>CANCELLED</c>, <c>ATTEMPT_LIMIT_REACHED</c> — however it is written. The
/// constructor is private and each outcome has one factory, so a draft cannot travel with a refusal
/// and a refusal cannot travel without its outcome.
/// </para>
/// <para>
/// <see cref="Diagnostic"/> is short and never carries text the model wrote. It goes to
/// <c>FailureDetail</c>, which the console shows.
/// </para>
/// </remarks>
public sealed class ExplanationProviderResult
{
    /// <summary>
    /// The longest diagnostic a provider may hand over. <c>FailureDetail</c> holds 200 characters,
    /// and an exhausted budget prefixes the original code to it.
    /// </summary>
    public const int MaximumDiagnosticLength = 120;

    private ExplanationProviderResult(
        ExplanationProviderOutcome outcome,
        ExplanationDraft? draft,
        string? diagnostic,
        string? providerVersion,
        int? inputTokens,
        int? outputTokens)
    {
        if (diagnostic is { Length: > MaximumDiagnosticLength })
        {
            throw new ArgumentOutOfRangeException(
                nameof(diagnostic),
                $"A diagnostic is at most {MaximumDiagnosticLength} characters.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens ?? 0, nameof(inputTokens));
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens ?? 0, nameof(outputTokens));

        Outcome = outcome;
        Draft = draft;
        Diagnostic = diagnostic;
        ProviderVersion = providerVersion;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    public ExplanationProviderOutcome Outcome { get; }

    /// <summary>Present exactly when <see cref="Outcome"/> is <see cref="ExplanationProviderOutcome.Drafted"/>.</summary>
    public ExplanationDraft? Draft { get; }

    /// <summary>
    /// A few words of what went wrong, from a vocabulary the adapter controls. Never model text.
    /// </summary>
    public string? Diagnostic { get; }

    /// <summary>
    /// The concrete model, taken from the answer and never from configuration, when there is one.
    /// </summary>
    public string? ProviderVersion { get; }

    /// <summary>What the attempt was charged for its input, in every outcome the provider reported it.</summary>
    public int? InputTokens { get; }

    /// <summary>The output counterpart of <see cref="InputTokens"/>.</summary>
    public int? OutputTokens { get; }

    public static ExplanationProviderResult Drafted(
        ExplanationDraft draft,
        string? providerVersion = null,
        int? inputTokens = null,
        int? outputTokens = null)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return new(ExplanationProviderOutcome.Drafted, draft, null, providerVersion, inputTokens, outputTokens);
    }

    public static ExplanationProviderResult Refused(
        string? diagnostic = null,
        string? providerVersion = null,
        int? inputTokens = null,
        int? outputTokens = null)
    {
        return new(ExplanationProviderOutcome.Refused, null, diagnostic, providerVersion, inputTokens, outputTokens);
    }

    public static ExplanationProviderResult Malformed(
        string? diagnostic = null,
        string? providerVersion = null,
        int? inputTokens = null,
        int? outputTokens = null)
    {
        return new(ExplanationProviderOutcome.Malformed, null, diagnostic, providerVersion, inputTokens, outputTokens);
    }

    public static ExplanationProviderResult Unavailable(
        string? diagnostic = null,
        string? providerVersion = null,
        int? inputTokens = null,
        int? outputTokens = null)
    {
        return new(ExplanationProviderOutcome.Unavailable, null, diagnostic, providerVersion, inputTokens, outputTokens);
    }
}

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

    /// <summary>
    /// Asks for the explanation once. A failure the provider knows about is an outcome, not an
    /// exception; what it did not foresee may still throw, and is recorded as unavailable.
    /// </summary>
    Task<ExplanationProviderResult> ExplainAsync(ExplanationInput input, CancellationToken cancellationToken);
}
