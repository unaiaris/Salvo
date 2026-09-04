using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class ExternalEvaluationReconciliationTests
{
    /// <summary>
    /// A timeout is indeterminate, and reconciliation is what resolves it.
    /// </summary>
    /// <remarks>
    /// The provider accepts the request and then times out, so this API never learns the verdict and
    /// never even learns the identifier. Closing the row in ERROR here would throw away the answer
    /// the provider already has, and the next request would create a second evaluation over there.
    /// Instead the row waits, reconciliation asks again — by reference, which is all it has — and
    /// the order ends with exactly one external evaluation carrying the real verdict.
    /// </remarks>
    [Fact]
    public async Task ATimeoutIsResolvedByReconciliationIntoASingleRow()
    {
        var provider = new TimingOutThenDecidingProvider();
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddSingleton<IAntifraudProvider>(provider),
        };
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference);
        var orderId = orders[ExternalEvaluationTestCorpus.ApprovedReference];

        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId);

        Assert.Equal("PENDING", requested.Evaluation.Status);
        Assert.Equal("TIMEOUT", requested.Evaluation.LastErrorCode);
        Assert.Null(requested.Evaluation.ErrorCode);
        Assert.Null(requested.Evaluation.SettledAt);
        Assert.Null(requested.Evaluation.ExternalEvaluationId);

        var summary = await ExternalEvaluationTestCorpus.ReconcileAsync(client);

        Assert.Equal(1, summary.Examined);
        Assert.Equal(1, summary.Settled);
        Assert.Equal(0, summary.StillPending);

        // The lookup had no identifier to offer, only the reference written during the reservation.
        Assert.Null(provider.LastLookup?.ExternalEvaluationId);
        Assert.Equal(requested.Evaluation.ReferenceId, provider.LastLookup?.ReferenceId);

        var history = await ExternalEvaluationTestCorpus.HistoryAsync(client, orderId);
        var settled = Assert.Single(history.Items);
        Assert.Equal(requested.Evaluation.Id, settled.Id);
        Assert.Equal("DENIED", settled.Status);
        Assert.Equal("RECONCILIATION", settled.SettledBy);
        Assert.NotNull(settled.SettledAt);
        Assert.Equal(1, settled.AttemptCount);
    }

    /// <summary>
    /// The mock resolves its own pending band, so a pending evaluation of the demo corpus has a way
    /// to finish without a callback. Without that, an integrated E6A would leave every pending row
    /// stuck until the next stage.
    /// </summary>
    [Fact]
    public async Task ThePendingBandOfTheMockIsClosedByReconciliation()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference,
            ExternalEvaluationTestCorpus.PendingThenDeniedReference);

        foreach (var orderId in orders.Values)
        {
            Assert.Equal("PENDING", (await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId))
                .Evaluation.Status);
        }

        var summary = await ExternalEvaluationTestCorpus.ReconcileAsync(client);

        Assert.Equal(2, summary.Examined);
        Assert.Equal(2, summary.Settled);
        Assert.Equal(0, summary.Failed);

        // The parity of the remainder decides which verdict a pending evaluation settles into, so
        // whoever writes the fixture controls where the divergences fall.
        var even = Assert.Single((await ExternalEvaluationTestCorpus.HistoryAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference])).Items);
        var odd = Assert.Single((await ExternalEvaluationTestCorpus.HistoryAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingThenDeniedReference])).Items);

        Assert.Equal("APPROVED", even.Status);
        Assert.Equal("DENIED", odd.Status);
        Assert.All([even, odd], evaluation => Assert.Equal("RECONCILIATION", evaluation.SettledBy));
    }

    /// <summary>
    /// The unit of work is one row, and this is why.
    /// </summary>
    /// <remarks>
    /// One evaluation of the sweep is moved by another writer while the sweep is working on it. With
    /// a single transaction over the whole sweep, that conflict would roll back every evaluation
    /// already resolved and the sweep would achieve nothing. Row by row, the loser is counted and
    /// the rest are still settled.
    /// </remarks>
    [Fact]
    public async Task AConflictOnOneEvaluationDoesNotRollBackTheRestOfTheSweep()
    {
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddSingleton<IAntifraudProvider, AlwaysPendingProvider>(),
        };
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference,
            ExternalEvaluationTestCorpus.PendingThenDeniedReference,
            ExternalEvaluationTestCorpus.ApprovedReference);

        foreach (var orderId in orders.Values)
        {
            await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId);
        }

        // Someone else settles one of the three between the sweep reading it and writing it, which
        // is the shape a callback of the next stage will have.
        var hijacked = await PickOnePendingAsync(factory);
        var summary = await ReconcileWithDecidingProviderAsync(factory, hijacked);

        Assert.Equal(3, summary.Examined);
        Assert.Equal(1, summary.Conflicted);
        Assert.Equal(2, summary.Settled);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.ExternalEvaluations.AsNoTracking().ToListAsync();

        Assert.Equal(3, stored.Count);
        Assert.All(stored, evaluation => Assert.True(evaluation.IsSettled));

        // The hijacked row keeps the verdict of whoever got there first: it knew more.
        var untouched = stored.Single(evaluation => evaluation.Id == hijacked);
        Assert.Equal(ExternalSettlementSource.Callback, untouched.SettledBy);
    }

    /// <summary>
    /// The evaluation another writer will take, chosen as the first one the sweep will reach.
    /// </summary>
    private static async Task<Guid> PickOnePendingAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var pending = await dbContext.ExternalEvaluations
            .OrderBy(evaluation => evaluation.RequestedAt)
            .ThenBy(evaluation => evaluation.Id)
            .FirstAsync();

        return pending.Id;
    }

    /// <summary>
    /// Runs the sweep with a provider that decides everything, and that settles
    /// <paramref name="hijacked"/> behind the sweep's back the moment it is asked about it.
    /// </summary>
    private static async Task<ReconciliationSummary> ReconcileWithDecidingProviderAsync(
        SalvoApiFactory factory,
        Guid hijacked)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var reference = (await dbContext.ExternalEvaluations.AsNoTracking().SingleAsync(
            evaluation => evaluation.Id == hijacked)).ReferenceId;

        var handler = new ReconcileExternalEvaluationsHandler(
            new EfExternalEvaluationStore(dbContext),
            new SingleProviderRegistry(new HijackingProvider(factory, reference)),
            ExternalEvaluationOptions.Default,
            TimeProvider.System);

        return await handler.HandleAsync(CancellationToken.None);
    }

    private sealed class TimingOutThenDecidingProvider : IAntifraudProvider
    {
        public ExternalEvaluationLookup? LastLookup { get; private set; }

        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        /// <summary>
        /// The provider accepted the request and then the answer never came back.
        /// </summary>
        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            throw new TimeoutException("The provider accepted the request and did not answer in time.");
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            LastLookup = lookup;

            return Task.FromResult(new ExternalEvaluationResult(
                ExternalProviderOutcome.Denied,
                "EXT-LATE-1",
                Score: 91));
        }
    }

    private sealed class AlwaysPendingProvider : IAntifraudProvider
    {
        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExternalEvaluationResult(ExternalProviderOutcome.Pending));
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExternalEvaluationResult(ExternalProviderOutcome.Pending));
        }
    }

    /// <summary>
    /// Decides every evaluation, and settles the one it is watching for on a separate connection
    /// first — which is exactly the shape of a callback arriving mid-sweep.
    /// </summary>
    private sealed class HijackingProvider(SalvoApiFactory factory, string reference) : IAntifraudProvider
    {
        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExternalEvaluationResult(ExternalProviderOutcome.Approved));
        }

        public async Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            if (string.Equals(lookup.ReferenceId, reference, StringComparison.Ordinal))
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
                var evaluation = await dbContext.ExternalEvaluations.SingleAsync(
                    candidate => candidate.ReferenceId == reference,
                    cancellationToken);
                evaluation.Settle(
                    ExternalEvaluationStatus.Denied,
                    "EXT-CALLBACK-1",
                    score: 99,
                    errorCode: null,
                    ExternalSettlementSource.Callback,
                    DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new(ExternalProviderOutcome.Approved, Score: 1);
        }
    }

    private sealed class SingleProviderRegistry(IAntifraudProvider provider) : IAntifraudProviderRegistry
    {
        public IAntifraudProvider? Find(ExternalProvider requested)
        {
            return requested == provider.Provider ? provider : null;
        }
    }
}
