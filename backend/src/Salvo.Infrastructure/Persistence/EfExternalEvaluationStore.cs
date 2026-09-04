using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfExternalEvaluationStore(SalvoDbContext dbContext) : IExternalEvaluationStore
{
    private const int ConstraintUnique = 2067;
    private const int ConstraintPrimaryKey = 1555;

    public async Task<Order?> FindOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    /// <summary>
    /// The pending evaluation if there is one — the partial unique index guarantees at most one —
    /// and otherwise the most recently requested. Tracked, because the caller may go on to move it.
    /// </summary>
    public async Task<ExternalEvaluation?> FindCurrentAsync(
        Guid orderId,
        ExternalProvider provider,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExternalEvaluations
            .Where(evaluation => evaluation.OrderId == orderId && evaluation.Provider == provider)
            .OrderBy(evaluation => evaluation.Status == ExternalEvaluationStatus.Pending ? 0 : 1)
            .ThenByDescending(evaluation => evaluation.RequestedAt)
            .ThenByDescending(evaluation => evaluation.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExternalEvaluation?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ExternalEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(evaluation => evaluation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalEvaluation>> ListByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExternalEvaluations
            .AsNoTracking()
            .Where(evaluation => evaluation.OrderId == orderId)
            .OrderBy(evaluation => evaluation.RequestedAt)
            .ThenBy(evaluation => evaluation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalEvaluation>> ListPendingAsync(
        DateTimeOffset requestedBefore,
        CancellationToken cancellationToken)
    {
        // Tracked on purpose: the sweep moves these rows and saves them one at a time.
        return await dbContext.ExternalEvaluations
            .Where(evaluation =>
                evaluation.Status == ExternalEvaluationStatus.Pending
                && evaluation.RequestedAt <= requestedBefore)
            .OrderBy(evaluation => evaluation.RequestedAt)
            .ThenBy(evaluation => evaluation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListOrdersWithoutEvaluationAsync(
        ExternalProvider provider,
        int limit,
        CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => !dbContext.ExternalEvaluations
                .Any(evaluation => evaluation.OrderId == order.Id && evaluation.Provider == provider))
            .OrderBy(order => order.OccurredAt)
            .ThenBy(order => order.Id)
            .Select(order => order.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task ReserveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        dbContext.ExternalEvaluations.Add(evaluation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            Detach(evaluation);

            throw new ExternalEvaluationConflictException(
                ExternalEvaluationConflictReason.PendingEvaluationExists,
                $"Order {evaluation.OrderId} already has an external evaluation waiting for the "
                + "provider.",
                exception);
        }
    }

    public async Task SaveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Detached before rethrowing: a row that lost its race must not ride along on the next
            // save of the sweep and fail it too.
            Detach(evaluation);

            throw new ExternalEvaluationConflictException(
                ExternalEvaluationConflictReason.ConcurrentUpdate,
                $"External evaluation {evaluation.Id} was moved by a concurrent writer.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            Detach(evaluation);

            throw new ExternalEvaluationConflictException(
                ExternalEvaluationConflictReason.ConcurrentUpdate,
                $"External evaluation {evaluation.Id} collided with another row of the same provider.",
                exception);
        }
    }

    private void Detach(ExternalEvaluation evaluation)
    {
        dbContext.Entry(evaluation).State = EntityState.Detached;
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey;
    }
}
