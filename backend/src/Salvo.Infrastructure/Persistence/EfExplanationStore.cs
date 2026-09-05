using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfExplanationStore(SalvoDbContext dbContext) : IExplanationStore
{
    private const int ConstraintUnique = 2067;
    private const int ConstraintPrimaryKey = 1555;

    /// <summary>
    /// The alert, what its snapshot froze, and what is current for its order now.
    /// </summary>
    /// <remarks>
    /// The projection is deliberately narrow. The city, the buyer, merchant and device references
    /// and the review note are not read at all, so no later step can pass along what it never
    /// received — a structural line of defence underneath the one the input type draws. Nothing
    /// here touches the ground-truth labels either.
    /// </remarks>
    public async Task<ExplanationTarget?> FindTargetAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var snapshot = await (
            from alert in dbContext.Alerts.AsNoTracking()
            where alert.Id == alertId
            join evaluation in dbContext.RiskEvaluations.AsNoTracking()
                on alert.RiskEvaluationId equals evaluation.Id
            join order in dbContext.Orders.AsNoTracking()
                on alert.OrderId equals order.Id
            where evaluation.Score != null
                && evaluation.SignalsJson != null
                && evaluation.RuleConfigVersion != null
            select new
            {
                AlertId = alert.Id,
                alert.RiskEvaluationId,
                alert.AlertPolicyVersion,
                alert.OrderId,
                evaluation.Score,
                evaluation.RuleConfigVersion,
                evaluation.SignalsJson,
                order.AmountCents,
                order.CurrencyCode,
                order.CountryCode,
                order.Channel,
                order.OccurredAt,
            }).FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return new(
            snapshot.AlertId,
            snapshot.RiskEvaluationId,
            await FindCurrentEvaluationIdAsync(snapshot.OrderId, cancellationToken),
            snapshot.AlertPolicyVersion,

            // The query filtered these out when null, which a local evaluation never is: the
            // database refuses a LOCAL row without a score, its signals and its configuration.
            snapshot.Score!.Value,
            snapshot.RuleConfigVersion!,
            snapshot.SignalsJson!,
            snapshot.AmountCents,
            snapshot.CurrencyCode,
            snapshot.CountryCode,
            snapshot.Channel,
            snapshot.OccurredAt);
    }

    public async Task<AlertExplanation?> FindAsync(
        Guid riskEvaluationId,
        ExplanationProvider provider,
        string templateVersion,
        string alertPolicyVersion,
        CancellationToken cancellationToken)
    {
        // Tracked: the caller goes on to move this row.
        return await dbContext.AlertExplanations.FirstOrDefaultAsync(
            explanation => explanation.RiskEvaluationId == riskEvaluationId
                && explanation.Provider == provider
                && explanation.TemplateVersion == templateVersion
                && explanation.AlertPolicyVersion == alertPolicyVersion,
            cancellationToken);
    }

    public async Task ReserveAsync(AlertExplanation explanation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        dbContext.AlertExplanations.Add(explanation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            Detach(explanation);

            throw new ExplanationConflictException(
                ExplanationConflictReason.PendingInFlight,
                $"Evaluation {explanation.RiskEvaluationId} is already being explained.",
                exception);
        }
    }

    public async Task SaveAsync(AlertExplanation explanation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Detached before rethrowing, so a row that lost its race cannot ride along on a later
            // save and fail that one too.
            Detach(explanation);

            throw new ExplanationConflictException(
                ExplanationConflictReason.ConcurrentUpdate,
                $"Explanation {explanation.Id} was moved by a concurrent writer.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            Detach(explanation);

            throw new ExplanationConflictException(
                ExplanationConflictReason.PendingInFlight,
                $"Evaluation {explanation.RiskEvaluationId} is already being explained.",
                exception);
        }
    }

    /// <summary>
    /// The evaluation the latest run made current for the order, read through the run rather than
    /// by insertion time, exactly as every other current-state read in this system does.
    /// </summary>
    private async Task<Guid?> FindCurrentEvaluationIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var lastRunId = await dbContext.ScoringRuns
            .AsNoTracking()
            .OrderByDescending(run => run.Sequence)
            .Select(run => (Guid?)run.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastRunId is not { } runId)
        {
            return null;
        }

        return await dbContext.RunEvaluations
            .AsNoTracking()
            .Where(link => link.RunId == runId && link.OrderId == orderId)
            .Select(link => (Guid?)link.EvaluationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private void Detach(AlertExplanation explanation)
    {
        dbContext.Entry(explanation).State = EntityState.Detached;
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey;
    }
}
