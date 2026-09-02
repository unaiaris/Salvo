namespace Salvo.Domain.Alerts;

/// <summary>
/// A flagged order queued for human review, with a frozen copy of the evaluation that raised it.
/// </summary>
/// <remarks>
/// The snapshot documents why the alert was opened and is never overwritten: it is the record the
/// verdict was based on. When a later import changes the corpus, the divergence between the
/// snapshot and the current evaluation is surfaced by the read model instead of being hidden behind
/// a silent update.
/// </remarks>
public sealed class Alert
{
    private Alert()
    {
        SignalsSnapshotJson = string.Empty;
        AlertPolicyVersion = string.Empty;
    }

    private Alert(
        Guid id,
        Guid orderId,
        Guid riskEvaluationId,
        int riskScoreSnapshot,
        string signalsSnapshotJson,
        string alertPolicyVersion,
        Guid? supersedesAlertId,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrderId = orderId;
        RiskEvaluationId = riskEvaluationId;
        RiskScoreSnapshot = riskScoreSnapshot;
        SignalsSnapshotJson = signalsSnapshotJson;
        AlertPolicyVersion = alertPolicyVersion;
        Status = AlertStatus.Open;
        SupersedesAlertId = supersedesAlertId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid RiskEvaluationId { get; private set; }

    public int RiskScoreSnapshot { get; private set; }

    public string SignalsSnapshotJson { get; private set; }

    public string AlertPolicyVersion { get; private set; }

    public AlertStatus Status { get; private set; }

    /// <summary>
    /// The reviewed alert this one escalates, when a retroactive import raised the order into a
    /// higher severity band.
    /// </summary>
    public Guid? SupersedesAlertId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>
    /// Derived from <see cref="RiskScoreSnapshot"/> through the policy version the alert was opened
    /// with, so it is never a stored column and never changes retroactively.
    /// </summary>
    public AlertSeverity Severity => AlertPolicy.ForVersion(AlertPolicyVersion).SeverityFor(RiskScoreSnapshot);

    public static Alert Open(
        Guid id,
        Guid orderId,
        Guid riskEvaluationId,
        int riskScoreSnapshot,
        string signalsSnapshotJson,
        string alertPolicyVersion,
        Guid? supersedesAlertId,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalsSnapshotJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(alertPolicyVersion);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("orderId must be a non-empty GUID.", nameof(orderId));
        }

        if (riskEvaluationId == Guid.Empty)
        {
            throw new ArgumentException("riskEvaluationId must be a non-empty GUID.", nameof(riskEvaluationId));
        }

        if (supersedesAlertId == Guid.Empty)
        {
            throw new ArgumentException(
                "supersedesAlertId must be null or a non-empty GUID.",
                nameof(supersedesAlertId));
        }

        if (supersedesAlertId == id)
        {
            throw new ArgumentException("An alert cannot supersede itself.", nameof(supersedesAlertId));
        }

        // Throws when the score falls outside every band of the policy, which is the situation
        // AlertPolicy.Validate is meant to prevent from ever reaching production.
        _ = AlertPolicy.ForVersion(alertPolicyVersion).SeverityFor(riskScoreSnapshot);

        return new(
            id,
            orderId,
            riskEvaluationId,
            riskScoreSnapshot,
            signalsSnapshotJson,
            alertPolicyVersion,
            supersedesAlertId,
            createdAt.ToUniversalTime());
    }

    /// <summary>
    /// Records the verdict. The only mutation the type allows, and only from
    /// <see cref="AlertStatus.Open"/>.
    /// </summary>
    /// <exception cref="AlertTransitionException">
    /// The alert is already reviewed, or <paramref name="newStatus"/> is not a verdict.
    /// </exception>
    public AlertReview Review(
        Guid reviewId,
        AlertStatus newStatus,
        string? note,
        DateTimeOffset reviewedAt)
    {
        if (newStatus is not (AlertStatus.ConfirmedSafe or AlertStatus.ReportedFraud))
        {
            throw new AlertTransitionException(
                $"'{AlertWireNames.ToWire(newStatus)}' is not a verdict; an alert can only be closed.");
        }

        if (Status != AlertStatus.Open)
        {
            throw new AlertTransitionException(
                $"Alert {Id} was already reviewed as '{AlertWireNames.ToWire(Status)}'.");
        }

        var previousStatus = Status;
        var reviewedAtUtc = reviewedAt.ToUniversalTime();
        Status = newStatus;
        ReviewedAt = reviewedAtUtc;

        return AlertReview.Record(reviewId, Id, previousStatus, newStatus, note, reviewedAtUtc);
    }
}
