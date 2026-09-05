using Salvo.Application.Providers;
using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <param name="FailureCode">Null exactly when the provider produced a draft.</param>
internal sealed record ExplanationAttempt(ExplanationDraft? Draft, ExplanationFailureCode? FailureCode);

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
            ProviderCallStatus.Completed when outcome.Value!.Summary is null =>
                new(null, ExplanationFailureCode.ProviderRefused),
            ProviderCallStatus.Completed => new(outcome.Value, null),
            ProviderCallStatus.TimedOut => new(null, ExplanationFailureCode.ProviderTimeout),
            ProviderCallStatus.CallerCancelled => new(null, ExplanationFailureCode.Cancelled),
            _ => new(null, ExplanationFailureCode.ProviderUnavailable),
        };
    }
}
