namespace Salvo.Domain.Risk;

/// <summary>
/// A single risk evaluation of an order. Append-only: once created, a row is never modified, and
/// the type therefore exposes no public mutator.
/// </summary>
/// <remarks>
/// For <see cref="RiskEvaluationSource.Local"/> the identity of the row is its
/// <see cref="EvaluationFingerprint"/>, so an unchanged corpus and rule configuration reuse the
/// existing row instead of appending a duplicate. Other sources leave the fingerprint,
/// the score, the signals and the rule configuration version null; their lifecycle is decided when
/// the external provider is implemented.
/// </remarks>
public sealed class RiskEvaluation
{
    private RiskEvaluation()
    {
    }

    private RiskEvaluation(
        Guid id,
        Guid orderId,
        RiskEvaluationSource source,
        string? ruleConfigVersion,
        int? score,
        RiskEvaluationStatus status,
        string? signalsJson,
        string? evaluationFingerprint,
        string? externalEvaluationId,
        string? errorCode,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrderId = orderId;
        Source = source;
        RuleConfigVersion = ruleConfigVersion;
        Score = score;
        Status = status;
        SignalsJson = signalsJson;
        EvaluationFingerprint = evaluationFingerprint;
        ExternalEvaluationId = externalEvaluationId;
        ErrorCode = errorCode;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public RiskEvaluationSource Source { get; private set; }

    public string? RuleConfigVersion { get; private set; }

    public int? Score { get; private set; }

    public RiskEvaluationStatus Status { get; private set; }

    public string? SignalsJson { get; private set; }

    public string? EvaluationFingerprint { get; private set; }

    public string? ExternalEvaluationId { get; private set; }

    public string? ErrorCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates the local evaluation that corresponds to <paramref name="assessment"/>. The canonical
    /// signal string is computed once, persisted in <see cref="SignalsJson"/> and hashed into
    /// <see cref="EvaluationFingerprint"/>; the two can never diverge.
    /// </summary>
    public static RiskEvaluation ForLocal(
        Guid id,
        string ruleConfigVersion,
        LocalRiskAssessment assessment,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleConfigVersion);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (assessment.OrderId == Guid.Empty)
        {
            throw new ArgumentException("The assessment must reference an order.", nameof(assessment));
        }

        var signalsCanonical = RiskSignalSerializer.Serialize(assessment.Signals);
        var fingerprint = RiskEvaluationFingerprint.Compute(
            assessment.OrderId,
            RiskEvaluationSource.Local,
            ruleConfigVersion,
            assessment.Score,
            signalsCanonical);

        return new(
            id,
            assessment.OrderId,
            RiskEvaluationSource.Local,
            ruleConfigVersion,
            assessment.Score,
            assessment.IsFlagged ? RiskEvaluationStatus.Denied : RiskEvaluationStatus.Approved,
            signalsCanonical,
            fingerprint,
            externalEvaluationId: null,
            errorCode: null,
            createdAt.ToUniversalTime());
    }

    /// <summary>
    /// Whether the deterministic rules flagged the order for review.
    /// </summary>
    public bool IsFlagged => Status == RiskEvaluationStatus.Denied;
}
