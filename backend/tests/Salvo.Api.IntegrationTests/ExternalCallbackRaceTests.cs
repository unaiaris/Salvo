using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The two interleavings this stage rests on, pinned so they are proofs rather than coin flips.
/// </summary>
public sealed class ExternalCallbackRaceTests
{
    /// <summary>
    /// The callback that arrives before there is anything to attach it to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The provider decides and delivers while the request that asked it is still in flight, and it
    /// identifies its message only by the identifier it is on the point of returning. Reserving the
    /// row first does not help: the row exists, but it does not carry that identifier yet, and the
    /// message carries no order reference to fall back on. The callback therefore correlates with
    /// nothing and is recorded as unmatched — which the first assertion here demands, so that the
    /// test cannot pass by never entering the window it is about.
    /// </para>
    /// <para>
    /// What closes it is late linking at the end of the request. Remove that one call from
    /// <c>RequestExternalEvaluationHandler</c> and the evaluation stays <c>PENDING</c> forever with
    /// its verdict sitting unread in the receipts table, and this test fails on the status.
    /// Reconciliation cannot rescue it either: this provider answers <c>PENDING</c> to every probe.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ACallbackThatArrivesBeforeTheIdentifierIsWrittenDownStillSettlesTheEvaluation()
    {
        const string identifier = "MOCK-CALLBACK-FIRST";

        await using var factory = new SalvoApiFactory();
        CallbackBeforeCommitProvider? provider = null;
        factory.ConfigureTestServices = services =>
            services.AddScoped<IAntifraudProvider>(scope =>
                provider = new CallbackBeforeCommitProvider(factory.Services, identifier));

        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);

        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);

        // The window really was entered: when the message arrived, nothing could be correlated.
        Assert.NotNull(provider);
        Assert.Equal(CallbackReceiptWireNames.Unmatched, provider.Outcome?.ReceiptStatus);

        // And it was closed before the request answered.
        Assert.Equal(ExternalEvaluationWireNames.Denied, requested.Evaluation.Status);
        Assert.Equal(ExternalEvaluationWireNames.Callback, requested.Evaluation.SettledBy);
        Assert.Equal(identifier, requested.Evaluation.ExternalEvaluationId);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal(CallbackReceiptStatus.Applied, receipt.Status);
        Assert.NotNull(receipt.ProcessedAt);

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(
            factory,
            requested.Evaluation.Id);
        Assert.Equal(ExternalEvaluationStatus.Denied, evaluation.Status);
        Assert.Equal(ExternalSettlementSource.Callback, evaluation.SettledBy);
    }

    /// <summary>
    /// A callback and a reconciliation sweep reaching for the same row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sweep reads the pending row, and while it is asking the provider, the callback settles
    /// that same row from another connection. The status is the concurrency token, so the sweep's
    /// write is refused rather than overwriting a verdict it never saw. One transition, one receipt,
    /// and the sweep says so: this sweep had exactly one row and lost it, which is the case E6A
    /// answers with 409 rather than a summary of zeros.
    /// </para>
    /// <para>
    /// A file database, because the shared in-memory connection of the default factory serialises
    /// every scope and this race could not even be expressed on it.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AReconciliationThatLosesToACallbackLeavesOneTransitionAndOneReceipt()
    {
        await using var factory = SalvoApiFactory.WithFileDatabase();
        factory.ConfigureTestServices = services =>
            services.AddScoped<IAntifraudProvider>(scope => new ReservingThenHijackedProvider(factory));

        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);
        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);
        Assert.Equal(ExternalEvaluationWireNames.Pending, requested.Evaluation.Status);

        using var sweep = await client.PostAsync("/api/external-evaluations:reconcile", null);

        Assert.Equal(HttpStatusCode.Conflict, sweep.StatusCode);
        Assert.Equal("RECONCILIATION_CONFLICT", await ExternalEvaluationTestCorpus.ReadCodeAsync(sweep));

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(
            factory,
            requested.Evaluation.Id);
        Assert.Equal(ExternalEvaluationStatus.Denied, evaluation.Status);

        // The callback won, and the sweep did not get to write a second settlement over it.
        Assert.Equal(ExternalSettlementSource.Callback, evaluation.SettledBy);
        Assert.Equal(0, evaluation.AttemptCount);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal(CallbackReceiptStatus.Applied, receipt.Status);
    }

    /// <summary>
    /// A provider that answers its request by callback, from inside the call, and then reports that
    /// it is still thinking. Its probe never resolves, so nothing but late linking can settle the row.
    /// </summary>
    private sealed class CallbackBeforeCommitProvider(IServiceProvider services, string identifier)
        : IAntifraudProvider
    {
        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        /// <summary>What the callback was told at the moment it arrived.</summary>
        public ExternalCallbackOutcome? Outcome { get; private set; }

        public async Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            await using var scope = services.CreateAsyncScope();
            var callbacks = scope.ServiceProvider.GetRequiredService<ApplyExternalCallbackHandler>();

            // Identified only by the provider's own identifier, which the caller has not been told
            // yet. No order reference: this models a provider that does not echo one.
            Outcome = await callbacks.HandleAsync(
                ExternalProvider.ExternalMock,
                new(
                    identifier,
                    ReferenceId: null,
                    ExternalEvaluationStatus.Denied,
                    Score: 88,
                    ExternalCallbackTestCorpus.ProviderInstant),
                cancellationToken);

            return new(ExternalProviderOutcome.Pending, identifier);
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExternalEvaluationResult(ExternalProviderOutcome.Pending, identifier));
        }
    }

    /// <summary>
    /// Pending on the way in, and on the way out it settles the row by callback before answering the
    /// sweep. That is the interleaving, made deterministic: the sweep is holding a row that is
    /// already stale by the time it tries to write it.
    /// </summary>
    private sealed class ReservingThenHijackedProvider(SalvoApiFactory factory) : IAntifraudProvider
    {
        private const string Identifier = "MOCK-HIJACKED-BY-CALLBACK";

        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExternalEvaluationResult(ExternalProviderOutcome.Pending, Identifier));
        }

        public async Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var callbacks = scope.ServiceProvider.GetRequiredService<ApplyExternalCallbackHandler>();
            await callbacks.HandleAsync(
                ExternalProvider.ExternalMock,
                new(
                    Identifier,
                    lookup.ReferenceId,
                    ExternalEvaluationStatus.Denied,
                    Score: 64,
                    ExternalCallbackTestCorpus.ProviderInstant),
                cancellationToken);

            return new(ExternalProviderOutcome.Approved, Identifier, Score: 11);
        }
    }
}
