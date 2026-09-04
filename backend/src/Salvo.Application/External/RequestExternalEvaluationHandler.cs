using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// Asks an external provider to evaluate an order, in two phases.
/// </summary>
/// <remarks>
/// <para>
/// The row is reserved and committed <em>before</em> the provider is called. That ordering is the
/// whole point, and it buys two things. Two concurrent requests for the same order collide on the
/// partial unique index while nothing exists on the provider side, so the one that loses costs
/// nothing — instead of both calling the provider and creating two evaluations there. And a result
/// that arrives before this request finishes has a row to correlate against, by reference, instead
/// of falling on the floor.
/// </para>
/// <para>
/// A request that finds an evaluation already in place changes nothing and says so, which is what
/// makes a double click harmless.
/// </para>
/// <para>
/// It ends by looking back at the callbacks that could not be correlated. That is the other half of
/// the fix: the reservation gives a message something to correlate <em>by reference</em>, and late
/// linking catches the message that names only the identifier this request was in the middle of
/// learning.
/// </para>
/// </remarks>
public sealed class RequestExternalEvaluationHandler(
    IExternalEvaluationStore store,
    IAntifraudProviderRegistry registry,
    IExternalEvaluationIdGenerator idGenerator,
    LinkUnmatchedCallbacksHandler linker,
    ExternalEvaluationOptions options,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Requests an evaluation, or returns <see langword="null"/> when no order carries that
    /// identifier.
    /// </summary>
    /// <param name="requestNew">
    /// Ask for a new evaluation even though the order already has one. Allowed only when the
    /// current evaluation failed.
    /// </param>
    /// <exception cref="ExternalProviderNotRegisteredException">
    /// This deployment has no adapter for <paramref name="provider"/>.
    /// </exception>
    /// <exception cref="ExternalEvaluationConflictException">
    /// A new evaluation was asked for while one is pending or already carries a verdict, or a
    /// concurrent request reserved the pending evaluation first.
    /// </exception>
    public async Task<RequestExternalEvaluationResult?> HandleAsync(
        Guid orderId,
        ExternalProvider provider,
        bool requestNew,
        CancellationToken cancellationToken)
    {
        var order = await store.FindOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var adapter = registry.Find(provider) ?? throw new ExternalProviderNotRegisteredException(provider);
        var current = await store.FindCurrentAsync(orderId, provider, cancellationToken);
        if (current is not null)
        {
            if (!requestNew)
            {
                return new(false, ExternalEvaluationProjection.ToView(current));
            }

            Refuse(current);
        }

        var requestedAt = timeProvider.GetUtcNow();
        var evaluation = ExternalEvaluation.Reserve(
            idGenerator.Create(),
            order.Id,
            provider,
            ExternalEvaluationReference.From(order.Reference),
            requestedAt);

        // Phase one. Nothing has been asked of the provider yet, so losing here is free.
        await store.ReserveAsync(evaluation, cancellationToken);

        // Phase two. Whatever happens from here on, the row exists and can be found again.
        var result = await ExternalProviderExchange.CallAsync(
            token => adapter.EvaluateAsync(ExternalProviderExchange.ToInput(evaluation, order), token),
            options.RequestTimeout,
            cancellationToken);

        ExternalProviderExchange.Apply(evaluation, result, ExternalSettlementSource.Sync, timeProvider.GetUtcNow());

        try
        {
            await store.SaveAsync(evaluation, cancellationToken);
        }
        catch (ExternalEvaluationConflictException)
        {
            // Something else settled the row between the two phases. It knew more than this
            // request does, so its version stands and this one reports what is now true.
            var settled = await store.FindAsync(evaluation.Id, cancellationToken);
            if (settled is null)
            {
                throw;
            }

            return new(true, ExternalEvaluationProjection.ToView(settled));
        }

        // The row now carries whatever identifier the provider gave, so a callback that arrived
        // while this request was in flight can finally be attached to it.
        await linker.HandleAsync(evaluation.Id, cancellationToken);

        var linked = await store.FindAsync(evaluation.Id, cancellationToken);

        return new(true, ExternalEvaluationProjection.ToView(linked ?? evaluation));
    }

    private static void Refuse(ExternalEvaluation current)
    {
        if (!current.IsSettled)
        {
            throw new ExternalEvaluationConflictException(
                ExternalEvaluationConflictReason.PendingEvaluationExists,
                $"Order {current.OrderId} already has an external evaluation waiting for the "
                + "provider. Reconcile it or wait for its callback before asking again.");
        }

        if (current.Status != ExternalEvaluationStatus.Error)
        {
            throw new ExternalEvaluationConflictException(
                ExternalEvaluationConflictReason.AlreadySettled,
                $"Order {current.OrderId} was already evaluated as "
                + $"'{ExternalEvaluationWireNames.ToWire(current.Status)}'. Only an evaluation that "
                + "failed may be replaced by a new request.");
        }
    }
}
