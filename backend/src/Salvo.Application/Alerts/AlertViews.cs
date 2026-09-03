namespace Salvo.Application.Alerts;

public sealed record AlertSignalView(string Rule, int Weight, string Detail);

/// <summary>
/// The frozen evaluation that opened the alert. It is never rewritten, so it always describes the
/// premise the verdict is being formed on.
/// </summary>
public sealed record AlertSnapshotView(
    Guid EvaluationId,
    int Score,
    string Severity,
    IReadOnlyList<AlertSignalView> Signals);

/// <summary>
/// The evaluation that is current for the order right now.
/// </summary>
/// <param name="Severity">
/// <see langword="null"/> when the current score no longer reaches the alerting floor.
/// </param>
/// <param name="EvaluatedAt">
/// When this evaluation was first computed, which is not when it became current: an unchanged
/// evaluation is reused by every later run and keeps the timestamp of the run that inserted it. The
/// instant of the current state is <c>currentRun.completedAt</c>.
/// </param>
public sealed record AlertEvaluationView(
    Guid EvaluationId,
    int Score,
    string? Severity,
    bool IsFlagged,
    IReadOnlyList<AlertSignalView> Signals,
    DateTimeOffset EvaluatedAt);

/// <summary>
/// How far the current evaluation has moved from the snapshot. A band divergence must be
/// acknowledged before a verdict is accepted: an import that changed the corpus can make the
/// snapshot text describe a situation that is no longer true.
/// </summary>
public sealed record AlertDivergenceView(
    bool HasBandDivergence,
    int SnapshotScore,
    string SnapshotSeverity,
    int? CurrentScore,
    string? CurrentSeverity);

public sealed record AlertOrderView(
    Guid Id,
    string MerchantId,
    string MerchantReferenceId,
    string BuyerReferenceId,
    DateTimeOffset OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    string? City,
    string? DeviceSessionId);

public sealed record AlertReviewView(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string? Note,
    DateTimeOffset ReviewedAt);

/// <summary>
/// The public projection of an alert in a listing. Fields are enumerated explicitly so that nothing
/// outside this list can reach a client; ground-truth labels live in a separate entity and are
/// never read by this path.
/// </summary>
public sealed record AlertListItem(
    Guid Id,
    Guid OrderId,
    string MerchantReferenceId,
    string BuyerReferenceId,
    DateTimeOffset OccurredAt,
    long AmountCents,
    string CurrencyCode,
    string CountryCode,
    string Status,
    string Severity,
    int RiskScoreSnapshot,
    int? CurrentRiskScore,
    string? CurrentSeverity,
    bool HasBandDivergence,
    string AlertPolicyVersion,
    Guid? SupersedesAlertId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt);

/// <param name="ScoringRunSequence">
/// Sequence of the run the page is current as of, or <see langword="null"/> when the corpus was
/// never scored. No ordering of this feed survives a run that happens between two pages — a run
/// inserts new alerts at the front and can move the score an alert is ordered by — so a client that
/// pages compares this value and restarts from the first page when it changes.
/// </param>
/// <param name="CurrentRun">
/// The same run, with the instant it completed, for screens that state the provenance of what they
/// show.
/// </param>
public sealed record ListAlertsResult(
    IReadOnlyList<AlertListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    long? ScoringRunSequence,
    ScoringRunReference? CurrentRun);

public sealed record AlertDetail(
    Guid Id,
    Guid OrderId,
    string Status,
    string Severity,
    string AlertPolicyVersion,
    Guid? SupersedesAlertId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    AlertOrderView Order,
    AlertSnapshotView Snapshot,
    AlertEvaluationView? CurrentEvaluation,
    ScoringRunReference? CurrentRun,
    AlertDivergenceView Divergence,
    AlertReviewView? Review);

/// <param name="Applied">
/// <see langword="false"/> when the alert already carried exactly this verdict and the request was
/// therefore a no-op.
/// </param>
public sealed record AlertReviewResult(bool Applied, AlertDetail Alert);
