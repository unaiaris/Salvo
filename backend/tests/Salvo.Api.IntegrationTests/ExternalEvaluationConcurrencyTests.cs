using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Domain.Orders;
using Salvo.Infrastructure.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class ExternalEvaluationConcurrencyTests
{
    /// <summary>
    /// The proof that the reservation precedes the call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two requests for the same order, forced to both read "this order has no evaluation" before
    /// either writes. One wins the partial unique index and one is refused. That much would also
    /// happen if the provider were called first — the loser would still get its 409, after the
    /// provider had already created a second evaluation, sent a second callback and charged for it.
    /// </para>
    /// <para>
    /// So the assertion that matters is the counter: the provider is invoked <em>once</em>. Invert
    /// the two phases in <c>RequestExternalEvaluationHandler</c> and this test fails on that count
    /// while every other assertion still passes.
    /// </para>
    /// <para>
    /// A file database, because the shared in-memory connection of the default factory serializes
    /// every scope and the race could not even be expressed on it.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task TwoConcurrentRequestsCallTheProviderOnceAndLeaveOneEvaluation()
    {
        using var gate = new ReservationGate();
        var counter = new ProviderCallCounter();

        await using var factory = SalvoApiFactory.WithFileDatabase();
        factory.ConfigureTestServices = services =>
        {
            services.AddSingleton(counter);
            services.AddScoped<IAntifraudProvider>(provider => new CountingAntifraudProvider(
                new MockAntifraudProvider(MockAntifraudProviderOptions.Default, TimeProvider.System),
                counter));
            services.AddScoped<IExternalEvaluationStore>(provider => new GatedExternalEvaluationStore(
                new EfExternalEvaluationStore(provider.GetRequiredService<SalvoDbContext>()),
                gate));
        };
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference);
        var orderId = orders[ExternalEvaluationTestCorpus.ApprovedReference];

        var responses = await Task.WhenAll(
            ExternalEvaluationTestCorpus.RequestAsync(client, orderId),
            ExternalEvaluationTestCorpus.RequestAsync(client, orderId));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(response => response.StatusCode).OrderBy(status => (int)status));

        var refused = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(
            "EXTERNAL_EVALUATION_PENDING",
            await ExternalEvaluationTestCorpus.ReadCodeAsync(refused));

        // The whole point. The request that lost was refused before anything was asked of the
        // provider, so the provider only ever saw one evaluation of this order.
        Assert.Equal(1, counter.Count);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var evaluation = await dbContext.ExternalEvaluations.AsNoTracking().SingleAsync();
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
        Assert.Equal(ExternalSettlementSource.Sync, evaluation.SettledBy);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Forces both requests to finish reading before either writes, and then lets them write one at
    /// a time. Without it the interleaving would depend on the scheduler and the test would be
    /// flaky rather than a proof.
    /// </summary>
    /// <summary>
    /// Pins the interleaving so the test is a proof rather than a coin flip.
    /// </summary>
    /// <remarks>
    /// Two barriers, and the second one matters as much as the first. Both requests must finish
    /// reading before either reserves, and both must finish reserving before either settles:
    /// the index that decides the race only covers rows that are still pending, so a winner allowed
    /// to settle first would leave nothing for the loser to collide with.
    /// </remarks>
    private sealed class ReservationGate : IDisposable
    {
        private readonly TaskCompletionSource bothLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource bothReserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim writeLock = new(1, 1);
        private int loaded;
        private int reserved;

        public async Task WaitForBothLoadsAsync()
        {
            if (Interlocked.Increment(ref loaded) == 2)
            {
                bothLoaded.TrySetResult();
            }

            await bothLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        /// <summary>Signalled by both requests, whether their reservation was accepted or refused.</summary>
        public void ReservationAttempted()
        {
            if (Interlocked.Increment(ref reserved) == 2)
            {
                bothReserved.TrySetResult();
            }
        }

        public Task WaitForBothReservationsAsync()
        {
            return bothReserved.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public Task EnterWriteAsync() => writeLock.WaitAsync();

        public void ExitWrite() => writeLock.Release();

        public void Dispose() => writeLock.Dispose();
    }

    private sealed class ProviderCallCounter
    {
        private int count;

        public int Count => Volatile.Read(ref count);

        public void Increment() => Interlocked.Increment(ref count);
    }

    private sealed class CountingAntifraudProvider(IAntifraudProvider inner, ProviderCallCounter counter)
        : IAntifraudProvider
    {
        public ExternalProvider Provider => inner.Provider;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            counter.Increment();

            return inner.EvaluateAsync(input, cancellationToken);
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            return inner.GetStatusAsync(lookup, cancellationToken);
        }
    }

    private sealed class GatedExternalEvaluationStore(IExternalEvaluationStore inner, ReservationGate gate)
        : IExternalEvaluationStore
    {
        public Task<Order?> FindOrderAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return inner.FindOrderAsync(orderId, cancellationToken);
        }

        public async Task<ExternalEvaluation?> FindCurrentAsync(
            Guid orderId,
            ExternalProvider provider,
            CancellationToken cancellationToken)
        {
            var current = await inner.FindCurrentAsync(orderId, provider, cancellationToken);
            await gate.WaitForBothLoadsAsync();

            return current;
        }

        public Task<ExternalEvaluation?> FindAsync(Guid id, CancellationToken cancellationToken)
        {
            return inner.FindAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<ExternalEvaluation>> ListByOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            return inner.ListByOrderAsync(orderId, cancellationToken);
        }

        public Task<IReadOnlyList<ExternalEvaluation>> ListPendingAsync(
            DateTimeOffset requestedBefore,
            CancellationToken cancellationToken)
        {
            return inner.ListPendingAsync(requestedBefore, cancellationToken);
        }

        public Task<IReadOnlyList<Guid>> ListOrdersWithoutEvaluationAsync(
            ExternalProvider provider,
            int limit,
            CancellationToken cancellationToken)
        {
            return inner.ListOrdersWithoutEvaluationAsync(provider, limit, cancellationToken);
        }

        public async Task ReserveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken)
        {
            await gate.EnterWriteAsync();
            try
            {
                await inner.ReserveAsync(evaluation, cancellationToken);
            }
            finally
            {
                gate.ReservationAttempted();
                gate.ExitWrite();
            }
        }

        public async Task SaveAsync(ExternalEvaluation evaluation, CancellationToken cancellationToken)
        {
            await gate.WaitForBothReservationsAsync();
            await inner.SaveAsync(evaluation, cancellationToken);
        }
    }
}
