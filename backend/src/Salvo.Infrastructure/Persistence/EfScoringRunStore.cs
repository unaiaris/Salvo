using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salvo.Application.Alerts;
using Salvo.Application.Risk;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfScoringRunStore(SalvoDbContext dbContext) : IScoringRunStore
{
    private const int ConstraintUnique = 2067;
    private const int ConstraintPrimaryKey = 1555;

    public async Task<long> GetLastRunSequenceAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ScoringRuns
            .AsNoTracking()
            .MaxAsync(run => (long?)run.Sequence, cancellationToken) ?? 0;
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetLocalEvaluationIdsByFingerprintAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken)
    {
        if (fingerprints.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.Ordinal);
        }

        var distinct = fingerprints.Distinct(StringComparer.Ordinal).ToArray();
        var matches = await dbContext.RiskEvaluations
            .AsNoTracking()
            .Where(evaluation =>
                evaluation.Source == RiskEvaluationSource.Local
                && evaluation.EvaluationFingerprint != null
                && distinct.Contains(evaluation.EvaluationFingerprint))
            .Select(evaluation => new
            {
                Fingerprint = evaluation.EvaluationFingerprint!,
                evaluation.Id,
            })
            .ToListAsync(cancellationToken);

        return matches.ToDictionary(
            match => match.Fingerprint,
            match => match.Id,
            StringComparer.Ordinal);
    }

    public async Task<IReadOnlyDictionary<Guid, OrderAlertState>> GetAlertStatesAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return new Dictionary<Guid, OrderAlertState>();
        }

        var distinct = orderIds.Distinct().ToArray();
        var alerts = await dbContext.Alerts
            .AsNoTracking()
            .Where(alert => distinct.Contains(alert.OrderId))
            .Select(alert => new
            {
                alert.Id,
                alert.OrderId,
                alert.Status,
                alert.RiskScoreSnapshot,
                alert.AlertPolicyVersion,
                alert.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return alerts
            .GroupBy(alert => alert.OrderId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group
                        .OrderByDescending(alert => alert.CreatedAt)
                        .ThenByDescending(alert => alert.Id)
                        .First();

                    return new OrderAlertState(
                        group.Any(alert => alert.Status == AlertStatus.Open),
                        latest.Id,
                        AlertPolicy.ForVersion(latest.AlertPolicyVersion).SeverityFor(latest.RiskScoreSnapshot));
                });
    }

    public async Task SaveRunAsync(
        ScoringRun run,
        IReadOnlyCollection<RiskEvaluation> evaluationsToAppend,
        IReadOnlyCollection<RunEvaluation> runEvaluations,
        IReadOnlyCollection<Alert> alertsToOpen,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        dbContext.ScoringRuns.Add(run);
        dbContext.RiskEvaluations.AddRange(evaluationsToAppend);
        dbContext.RunEvaluations.AddRange(runEvaluations);
        dbContext.Alerts.AddRange(alertsToOpen);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
        {
            throw new ScoringRunConflictException(
                "A concurrent scoring run already persisted conflicting state.",
                exception);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, RiskEvaluation>> GetCurrentEvaluationsAsync(
        CancellationToken cancellationToken)
    {
        var lastRun = await dbContext.ScoringRuns
            .AsNoTracking()
            .OrderByDescending(run => run.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (lastRun is null)
        {
            return new Dictionary<Guid, RiskEvaluation>();
        }

        var current = await dbContext.RunEvaluations
            .AsNoTracking()
            .Where(link => link.RunId == lastRun.Id)
            .Join(
                dbContext.RiskEvaluations.AsNoTracking(),
                link => link.EvaluationId,
                evaluation => evaluation.Id,
                (link, evaluation) => new { link.OrderId, Evaluation = evaluation })
            .ToListAsync(cancellationToken);

        return current.ToDictionary(entry => entry.OrderId, entry => entry.Evaluation);
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey;
    }
}
