using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// Applies the callbacks that arrived before there was anything to apply them to.
/// </summary>
/// <remarks>
/// <para>
/// Reserving the row before calling the provider closes most of the window, but not the part where
/// the provider answers by callback, identifying the evaluation only by the identifier it is in the
/// middle of telling us about. That message correlates with nothing, is recorded as unmatched, and —
/// without this — is never looked at again: the evaluation stays pending forever while its verdict
/// sits in the receipts table.
/// </para>
/// <para>
/// So every path that learns something new about an evaluation looks back: the end of a request, and
/// every row of a reconciliation sweep. The receipts are applied oldest first, one unit of work
/// each, under the same transition table as a live callback — which is what keeps a stale unmatched
/// message from overwriting a verdict that arrived in the meantime.
/// </para>
/// </remarks>
public sealed class LinkUnmatchedCallbacksHandler(
    IExternalCallbackStore store,
    TimeProvider timeProvider)
{
    public async Task<CallbackLinkSummary> HandleAsync(
        Guid evaluationId,
        CancellationToken cancellationToken)
    {
        var evaluation = await store.FindEvaluationAsync(evaluationId, cancellationToken);
        if (evaluation is null)
        {
            return new(0, 0);
        }

        var receipts = await store.ListUnmatchedAsync(
            evaluation.Provider,
            evaluation.ExternalEvaluationId,
            evaluation.ReferenceId,
            cancellationToken);

        var linked = 0;
        foreach (var receipt in receipts)
        {
            var message = new ExternalCallbackMessage(
                receipt.ExternalEvaluationId,
                receipt.ReferenceId,
                receipt.ReportedStatus,
                receipt.ReportedScore,
                receipt.ProviderInstant);

            var at = timeProvider.GetUtcNow();
            var classification = ExternalCallbackTransition.Classify(evaluation, message.Status);
            classification = ExternalCallbackTransition.Apply(evaluation, message, classification, at);

            // Classification never returns unmatched here: the evaluation was found. The guard is
            // for the impossible case, where resolving would throw instead of skipping.
            if (classification == CallbackReceiptStatus.Unmatched)
            {
                continue;
            }

            receipt.Resolve(classification, at);

            // One receipt per unit of work, like the reconciliation sweep: a conflict on the third
            // must not undo the two that were already applied.
            if (await store.SaveAsync(receipt, evaluation, cancellationToken) != CallbackWriteResult.Written)
            {
                store.Forget();
                break;
            }

            linked++;
        }

        return new(receipts.Count, linked);
    }
}
