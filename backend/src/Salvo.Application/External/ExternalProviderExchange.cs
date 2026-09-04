using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The one place a provider is actually called, and the one place its answer is turned into a
/// transition. Both the request and the reconciliation go through it, so the failure taxonomy
/// cannot drift between them.
/// </summary>
internal static class ExternalProviderExchange
{
    /// <summary>
    /// Calls <paramref name="call"/> under an explicit timeout and classifies whatever comes back,
    /// including what it throws.
    /// </summary>
    /// <remarks>
    /// An exception that is not a cancellation of the caller is classified as
    /// <see cref="ExternalProviderOutcome.Transient"/>, never as a settling failure. Once the
    /// request may have left the process, the safe reading of silence is "unknown", not "failed":
    /// settling here would discard a verdict the provider is about to send and make the next
    /// request duplicate the evaluation.
    /// </remarks>
    public static async Task<ExternalEvaluationResult> CallAsync(
        Func<CancellationToken, Task<ExternalEvaluationResult>> call,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            return await call(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(ExternalProviderOutcome.Transient, ErrorCode: ExternalEvaluationErrorCode.Timeout);
        }
        catch (TimeoutException)
        {
            return new(ExternalProviderOutcome.Transient, ErrorCode: ExternalEvaluationErrorCode.Timeout);
        }
#pragma warning disable CA1031 // A provider that fails in an unforeseen way must not take the request down with it.
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new(ExternalProviderOutcome.Transient, ErrorCode: ExternalEvaluationErrorCode.ProviderError);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Applies a classified answer to the row, according to the transition table.
    /// </summary>
    /// <returns>Whether the evaluation is settled afterwards.</returns>
    public static bool Apply(
        ExternalEvaluation evaluation,
        ExternalEvaluationResult result,
        ExternalSettlementSource source,
        DateTimeOffset at)
    {
        switch (result.Outcome)
        {
            case ExternalProviderOutcome.Approved:
                evaluation.Settle(
                    ExternalEvaluationStatus.Approved,
                    result.ExternalEvaluationId,
                    result.Score,
                    errorCode: null,
                    source,
                    at);
                return true;

            case ExternalProviderOutcome.Denied:
                evaluation.Settle(
                    ExternalEvaluationStatus.Denied,
                    result.ExternalEvaluationId,
                    result.Score,
                    errorCode: null,
                    source,
                    at);
                return true;

            // The two failures that are known to be final. The error code is fixed by the outcome
            // rather than taken from the result, so an adapter cannot report "never sent" while
            // naming a timeout.
            case ExternalProviderOutcome.Unreachable:
                evaluation.Settle(
                    ExternalEvaluationStatus.Error,
                    result.ExternalEvaluationId,
                    score: null,
                    ExternalEvaluationErrorCode.Unreachable,
                    source,
                    at);
                return true;

            case ExternalProviderOutcome.Rejected:
                evaluation.Settle(
                    ExternalEvaluationStatus.Error,
                    result.ExternalEvaluationId,
                    score: null,
                    ExternalEvaluationErrorCode.ProviderRejected,
                    source,
                    at);
                return true;

            case ExternalProviderOutcome.Pending:
                evaluation.RecordPendingAttempt(result.ExternalEvaluationId, lastErrorCode: null, source, at);
                return false;

            case ExternalProviderOutcome.Transient:
                evaluation.RecordPendingAttempt(
                    result.ExternalEvaluationId,
                    result.ErrorCode ?? ExternalEvaluationErrorCode.ProviderError,
                    source,
                    at);
                return false;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(result),
                    result.Outcome,
                    "Unsupported provider outcome.");
        }
    }

    public static ExternalEvaluationInput ToInput(ExternalEvaluation evaluation, Domain.Orders.Order order)
    {
        return new(
            order.Id,
            evaluation.ReferenceId,
            order.MerchantId,
            order.MerchantReferenceId,
            order.BuyerReferenceId,
            order.OccurredAt,
            order.AmountCents,
            order.CurrencyCode,
            order.CountryCode);
    }
}
