namespace Salvo.Application.Dashboard;

/// <summary>
/// The run every figure on the dashboard belongs to. Present on the response so that no screen can
/// show a number without saying which execution produced it.
/// </summary>
public sealed record DashboardScoringRunView(
    long Sequence,
    DateTimeOffset CompletedAt,
    int OrderCount);

public sealed record DashboardSeverityCountView(string Severity, int AlertCount);

public sealed record DashboardOpenAlertsView(
    int Total,
    IReadOnlyList<DashboardSeverityCountView> BySeverity);

/// <summary>
/// Money still awaiting a verdict, in one currency.
/// </summary>
/// <remarks>
/// There is one entry per currency and deliberately no total: the corpus mixes BRL, USD and UYU,
/// and adding them would produce a number without a unit. Converting them would require a rate
/// source, a reference date and a policy for orders that are months old, none of which this stage
/// has.
/// </remarks>
public sealed record DashboardAmountAtRiskView(string CurrencyCode, long AmountCents, int AlertCount);

/// <summary>
/// Money an analyst reported as fraud, in one currency, aggregated by distinct order.
/// </summary>
/// <param name="OrderCount">
/// Orders, not alerts. An order escalated into a higher band can carry two reported alerts while
/// still being a single loss, so counting alerts here would double the amount.
/// </param>
public sealed record DashboardReportedFraudView(
    string CurrencyCode,
    long AmountCents,
    int OrderCount);

/// <summary>
/// One week of the corpus.
/// </summary>
/// <param name="WeekStart">
/// The Monday of the week in the business time zone of the rule configuration, which is the same
/// zone the rules use to decide what day an order belongs to.
/// </param>
/// <param name="FlaggedCount">Orders the current run denied.</param>
public sealed record DashboardRiskBucketView(DateOnly WeekStart, int OrderCount, int FlaggedCount);

/// <param name="AlertCount">Open alerts whose frozen snapshot carries this rule.</param>
public sealed record DashboardSignalView(string Rule, int AlertCount);

/// <summary>
/// The operational dashboard. Every field is derived from the deterministic evaluations of the
/// current run and from the verdicts of the analyst; none of them reads
/// <see cref="Salvo.Domain.Evaluation.OrderEvaluationLabel"/>, because outside a demo corpus that
/// ground truth does not exist.
/// </summary>
/// <param name="ScoringRun"><see langword="null"/> when the corpus was never scored.</param>
/// <param name="FlagRate">
/// Share of the current run the rules denied, or <see langword="null"/> when the run covered no
/// order.
/// </param>
public sealed record DashboardResult(
    DashboardScoringRunView? ScoringRun,
    int OrdersPendingScoring,
    DashboardOpenAlertsView OpenAlerts,
    IReadOnlyList<DashboardAmountAtRiskView> AmountAtRisk,
    IReadOnlyList<DashboardReportedFraudView> ReportedFraud,
    decimal? FlagRate,
    IReadOnlyList<DashboardRiskBucketView> RiskOverTime,
    IReadOnlyList<DashboardSignalView> TopSignals);
