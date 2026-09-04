using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The unit of work of a callback: the receipt and the evaluation it moves, written together.
/// </summary>
/// <remarks>
/// They share a port rather than living in two because they have to share a transaction. A receipt
/// that outlives a transition that failed is the exact shape of a lost verdict: the provider
/// redelivers, the key collides, the answer is <c>200</c>, and nothing ever applies the message.
/// </remarks>
public interface IExternalCallbackStore
{
    /// <summary>
    /// The evaluation a message belongs to: by provider identifier first, and by order reference
    /// when there is no identifier or no row carries it yet.
    /// </summary>
    /// <remarks>
    /// Correlating by reference can match more than one row, because an order accumulates
    /// evaluations over time. The one still waiting for an answer wins; otherwise the most recently
    /// requested. Tracked, because the caller goes on to move it.
    /// </remarks>
    Task<ExternalEvaluation?> CorrelateAsync(
        ExternalProvider provider,
        string? externalEvaluationId,
        string? referenceId,
        CancellationToken cancellationToken);

    /// <summary>The evaluation with this identifier, tracked.</summary>
    Task<ExternalEvaluation?> FindEvaluationAsync(Guid id, CancellationToken cancellationToken);

    Task<CallbackReceipt?> FindReceiptAsync(
        ExternalProvider provider,
        string deduplicationKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Receipts of this provider that never found their evaluation and now might, oldest first.
    /// </summary>
    Task<IReadOnlyList<CallbackReceipt>> ListUnmatchedAsync(
        ExternalProvider provider,
        string? externalEvaluationId,
        string referenceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every receipt that correlates with this evaluation, newest first. What the console reads to
    /// be able to say that the provider contradicted itself.
    /// </summary>
    Task<IReadOnlyList<CallbackReceipt>> ListForEvaluationAsync(
        ExternalEvaluation evaluation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes a new receipt and, in the <em>same</em> <c>SaveChangesAsync</c>, whatever the message
    /// changed on <paramref name="evaluation"/>.
    /// </summary>
    /// <returns>
    /// <see cref="CallbackWriteResult.Duplicate"/> when this exact message was already recorded —
    /// which is how a redelivery is recognised — and <see cref="CallbackWriteResult.Conflict"/> when
    /// another writer moved the evaluation first.
    /// </returns>
    Task<CallbackWriteResult> AddAsync(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to a receipt that already exists, together with the evaluation it moved.
    /// </summary>
    Task<CallbackWriteResult> SaveAsync(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Drops everything tracked, so that a retry reads the database again instead of resending the
    /// state that just lost.
    /// </summary>
    void Forget();
}

public enum CallbackWriteResult
{
    Written = 1,

    /// <summary>This exact message was already recorded. There is no second row, by design.</summary>
    Duplicate = 2,

    /// <summary>Another writer moved the evaluation first. Nothing was written.</summary>
    Conflict = 3,
}
