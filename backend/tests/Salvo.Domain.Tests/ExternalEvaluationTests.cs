using Salvo.Domain.External;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class ExternalEvaluationTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AReservationStartsPendingAndSettlesNothing()
    {
        var evaluation = Reserve();

        Assert.Equal(ExternalEvaluationStatus.Pending, evaluation.Status);
        Assert.False(evaluation.IsSettled);
        Assert.Null(evaluation.ExternalEvaluationId);
        Assert.Null(evaluation.Score);
        Assert.Null(evaluation.ErrorCode);
        Assert.Null(evaluation.LastErrorCode);
        Assert.Null(evaluation.SettledAt);
        Assert.Null(evaluation.SettledBy);
        Assert.Equal(0, evaluation.AttemptCount);
        Assert.Equal(RequestedAt, evaluation.RequestedAt);
        Assert.Equal(RequestedAt, evaluation.UpdatedAt);
    }

    [Fact]
    public void SettlingRecordsTheVerdictItsInstantAndItsProvenance()
    {
        var evaluation = Reserve();

        evaluation.Settle(
            ExternalEvaluationStatus.Denied,
            "EXT-1",
            42,
            errorCode: null,
            ExternalSettlementSource.Sync,
            RequestedAt.AddSeconds(2));

        Assert.Equal(ExternalEvaluationStatus.Denied, evaluation.Status);
        Assert.True(evaluation.IsSettled);
        Assert.Equal("EXT-1", evaluation.ExternalEvaluationId);
        Assert.Equal(42, evaluation.Score);
        Assert.Equal(RequestedAt.AddSeconds(2), evaluation.SettledAt);
        Assert.Equal(ExternalSettlementSource.Sync, evaluation.SettledBy);
    }

    /// <summary>
    /// The verdict of a provider is terminal for the same reason the verdict of an analyst is: a
    /// message that arrives late has nowhere to be recorded, and must not silently overwrite what
    /// was already decided.
    /// </summary>
    [Fact]
    public void ASettledEvaluationRefusesEveryFurtherTransition()
    {
        var evaluation = Reserve();
        evaluation.Settle(
            ExternalEvaluationStatus.Approved,
            "EXT-1",
            score: 10,
            errorCode: null,
            ExternalSettlementSource.Callback,
            RequestedAt);

        Assert.Throws<ExternalEvaluationTransitionException>(() => evaluation.Settle(
            ExternalEvaluationStatus.Denied,
            "EXT-1",
            score: 90,
            errorCode: null,
            ExternalSettlementSource.Reconciliation,
            RequestedAt));
        Assert.Throws<ExternalEvaluationTransitionException>(() => evaluation.RecordPendingAttempt(
            "EXT-1",
            ExternalEvaluationErrorCode.Timeout,
            ExternalSettlementSource.Reconciliation,
            RequestedAt));
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
        Assert.Equal(10, evaluation.Score);
    }

    [Fact]
    public void PendingIsNotAVerdictAndDoesNotSettle()
    {
        var evaluation = Reserve();

        Assert.Throws<ExternalEvaluationTransitionException>(() => evaluation.Settle(
            ExternalEvaluationStatus.Pending,
            externalEvaluationId: null,
            score: null,
            errorCode: null,
            ExternalSettlementSource.Sync,
            RequestedAt));
    }

    /// <summary>
    /// The error code and the status can never tell different stories, which is also what the
    /// database check constraint says.
    /// </summary>
    [Fact]
    public void AnErrorCodeBelongsToAnErrorAndOnlyToAnError()
    {
        Assert.Throws<ArgumentNullException>(() => Reserve().Settle(
            ExternalEvaluationStatus.Error,
            externalEvaluationId: null,
            score: null,
            errorCode: null,
            ExternalSettlementSource.Sync,
            RequestedAt));
        Assert.Throws<ArgumentException>(() => Reserve().Settle(
            ExternalEvaluationStatus.Approved,
            externalEvaluationId: null,
            score: null,
            ExternalEvaluationErrorCode.Timeout,
            ExternalSettlementSource.Sync,
            RequestedAt));
    }

    /// <summary>
    /// A failure after the request was sent is indeterminate: the provider may have registered the
    /// evaluation. Closing it here would lose the real verdict and make the next request create a
    /// second evaluation on the provider side.
    /// </summary>
    [Fact]
    public void AFailedAttemptLeavesTheEvaluationPendingAndCarriesTheReason()
    {
        var evaluation = Reserve();

        evaluation.RecordPendingAttempt(
            externalEvaluationId: null,
            ExternalEvaluationErrorCode.Timeout,
            ExternalSettlementSource.Sync,
            RequestedAt.AddSeconds(30));

        Assert.Equal(ExternalEvaluationStatus.Pending, evaluation.Status);
        Assert.Equal(ExternalEvaluationErrorCode.Timeout, evaluation.LastErrorCode);
        Assert.Null(evaluation.ErrorCode);
        Assert.Null(evaluation.SettledAt);
        Assert.Null(evaluation.SettledBy);
        Assert.Equal(RequestedAt.AddSeconds(30), evaluation.UpdatedAt);
    }

    /// <summary>
    /// The counter measures how often reconciliation went looking, not how often the request was
    /// retried: a retry is a new row, so that number would always be one.
    /// </summary>
    [Fact]
    public void OnlyReconciliationProbesAreCounted()
    {
        var evaluation = Reserve();

        evaluation.RecordPendingAttempt(null, null, ExternalSettlementSource.Sync, RequestedAt);
        Assert.Equal(0, evaluation.AttemptCount);

        evaluation.RecordPendingAttempt(null, null, ExternalSettlementSource.Reconciliation, RequestedAt);
        evaluation.RecordPendingAttempt(null, null, ExternalSettlementSource.Reconciliation, RequestedAt);
        Assert.Equal(2, evaluation.AttemptCount);

        evaluation.Settle(
            ExternalEvaluationStatus.Denied,
            null,
            score: null,
            errorCode: null,
            ExternalSettlementSource.Reconciliation,
            RequestedAt);
        Assert.Equal(3, evaluation.AttemptCount);
    }

    [Fact]
    public void TheProviderIdentifierIsAdoptedOnceAndNeverReplaced()
    {
        var evaluation = Reserve();

        evaluation.RecordPendingAttempt("EXT-1", null, ExternalSettlementSource.Sync, RequestedAt);
        evaluation.RecordPendingAttempt("EXT-1", null, ExternalSettlementSource.Reconciliation, RequestedAt);
        Assert.Equal("EXT-1", evaluation.ExternalEvaluationId);

        // A blank identifier is the provider not saying anything, not the provider retracting.
        evaluation.RecordPendingAttempt("  ", null, ExternalSettlementSource.Reconciliation, RequestedAt);
        Assert.Equal("EXT-1", evaluation.ExternalEvaluationId);

        Assert.Throws<ExternalEvaluationTransitionException>(() => evaluation.RecordPendingAttempt(
            "EXT-2",
            null,
            ExternalSettlementSource.Reconciliation,
            RequestedAt));
    }

    [Fact]
    public void AReservationRejectsInputThatCannotIdentifyItself()
    {
        Assert.Throws<ArgumentException>(() => ExternalEvaluation.Reserve(
            Guid.Empty,
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            "MER|ORD",
            RequestedAt));
        Assert.Throws<ArgumentException>(() => ExternalEvaluation.Reserve(
            Guid.NewGuid(),
            Guid.Empty,
            ExternalProvider.ExternalMock,
            "MER|ORD",
            RequestedAt));
        Assert.Throws<ArgumentException>(() => ExternalEvaluation.Reserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            " ",
            RequestedAt));
        Assert.Throws<ArgumentException>(() => ExternalEvaluation.Reserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            new string('A', ExternalEvaluationReference.MaximumLength + 1),
            RequestedAt));
    }

    [Fact]
    public void TheReferenceIsThePairAnOrderIsCommerciallyIdentifiedBy()
    {
        var reference = new OrderReference("MER_UY_STORE", "ORD_000042");

        Assert.Equal("MER_UY_STORE|ORD_000042", ExternalEvaluationReference.From(reference));
    }

    /// <summary>
    /// The separation the whole design rests on, asserted at the type level. Sharing an enumeration
    /// with the local evaluation would reintroduce exactly the mixture that separate tables prevent.
    /// </summary>
    [Fact]
    public void TheExternalEvaluationSharesNoTypeWithTheLocalOne()
    {
        Assert.All(
            typeof(ExternalEvaluation).GetProperties(),
            property => Assert.NotEqual(
                typeof(RiskEvaluation).Namespace,
                Nullable.GetUnderlyingType(property.PropertyType)?.Namespace ?? property.PropertyType.Namespace));

        Assert.NotEqual(typeof(RiskEvaluationStatus), typeof(ExternalEvaluationStatus));
        Assert.NotEqual(typeof(RiskEvaluationSource), typeof(ExternalProvider));
    }

    private static ExternalEvaluation Reserve()
    {
        return ExternalEvaluation.Reserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExternalProvider.ExternalMock,
            "MER_UY_STORE|ORD_000042",
            RequestedAt);
    }
}
