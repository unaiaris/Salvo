using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The demo trigger: asks the provider what it would say and feeds that back in as a callback.
/// </summary>
/// <remarks>
/// <para>
/// It exists because the console cannot press the real button. Reaching the authenticated endpoint
/// from the browser would put the shared secret in the page, and reaching it from a server action
/// would put the secret in the Next process — which section 10 of the Blueprint forbids. Worse, if
/// the client composed the message, anyone with the console open could close any pending evaluation
/// in whatever state they liked: monotonicity protects a verdict that already exists, and a pending
/// row has no verdict to protect.
/// </para>
/// <para>
/// So the caller picks <em>which</em> evaluation and never <em>what</em> it says. The verdict comes
/// from the provider adapter, and the message goes through the same use case as a real callback:
/// same receipt, same deduplication, same transition table. What the demo exercises is the real
/// path, which is the only reason a demo is worth anything.
/// </para>
/// <para>
/// The provider's own instant is the moment the evaluation was requested, not the moment of
/// delivery. That makes redelivering the same evaluation produce the same deduplication key, so the
/// second press is recognised as the replay it is instead of quietly minting a new message.
/// </para>
/// </remarks>
public sealed class DeliverPendingCallbacksHandler(
    IExternalEvaluationStore store,
    IAntifraudProviderRegistry registry,
    ApplyExternalCallbackHandler callbacks,
    ExternalEvaluationOptions options,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Delivers the callback of one pending evaluation, or of every pending evaluation when
    /// <paramref name="externalEvaluationId"/> is <see langword="null"/>.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when a specific evaluation was named and no row carries that
    /// identifier.
    /// </returns>
    public async Task<CallbackDeliverySummary?> HandleAsync(
        Guid? externalEvaluationId,
        CancellationToken cancellationToken)
    {
        var targets = await ResolveTargetsAsync(externalEvaluationId, cancellationToken);
        if (targets is null)
        {
            return null;
        }

        var delivered = 0;
        var settled = 0;
        var replayed = 0;
        var unavailable = 0;

        foreach (var target in targets)
        {
            var adapter = registry.Find(target.Provider);
            if (adapter is null)
            {
                continue;
            }

            var lookup = new ExternalEvaluationLookup(target.ExternalEvaluationId, target.ReferenceId);
            var result = await ExternalProviderExchange.CallAsync(
                token => adapter.GetStatusAsync(lookup, token),
                options.RequestTimeout,
                cancellationToken);

            if (ToStatus(result.Outcome) is not { } reported)
            {
                // The provider still has nothing to say. There is no callback to deliver, and
                // inventing one would be exactly the manipulation this endpoint refuses.
                continue;
            }

            var message = new ExternalCallbackMessage(
                result.ExternalEvaluationId ?? target.ExternalEvaluationId,
                target.ReferenceId,
                reported,
                result.Score,
                target.RequestedAt);

            try
            {
                var outcome = await callbacks.HandleAsync(target.Provider, message, cancellationToken);
                delivered++;

                if (outcome.IsReplay)
                {
                    replayed++;
                }

                if (outcome.EvaluationStatus is not null
                    and not ExternalEvaluationWireNames.Pending)
                {
                    settled++;
                }
            }
            catch (ExternalCallbackUnavailableException)
            {
                unavailable++;
            }
        }

        return new(targets.Count, delivered, settled, replayed, unavailable);
    }

    private async Task<IReadOnlyList<ExternalEvaluation>?> ResolveTargetsAsync(
        Guid? externalEvaluationId,
        CancellationToken cancellationToken)
    {
        if (externalEvaluationId is not { } id)
        {
            return await store.ListPendingAsync(timeProvider.GetUtcNow(), cancellationToken);
        }

        var one = await store.FindAsync(id, cancellationToken);

        return one is null ? null : [one];
    }

    /// <summary>
    /// What the provider said, as the status a callback would report. A pending or transient answer
    /// is not a callback at all.
    /// </summary>
    private static ExternalEvaluationStatus? ToStatus(ExternalProviderOutcome outcome)
    {
        return outcome switch
        {
            ExternalProviderOutcome.Approved => ExternalEvaluationStatus.Approved,
            ExternalProviderOutcome.Denied => ExternalEvaluationStatus.Denied,
            ExternalProviderOutcome.Rejected or ExternalProviderOutcome.Unreachable =>
                ExternalEvaluationStatus.Error,
            _ => null,
        };
    }
}
