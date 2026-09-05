using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
using Salvo.Domain.Explanations;
using Salvo.Domain.External;
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
            await ComposeAsync(alerts, lastRun, withDetail: false, cancellationToken),
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
                withDetail: true,
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
                withDetail: true,
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

    /// <summary>
    /// Assembles the contexts a caller needs.
    /// </summary>
    /// <param name="withDetail">
    /// Whether to also read the blocks only the detail page shows: the external evaluation and the
    /// explanation. Off for the feed, which renders neither, and paying several more queries per
    /// page for columns nobody displays would be a cost the feed has no reason to carry.
    /// </param>
    private async Task<AlertContext[]> ComposeAsync(
        List<Alert> alerts,
        ScoringRun? lastRun,
        bool withDetail,
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

        var external = withDetail
            ? await GetExternalEvaluationsAsync(orderIds, cancellationToken)
            : [];
        var contradicted = withDetail
            ? await GetContradictedAsync(external.Values, cancellationToken)
            : [];
        var explanations = withDetail
            ? await GetExplanationsAsync(alerts, currentEvaluations, cancellationToken)
            : [];

        return alerts
            .Select(alert => new AlertContext(
                alert,
                orders[alert.OrderId],
                currentEvaluations.GetValueOrDefault(alert.OrderId),
                reviews.GetValueOrDefault(alert.Id),
                ToReference(lastRun),
                external.GetValueOrDefault(alert.OrderId),
                external.GetValueOrDefault(alert.OrderId) is { } one && contradicted.Contains(one.Id),
                explanations.GetValueOrDefault(alert.RiskEvaluationId),
                CurrentExplanationOf(alert, currentEvaluations, explanations)))
            .ToArray();
    }

    /// <summary>
    /// The explanation of the evaluation that is current, when that is a different evaluation from
    /// the one the snapshot froze and somebody has explained it.
    /// </summary>
    /// <remarks>
    /// Usually absent, and it exists for the case an escalation creates: the alert opened over the
    /// current evaluation may already carry its explanation, and showing the reader that the corpus
    /// moved <em>and</em> where it moved to is better than showing only that it moved.
    /// </remarks>
    private static AlertExplanation? CurrentExplanationOf(
        Alert alert,
        Dictionary<Guid, RiskEvaluation> currentEvaluations,
        Dictionary<Guid, AlertExplanation> explanations)
    {
        return currentEvaluations.GetValueOrDefault(alert.OrderId) is { } current
            && current.Id != alert.RiskEvaluationId
            ? explanations.GetValueOrDefault(current.Id)
            : null;
    }

    /// <summary>
    /// The explanations of the evaluations these alerts point at, keyed by evaluation.
    /// </summary>
    /// <remarks>
    /// Keyed by evaluation rather than by alert because that is the identity of an explanation: an
    /// escalation over an evaluation somebody already explained finds the paragraph written, and
    /// nobody pays for it twice. Narrowed to the policy version of the alert, since a summary names
    /// a severity band and a different policy names it differently. The most recent request wins if
    /// several templates have run.
    /// </remarks>
    private async Task<Dictionary<Guid, AlertExplanation>> GetExplanationsAsync(
        List<Alert> alerts,
        Dictionary<Guid, RiskEvaluation> currentEvaluations,
        CancellationToken cancellationToken)
    {
        var policyVersions = alerts.Select(alert => alert.AlertPolicyVersion).Distinct().ToArray();
        var evaluationIds = alerts
            .Select(alert => alert.RiskEvaluationId)
            .Concat(currentEvaluations.Values.Select(evaluation => evaluation.Id))
            .Distinct()
            .ToArray();

        var candidates = await dbContext.AlertExplanations
            .AsNoTracking()
            .Where(explanation => evaluationIds.Contains(explanation.RiskEvaluationId)
                && policyVersions.Contains(explanation.AlertPolicyVersion))
            .ToListAsync(cancellationToken);

        return candidates
            .GroupBy(explanation => explanation.RiskEvaluationId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(explanation => explanation.RequestedAt)
                    .ThenByDescending(explanation => explanation.Id)
                    .First());
    }

    /// <summary>
    /// The external evaluation that speaks for each order right now: the one still waiting for an
    /// answer if there is one, and otherwise the most recently requested. The same rule the request
    /// path uses, so the console and the API never disagree about which row is current.
    /// </summary>
    private async Task<Dictionary<Guid, ExternalEvaluation>> GetExternalEvaluationsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.ExternalEvaluations
            .AsNoTracking()
            .Where(evaluation => orderIds.Contains(evaluation.OrderId))
            .ToListAsync(cancellationToken);

        return candidates
            .GroupBy(evaluation => evaluation.OrderId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(evaluation => evaluation.Status == ExternalEvaluationStatus.Pending ? 0 : 1)
                    .ThenByDescending(evaluation => evaluation.RequestedAt)
                    .ThenByDescending(evaluation => evaluation.Id)
                    .First());
    }

    /// <summary>
    /// Which of these evaluations the provider contradicted itself about.
    /// </summary>
    /// <remarks>
    /// A conflicting receipt is kept precisely so it can be shown. Correlating it back is done by
    /// the same two halves the callback correlated by in the first place: the provider identifier
    /// once the row has one, and the order reference until then.
    /// </remarks>
    private async Task<HashSet<Guid>> GetContradictedAsync(
        IEnumerable<ExternalEvaluation> evaluations,
        CancellationToken cancellationToken)
    {
        var byOrder = evaluations.ToArray();
        if (byOrder.Length == 0)
        {
            return [];
        }

        var identifiers = byOrder
            .Select(evaluation => evaluation.ExternalEvaluationId)
            .Where(identifier => identifier is not null)
            .ToArray();
        var references = byOrder.Select(evaluation => evaluation.ReferenceId).ToArray();

        var conflicting = await dbContext.CallbackReceipts
            .AsNoTracking()
            .Where(receipt => receipt.Status == CallbackReceiptStatus.Conflicting
                && (identifiers.Contains(receipt.ExternalEvaluationId)
                    || references.Contains(receipt.ReferenceId)))
            .Select(receipt => new
            {
                receipt.Provider,
                receipt.ExternalEvaluationId,
                receipt.ReferenceId,
            })
            .ToListAsync(cancellationToken);

        return byOrder
            .Where(evaluation => conflicting.Any(receipt =>
                receipt.Provider == evaluation.Provider
                && (evaluation.ExternalEvaluationId is null
                    ? receipt.ReferenceId == evaluation.ReferenceId
                    : receipt.ExternalEvaluationId == evaluation.ExternalEvaluationId)))
            .Select(evaluation => evaluation.Id)
            .ToHashSet();
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
