using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;

namespace Salvo.Application.Dashboard;

/// <summary>
/// Builds the operational dashboard from the current scoring run and from the alerts an analyst
/// still has to judge.
/// </summary>
/// <remarks>
/// The handler depends on a single port that cannot return a ground-truth label. That is a
/// structural guarantee only as far as the port goes; what actually holds the invariant is the
/// differential test, which flips every label in the database and requires the response to stay
/// identical.
/// </remarks>
public sealed class GetDashboardHandler(IDashboardReader reader)
{
    public async Task<DashboardResult> HandleAsync(CancellationToken cancellationToken)
    {
        var openAlerts = await reader.GetOpenAlertsAsync(cancellationToken);
        var reportedFraud = await reader.GetReportedFraudOrdersAsync(cancellationToken);
        var run = await reader.GetCurrentRunAsync(cancellationToken);

        if (run is null)
        {
            // Without a run nothing is current: there are no evaluations to summarize, and every
            // order is waiting to be scored.
            return new(
                null,
                await reader.CountOrdersPendingScoringAsync(null, cancellationToken),
                ToOpenAlertsView(openAlerts),
                ToAmountAtRisk(openAlerts),
                ToReportedFraud(reportedFraud),
                null,
                [],
                ToTopSignals(openAlerts));
        }

        var evaluations = await reader.GetCurrentEvaluationsAsync(run.Id, cancellationToken);
        var pending = await reader.CountOrdersPendingScoringAsync(run.Id, cancellationToken);

        return new(
            new(run.Sequence, run.CompletedAt, run.OrderCount),
            pending,
            ToOpenAlertsView(openAlerts),
            ToAmountAtRisk(openAlerts),
            ToReportedFraud(reportedFraud),
            ToFlagRate(evaluations),
            ToRiskOverTime(evaluations),
            ToTopSignals(openAlerts));
    }

    /// <summary>
    /// Counts open alerts per severity band. Severity is derived through the policy version each
    /// alert was opened with, never through the current one, so a future policy cannot reclassify
    /// history. Every band of the current policy is reported, including the empty ones.
    /// </summary>
    private static DashboardOpenAlertsView ToOpenAlertsView(IReadOnlyList<OpenAlertRow> alerts)
    {
        var counts = new Dictionary<AlertSeverity, int>();
        foreach (var alert in alerts)
        {
            var severity = AlertPolicy.ForVersion(alert.AlertPolicyVersion)
                .SeverityFor(alert.RiskScoreSnapshot);
            counts[severity] = counts.GetValueOrDefault(severity) + 1;
        }

        var bands = AlertPolicy.E4V1.Bands
            .Select(band => band.Severity)
            .Distinct()
            .OrderByDescending(severity => severity)
            .Select(severity => new DashboardSeverityCountView(
                AlertWireNames.ToWire(severity),
                counts.GetValueOrDefault(severity)))
            .ToArray();

        return new(alerts.Count, bands);
    }

    private static DashboardAmountAtRiskView[] ToAmountAtRisk(IReadOnlyList<OpenAlertRow> alerts)
    {
        return alerts
            .GroupBy(alert => alert.CurrencyCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DashboardAmountAtRiskView(
                group.Key,
                group.Sum(alert => alert.AmountCents),
                group.Count()))
            .ToArray();
    }

    private static DashboardReportedFraudView[] ToReportedFraud(
        IReadOnlyList<ReportedFraudOrderRow> orders)
    {
        return orders
            .GroupBy(order => order.CurrencyCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DashboardReportedFraudView(
                group.Key,
                group.Sum(order => order.AmountCents),
                group.Count()))
            .ToArray();
    }

    private static decimal? ToFlagRate(IReadOnlyList<CurrentEvaluationRow> evaluations)
    {
        return evaluations.Count == 0
            ? null
            : (decimal)evaluations.Count(evaluation => evaluation.IsFlagged) / evaluations.Count;
    }

    /// <summary>
    /// Weekly buckets over the business time of the orders, not over the moment they were
    /// evaluated: a corpus imported today can describe purchases of three months ago.
    /// </summary>
    /// <remarks>
    /// Weeks with no order are emitted with zeros so that the series is contiguous and a gap in the
    /// corpus reads as a gap instead of as a shorter bar next to its neighbour.
    /// </remarks>
    private static DashboardRiskBucketView[] ToRiskOverTime(
        IReadOnlyList<CurrentEvaluationRow> evaluations)
    {
        if (evaluations.Count == 0)
        {
            return [];
        }

        var zone = RuleConfig.E3V1.BusinessTimeZone;
        var buckets = new Dictionary<DateOnly, (int Orders, int Flagged)>();
        foreach (var evaluation in evaluations)
        {
            var week = WeekStart(evaluation.OccurredAt, zone);
            var current = buckets.GetValueOrDefault(week);
            buckets[week] = (
                current.Orders + 1,
                current.Flagged + (evaluation.IsFlagged ? 1 : 0));
        }

        var first = buckets.Keys.Min();
        var last = buckets.Keys.Max();
        var series = new List<DashboardRiskBucketView>();
        for (var week = first; week <= last; week = week.AddDays(7))
        {
            var bucket = buckets.GetValueOrDefault(week);
            series.Add(new(week, bucket.Orders, bucket.Flagged));
        }

        return [.. series];
    }

    /// <summary>
    /// Counts rules over the snapshots of the open alerts, not over every current evaluation.
    /// </summary>
    /// <remarks>
    /// The wider population would be dominated by low-weight signals that never raised an alert:
    /// in the demo corpus <c>foreign_country</c> fires on sixteen orders that nobody has to look
    /// at. What an analyst needs is why the queue in front of them exists.
    /// </remarks>
    private static DashboardSignalView[] ToTopSignals(IReadOnlyList<OpenAlertRow> alerts)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var alert in alerts)
        {
            foreach (var signal in RiskSignalSerializer.Deserialize(alert.SignalsSnapshotJson))
            {
                counts[signal.Rule] = counts.GetValueOrDefault(signal.Rule) + 1;
            }
        }

        return counts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => RiskRuleNames.CanonicalIndexOf(entry.Key))
            .Select(entry => new DashboardSignalView(entry.Key, entry.Value))
            .ToArray();
    }

    private static DateOnly WeekStart(DateTimeOffset occurredAt, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(occurredAt, zone);
        var date = DateOnly.FromDateTime(local.DateTime);

        // Monday is the first day of the week here; DayOfWeek counts from Sunday.
        return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    }
}
