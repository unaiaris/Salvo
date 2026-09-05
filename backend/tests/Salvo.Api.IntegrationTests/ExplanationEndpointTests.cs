using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Explanations;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// What asking for an explanation does, in every state the row can already be in.
/// </summary>
/// <remarks>
/// The shape is the one stage 6 settled on for the antifraud request: repeating a request returns
/// what is there with <c>applied</c> false, and a conflict exists only when something new was asked
/// for that cannot be given. What is new here is that the lifecycle must also be impossible to jam,
/// which is why an abandoned reservation is taken over rather than defended.
/// </remarks>
public sealed class ExplanationEndpointTests
{
    [Fact]
    public async Task AnAlertThatDoesNotExistIsNotFound()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var response = await ExplanationTestCorpus.RequestAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ALERT_NOT_FOUND", await ExplanationTestCorpus.ProblemCodeAsync(response));
    }

    /// <summary>
    /// Rows one and two of the table: nothing exists, so both forms of the request generate.
    /// </summary>
    [Fact]
    public async Task WithNothingStoredBothFormsOfTheRequestGenerate()
    {
        await using var plain = new SalvoApiFactory();
        using var plainClient = await plain.CreateMigratedClientAsync();
        var plainAlert = await OneAlertAsync(plainClient);

        await using var forced = new SalvoApiFactory();
        using var forcedClient = await forced.CreateMigratedClientAsync();
        var forcedAlert = await OneAlertAsync(forcedClient);

        var first = await ExplanationTestCorpus.RequestOkAsync(plainClient, plainAlert.Id);
        var second = await ExplanationTestCorpus.RequestOkAsync(
            forcedClient,
            forcedAlert.Id,
            regenerate: true);

        Assert.True(first.Applied);
        Assert.Equal(ExplanationWireNames.Ready, first.Explanation.Status);
        Assert.True(second.Applied);
        Assert.Equal(ExplanationWireNames.Ready, second.Explanation.Status);
    }

    /// <summary>
    /// Rows three and four: a provider is answering right now.
    /// </summary>
    /// <remarks>
    /// The reservation is inserted directly, because the handler never leaves one behind — it
    /// settles whatever happens. What is being modelled is the window during which another request
    /// arrives while the first is still in flight.
    /// </remarks>
    [Fact]
    public async Task AReservationInFlightIsReportedAndNeverDuplicated()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);
        var reserved = await ReserveDirectlyAsync(factory, client, alert.Id, DateTimeOffset.UtcNow);

        var repeated = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var forced = await ExplanationTestCorpus.RequestAsync(client, alert.Id, regenerate: true);

        Assert.False(repeated.Applied);
        Assert.Equal(reserved, repeated.Explanation.Id);
        Assert.Equal(ExplanationWireNames.Pending, repeated.Explanation.Status);

        Assert.Equal(HttpStatusCode.Conflict, forced.StatusCode);
        Assert.Equal("EXPLANATION_PENDING", await ExplanationTestCorpus.ProblemCodeAsync(forced));

        Assert.Equal(1, await CountAsync(factory));
    }

    /// <summary>
    /// Row five: a reservation nobody is waiting on is taken over, with or without
    /// <c>regenerate</c>.
    /// </summary>
    /// <remarks>
    /// This is the rule that replaces a reconciliation sweep in a stage that has none. Without it a
    /// request whose process died leaves a pending row the partial unique index defends forever, and
    /// that evaluation can never be explained again.
    /// </remarks>
    [Fact]
    public async Task AnAbandonedReservationIsTakenOverOnTheSameRow()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);
        var reserved = await ReserveDirectlyAsync(
            factory,
            client,
            alert.Id,
            DateTimeOffset.UtcNow.AddMinutes(-10));

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.True(result.Applied);
        Assert.Equal(reserved, result.Explanation.Id);
        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);

        // The second attempt on the same row, which is the whole point of not creating another.
        Assert.Equal(2, result.Explanation.AttemptCount);
        Assert.Equal(1, await CountAsync(factory));
    }

    /// <summary>
    /// Rows six and seven: an explanation that is written is returned, and never replaced.
    /// </summary>
    [Fact]
    public async Task AWrittenExplanationIsReturnedAndRefusesToBeRegenerated()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var first = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var repeated = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var forced = await ExplanationTestCorpus.RequestAsync(client, alert.Id, regenerate: true);

        Assert.True(first.Applied);
        Assert.False(repeated.Applied);
        Assert.Equal(first.Explanation.Id, repeated.Explanation.Id);
        Assert.Equal(first.Explanation.Summary, repeated.Explanation.Summary);

        Assert.Equal(HttpStatusCode.Conflict, forced.StatusCode);
        Assert.Equal(
            "EXPLANATION_ALREADY_READY",
            await ExplanationTestCorpus.ProblemCodeAsync(forced));

        Assert.Equal(1, await CountAsync(factory));
    }

    /// <summary>
    /// Rows eight to eleven: a failure is returned as it stands, retried on the same row, and
    /// eventually refuses to be retried at all.
    /// </summary>
    /// <remarks>
    /// The attempt budget is what stands between a paid provider and an endpoint with no
    /// authentication in front of it. The row keeps <c>ATTEMPT_LIMIT_REACHED</c> once it is spent,
    /// so a reader can tell a failure worth retrying from one that is finished.
    /// </remarks>
    [Fact]
    public async Task AFailureIsRetriedOnTheSameRowUntilTheBudgetIsSpent()
    {
        var provider = new ExplanationTestCorpus.SwitchableProvider
        {
            Behaviour = ExplanationTestCorpus.ProviderBehaviour.InventANumber,
        };
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddScoped<IExplanationProvider>(_ => provider),
        };
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var first = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var repeated = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var second = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id, regenerate: true);
        var third = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id, regenerate: true);

        // Repeating without asking to regenerate reports the failure and asks nobody anything.
        Assert.True(first.Applied);
        Assert.False(repeated.Applied);
        Assert.Equal(ExplanationWireNames.NotGroundedNumber, repeated.Explanation.FailureCode);

        // Every retry is the same row.
        Assert.Equal(first.Explanation.Id, second.Explanation.Id);
        Assert.Equal(first.Explanation.Id, third.Explanation.Id);
        Assert.Equal(1, second.Explanation.AttemptCount - 1);
        Assert.Equal(3, third.Explanation.AttemptCount);
        Assert.Equal(3, provider.Calls);

        // The third attempt spends the budget, and the row says so without losing what went wrong.
        Assert.True(third.Explanation.AttemptsExhausted);
        Assert.Equal(ExplanationWireNames.AttemptLimitReached, third.Explanation.FailureCode);

        var fourth = await ExplanationTestCorpus.RequestAsync(client, alert.Id, regenerate: true);
        var quiet = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(HttpStatusCode.Conflict, fourth.StatusCode);
        Assert.Equal(
            "EXPLANATION_ATTEMPTS_EXHAUSTED",
            await ExplanationTestCorpus.ProblemCodeAsync(fourth));
        Assert.False(quiet.Applied);
        Assert.Equal(3, provider.Calls);
        Assert.Equal(1, await CountAsync(factory));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.AlertExplanations.AsNoTracking().SingleAsync();

        // The real reason survives beside the code that supersedes it.
        Assert.Contains(
            ExplanationWireNames.NotGroundedNumber,
            stored.FailureDetail,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A caller that goes away mid-call leaves the row settled, not reserved.
    /// </summary>
    /// <remarks>
    /// The direct correction of the defect the adversarial review found: settling uses
    /// <see cref="CancellationToken.None"/>, so closing a browser cannot leave behind a reservation
    /// that jams the evaluation for good. Exercised through the handler rather than through HTTP
    /// because what is under test is which token the write uses, and that has to be deterministic.
    /// </remarks>
    [Fact]
    public async Task ACallerThatGoesAwayLeavesTheRowSettled()
    {
        using var caller = new CancellationTokenSource();
        var provider = new ExplanationTestCorpus.CallerCancellingProvider(caller);
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddScoped<IExplanationProvider>(_ => provider),
        };
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RequestExplanationHandler>();

        var result = await handler.HandleAsync(alert.Id, regenerate: false, caller.Token);

        Assert.NotNull(result);
        Assert.True(caller.IsCancellationRequested);
        Assert.Equal(ExplanationWireNames.Failed, result.Explanation.Status);
        Assert.Equal(ExplanationWireNames.Cancelled, result.Explanation.FailureCode);

        await using var verification = factory.Services.CreateAsyncScope();
        var dbContext = verification.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var stored = await dbContext.AlertExplanations.AsNoTracking().SingleAsync();

        Assert.Equal(ExplanationStatus.Failed, stored.Status);
        Assert.NotNull(stored.SettledAt);
    }

    /// <summary>
    /// The alert detail carries the explanation, which is why there is no endpoint to read one.
    /// </summary>
    [Fact]
    public async Task TheAlertDetailCarriesTheExplanationAndSaysWhetherItIsOutdated()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var before = await AlertTestCorpus.GetAlertAsync(client, alert.Id);
        var generated = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        var after = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

        // Nobody asked yet: an absent explanation is an ordinary state, not an error.
        Assert.Null(before.Explanation);
        Assert.Null(before.CurrentExplanation);

        Assert.NotNull(after.Explanation);
        Assert.Equal(generated.Explanation.Id, after.Explanation.Id);
        Assert.Equal(generated.Explanation.Summary, after.Explanation.Summary);
        Assert.False(after.Explanation.IsOutdated);

        // A backfill moves the corpus underneath it. Nothing is written, and the reader is told.
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBackfill());
        await AlertTestCorpus.RunScoringAsync(client);
        var moved = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

        Assert.NotNull(moved.Explanation);
        Assert.Equal(generated.Explanation.Id, moved.Explanation.Id);
        Assert.True(moved.Explanation.IsOutdated);
        Assert.Equal(generated.Explanation.Summary, moved.Explanation.Summary);
    }

    private static async Task<AlertListItem> OneAlertAsync(HttpClient client)
    {
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        return Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
    }

    /// <summary>
    /// Writes a reservation straight to the database, which is what a request that died mid-flight
    /// leaves behind. The handler cannot produce one: it always settles.
    /// </summary>
    private static async Task<Guid> ReserveDirectlyAsync(
        SalvoApiFactory factory,
        HttpClient client,
        Guid alertId,
        DateTimeOffset requestedAt)
    {
        var detail = await AlertTestCorpus.GetAlertAsync(client, alertId);
        var explanation = AlertExplanation.Reserve(
            Guid.NewGuid(),
            detail.Snapshot.EvaluationId,
            ExplanationProvider.Mock,
            DeterministicExplanationProvider.Version,
            detail.AlertPolicyVersion,
            alertId,
            requestedAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        dbContext.AlertExplanations.Add(explanation);
        await dbContext.SaveChangesAsync();

        return explanation.Id;
    }

    private static async Task<int> CountAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.AlertExplanations.CountAsync();
    }
}
