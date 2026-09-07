using Microsoft.EntityFrameworkCore;
using Salvo.Application.Dashboard;
using Salvo.Domain.Alerts;
using Salvo.Domain.External;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

/// <summary>
/// Reads the operational dashboard out of SQLite.
/// </summary>
/// <remarks>
/// No query in this class touches <c>order_evaluation_labels</c>, and none reads
/// <c>risk_evaluations.status</c> without going through <c>run_evaluations</c> first. Both rules
/// are enforced by tests rather than by convention: one flips every label in the database and
/// requires an identical response, the other is what keeps the external evaluations of stage 6 from
/// silently entering these counts.
/// </remarks>
public sealed class EfDashboardReader(SalvoDbContext dbContext) : IDashboardReader
{
    public async Task<DashboardRun?> GetCurrentRunAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ScoringRuns
            .AsNoTracking()
            .OrderByDescending(run => run.Sequence)
            .Select(run => new DashboardRun(run.Id, run.Sequence, run.CompletedAt, run.OrderCount))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountOrdersPendingScoringAsync(
        Guid? runId,
        CancellationToken cancellationToken)
    {
        if (runId is not { } id)
        {
            return await dbContext.Orders.AsNoTracking().CountAsync(cancellationToken);
        }

        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => !dbContext.RunEvaluations
                .Any(link => link.RunId == id && link.OrderId == order.Id))
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpenAlertRow>> GetOpenAlertsAsync(
        CancellationToken cancellationToken)
    {
        return await (from alert in dbContext.Alerts.AsNoTracking()
                      where alert.Status == AlertStatus.Open
                      join order in dbContext.Orders.AsNoTracking()
                          on alert.OrderId equals order.Id
                      select new OpenAlertRow(
                          alert.AlertPolicyVersion,
                          alert.RiskScoreSnapshot,
                          order.CurrencyCode,
                          order.AmountCents,
                          alert.SignalsSnapshotJson))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The orders an analyst reported as fraud, one row each.
    /// </summary>
    /// <remarks>
    /// The distinct projection is the point: escalation opens a second alert linked to the first,
    /// and the creation predicate compares bands without looking at the earlier verdict, so one
    /// order can carry two reported alerts. Summing alerts would count its amount twice.
    /// </remarks>
    public async Task<IReadOnlyList<ReportedFraudOrderRow>> GetReportedFraudOrdersAsync(
        CancellationToken cancellationToken)
    {
        return await (from alert in dbContext.Alerts.AsNoTracking()
                      where alert.Status == AlertStatus.ReportedFraud
                      join order in dbContext.Orders.AsNoTracking()
                          on alert.OrderId equals order.Id
                      select new ReportedFraudOrderRow(
                          order.Id,
                          order.CurrencyCode,
                          order.AmountCents))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrentEvaluationRow>> GetCurrentEvaluationsAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        return await (from link in dbContext.RunEvaluations.AsNoTracking()
                      where link.RunId == runId
                      join evaluation in dbContext.RiskEvaluations.AsNoTracking()
                          on link.EvaluationId equals evaluation.Id
                      join order in dbContext.Orders.AsNoTracking()
                          on link.OrderId equals order.Id
                      select new CurrentEvaluationRow(
                          order.OccurredAt,
                          evaluation.Status == RiskEvaluationStatus.Denied))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Orders a provider denied that never produced an alert, newest first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "No alert" means no alert row at all, not "no open alert": an order whose alert an analyst
    /// already judged was surfaced, and this panel is about the ones that never were.
    /// </para>
    /// <para>
    /// The distinct projection matters because an order can hold more than one external evaluation
    /// over its life — a failed attempt is retried as a new row — and the panel counts orders.
    /// </para>
    /// </remarks>
    public async Task<ExternalDenialPage> GetExternalDenialsWithoutAlertAsync(
        Guid? runId,
        int limit,
        CancellationToken cancellationToken)
    {
        var denied = from evaluation in dbContext.ExternalEvaluations.AsNoTracking()
                     where evaluation.Status == ExternalEvaluationStatus.Denied
                     join order in dbContext.Orders.AsNoTracking()
                         on evaluation.OrderId equals order.Id
                     where !dbContext.Alerts.Any(alert => alert.OrderId == order.Id)
                     select order;
        var orders = denied.Distinct();

        var total = await orders.CountAsync(cancellationToken);
        var page = await orders
            .OrderByDescending(order => order.OccurredAt)
            .ThenBy(order => order.MerchantReferenceId)
            .Take(limit)
            .Select(order => new ExternalDenialRow(
                order.MerchantReferenceId,
                order.OccurredAt,
                order.AmountCents,
                order.CurrencyCode,
                order.CountryCode,
                runId == null
                    ? null
                    : (from link in dbContext.RunEvaluations
                       where link.RunId == runId && link.OrderId == order.Id
                       join evaluation in dbContext.RiskEvaluations
                           on link.EvaluationId equals evaluation.Id
                       select evaluation.Score).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new(total, page);
    }
}
