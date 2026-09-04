namespace Salvo.Domain.External;

/// <summary>
/// One request for an external verdict on an order, and everything learned about it since.
/// </summary>
/// <remarks>
/// <para>
/// Mutable on purpose, and the opposite of the local evaluation in every way that matters: it has
/// no content identity, it can be probed again, and it carries correlation identifiers. That is why
/// it lives in its own table with its own types.
/// </para>
/// <para>
/// The row is reserved in <see cref="Pending"/> before the provider is called. A partial unique
/// index over the pending rows of an order then serializes concurrent requests at a moment when
/// nothing exists on the provider side yet, so the loser of that race is refused without any
/// external effect.
/// </para>
/// <para>
/// Only what is known to be final settles it: a request that was never sent, or a definitive
/// refusal. A timeout or an unreadable answer is indeterminate — the provider may well have
/// registered the evaluation — so it leaves the row pending with <see cref="LastErrorCode"/> and
/// waits for the callback or for reconciliation.
/// </para>
/// </remarks>
public sealed class ExternalEvaluation
{
    private ExternalEvaluation()
    {
        ReferenceId = string.Empty;
    }

    private ExternalEvaluation(
        Guid id,
        Guid orderId,
        ExternalProvider provider,
        string referenceId,
        DateTimeOffset requestedAt)
    {
        Id = id;
        OrderId = orderId;
        Provider = provider;
        ReferenceId = referenceId;
        Status = ExternalEvaluationStatus.Pending;
        RequestedAt = requestedAt;
        UpdatedAt = requestedAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public ExternalProvider Provider { get; private set; }

    /// <summary>
    /// The stable reference of the order, written during the reservation.
    /// </summary>
    public string ReferenceId { get; private set; }

    /// <summary>
    /// The identifier the provider assigned, once it answered. Null until then, and sometimes
    /// forever: it is exactly the case reconciliation by reference exists to resolve.
    /// </summary>
    public string? ExternalEvaluationId { get; private set; }

    /// <summary>
    /// The lifecycle state, and the concurrency token of the row. Reconciliation and a callback
    /// compete for the same evaluation, and the token is what makes one of them lose cleanly.
    /// </summary>
    public ExternalEvaluationStatus Status { get; private set; }

    /// <summary>
    /// The score of the provider, on the scale of the provider. It is never compared numerically
    /// with the local score: they are different scales of different systems.
    /// </summary>
    public int? Score { get; private set; }

    /// <summary>
    /// Why the evaluation ended in <see cref="ExternalEvaluationStatus.Error"/>. Set only together
    /// with that status.
    /// </summary>
    public ExternalEvaluationErrorCode? ErrorCode { get; private set; }

    /// <summary>
    /// The failure a still-pending evaluation is carrying. Distinct from <see cref="ErrorCode"/>:
    /// this one describes an attempt, not an outcome.
    /// </summary>
    public ExternalEvaluationErrorCode? LastErrorCode { get; private set; }

    /// <summary>
    /// How many times reconciliation probed this evaluation. Not a retry count: a retry would be a
    /// new row, so that number would always be one.
    /// </summary>
    public int AttemptCount { get; private set; }

    public ExternalSettlementSource? SettledBy { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    public bool IsSettled => Status != ExternalEvaluationStatus.Pending;

    /// <summary>
    /// Reserves the row, before anything is asked of the provider.
    /// </summary>
    public static ExternalEvaluation Reserve(
        Guid id,
        Guid orderId,
        ExternalProvider provider,
        string referenceId,
        DateTimeOffset requestedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("orderId must be a non-empty GUID.", nameof(orderId));
        }

        if (referenceId.Length > ExternalEvaluationReference.MaximumLength)
        {
            throw new ArgumentException(
                $"referenceId must not exceed {ExternalEvaluationReference.MaximumLength} characters.",
                nameof(referenceId));
        }

        // Throws on an unknown member, so an enumeration that grows without its wire name cannot
        // reach the database.
        _ = ExternalEvaluationWireNames.ToWire(provider);

        return new(id, orderId, provider, referenceId, requestedAt.ToUniversalTime());
    }

    /// <summary>
    /// Records an attempt that left the evaluation pending: the provider accepted it and has not
    /// decided, or the answer never arrived and the outcome is unknown.
    /// </summary>
    /// <exception cref="ExternalEvaluationTransitionException">
    /// The evaluation is already settled. A verdict is not reopened by a message that arrives late.
    /// </exception>
    public void RecordPendingAttempt(
        string? externalEvaluationId,
        ExternalEvaluationErrorCode? lastErrorCode,
        ExternalSettlementSource source,
        DateTimeOffset at)
    {
        if (IsSettled)
        {
            throw new ExternalEvaluationTransitionException(
                $"External evaluation {Id} is already settled as "
                + $"'{ExternalEvaluationWireNames.ToWire(Status)}'.");
        }

        AdoptExternalIdentifier(externalEvaluationId);
        LastErrorCode = lastErrorCode ?? LastErrorCode;
        Touch(source, at);
    }

    /// <summary>
    /// Settles the evaluation. The only transition out of <see cref="ExternalEvaluationStatus.Pending"/>.
    /// </summary>
    /// <exception cref="ExternalEvaluationTransitionException">
    /// The evaluation is already settled, or <paramref name="status"/> is not a terminal one.
    /// </exception>
    public void Settle(
        ExternalEvaluationStatus status,
        string? externalEvaluationId,
        int? score,
        ExternalEvaluationErrorCode? errorCode,
        ExternalSettlementSource source,
        DateTimeOffset at)
    {
        if (status == ExternalEvaluationStatus.Pending)
        {
            throw new ExternalEvaluationTransitionException(
                "'PENDING' does not settle an external evaluation.");
        }

        if (IsSettled)
        {
            throw new ExternalEvaluationTransitionException(
                $"External evaluation {Id} is already settled as "
                + $"'{ExternalEvaluationWireNames.ToWire(Status)}'.");
        }

        if (status == ExternalEvaluationStatus.Error)
        {
            if (errorCode is null)
            {
                throw new ArgumentNullException(
                    nameof(errorCode),
                    "An evaluation that settles in ERROR must name why.");
            }
        }
        else if (errorCode is not null)
        {
            throw new ArgumentException(
                "An error code only belongs to an evaluation that settles in ERROR.",
                nameof(errorCode));
        }

        if (score is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, "score must not be negative.");
        }

        // Throws on an unknown member, for the same reason the reservation does.
        _ = ExternalEvaluationWireNames.ToWire(status);

        AdoptExternalIdentifier(externalEvaluationId);
        Status = status;
        Score = score;
        ErrorCode = errorCode;
        SettledAt = at.ToUniversalTime();
        SettledBy = source;
        Touch(source, at);
    }

    /// <summary>
    /// Takes the identifier the provider assigned, once. A provider that later reports a different
    /// one for the same evaluation is contradicting itself, and the first one is the one every
    /// earlier message was correlated by.
    /// </summary>
    private void AdoptExternalIdentifier(string? externalEvaluationId)
    {
        var normalized = string.IsNullOrWhiteSpace(externalEvaluationId) ? null : externalEvaluationId.Trim();
        if (normalized is null || string.Equals(ExternalEvaluationId, normalized, StringComparison.Ordinal))
        {
            return;
        }

        if (ExternalEvaluationId is not null)
        {
            throw new ExternalEvaluationTransitionException(
                $"External evaluation {Id} is already correlated with another provider identifier.");
        }

        ExternalEvaluationId = normalized;
    }

    private void Touch(ExternalSettlementSource source, DateTimeOffset at)
    {
        if (source == ExternalSettlementSource.Reconciliation)
        {
            AttemptCount++;
        }

        UpdatedAt = at.ToUniversalTime();
    }
}
