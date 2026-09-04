using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.External;
using Salvo.Domain.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class ExternalEvaluationRequestTests
{
    [Fact]
    public async Task AVerdictIsRecordedWithItsProvenanceAndItsIdentifier()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference,
            ExternalEvaluationTestCorpus.DeniedReference);

        var approved = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.ApprovedReference]);
        var denied = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.DeniedReference]);

        Assert.True(approved.Applied);
        Assert.Equal("APPROVED", approved.Evaluation.Status);
        Assert.Equal("SYNC", approved.Evaluation.SettledBy);
        Assert.NotNull(approved.Evaluation.SettledAt);
        Assert.NotNull(approved.Evaluation.ExternalEvaluationId);

        // The reference is written during the reservation, before the provider is called, and it is
        // the pair an order is commercially identified by.
        Assert.Equal(
            $"{ExternalEvaluationTestCorpus.Merchant}|{ExternalEvaluationTestCorpus.ApprovedReference}",
            approved.Evaluation.ReferenceId);
        Assert.Equal("DENIED", denied.Evaluation.Status);
    }

    /// <summary>
    /// A double click must not become two evaluations on the provider side.
    /// </summary>
    [Fact]
    public async Task RepeatingTheRequestChangesNothingAndSaysSo()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference);
        var orderId = orders[ExternalEvaluationTestCorpus.ApprovedReference];

        var first = await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId);
        var second = await ExternalEvaluationTestCorpus.RequestOkAsync(client, orderId);

        Assert.True(first.Applied);
        Assert.False(second.Applied);
        Assert.Equal(first.Evaluation.Id, second.Evaluation.Id);
        Assert.Single((await ExternalEvaluationTestCorpus.HistoryAsync(client, orderId)).Items);
    }

    /// <summary>
    /// A request that never left is the one failure it is safe to close on: the provider has no
    /// evaluation of its own to tell us about later.
    /// </summary>
    [Fact]
    public async Task ARequestThatNeverLeftSettlesInErrorAndNamesWhy()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.UnreachableReference,
            ExternalEvaluationTestCorpus.RejectedReference);

        var unreachable = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.UnreachableReference]);
        var rejected = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.RejectedReference]);

        Assert.Equal("ERROR", unreachable.Evaluation.Status);
        Assert.Equal("UNREACHABLE", unreachable.Evaluation.ErrorCode);
        Assert.NotNull(unreachable.Evaluation.SettledAt);
        Assert.Equal("ERROR", rejected.Evaluation.Status);
        Assert.Equal("PROVIDER_REJECTED", rejected.Evaluation.ErrorCode);
    }

    /// <summary>
    /// An accepted request that has no answer yet stays pending, with nothing settled.
    /// </summary>
    [Fact]
    public async Task AnAcceptedRequestWithNoAnswerYetStaysPending()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);

        var pending = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);

        Assert.Equal("PENDING", pending.Evaluation.Status);
        Assert.Null(pending.Evaluation.SettledAt);
        Assert.Null(pending.Evaluation.SettledBy);
        Assert.Null(pending.Evaluation.ErrorCode);
        Assert.Equal(0, pending.Evaluation.AttemptCount);
    }

    /// <summary>
    /// Only a failed evaluation may be replaced. Asking again after a verdict would create a second
    /// evaluation on the provider side for a question already answered, and asking again while one
    /// is pending would create the duplicate the reservation exists to prevent.
    /// </summary>
    [Fact]
    public async Task ANewEvaluationIsAllowedAfterAnErrorAndRefusedOtherwise()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.UnreachableReference,
            ExternalEvaluationTestCorpus.PendingReference,
            ExternalEvaluationTestCorpus.ApprovedReference);

        var failedOrder = orders[ExternalEvaluationTestCorpus.UnreachableReference];
        var pendingOrder = orders[ExternalEvaluationTestCorpus.PendingReference];
        var approvedOrder = orders[ExternalEvaluationTestCorpus.ApprovedReference];

        var first = await ExternalEvaluationTestCorpus.RequestOkAsync(client, failedOrder);
        var second = await ExternalEvaluationTestCorpus.RequestOkAsync(client, failedOrder, requestNew: true);
        await ExternalEvaluationTestCorpus.RequestOkAsync(client, pendingOrder);
        await ExternalEvaluationTestCorpus.RequestOkAsync(client, approvedOrder);

        using var refusedWhilePending = await ExternalEvaluationTestCorpus.RequestAsync(
            client,
            pendingOrder,
            requestNew: true);
        using var refusedAfterVerdict = await ExternalEvaluationTestCorpus.RequestAsync(
            client,
            approvedOrder,
            requestNew: true);

        Assert.NotEqual(first.Evaluation.Id, second.Evaluation.Id);
        Assert.Equal(2, (await ExternalEvaluationTestCorpus.HistoryAsync(client, failedOrder)).Items.Count);

        Assert.Equal(HttpStatusCode.Conflict, refusedWhilePending.StatusCode);
        Assert.Equal(
            "EXTERNAL_EVALUATION_PENDING",
            await ExternalEvaluationTestCorpus.ReadCodeAsync(refusedWhilePending));
        Assert.Equal(HttpStatusCode.Conflict, refusedAfterVerdict.StatusCode);
        Assert.Equal(
            "EXTERNAL_EVALUATION_SETTLED",
            await ExternalEvaluationTestCorpus.ReadCodeAsync(refusedAfterVerdict));
        Assert.Single((await ExternalEvaluationTestCorpus.HistoryAsync(client, pendingOrder)).Items);
    }

    [Fact]
    public async Task AnUnknownOrderProviderOrEvaluationIsRefusedWithItsOwnCode()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var missingOrder = await ExternalEvaluationTestCorpus.RequestAsync(client, Guid.NewGuid());
        using var missingEvaluation = await client.GetAsync($"/api/external-evaluations/{Guid.NewGuid()}");
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference);
        using var unregistered = await ExternalEvaluationTestCorpus.RequestAsync(
            client,
            orders[ExternalEvaluationTestCorpus.ApprovedReference],
            provider: ExternalEvaluationWireNames.KoinSandbox);
        using var nonsense = await ExternalEvaluationTestCorpus.RequestAsync(
            client,
            orders[ExternalEvaluationTestCorpus.ApprovedReference],
            provider: "NOT_A_PROVIDER");

        Assert.Equal(HttpStatusCode.NotFound, missingOrder.StatusCode);
        Assert.Equal("ORDER_NOT_FOUND", await ExternalEvaluationTestCorpus.ReadCodeAsync(missingOrder));
        Assert.Equal(HttpStatusCode.NotFound, missingEvaluation.StatusCode);
        Assert.Equal(
            "EXTERNAL_EVALUATION_NOT_FOUND",
            await ExternalEvaluationTestCorpus.ReadCodeAsync(missingEvaluation));

        // A provider this build does not register is a 404, not a 500: the name is valid, the
        // adapter is simply not deployed here.
        Assert.Equal(HttpStatusCode.NotFound, unregistered.StatusCode);
        Assert.Equal(
            "PROVIDER_NOT_REGISTERED",
            await ExternalEvaluationTestCorpus.ReadCodeAsync(unregistered));
        Assert.Equal(HttpStatusCode.BadRequest, nonsense.StatusCode);
        Assert.Equal("INVALID_PROVIDER", await ExternalEvaluationTestCorpus.ReadCodeAsync(nonsense));
    }

    /// <summary>
    /// The history of an order and one evaluation are readable, and an order with no external
    /// evaluation is an empty history rather than a missing resource.
    /// </summary>
    [Fact]
    public async Task TheHistoryOfAnOrderIsReadableAndEmptyIsNotMissing()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference,
            ExternalEvaluationTestCorpus.DeniedReference);
        var untouched = orders[ExternalEvaluationTestCorpus.DeniedReference];
        var evaluated = orders[ExternalEvaluationTestCorpus.ApprovedReference];

        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(client, evaluated);
        var history = await ExternalEvaluationTestCorpus.HistoryAsync(client, evaluated);
        var empty = await ExternalEvaluationTestCorpus.HistoryAsync(client, untouched);
        var single = await client.GetFromJsonAsync<ExternalEvaluationView>(
            $"/api/external-evaluations/{requested.Evaluation.Id}");

        Assert.Equal(requested.Evaluation.Id, Assert.Single(history.Items).Id);
        Assert.Empty(empty.Items);
        Assert.NotNull(single);
        Assert.Equal(requested.Evaluation.Status, single.Status);
    }

    /// <summary>
    /// The row is written before the provider is called, so it exists even when the call goes wrong.
    /// </summary>
    [Fact]
    public async Task AProviderThatThrowsLeavesTheEvaluationPendingRatherThanLosingIt()
    {
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddScoped<IAntifraudProvider, ThrowingAntifraudProvider>(),
        };
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.ApprovedReference);

        var result = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.ApprovedReference]);

        // Not a 5xx, and not an error either: the request may have reached the provider, so the row
        // waits for reconciliation instead of pretending to know.
        Assert.Equal("PENDING", result.Evaluation.Status);
        Assert.Equal("PROVIDER_ERROR", result.Evaluation.LastErrorCode);
        Assert.Null(result.Evaluation.ErrorCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        Assert.Equal(1, await dbContext.ExternalEvaluations.CountAsync());
    }

    private sealed class ThrowingAntifraudProvider : IAntifraudProvider
    {
        public ExternalProvider Provider => ExternalProvider.ExternalMock;

        public Task<ExternalEvaluationResult> EvaluateAsync(
            ExternalEvaluationInput input,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The provider misbehaved.");
        }

        public Task<ExternalEvaluationResult> GetStatusAsync(
            ExternalEvaluationLookup lookup,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The provider misbehaved.");
        }
    }
}
