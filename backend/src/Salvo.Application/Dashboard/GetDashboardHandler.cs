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
    /// <summary>
    /// How many denied-without-alert orders the panel lists.
    /// </summary>
    /// <remarks>
    /// Generous on purpose. The panel exists so that orders nobody ever looked at become visible,
    /// and a cap tight enough to hide them would defeat the reason it was added; the bound is here
    /// so the contract stays a bounded array rather than "however many there are". The total is
    /// always reported in full, capped or not.
    /// </remarks>
    public const int ExternalDenialListLimit = 100;

    public async Task<DashboardResult> HandleAsync(CancellationToken cancellationToken)
    {
        var openAlerts = await reader.GetOpenAlertsAsync(cancellationToken);
        var reportedFraud = await reader.GetReportedFraudOrdersAsync(cancellationToken);
        var run = await reader.GetCurrentRunAsync(cancellationToken);

        if (run is null)
        {
            // Without a run nothing is current: there are no evaluations to summarize, and every
            // order is waiting to be scored. A provider can still have denied orders, and with no
            // run behind them none of them carries a local score.
            return new(
                null,
                await reader.CountOrdersPendingScoringAsync(null, cancellationToken),
                ToOpenAlertsView(openAlerts),
                ToAmountAtRisk(openAlerts),
                ToReportedFraud(reportedFraud),
                null,
                [],
                ToTopSignals(openAlerts),
                ToExternalDenials(await reader.GetExternalDenialsWithoutAlertAsync(
                    null,
                    ExternalDenialListLimit,
                    cancellationToken)));
        }

        var evaluations = await reader.GetCurrentEvaluationsAsync(run.Id, cancellationToken);
        var pending = await reader.CountOrdersPendingScoringAsync(run.Id, cancellationToken);
        var denials = await reader.GetExternalDenialsWithoutAlertAsync(
            run.Id,
            ExternalDenialListLimit,
            cancellationToken);

        return new(
            new(run.Sequence, run.CompletedAt, run.OrderCount),
            pending,
            ToOpenAlertsView(openAlerts),
            ToAmountAtRisk(openAlerts),
            ToReportedFraud(reportedFraud),
            ToFlagRate(evaluations),
            ToRiskOverTime(evaluations),
            ToTopSignals(openAlerts),
            ToExternalDenials(denials));
    }

    /// <summary>
    /// The orders a provider denied and nobody ever looked at.
    /// </summary>
    /// <remarks>
    /// It reads no ground-truth label, exactly like every other figure here. What makes the panel
    /// worth its space is that it is the only place in the console where an order without an alert
    /// appears at all.
    /// </remarks>
    private static DashboardExternalDenialsView ToExternalDenials(ExternalDenialPage page)
    {
        var items = page.Items
            .Select(row => new DashboardExternalDenialView(
                row.MerchantReferenceId,
                row.OccurredAt,
                row.AmountCents,
                row.CurrencyCode,
                row.CountryCode,
                row.LocalRiskScore))
            .ToArray();

        return new(page.Total, items.Length, items);
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

        // The week a run happened in, not a property of any stored row: the business zone is the
        // same in every known configuration, so the current one is the honest thing to ask.
        var zone = RuleConfig.Current.BusinessTimeZone;
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
