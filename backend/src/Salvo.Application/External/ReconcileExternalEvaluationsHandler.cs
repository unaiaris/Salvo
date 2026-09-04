using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// Asks the provider again about every evaluation still waiting for an answer.
/// </summary>
/// <remarks>
/// <para>
/// This is what closes the pending rows that nothing else can: the one whose request timed out and
/// whose identifier never arrived, and the one whose callback will never come. It is deliberately
/// explicit and manual, like the scoring run — there are no background jobs and no automatic
/// retries in this system.
/// </para>
/// <para>
/// The unit of work is one row. A conflict on one evaluation must not roll back the ones the sweep
/// already resolved, so each is saved on its own and a loser is counted and skipped.
/// </para>
/// </remarks>
public sealed class ReconcileExternalEvaluationsHandler(
    IExternalEvaluationStore store,
    IAntifraudProviderRegistry registry,
    ExternalEvaluationOptions options,
    TimeProvider timeProvider)
{
    public async Task<ReconciliationSummary> HandleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var pending = await store.ListPendingAsync(now - options.ReconciliationMinimumAge, cancellationToken);

        var settled = 0;
        var stillPending = 0;
        var failed = 0;
        var conflicted = 0;

        foreach (var evaluation in pending)
        {
            var adapter = registry.Find(evaluation.Provider);
            if (adapter is null)
            {
                // A row of a provider this deployment no longer registers. Nothing can be learned
                // about it here, and inventing a verdict would be worse than leaving it pending.
                stillPending++;
                continue;
            }

            var lookup = new ExternalEvaluationLookup(evaluation.ExternalEvaluationId, evaluation.ReferenceId);
            var result = await ExternalProviderExchange.CallAsync(
                token => adapter.GetStatusAsync(lookup, token),
                options.RequestTimeout,
                cancellationToken);

            var isSettled = ExternalProviderExchange.Apply(
                evaluation,
                result,
                ExternalSettlementSource.Reconciliation,
                timeProvider.GetUtcNow());

            try
            {
                await store.SaveAsync(evaluation, cancellationToken);
            }
            catch (ExternalEvaluationConflictException)
            {
                conflicted++;
                continue;
            }

            if (isSettled)
            {
                settled++;
            }
            else
            {
                stillPending++;
            }

            if (result.Outcome == ExternalProviderOutcome.Transient)
            {
                failed++;
            }
        }

        return new(pending.Count, settled, stillPending, failed, conflicted, now);
    }
}
