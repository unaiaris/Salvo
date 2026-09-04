namespace Salvo.Domain.External;

/// <summary>
/// The record that one callback message was received, and what was done about it.
/// </summary>
/// <remarks>
/// <para>
/// A receipt exists so that a provider which redelivers — and every provider redelivers — cannot
/// cause an effect twice. It is written in the <em>same</em> unit of work as the transition it
/// describes. Written separately, a transition that lost a race would leave the receipt behind, the
/// redelivery would be recognised as a duplicate, the provider would be answered <c>200</c>, and the
/// verdict would be lost for good.
/// </para>
/// <para>
/// The message itself is not stored: only the fields that correlate it, the verdict it reported and
/// the instant the provider stamped on it. Those three are what late linking needs to apply a
/// receipt whose row did not exist yet; anything more would be persisting a foreign payload.
/// </para>
/// </remarks>
public sealed class CallbackReceipt
{
    private CallbackReceipt()
    {
        DeduplicationKey = string.Empty;
    }

    private CallbackReceipt(
        Guid id,
        ExternalProvider provider,
        string deduplicationKey,
        string? externalEvaluationId,
        string? referenceId,
        ExternalEvaluationStatus reportedStatus,
        int? reportedScore,
        DateTimeOffset? providerInstant,
        CallbackReceiptStatus status,
        DateTimeOffset receivedAt)
    {
        Id = id;
        Provider = provider;
        DeduplicationKey = deduplicationKey;
        ExternalEvaluationId = externalEvaluationId;
        ReferenceId = referenceId;
        ReportedStatus = reportedStatus;
        ReportedScore = reportedScore;
        ProviderInstant = providerInstant?.ToUniversalTime();
        Status = status;
        ReplayCount = 0;
        ReceivedAt = receivedAt;
        LastSeenAt = receivedAt;
        ProcessedAt = status == CallbackReceiptStatus.Unmatched ? null : receivedAt;
    }

    public Guid Id { get; private set; }

    public ExternalProvider Provider { get; private set; }

    /// <summary>Unique within its provider. See <see cref="CallbackDeduplicationKey"/>.</summary>
    public string DeduplicationKey { get; private set; }

    /// <summary>The identifier the provider used, when it sent one.</summary>
    public string? ExternalEvaluationId { get; private set; }

    /// <summary>The order reference the provider echoed, when it echoed one.</summary>
    public string? ReferenceId { get; private set; }

    /// <summary>The verdict the message carried, which is not the same as what was done with it.</summary>
    public ExternalEvaluationStatus ReportedStatus { get; private set; }

    /// <summary>The score the message carried, on the scale of the provider.</summary>
    public int? ReportedScore { get; private set; }

    /// <summary>When the provider says it decided. Absent when the provider does not say.</summary>
    public DateTimeOffset? ProviderInstant { get; private set; }

    public CallbackReceiptStatus Status { get; private set; }

    /// <summary>
    /// How many times this exact message arrived again. Counted rather than ignored: a provider
    /// redelivering forever is a symptom worth being able to see.
    /// </summary>
    public int ReplayCount { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>
    /// When the message was resolved against an evaluation. Null exactly while the receipt is
    /// <see cref="CallbackReceiptStatus.Unmatched"/>.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    public bool IsUnmatched => Status == CallbackReceiptStatus.Unmatched;

    public static CallbackReceipt Record(
        Guid id,
        ExternalProvider provider,
        string? externalEvaluationId,
        string? referenceId,
        ExternalEvaluationStatus reportedStatus,
        int? reportedScore,
        DateTimeOffset? providerInstant,
        CallbackReceiptStatus status,
        DateTimeOffset receivedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(externalEvaluationId) && string.IsNullOrWhiteSpace(referenceId))
        {
            throw new ArgumentException(
                "A callback must carry at least one correlating value: a provider identifier or an "
                + "order reference.",
                nameof(externalEvaluationId));
        }

        if (reportedScore is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reportedScore),
                reportedScore,
                "reportedScore must not be negative.");
        }

        // Both throw on an unknown member, so an enumeration that grew without its wire name cannot
        // reach the database.
        _ = ExternalEvaluationWireNames.ToWire(provider);
        _ = ExternalEvaluationWireNames.ToWire(reportedStatus);
        _ = CallbackReceiptWireNames.ToWire(status);

        var key = CallbackDeduplicationKey.From(
            provider,
            externalEvaluationId,
            referenceId,
            reportedStatus,
            providerInstant);

        if (key.Length > CallbackDeduplicationKey.MaximumLength)
        {
            throw new ArgumentException(
                $"The deduplication key must not exceed {CallbackDeduplicationKey.MaximumLength} "
                + "characters.",
                nameof(externalEvaluationId));
        }

        return new(
            id,
            provider,
            key,
            Normalize(externalEvaluationId),
            Normalize(referenceId),
            reportedStatus,
            reportedScore,
            providerInstant,
            status,
            receivedAt.ToUniversalTime());
    }

    /// <summary>
    /// Records that the same message arrived again. The receipt is not rewritten beyond this: the
    /// first arrival is what decided the effect, and the replay only ever adds evidence.
    /// </summary>
    public void RecordReplay(DateTimeOffset at)
    {
        ReplayCount++;
        LastSeenAt = at.ToUniversalTime();
    }

    /// <summary>
    /// Resolves an unmatched receipt once its evaluation exists.
    /// </summary>
    /// <exception cref="ExternalEvaluationTransitionException">
    /// The receipt was already resolved. A receipt is settled once, by whichever path found the row
    /// first.
    /// </exception>
    public void Resolve(CallbackReceiptStatus status, DateTimeOffset at)
    {
        if (!IsUnmatched)
        {
            throw new ExternalEvaluationTransitionException(
                $"Callback receipt {Id} was already resolved as "
                + $"'{CallbackReceiptWireNames.ToWire(Status)}'.");
        }

        if (status == CallbackReceiptStatus.Unmatched)
        {
            throw new ArgumentException(
                "Resolving a receipt means it found its evaluation.",
                nameof(status));
        }

        _ = CallbackReceiptWireNames.ToWire(status);

        Status = status;
        ProcessedAt = at.ToUniversalTime();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
