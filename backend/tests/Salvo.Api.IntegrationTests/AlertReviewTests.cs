using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class AlertReviewTests
{
    [Fact]
    public async Task AVerdictClosesTheAlertAndWritesItsAudit()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenOneAlertAsync(client);

        var response = await AlertTestCorpus.ReviewAsync(client, alertId, "REPORTED_FRAUD", "chargeback filed");
        var result = Assert.IsType<AlertReviewResult>(
            await response.Content.ReadFromJsonAsync<AlertReviewResult>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var alert = await dbContext.Alerts.AsNoTracking().SingleAsync();
        var review = await dbContext.AlertReviews.AsNoTracking().SingleAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result.Applied);
        Assert.Equal("REPORTED_FRAUD", result.Alert.Status);
        Assert.Equal(AlertStatus.ReportedFraud, alert.Status);
        Assert.NotNull(alert.ReviewedAt);

        // The order itself is untouched: a verdict is operational judgement, not a fact of the order.
        Assert.Equal(70, alert.RiskScoreSnapshot);
        Assert.Equal(alertId, review.AlertId);
        Assert.Equal(AlertStatus.Open, review.PreviousStatus);
        Assert.Equal(AlertStatus.ReportedFraud, review.NewStatus);
        Assert.Equal("chargeback filed", review.Note);
        Assert.Equal("chargeback filed", result.Alert.Review?.Note);
    }

    [Fact]
    public async Task RepeatingTheSameVerdictIsIdempotentAndChangingItsNoteConflicts()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenOneAlertAsync(client);

        (await AlertTestCorpus.ReviewAsync(client, alertId, "CONFIRMED_SAFE", "known buyer"))
            .EnsureSuccessStatusCode();

        var repeated = await AlertTestCorpus.ReviewAsync(client, alertId, "CONFIRMED_SAFE", "known buyer");
        var repeatedResult = Assert.IsType<AlertReviewResult>(
            await repeated.Content.ReadFromJsonAsync<AlertReviewResult>());
        var otherNote = await AlertTestCorpus.ReviewAsync(client, alertId, "CONFIRMED_SAFE", "actually suspicious");
        var otherVerdict = await AlertTestCorpus.ReviewAsync(client, alertId, "REPORTED_FRAUD", "known buyer");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.False(repeatedResult.Applied);

        // A second, differing decision has nowhere to be recorded while there is no reviewer
        // identity, so it is refused instead of silently discarded.
        Assert.Equal(HttpStatusCode.Conflict, otherNote.StatusCode);
        Assert.Equal("ALERT_REVIEW_NOTE_CONFLICT", await ReadCodeAsync(otherNote));
        Assert.Equal(HttpStatusCode.Conflict, otherVerdict.StatusCode);
        Assert.Equal("ALERT_ALREADY_REVIEWED", await ReadCodeAsync(otherVerdict));
        Assert.Equal(1, await dbContext.AlertReviews.CountAsync());
        Assert.Equal(AlertStatus.ConfirmedSafe, (await dbContext.Alerts.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task ReviewingAnAlertWhoseBandDivergedRequiresAcknowledgingIt()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenOneAlertAsync(client);

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBackfill());
        await AlertTestCorpus.RunScoringAsync(client);

        var refused = await AlertTestCorpus.ReviewAsync(client, alertId, "REPORTED_FRAUD", "looks new");
        var accepted = await AlertTestCorpus.ReviewAsync(
            client,
            alertId,
            "REPORTED_FRAUD",
            "looks new",
            acknowledgedDivergence: true);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        // The snapshot still says the buyer had no history; the corpus now says otherwise. The
        // reviewer is not blocked, but they cannot decide without having been shown the change.
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("ALERT_DIVERGENCE_NOT_ACKNOWLEDGED", await ReadCodeAsync(refused));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(1, await dbContext.AlertReviews.CountAsync());
    }

    [Fact]
    public async Task TwoConcurrentReviewsLeaveOneVerdictAndExactlyOneAudit()
    {
        using var gate = new ReviewGate();

        // A file database, because the shared in-memory connection of the default factory
        // serializes every scope: the race under test could not even be expressed on it.
        await using var factory = SalvoApiFactory.WithFileDatabase();
        factory.ConfigureTestServices = services => services.AddScoped<IAlertStore>(provider =>
            new GatedAlertStore(
                new EfAlertStore(provider.GetRequiredService<SalvoDbContext>()),
                gate));
        using var client = await factory.CreateMigratedClientAsync();
        var alertId = await OpenOneAlertAsync(client);

        // Both requests read the alert as OPEN before either of them writes, which is exactly the
        // check-then-act a transaction alone does not protect.
        var responses = await Task.WhenAll(
            AlertTestCorpus.ReviewAsync(client, alertId, "CONFIRMED_SAFE", "safe"),
            AlertTestCorpus.ReviewAsync(client, alertId, "REPORTED_FRAUD", "fraud"));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var alert = await dbContext.Alerts.AsNoTracking().SingleAsync();
        var review = await dbContext.AlertReviews.AsNoTracking().SingleAsync();

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(response => response.StatusCode).OrderBy(status => (int)status));
        Assert.Equal(1, await dbContext.AlertReviews.CountAsync());
        Assert.Equal(AlertStatus.Open, review.PreviousStatus);
        Assert.Equal(review.NewStatus, alert.Status);
        Assert.NotEqual(AlertStatus.Open, alert.Status);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    private static async Task<Guid> OpenOneAlertAsync(HttpClient client)
    {
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        return Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items).Id;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        return problem?["code"].ToString();
    }

    /// <summary>
    /// Forces both requests to finish reading before either writes, and then lets them write one at
    /// a time. Without it the interleaving would depend on the scheduler and the test would be
    /// flaky rather than a proof.
    /// </summary>
    private sealed class ReviewGate : IDisposable
    {
        private readonly TaskCompletionSource bothLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim writeLock = new(1, 1);
        private int loaded;

        public async Task WaitForBothLoadsAsync()
        {
            if (Interlocked.Increment(ref loaded) == 2)
            {
                bothLoaded.TrySetResult();
            }

            await bothLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public Task EnterWriteAsync() => writeLock.WaitAsync();

        public void ExitWrite() => writeLock.Release();

        public void Dispose() => writeLock.Dispose();
    }

    private sealed class GatedAlertStore(IAlertStore inner, ReviewGate gate) : IAlertStore
    {
        public Task<AlertPage> GetPageAsync(
            AlertStatus? status,
            AlertSeverity? severity,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return inner.GetPageAsync(status, severity, page, pageSize, cancellationToken);
        }

        public Task<AlertContext?> FindAsync(Guid alertId, CancellationToken cancellationToken)
        {
            return inner.FindAsync(alertId, cancellationToken);
        }

        public async Task<AlertContext?> FindForReviewAsync(Guid alertId, CancellationToken cancellationToken)
        {
            var context = await inner.FindForReviewAsync(alertId, cancellationToken);
            await gate.WaitForBothLoadsAsync();

            return context;
        }

        public async Task SaveReviewAsync(Alert alert, AlertReview review, CancellationToken cancellationToken)
        {
            await gate.EnterWriteAsync();
            try
            {
                await inner.SaveReviewAsync(alert, review, cancellationToken);
            }
            finally
            {
                gate.ExitWrite();
            }
        }
    }
}
