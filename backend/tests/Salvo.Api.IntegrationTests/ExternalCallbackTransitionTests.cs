using System.Net;
using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The transition table of section 4.5, exercised through the endpoint an external provider reaches.
/// </summary>
/// <remarks>
/// Every case here is one row of that table. The two that matter most are the last pair: a
/// non-terminal message after a verdict is the network delivering out of order and is discarded,
/// while a <em>different</em> verdict after a verdict is the provider contradicting itself and is
/// kept. An earlier draft of the design treated both as "discarded with a marker", which would have
/// swallowed the contradiction in silence.
/// </remarks>
public sealed class ExternalCallbackTransitionTests
{
    /// <summary>
    /// A redelivery. The provider retries, the key collides, and the effect happens once.
    /// </summary>
    /// <remarks>
    /// The count of receipts is the assertion that could catch a design regression: if the
    /// deduplication became a read before the write, or if a DUPLICATE state were introduced, there
    /// would be two rows here.
    /// </remarks>
    [Fact]
    public async Task TheSameCallbackTwiceLeavesOneTransitionAndOneReceipt()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var pending = await ReservePendingAsync(client);

        var first = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Denied,
            score: 71);
        var second = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Denied,
            score: 71);

        Assert.Equal(CallbackReceiptWireNames.Applied, first.ReceiptStatus);
        Assert.False(first.IsReplay);
        Assert.Equal(CallbackReceiptWireNames.Applied, second.ReceiptStatus);
        Assert.True(second.IsReplay);
        Assert.Equal(1, second.ReplayCount);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal(CallbackReceiptStatus.Applied, receipt.Status);
        Assert.Equal(1, receipt.ReplayCount);
        Assert.True(receipt.LastSeenAt >= receipt.ReceivedAt);

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(factory, pending.Id);
        Assert.Equal(ExternalEvaluationStatus.Denied, evaluation.Status);
        Assert.Equal(ExternalSettlementSource.Callback, evaluation.SettledBy);
        Assert.Equal(71, evaluation.Score);
    }

    /// <summary>
    /// A resend of the same verdict with a different instant is not a duplicate — the key differs —
    /// and must be a no-op rather than a conflict.
    /// </summary>
    [Fact]
    public async Task TheSameVerdictWithAnotherInstantIsANoOp()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var pending = await ReservePendingAsync(client);

        await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Approved);
        var resent = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Approved,
            occurredAt: ExternalCallbackTestCorpus.ProviderInstant.AddMinutes(5));

        Assert.Equal(CallbackReceiptWireNames.NoOp, resent.ReceiptStatus);
        Assert.False(resent.IsReplay);

        var receipts = await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory);
        Assert.Equal(
            [CallbackReceiptStatus.Applied, CallbackReceiptStatus.NoOp],
            receipts.Select(receipt => receipt.Status));

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(factory, pending.Id);
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
    }

    /// <summary>
    /// Out of order. The network delivered a stale non-terminal message after the verdict, and it
    /// changes nothing — but it is recorded as what it is, not as a contradiction.
    /// </summary>
    [Fact]
    public async Task APendingCallbackAfterAVerdictIsSuperseded()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var pending = await ReservePendingAsync(client);

        await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Approved);
        var late = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Pending);

        Assert.Equal(CallbackReceiptWireNames.Superseded, late.ReceiptStatus);
        Assert.Equal(ExternalEvaluationWireNames.Approved, late.EvaluationStatus);

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(factory, pending.Id);
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
        Assert.NotNull(evaluation.SettledAt);
    }

    /// <summary>
    /// The provider says two different things. The first verdict stands, and the disagreement is
    /// kept so that the console can show it.
    /// </summary>
    [Fact]
    public async Task ADifferentVerdictAfterAVerdictIsRecordedAsConflicting()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var pending = await ReservePendingAsync(client);

        await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Approved);
        var contradiction = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            pending.ExternalEvaluationId,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Denied);

        Assert.Equal(CallbackReceiptWireNames.Conflicting, contradiction.ReceiptStatus);

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(factory, pending.Id);
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
        Assert.Equal(ExternalSettlementSource.Callback, evaluation.SettledBy);

        var receipts = await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory);
        Assert.Equal(
            [CallbackReceiptStatus.Applied, CallbackReceiptStatus.Conflicting],
            receipts.Select(receipt => receipt.Status));
    }

    /// <summary>
    /// A message about an order this API has never sent anywhere. It is kept, because it may belong
    /// to a row that is about to exist, and answered 202 rather than an error the provider would
    /// retry against forever.
    /// </summary>
    [Fact]
    public async Task AMessageThatCorrelatesWithNothingIsAcceptedAndKept()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var response = await ExternalCallbackTestCorpus.PostAsync(
            client,
            "MOCK-NEVER-SEEN",
            referenceId: null,
            ExternalEvaluationWireNames.Approved);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal(CallbackReceiptStatus.Unmatched, receipt.Status);
        Assert.Null(receipt.ProcessedAt);
    }

    /// <summary>
    /// Correlation by order reference, which is the half that exists from the moment the row is
    /// reserved and the only one available when the provider never sent an identifier.
    /// </summary>
    [Fact]
    public async Task ACallbackWithOnlyTheOrderReferenceStillFindsItsEvaluation()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var pending = await ReservePendingAsync(client);

        var outcome = await ExternalCallbackTestCorpus.PostOkAsync(
            client,
            externalEvaluationId: null,
            pending.ReferenceId,
            ExternalEvaluationWireNames.Denied);

        Assert.Equal(CallbackReceiptWireNames.Applied, outcome.ReceiptStatus);
        Assert.Equal(pending.Id, outcome.ExternalEvaluationId);

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(factory, pending.Id);
        Assert.Equal(ExternalEvaluationStatus.Denied, evaluation.Status);
    }

    /// <summary>
    /// An order whose reference puts it in the pending band of the mock, so the request leaves a row
    /// waiting for an answer and there is something for a callback to move.
    /// </summary>
    private static async Task<ExternalEvaluationView> ReservePendingAsync(HttpClient client)
    {
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);
        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);

        Assert.Equal(ExternalEvaluationWireNames.Pending, requested.Evaluation.Status);

        return requested.Evaluation;
    }
}
