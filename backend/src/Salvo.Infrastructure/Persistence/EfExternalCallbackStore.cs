using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfExternalCallbackStore(SalvoDbContext dbContext) : IExternalCallbackStore
{
    private const int ConstraintUnique = 2067;
    private const int ConstraintPrimaryKey = 1555;

    public async Task<ExternalEvaluation?> CorrelateAsync(
        ExternalProvider provider,
        string? externalEvaluationId,
        string? referenceId,
        CancellationToken cancellationToken)
    {
        var identifier = Normalize(externalEvaluationId);
        if (identifier is not null)
        {
            var byIdentifier = await dbContext.ExternalEvaluations
                .FirstOrDefaultAsync(
                    evaluation => evaluation.Provider == provider
                        && evaluation.ExternalEvaluationId == identifier,
                    cancellationToken);

            if (byIdentifier is not null)
            {
                return byIdentifier;
            }
        }

        var reference = Normalize(referenceId);
        if (reference is null)
        {
            return null;
        }

        // The pending one speaks for the order right now; failing that, the most recent. An order
        // accumulates evaluations over time, so a reference alone does not name a single row.
        return await dbContext.ExternalEvaluations
            .Where(evaluation => evaluation.Provider == provider && evaluation.ReferenceId == reference)
            .OrderBy(evaluation => evaluation.Status == ExternalEvaluationStatus.Pending ? 0 : 1)
            .ThenByDescending(evaluation => evaluation.RequestedAt)
            .ThenByDescending(evaluation => evaluation.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExternalEvaluation?> FindEvaluationAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ExternalEvaluations
            .FirstOrDefaultAsync(evaluation => evaluation.Id == id, cancellationToken);
    }

    public async Task<CallbackReceipt?> FindReceiptAsync(
        ExternalProvider provider,
        string deduplicationKey,
        CancellationToken cancellationToken)
    {
        return await dbContext.CallbackReceipts
            .FirstOrDefaultAsync(
                receipt => receipt.Provider == provider && receipt.DeduplicationKey == deduplicationKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CallbackReceipt>> ListUnmatchedAsync(
        ExternalProvider provider,
        string? externalEvaluationId,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var identifier = Normalize(externalEvaluationId);

        return await dbContext.CallbackReceipts
            .Where(receipt =>
                receipt.Provider == provider
                && receipt.Status == CallbackReceiptStatus.Unmatched
                && ((identifier != null && receipt.ExternalEvaluationId == identifier)
                    || receipt.ReferenceId == referenceId))
            .OrderBy(receipt => receipt.ReceivedAt)
            .ThenBy(receipt => receipt.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The receipts of one evaluation. Matched by provider identifier once the evaluation has one,
    /// and by order reference until then — the same two halves the correlation uses, so a receipt is
    /// read back by whatever made it findable in the first place.
    /// </summary>
    public async Task<IReadOnlyList<CallbackReceipt>> ListForEvaluationAsync(
        ExternalEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        var provider = evaluation.Provider;
        var identifier = evaluation.ExternalEvaluationId;
        var reference = evaluation.ReferenceId;

        return await dbContext.CallbackReceipts
            .AsNoTracking()
            .Where(receipt =>
                receipt.Provider == provider
                && (identifier == null
                    ? receipt.ReferenceId == reference
                    : receipt.ExternalEvaluationId == identifier))
            .OrderByDescending(receipt => receipt.ReceivedAt)
            .ThenByDescending(receipt => receipt.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CallbackWriteResult> AddAsync(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        dbContext.CallbackReceipts.Add(receipt);

        return await CommitAsync(receipt, evaluation, cancellationToken);
    }

    public Task<CallbackWriteResult> SaveAsync(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        return CommitAsync(receipt, evaluation, cancellationToken);
    }

    /// <summary>
    /// Detaches everything this context is tracking.
    /// </summary>
    /// <remarks>
    /// Called after a write lost its race. The entities in memory carry changes that the database
    /// refused, and leaving them attached would make the next save resend exactly the state that
    /// just failed.
    /// </remarks>
    public void Forget()
    {
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// The single <c>SaveChangesAsync</c> the whole design rests on: the receipt and the transition
    /// are one write or neither.
    /// </summary>
    private async Task<CallbackWriteResult> CommitAsync(
        CallbackReceipt receipt,
        ExternalEvaluation? evaluation,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            return CallbackWriteResult.Written;
        }
        catch (DbUpdateConcurrencyException)
        {
            Detach(receipt, evaluation);

            return CallbackWriteResult.Conflict;
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            // The deduplication key collided, which is precisely how a redelivery is recognised.
            // Nothing was written, transition included — which is the point of writing them together.
            Detach(receipt, evaluation);

            return CallbackWriteResult.Duplicate;
        }
    }

    private void Detach(CallbackReceipt receipt, ExternalEvaluation? evaluation)
    {
        dbContext.Entry(receipt).State = EntityState.Detached;

        if (evaluation is not null)
        {
            dbContext.Entry(evaluation).State = EntityState.Detached;
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey;
    }
}
