using Salvo.Domain.External;

namespace Salvo.Domain.Tests;

public sealed class CallbackReceiptTests
{
    private static readonly DateTimeOffset Instant = new(2026, 8, 21, 9, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// The key is what two implementations have to agree on, so its text is pinned rather than left
    /// to whatever <c>string.Join</c> happens to produce.
    /// </summary>
    [Fact]
    public void TheDeduplicationKeyIsTheCanonicalTextTheDesignFixes()
    {
        var key = CallbackDeduplicationKey.From(
            ExternalProvider.ExternalMock,
            "MOCK-ABC",
            referenceId: "MER|ORD_1",
            ExternalEvaluationStatus.Approved,
            Instant);

        Assert.Equal("EXTERNAL_MOCK|MOCK-ABC|APPROVED|2026-08-21T09:30:00.000Z", key);
    }

    /// <summary>
    /// The instant of reception is never part of it. Including it would make every redelivery look
    /// like a message nobody had seen before, which is the one thing the key exists to prevent.
    /// </summary>
    [Fact]
    public void TheSameMessageProducesTheSameKeyWheneverItArrives()
    {
        var first = CallbackReceipt.Record(
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            "MOCK-ABC",
            referenceId: null,
            ExternalEvaluationStatus.Denied,
            reportedScore: 40,
            Instant,
            CallbackReceiptStatus.Applied,
            receivedAt: Instant.AddSeconds(1));
        var later = CallbackReceipt.Record(
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            "MOCK-ABC",
            referenceId: null,
            ExternalEvaluationStatus.Denied,
            reportedScore: 40,
            Instant,
            CallbackReceiptStatus.Applied,
            receivedAt: Instant.AddHours(9));

        Assert.Equal(first.DeduplicationKey, later.DeduplicationKey);
    }

    /// <summary>
    /// Two providers may mint the same identifier, and a key that did not name the provider would
    /// make one of them discard the other's callbacks as duplicates.
    /// </summary>
    [Fact]
    public void TwoProvidersSayingTheSameThingDoNotShareAKey()
    {
        var mock = CallbackDeduplicationKey.From(
            ExternalProvider.ExternalMock,
            "ID-1",
            referenceId: null,
            ExternalEvaluationStatus.Approved,
            Instant);
        var koin = CallbackDeduplicationKey.From(
            ExternalProvider.KoinSandbox,
            "ID-1",
            referenceId: null,
            ExternalEvaluationStatus.Approved,
            Instant);

        Assert.NotEqual(mock, koin);
    }

    /// <summary>
    /// With no provider identifier the correlating slot falls back to the order reference, tagged so
    /// the two can never be confused. Leaving it empty would give every identifier-less message of a
    /// given status and instant the same key, and a callback about one order would be discarded as a
    /// duplicate of a callback about another.
    /// </summary>
    [Fact]
    public void WithoutAnIdentifierTwoOrdersStillGetDifferentKeys()
    {
        var first = CallbackDeduplicationKey.From(
            ExternalProvider.ExternalMock,
            externalEvaluationId: null,
            "MER|ORD_1",
            ExternalEvaluationStatus.Approved,
            Instant);
        var second = CallbackDeduplicationKey.From(
            ExternalProvider.ExternalMock,
            externalEvaluationId: null,
            "MER|ORD_2",
            ExternalEvaluationStatus.Approved,
            Instant);

        Assert.NotEqual(first, second);
        Assert.Contains(CallbackDeduplicationKey.ReferencePrefix, first, StringComparison.Ordinal);
    }

    [Fact]
    public void AMessageWithNothingToCorrelateByIsRefused()
    {
        Assert.Throws<ArgumentException>(() => CallbackReceipt.Record(
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            externalEvaluationId: null,
            referenceId: null,
            ExternalEvaluationStatus.Approved,
            reportedScore: null,
            Instant,
            CallbackReceiptStatus.Unmatched,
            Instant));
    }

    /// <summary>
    /// Unmatched means unprocessed, and resolving is what makes it processed. The two can never say
    /// different things, which the database also enforces.
    /// </summary>
    [Fact]
    public void AnUnmatchedReceiptIsUnprocessedUntilItFindsItsEvaluation()
    {
        var receipt = Unmatched();

        Assert.Null(receipt.ProcessedAt);

        receipt.Resolve(CallbackReceiptStatus.Applied, Instant.AddMinutes(1));

        Assert.Equal(CallbackReceiptStatus.Applied, receipt.Status);
        Assert.NotNull(receipt.ProcessedAt);
    }

    /// <summary>
    /// A receipt is settled once, by whichever path found its evaluation first. Late linking and a
    /// reconciliation sweep can both reach the same one.
    /// </summary>
    [Fact]
    public void AReceiptIsNotResolvedTwice()
    {
        var receipt = Unmatched();
        receipt.Resolve(CallbackReceiptStatus.Applied, Instant);

        Assert.Throws<ExternalEvaluationTransitionException>(
            () => receipt.Resolve(CallbackReceiptStatus.NoOp, Instant));
    }

    [Fact]
    public void AReplayAddsEvidenceAndChangesNothingElse()
    {
        var receipt = Unmatched();
        var received = receipt.ReceivedAt;

        receipt.RecordReplay(Instant.AddHours(2));

        Assert.Equal(1, receipt.ReplayCount);
        Assert.Equal(received, receipt.ReceivedAt);
        Assert.Equal(Instant.AddHours(2), receipt.LastSeenAt);
        Assert.Equal(CallbackReceiptStatus.Unmatched, receipt.Status);
    }

    private static CallbackReceipt Unmatched()
    {
        return CallbackReceipt.Record(
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            "MOCK-ABC",
            referenceId: null,
            ExternalEvaluationStatus.Approved,
            reportedScore: 7,
            Instant,
            CallbackReceiptStatus.Unmatched,
            Instant);
    }
}
