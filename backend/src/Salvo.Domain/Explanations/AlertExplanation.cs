namespace Salvo.Domain.Explanations;

/// <summary>
/// One attempt, and everything since, at explaining a risk evaluation in words.
/// </summary>
/// <remarks>
/// <para>
/// Its identity is the <em>evaluation</em>, not the alert: everything a provider is told is a
/// function of the evaluation and the order, and the only thing the alert contributes is the policy
/// version that names its severity band. Keying by alert would mean that an escalation — a second
/// alert opened over the same evaluation — starts with no explanation and pays again for the same
/// paragraph.
/// </para>
/// <para>
/// Mutable on purpose, and the opposite of <c>RiskEvaluation</c> in the way that matters: an
/// evaluation is a pure function of the corpus and this is a conversation with something that can
/// fail. <c>Alert</c> could not host it — that snapshot is frozen and its status is a concurrency
/// token — which is the same reason the external verdict got its own table in stage 6.
/// </para>
/// <para>
/// The lifecycle exists to be impossible to jam. The row is reserved before the provider is called,
/// so two requests cannot both call it. Settling never depends on the caller still waiting, so a
/// closed browser cannot leave a row pending. A failure can be retried on this same row, so the
/// unique index over the identity refuses nothing. And an attempt budget stops a paid provider from
/// being asked forever.
/// </para>
/// </remarks>
public sealed class AlertExplanation
{
    /// <summary>
    /// How many times one evaluation may be sent to a provider. Also the ceiling the database
    /// enforces.
    /// </summary>
    public const int MaximumAttempts = 3;

    private AlertExplanation()
    {
        TemplateVersion = string.Empty;
        AlertPolicyVersion = string.Empty;
    }

    private AlertExplanation(
        Guid id,
        Guid riskEvaluationId,
        ExplanationProvider provider,
        string templateVersion,
        string alertPolicyVersion,
        Guid requestedFromAlertId,
        DateTimeOffset requestedAt)
    {
        Id = id;
        RiskEvaluationId = riskEvaluationId;
        Provider = provider;
        TemplateVersion = templateVersion;
        AlertPolicyVersion = alertPolicyVersion;
        RequestedFromAlertId = requestedFromAlertId;
        Status = ExplanationStatus.Pending;
        AttemptCount = 1;
        RequestedAt = requestedAt;
        RowVersion = 1;
    }

    public Guid Id { get; private set; }

    /// <summary>What is explained. Part of the identity.</summary>
    public Guid RiskEvaluationId { get; private set; }

    /// <summary>Who explained it. Part of the identity: a template and a model are not the same author.</summary>
    public ExplanationProvider Provider { get; private set; }

    /// <summary>Which prompt or template produced it. Part of the identity.</summary>
    public string TemplateVersion { get; private set; }

    /// <summary>
    /// Which severity policy named the band the text mentions. Part of the identity, and the only
    /// thing here the alert contributes.
    /// </summary>
    public string AlertPolicyVersion { get; private set; }

    /// <summary>
    /// The concrete model, when there is one. Never part of the identity: the same prompt answered
    /// by another model is another explanation, but it replaces this one rather than coexisting.
    /// </summary>
    public string? ProviderVersion { get; private set; }

    /// <summary>
    /// The alert this was asked from. Provenance, not identity — which is why an escalation finds
    /// the explanation of its evaluation already written.
    /// </summary>
    public Guid RequestedFromAlertId { get; private set; }

    /// <summary>The lifecycle state.</summary>
    public ExplanationStatus Status { get; private set; }

    /// <summary>The text. Present exactly when <see cref="Status"/> is ready, enforced by the schema.</summary>
    public string? Summary { get; private set; }

    /// <summary>The rules the provider says it cited, canonically serialized.</summary>
    public string? ReferencedRulesJson { get; private set; }

    public ExplanationFailureCode? FailureCode { get; private set; }

    /// <summary>
    /// Why it failed, in a few words. <strong>Never the rejected text</strong>: a figure that was
    /// not backed is named by the figure, not by the sentence around it.
    /// </summary>
    public string? FailureDetail { get; private set; }

    public int? InputTokens { get; private set; }

    public int? OutputTokens { get; private set; }

    /// <summary>How many times a provider has been asked. Rises on every reservation.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>When the current attempt began. Rewritten on every retry.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    /// <summary>
    /// The concurrency token, incremented by hand on every transition.
    /// </summary>
    /// <remarks>
    /// A version stamp rather than <see cref="Status"/> alone, because retaking an expired
    /// reservation is a <c>PENDING</c> to <c>PENDING</c> transition: two callers would both match
    /// on the status and both win. SQLite has no automatic row version, so the domain keeps it.
    /// </remarks>
    public long RowVersion { get; private set; }

    public bool IsSettled => Status != ExplanationStatus.Pending;

    /// <summary>Whether the attempt budget is spent, whatever the last failure was.</summary>
    public bool AttemptsExhausted => AttemptCount >= MaximumAttempts;

    /// <summary>
    /// Whether this reservation began at or before <paramref name="cutoff"/> and is therefore
    /// abandoned rather than in flight.
    /// </summary>
    /// <remarks>
    /// This is what replaces a reconciliation sweep inside the scope of this stage. Without it a
    /// request whose process died leaves a pending row that the partial unique index protects
    /// forever, and the evaluation can never be explained again.
    /// </remarks>
    public bool IsAbandonedAt(DateTimeOffset cutoff)
    {
        return Status == ExplanationStatus.Pending && RequestedAt <= cutoff;
    }

    /// <summary>
    /// Reserves the row, before the provider is asked anything.
    /// </summary>
    public static AlertExplanation Reserve(
        Guid id,
        Guid riskEvaluationId,
        ExplanationProvider provider,
        string templateVersion,
        string alertPolicyVersion,
        Guid requestedFromAlertId,
        DateTimeOffset requestedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(alertPolicyVersion);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (riskEvaluationId == Guid.Empty)
        {
            throw new ArgumentException(
                "riskEvaluationId must be a non-empty GUID.",
                nameof(riskEvaluationId));
        }

        if (requestedFromAlertId == Guid.Empty)
        {
            throw new ArgumentException(
                "requestedFromAlertId must be a non-empty GUID.",
                nameof(requestedFromAlertId));
        }

        // Throws on a member without a wire name, so an enumeration that grew cannot reach the
        // database.
        _ = ExplanationWireNames.ToWire(provider);

        return new(
            id,
            riskEvaluationId,
            provider,
            templateVersion,
            alertPolicyVersion,
            requestedFromAlertId,
            requestedAt.ToUniversalTime());
    }

    /// <summary>
    /// Starts another attempt on this same row: a failure being retried, or an abandoned
    /// reservation being taken over.
    /// </summary>
    /// <exception cref="ExplanationTransitionException">
    /// The explanation is ready — nothing to retry — or the attempt budget is spent.
    /// </exception>
    public void Retake(Guid requestedFromAlertId, DateTimeOffset requestedAt)
    {
        if (requestedFromAlertId == Guid.Empty)
        {
            throw new ArgumentException(
                "requestedFromAlertId must be a non-empty GUID.",
                nameof(requestedFromAlertId));
        }

        if (Status == ExplanationStatus.Ready)
        {
            throw new ExplanationTransitionException(
                $"Explanation {Id} is already written; a ready explanation is replaced, not retried.");
        }

        if (AttemptsExhausted)
        {
            throw new ExplanationTransitionException(
                $"Explanation {Id} has used its {MaximumAttempts} attempts.");
        }

        Status = ExplanationStatus.Pending;
        Summary = null;
        ReferencedRulesJson = null;
        FailureCode = null;
        FailureDetail = null;
        ProviderVersion = null;
        InputTokens = null;
        OutputTokens = null;
        SettledAt = null;
        RequestedFromAlertId = requestedFromAlertId;
        AttemptCount++;
        RequestedAt = requestedAt.ToUniversalTime();
        RowVersion++;
    }

    /// <summary>
    /// Records a summary that has already been verified. The check is the caller's, deliberately:
    /// an entity that validated its own text would let a provider hand text straight to it.
    /// </summary>
    /// <exception cref="ExplanationTransitionException">The explanation is not awaiting an answer.</exception>
    public void Complete(
        string summary,
        IReadOnlyList<string> referencedRules,
        string? providerVersion,
        int? inputTokens,
        int? outputTokens,
        DateTimeOffset settledAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentNullException.ThrowIfNull(referencedRules);
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens ?? 0);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens ?? 0);
        EnsurePending();

        if (summary.Length > ExplanationGrounding.MaximumSummaryLength)
        {
            throw new ArgumentException(
                $"A summary must not exceed {ExplanationGrounding.MaximumSummaryLength} characters.",
                nameof(summary));
        }

        Status = ExplanationStatus.Ready;
        Summary = summary;
        ReferencedRulesJson = ReferencedRuleSerializer.Serialize(referencedRules);
        ProviderVersion = Normalize(providerVersion);
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        FailureCode = null;
        FailureDetail = null;
        SettledAt = settledAt.ToUniversalTime();
        RowVersion++;
    }

    /// <summary>
    /// Closes the attempt without a summary.
    /// </summary>
    /// <remarks>
    /// When this is the attempt that spends the budget, the stored code becomes
    /// <see cref="ExplanationFailureCode.AttemptLimitReached"/> so that a reader can tell an
    /// explanation worth retrying from one that is finished. What actually went wrong is kept in
    /// <see cref="FailureDetail"/>, which costs nothing and would otherwise be lost.
    /// </remarks>
    /// <exception cref="ExplanationTransitionException">The explanation is not awaiting an answer.</exception>
    public void Fail(ExplanationFailureCode code, string? detail, DateTimeOffset settledAt)
    {
        EnsurePending();

        // Throws on a member without a wire name, for the same reason the reservation does.
        _ = ExplanationWireNames.ToWire(code);

        var exhausted = AttemptsExhausted && code != ExplanationFailureCode.AttemptLimitReached;

        Status = ExplanationStatus.Failed;
        Summary = null;
        ReferencedRulesJson = null;
        FailureCode = exhausted ? ExplanationFailureCode.AttemptLimitReached : code;
        FailureDetail = exhausted
            ? Compose(ExplanationWireNames.ToWire(code), Normalize(detail))
            : Normalize(detail);
        SettledAt = settledAt.ToUniversalTime();
        RowVersion++;
    }

    /// <summary>The rules the provider cited, or an empty list while there is no summary.</summary>
    public IReadOnlyList<string> ReferencedRules()
    {
        return ReferencedRulesJson is null
            ? []
            : ReferencedRuleSerializer.Deserialize(ReferencedRulesJson);
    }

    private void EnsurePending()
    {
        if (IsSettled)
        {
            throw new ExplanationTransitionException(
                $"Explanation {Id} is already "
                + $"'{ExplanationWireNames.ToWire(Status)}' and settles once per attempt.");
        }
    }

    private static string Compose(string code, string? detail)
    {
        return detail is null ? code : $"{code}: {detail}";
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
