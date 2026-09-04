using Salvo.Domain.External;
using Salvo.Domain.Orders;

namespace Salvo.Application.External;

public interface IExternalEvaluationStore
{
    Task<Order?> FindOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// The evaluation that speaks for this order and provider right now: the one still waiting for
    /// an answer if there is one, otherwise the most recently requested.
    /// </summary>
    Task<ExternalEvaluation?> FindCurrentAsync(
        Guid orderId,
        ExternalProvider provider,
        CancellationToken cancellationToken);

    Task<ExternalEvaluation?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Every external evaluation of an order, oldest first. The history is the record of what was
    /// asked and when.
    /// </summary>
    Task<IReadOnlyList<ExternalEvaluation>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Pending evaluations requested no later than <paramref name="requestedBefore"/>, oldest first.
    /// </summary>
    Task<IReadOnlyList<ExternalEvaluation>> ListPendingAsync(
        DateTimeOffset requestedBefore,
        CancellationToken cancellationToken);

    /// <summary>
    /// Orders this provider has never been asked about, oldest first, capped at
    /// <paramref name="limit"/>.
    /// </summary>
    /// <remarks>
    /// "Never asked about" and not "with no current evaluation": an order whose evaluation ended in
    /// ERROR is deliberately left out, because asking again after a failure is a decision somebody
    /// makes per order, not something a sweep does on its own.
    /// </remarks>
    Task<IReadOnlyList<Guid>> ListOrdersWithoutEvaluationAsync(
        ExternalProvider provider,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the reservation. This runs <em>before</em> the provider is called, so that the
    /// partial unique index decides which of two concurrent requests proceeds while nothing exists
    /// on the provider side yet.
    /// </summary>
    /// <exception cref="ExternalEvaluationConflictException">
    /// A concurrent request already reserved the pending evaluation of this order.
    /// </exception>
    Task ReserveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken);

    /// <summary>
    /// Persists one evaluation, on its own. Reconciliation saves row by row on purpose: a conflict
    /// on one evaluation must not roll back the ones the sweep already resolved.
    /// </summary>
    /// <exception cref="ExternalEvaluationConflictException">
    /// Another writer moved the row after it was read. The instance is detached, so a failed write
    /// cannot ride along on the next one.
    /// </exception>
    Task SaveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken);
}
