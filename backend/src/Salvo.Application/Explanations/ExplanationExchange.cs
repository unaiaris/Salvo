using Salvo.Application.Providers;
using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <param name="FailureCode">Null exactly when the provider produced a draft.</param>
/// <param name="Detail">
/// What goes to <c>FailureDetail</c> when the attempt failed at the provider: its diagnostic, which
/// never carries model text.
/// </param>
/// <param name="InputTokens">
/// What the attempt was charged, when the provider said, whether or not it produced a draft: a
/// refusal is paid for too.
/// </param>
internal sealed record ExplanationAttempt(
    ExplanationDraft? Draft,
    ExplanationFailureCode? FailureCode,
    string? Detail = null,
    string? ProviderVersion = null,
    int? InputTokens = null,
    int? OutputTokens = null);

/// <summary>
/// Calls a provider under the explicit timeout of the port and names whatever comes back.
/// </summary>
/// <remarks>
/// <para>
/// It shares <see cref="ProviderCall"/> with the antifraud exchange rather than repeating its
/// classification, because two versions of «what a timeout is» is precisely the defect worth
/// avoiding. What differs is the mapping afterwards, and it differs for a reason: an antifraud
/// timeout is indeterminate — the provider may have registered the evaluation — so it leaves the
/// row pending, while an explanation that did not arrive is simply an explanation that did not
/// arrive, and the row says so and can be retried.
/// </para>
/// <para>
/// <strong>The code is fixed here, from the outcome, and never taken from the provider</strong>
/// (decision 73). A provider says what it knows about its answer — drafted, refused, malformed,
/// unavailable — and this maps each to the one code that names it. The codes that belong to the
/// verifier, to the lifecycle or to the system cannot be reached from a provider at all.
/// </para>
/// <para>
/// A caller that goes away is named rather than propagated. The row was reserved before the call,
/// and leaving it pending because a browser closed is exactly the way this lifecycle would jam.
/// </para>
/// </remarks>
internal static class ExplanationExchange
{
    public static async Task<ExplanationAttempt> CallAsync(
        IExplanationProvider provider,
        ExplanationInput input,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var outcome = await ProviderCall.InvokeAsync(
            token => provider.ExplainAsync(input, token),
            timeout,
            cancellationToken);

        return outcome.Status switch
        {
            ProviderCallStatus.Completed when outcome.Value is { } result => Name(result),
            ProviderCallStatus.TimedOut => new(null, ExplanationFailureCode.ProviderTimeout),
            ProviderCallStatus.CallerCancelled => new(null, ExplanationFailureCode.Cancelled),
            _ => new(null, ExplanationFailureCode.ProviderUnavailable),
        };
    }

    private static ExplanationAttempt Name(ExplanationProviderResult result)
    {
        if (result is { Outcome: ExplanationProviderOutcome.Drafted, Draft: { } draft })
        {
            return new(draft, null, null, result.ProviderVersion, result.InputTokens, result.OutputTokens);
        }

        var code = result.Outcome switch
        {
            ExplanationProviderOutcome.Refused => ExplanationFailureCode.ProviderRefused,
            ExplanationProviderOutcome.Malformed => ExplanationFailureCode.MalformedOutput,
            _ => ExplanationFailureCode.ProviderUnavailable,
        };

        return new(
            null,
            code,
            result.Diagnostic,
            result.ProviderVersion,
            result.InputTokens,
            result.OutputTokens);
    }
}
