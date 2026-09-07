namespace Salvo.Application.Dashboard;

/// <summary>
/// The scoring run that defines the current state of the corpus.
/// </summary>
public sealed record DashboardRun(Guid Id, long Sequence, DateTimeOffset CompletedAt, int OrderCount);

/// <summary>
/// One alert awaiting a verdict, with the order amount it puts at risk and the frozen signals it
/// was opened with.
/// </summary>
public sealed record OpenAlertRow(
    string AlertPolicyVersion,
    int RiskScoreSnapshot,
    string CurrencyCode,
    long AmountCents,
    string SignalsSnapshotJson);

/// <summary>
/// One order an analyst reported as fraud. Rows are distinct by order: an escalated order can carry
/// two reported alerts and still represents a single amount.
/// </summary>
public sealed record ReportedFraudOrderRow(Guid OrderId, string CurrencyCode, long AmountCents);

/// <summary>
/// The evaluation the current run assigned to one order, reduced to what the dashboard aggregates.
/// </summary>
public sealed record CurrentEvaluationRow(DateTimeOffset OccurredAt, bool IsFlagged);

/// <summary>
/// One order an external provider denied and the local rules never raised an alert on.
/// </summary>
/// <param name="LocalRiskScore">
/// What the current run scored it, or <see langword="null"/> when the run does not cover the order.
/// Read through the run like every other figure here, never off <c>risk_evaluations.status</c>.
/// </param>
public sealed record ExternalDenialRow(
    string MerchantReferenceId,
    DateTimeOffset OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    int? LocalRiskScore);

/// <param name="Total">
/// Every such order, not just the ones listed: the page is capped and the count is not.
/// </param>
public sealed record ExternalDenialPage(int Total, IReadOnlyList<ExternalDenialRow> Items);

/// <summary>
/// Everything the operational dashboard reads. The port is deliberately separate from
/// <see cref="Salvo.Application.Risk.IEvaluationLabelReader"/> and from
/// <see cref="Salvo.Application.Orders.IOrderDataStore"/>: no row it returns carries ground truth,
/// so no dashboard metric can be a function of the fraud label.
/// </summary>
/// <remarks>
/// Every method that describes the state of an order resolves it through the run rather than
/// through <c>risk_evaluations.status</c>: from stage 6 on, an order will also have external
/// evaluations with a status of their own, and a query that skipped the run would start counting
/// them.
/// </remarks>
public interface IDashboardReader
{
    /// <summary>
    /// The latest scoring run, or <see langword="null"/> when the corpus was never scored.
    /// </summary>
    Task<DashboardRun?> GetCurrentRunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Orders with no evaluation in the given run, or every order when
    /// <paramref name="runId"/> is <see langword="null"/> because no run exists. Non-zero after an
    /// import that no run covered yet, which is exactly when the rest of the dashboard would
    /// otherwise describe a stale corpus as if it were current.
    /// </summary>
    Task<int> CountOrdersPendingScoringAsync(Guid? runId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenAlertRow>> GetOpenAlertsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportedFraudOrderRow>> GetReportedFraudOrdersAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CurrentEvaluationRow>> GetCurrentEvaluationsAsync(
        Guid runId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Orders an external provider denied that carry no local alert of any kind.
    /// </summary>
    /// <remarks>
    /// This is the only surface where an order without an alert can be seen at all: the provider's
    /// opinion is otherwise shown solely inside the detail of an alert, and an order the rules
    /// never flagged has no detail to open. Without it, fraud that the deterministic engine cannot
    /// see and a provider can is a claim the console cannot show.
    /// </remarks>
    Task<ExternalDenialPage> GetExternalDenialsWithoutAlertAsync(
        Guid? runId,
        int limit,
        CancellationToken cancellationToken);
}
