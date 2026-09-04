using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The one place a callback becomes an effect, whichever door it came in through.
/// </summary>
/// <remarks>
/// <para>
/// The authenticated endpoint and the demo trigger both end here, on purpose. Two paths that each
/// deduplicated and transitioned in their own way would be two chances to get monotonicity wrong,
/// and the demo would stop being evidence about the real path.
/// </para>
/// <para>
/// The receipt and the transition are written in a single unit of work. Everything else about this
/// class follows from that: the duplicate is a uniqueness violation rather than a prior read, and a
/// lost race is reverted whole and reclassified against what is now true.
/// </para>
/// </remarks>
public sealed class ApplyExternalCallbackHandler(
    IExternalCallbackStore store,
    ICallbackReceiptIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Records the message and applies whatever it changes.
    /// </summary>
    /// <exception cref="ExternalCallbackUnavailableException">
    /// The unit of work lost its race twice. The caller is asked to send the message again, and the
    /// retry converges because by then the evaluation is settled.
    /// </exception>
    public async Task<ExternalCallbackOutcome> HandleAsync(
        ExternalProvider provider,
        ExternalCallbackMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var attempt = await AttemptAsync(provider, message, cancellationToken);
        if (attempt is not null)
        {
            return attempt;
        }

        // Lost the race. The whole unit went back, so nothing of the first attempt survives; what
        // is read now includes whatever the other writer did, and the message is judged against
        // that instead of against a state that no longer exists.
        store.Forget();

        attempt = await AttemptAsync(provider, message, cancellationToken);

        return attempt ?? throw new ExternalCallbackUnavailableException(
            "The external evaluation this callback refers to is being written by another request. "
            + "Send the callback again.");
    }

    /// <summary>
    /// One pass. Returns <see langword="null"/> when the write lost its race and the caller should
    /// read again.
    /// </summary>
    private async Task<ExternalCallbackOutcome?> AttemptAsync(
        ExternalProvider provider,
        ExternalCallbackMessage message,
        CancellationToken cancellationToken)
    {
        var receivedAt = timeProvider.GetUtcNow();
        var evaluation = await store.CorrelateAsync(
            provider,
            message.ExternalEvaluationId,
            message.ReferenceId,
            cancellationToken);

        var classification = ExternalCallbackTransition.Classify(evaluation, message.Status);
        classification = ExternalCallbackTransition.Apply(evaluation, message, classification, receivedAt);

        var receipt = CallbackReceipt.Record(
            idGenerator.Create(),
            provider,
            message.ExternalEvaluationId,
            message.ReferenceId,
            message.Status,
            message.Score,
            message.ProviderInstant,
            classification,
            receivedAt);

        var write = await store.AddAsync(receipt, evaluation, cancellationToken);

        return write switch
        {
            CallbackWriteResult.Written => Describe(receipt, evaluation, isReplay: false),
            CallbackWriteResult.Duplicate => await ReplayAsync(provider, receipt, cancellationToken),
            _ => null,
        };
    }

    /// <summary>
    /// The message arrived again. Its effect happened the first time; all that is left is to record
    /// that the provider is still sending it.
    /// </summary>
    private async Task<ExternalCallbackOutcome> ReplayAsync(
        ExternalProvider provider,
        CallbackReceipt rejected,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindReceiptAsync(provider, rejected.DeduplicationKey, cancellationToken);
        if (existing is null)
        {
            // The row that collided was rolled back by its own writer between the violation and this
            // read. Reporting the message as recorded is still true — some writer recorded it — and
            // it keeps the provider from retrying forever over a race this system already survived.
            return Describe(rejected, evaluation: null, isReplay: true);
        }

        existing.RecordReplay(timeProvider.GetUtcNow());

        // Best effort: the replay count is evidence, not correctness, and a conflict on it must not
        // turn an already-recorded message into an error the provider will keep resending.
        _ = await store.SaveAsync(existing, evaluation: null, cancellationToken);

        var evaluation = await store.CorrelateAsync(
            provider,
            existing.ExternalEvaluationId,
            existing.ReferenceId,
            cancellationToken);

        return Describe(existing, evaluation, isReplay: true);
    }

    private static ExternalCallbackOutcome Describe(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        bool isReplay)
    {
        return new(
            CallbackReceiptWireNames.ToWire(receipt.Status),
            isReplay,
            receipt.ReplayCount,
            evaluation?.Id,
            evaluation is null ? null : ExternalEvaluationWireNames.ToWire(evaluation.Status));
    }
}
