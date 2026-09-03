using Microsoft.EntityFrameworkCore;
using Salvo.Application.Metrics;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfEvaluationMetricsReader(SalvoDbContext dbContext) : IEvaluationMetricsReader
{
    public async Task<MetricsRun?> GetCurrentRunAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ScoringRuns
            .AsNoTracking()
            .OrderByDescending(run => run.Sequence)
            .Select(run => new MetricsRun(
                run.Id,
                run.Sequence,
                run.RuleConfigVersion,
                run.CompletedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The orders the run covered with the score of the evaluation it made current. Read through
    /// the run, so the quality surface and the operational dashboard always describe the same
    /// state of the corpus.
    /// </summary>
    public async Task<IReadOnlyList<ScoredOrderRow>> GetScoredOrdersAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        return await (from link in dbContext.RunEvaluations.AsNoTracking()
                      where link.RunId == runId
                      join evaluation in dbContext.RiskEvaluations.AsNoTracking()
                          on link.EvaluationId equals evaluation.Id
                      join order in dbContext.Orders.AsNoTracking()
                          on link.OrderId equals order.Id
                      select new ScoredOrderRow(order.Id, order.OccurredAt, evaluation.Score))
            .ToListAsync(cancellationToken);
    }
}
