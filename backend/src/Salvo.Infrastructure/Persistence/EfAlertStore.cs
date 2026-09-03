using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfAlertStore(SalvoDbContext dbContext) : IAlertStore
{
    private const int ConstraintUnique = 2067;
    private const int ConstraintPrimaryKey = 1555;

    public async Task<AlertPage> GetPageAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        AlertSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Alerts.AsNoTracking();
        if (status is not null)
        {
            var requested = status.Value;
            query = query.Where(alert => alert.Status == requested);
        }

        if (severity is not null)
        {
            query = ApplySeverityFilter(query, severity.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var lastRun = await GetLastRunAsync(cancellationToken);
        var alerts = await OrderAndPage(query, sort, lastRun?.Id, page, pageSize)
            .ToListAsync(cancellationToken);

        return new(
            await ComposeAsync(alerts, lastRun, cancellationToken),
            totalCount,
            ToReference(lastRun));
    }

    /// <summary>
    /// Applies the requested order and takes one page of it.
    /// </summary>
    /// <remarks>
    /// The join that resolves the current score is part of this query, ahead of
    /// <c>Skip</c>/<c>Take</c>. Composing it afterwards would order each page by a score the
    /// database never saw, which is to say it would not order the feed at all.
    /// <para>
    /// An alert whose order the current run did not cover has no current score. In SQLite a null
    /// sorts below every value, so those alerts land at the end of a descending order, which is
    /// where an alert nobody can compare belongs.
    /// </para>
    /// </remarks>
    private IQueryable<Alert> OrderAndPage(
        IQueryable<Alert> query,
        AlertSortOrder sort,
        Guid? runId,
        int page,
        int pageSize)
    {
        var skip = (page - 1) * pageSize;

        // Timestamps are stored as fixed-width ISO 8601 in UTC, so ordering them as text is
        // ordering them chronologically. The identifier breaks ties and keeps paging stable.
        if (sort != AlertSortOrder.LocalScoreDesc || runId is not { } id)
        {
            return query
                .OrderByDescending(alert => alert.CreatedAt)
                .ThenBy(alert => alert.Id)
                .Skip(skip)
                .Take(pageSize);
        }

        var currentScores = from link in dbContext.RunEvaluations.AsNoTracking()
                            where link.RunId == id
                            join evaluation in dbContext.RiskEvaluations.AsNoTracking()
                                on link.EvaluationId equals evaluation.Id
                            select new { link.OrderId, evaluation.Score };

        var ordered = from alert in query
                      join current in currentScores on alert.OrderId equals current.OrderId into matches
                      from current in matches.DefaultIfEmpty()
                      orderby (current == null ? null : current.Score) descending,
                          alert.CreatedAt descending,
                          alert.Id
                      select alert;

        return ordered.Skip(skip).Take(pageSize);
    }

    public async Task<AlertContext?> FindAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == alertId, cancellationToken);

        return alert is null
            ? null
            : (await ComposeAsync(
                [alert],
                await GetLastRunAsync(cancellationToken),
                cancellationToken))[0];
    }

    public async Task<AlertContext?> FindForReviewAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts
            .FirstOrDefaultAsync(candidate => candidate.Id == alertId, cancellationToken);

        return alert is null
            ? null
            : (await ComposeAsync(
                [alert],
                await GetLastRunAsync(cancellationToken),
                cancellationToken))[0];
    }

    public async Task SaveReviewAsync(Alert alert, AlertReview review, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(review);

        dbContext.AlertReviews.Add(review);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new AlertReviewConflictException(
                AlertReviewConflictReason.ConcurrentReview,
                $"Alert {alert.Id} was reviewed by a concurrent request.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            throw new AlertReviewConflictException(
                AlertReviewConflictReason.ConcurrentReview,
                $"Alert {alert.Id} already carries an audit record.",
                exception);
        }
    }

    /// <summary>
    /// Narrows a query to a severity band. Severity is derived, not stored, so the filter is
    /// expressed as the score range each known policy assigns to that severity.
    /// </summary>
    private static IQueryable<Alert> ApplySeverityFilter(IQueryable<Alert> query, AlertSeverity severity)
    {
        IQueryable<Alert>? filtered = null;

        foreach (var policy in AlertPolicy.All)
        {
            if (policy.BandFor(severity) is not { } band)
            {
                continue;
            }

            var version = policy.Version;
            var minimumScore = band.MinimumScore;
            var maximumScore = band.MaximumScore;
            var matches = query.Where(alert =>
                alert.AlertPolicyVersion == version
                && alert.RiskScoreSnapshot >= minimumScore
                && alert.RiskScoreSnapshot <= maximumScore);

            filtered = filtered is null ? matches : filtered.Union(matches);
        }

        return filtered ?? query.Where(alert => false);
    }

    private async Task<AlertContext[]> ComposeAsync(
        List<Alert> alerts,
        ScoringRun? lastRun,
        CancellationToken cancellationToken)
    {
        if (alerts.Count == 0)
        {
            return [];
        }

        var orderIds = alerts.Select(alert => alert.OrderId).Distinct().ToArray();
        var alertIds = alerts.Select(alert => alert.Id).ToArray();

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => orderIds.Contains(order.Id))
            .ToDictionaryAsync(order => order.Id, cancellationToken);
        var currentEvaluations = await GetCurrentEvaluationsAsync(orderIds, lastRun, cancellationToken);
        var reviews = await dbContext.AlertReviews
            .AsNoTracking()
            .Where(review => alertIds.Contains(review.AlertId))
            .ToDictionaryAsync(review => review.AlertId, cancellationToken);

        return alerts
            .Select(alert => new AlertContext(
                alert,
                orders[alert.OrderId],
                currentEvaluations.GetValueOrDefault(alert.OrderId),
                reviews.GetValueOrDefault(alert.Id),
                ToReference(lastRun)))
            .ToArray();
    }

    /// <summary>
    /// The run that defines the current state of every order, or <see langword="null"/> when the
    /// corpus was never scored.
    /// </summary>
    private async Task<ScoringRun?> GetLastRunAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ScoringRuns
            .AsNoTracking()
            .OrderByDescending(run => run.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The evaluation <paramref name="lastRun"/> referenced for each of the given orders. Reading
    /// it through the run rather than by insertion time is what keeps a score that bounced back to
    /// an earlier value from resolving to a stale row.
    /// </summary>
    private async Task<Dictionary<Guid, RiskEvaluation>> GetCurrentEvaluationsAsync(
        IReadOnlyCollection<Guid> orderIds,
        ScoringRun? lastRun,
        CancellationToken cancellationToken)
    {
        if (lastRun is null)
        {
            return [];
        }

        var runId = lastRun.Id;
        var current = await dbContext.RunEvaluations
            .AsNoTracking()
            .Where(link => link.RunId == runId && orderIds.Contains(link.OrderId))
            .Join(
                dbContext.RiskEvaluations.AsNoTracking(),
                link => link.EvaluationId,
                evaluation => evaluation.Id,
                (link, evaluation) => new { link.OrderId, Evaluation = evaluation })
            .ToListAsync(cancellationToken);

        return current.ToDictionary(entry => entry.OrderId, entry => entry.Evaluation);
    }

    private static ScoringRunReference? ToReference(ScoringRun? run)
    {
        return run is null ? null : new(run.Sequence, run.CompletedAt);
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey;
    }
}
