using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Risk;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class AlertCreationTests
{
    [Fact]
    public async Task TheDemoCorpusOpensOneAlertPerFlaggedOrderInItsSeverityBand()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var first = await AlertTestCorpus.RunScoringAsync(client);
        var second = await AlertTestCorpus.RunScoringAsync(client);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var alerts = await dbContext.Alerts.AsNoTracking().ToListAsync();

        // The twenty-three flagged orders of the demo corpus, spread over the three bands. The
        // middle one is no longer empty: the stage 9 corpus reaches every band on purpose, and six
        // of these alerts sit in it — one of them opened by an order that is not fraud at all.
        Assert.Equal(23, alerts.Count);
        Assert.Equal(11, alerts.Count(alert => alert.Severity == AlertSeverity.Medium));
        Assert.Equal(6, alerts.Count(alert => alert.Severity == AlertSeverity.High));
        Assert.Equal(6, alerts.Count(alert => alert.Severity == AlertSeverity.Critical));
        Assert.All(alerts, alert =>
        {
            Assert.Equal(AlertStatus.Open, alert.Status);
            Assert.Equal("e4-v1", alert.AlertPolicyVersion);
            Assert.Null(alert.SupersedesAlertId);
            Assert.Null(alert.ReviewedAt);
        });

        Assert.Equal((23, 0, 0), (first.AlertsCreated, first.AlertsSkippedOpen, first.AlertsSkippedReviewed));

        // A second run over an unchanged corpus opens nothing: every flagged order already has an
        // alert waiting for a verdict.
        Assert.Equal((0, 23, 0), (second.AlertsCreated, second.AlertsSkippedOpen, second.AlertsSkippedReviewed));
        Assert.Equal(23, await dbContext.Alerts.CountAsync());
    }

    [Fact]
    public async Task AnEscalationAfterABackfillOpensANewAlertLinkedToTheReviewedOne()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBase());
        var first = await AlertTestCorpus.RunScoringAsync(client);

        var opened = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        Assert.Equal("ORD_ESC_TARGET", opened.MerchantReferenceId);
        Assert.Equal(60, opened.RiskScoreSnapshot);
        Assert.Equal("MEDIUM", opened.Severity);

        // A reviewed order is out of the queue; only a change of band brings it back.
        (await AlertTestCorpus.ReviewAsync(client, opened.Id, "CONFIRMED_SAFE", "known buyer"))
            .EnsureSuccessStatusCode();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBackfill());
        var second = await AlertTestCorpus.RunScoringAsync(client);

        var alerts = (await AlertTestCorpus.ListAlertsAsync(client)).Items;
        var escalated = Assert.Single(alerts, alert => alert.Id != opened.Id);

        Assert.Equal((1, 0, 0), (first.AlertsCreated, first.AlertsSkippedOpen, first.AlertsSkippedReviewed));
        Assert.Equal((1, 0, 0), (second.AlertsCreated, second.AlertsSkippedOpen, second.AlertsSkippedReviewed));
        Assert.Equal("ORD_ESC_TARGET", escalated.MerchantReferenceId);
        Assert.Equal(opened.OrderId, escalated.OrderId);
        Assert.Equal(100, escalated.RiskScoreSnapshot);
        Assert.Equal("CRITICAL", escalated.Severity);
        Assert.Equal("OPEN", escalated.Status);
        Assert.Equal(opened.Id, escalated.SupersedesAlertId);

        // The reviewed alert keeps the score it was judged on.
        var previous = Assert.Single(alerts, alert => alert.Id == opened.Id);
        Assert.Equal(60, previous.RiskScoreSnapshot);
        Assert.Equal("CONFIRMED_SAFE", previous.Status);
    }

    [Fact]
    public async Task AHigherScoreInsideTheSameBandOpensNothing()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBase());
        await AlertTestCorpus.RunScoringAsync(client);

        var opened = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        Assert.Equal("ORD_BAND_TARGET", opened.MerchantReferenceId);
        Assert.Equal(90, opened.RiskScoreSnapshot);
        Assert.Equal("CRITICAL", opened.Severity);

        (await AlertTestCorpus.ReviewAsync(client, opened.Id, "CONFIRMED_SAFE")).EnsureSuccessStatusCode();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.SameBandBackfill());
        var second = await AlertTestCorpus.RunScoringAsync(client);

        var detail = await AlertTestCorpus.GetAlertAsync(client, opened.Id);
        var alerts = (await AlertTestCorpus.ListAlertsAsync(client)).Items;

        // The score rose from 90 to the cap, but the nature of the risk did not change, so the
        // reviewed order is not put back in front of an analyst.
        Assert.Equal(100, detail.CurrentEvaluation?.Score);
        Assert.Equal("CRITICAL", detail.CurrentEvaluation?.Severity);
        Assert.False(detail.Divergence.HasBandDivergence);
        Assert.Equal((0, 0, 1), (second.AlertsCreated, second.AlertsSkippedOpen, second.AlertsSkippedReviewed));
        Assert.Equal(opened.Id, Assert.Single(alerts).Id);
    }

    [Fact]
    public async Task AnOpenAlertKeepsItsSnapshotAndExposesTheDivergenceAfterABackfill()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        var opened = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        var before = await AlertTestCorpus.GetAlertAsync(client, opened.Id);
        Assert.Equal(70, before.Snapshot.Score);
        Assert.Equal("HIGH", before.Snapshot.Severity);
        Assert.Contains(
            before.Snapshot.Signals,
            signal => signal.Rule == "new_buyer_high_value");
        Assert.False(before.Divergence.HasBandDivergence);

        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBackfill());
        var second = await AlertTestCorpus.RunScoringAsync(client);

        var after = await AlertTestCorpus.GetAlertAsync(client, opened.Id);

        // The premise the alert was opened on is no longer true. The snapshot is not rewritten,
        // because it is the record the verdict is being formed on, but the reader is told.
        Assert.Equal(70, after.Snapshot.Score);
        Assert.Equal("HIGH", after.Snapshot.Severity);
        Assert.Equal(before.Snapshot.EvaluationId, after.Snapshot.EvaluationId);
        Assert.Equal(0, after.CurrentEvaluation?.Score);
        Assert.False(after.CurrentEvaluation?.IsFlagged);
        Assert.Null(after.CurrentEvaluation?.Severity);
        Assert.Empty(after.CurrentEvaluation?.Signals ?? []);
        Assert.True(after.Divergence.HasBandDivergence);
        Assert.Equal(70, after.Divergence.SnapshotScore);
        Assert.Equal(0, after.Divergence.CurrentScore);
        Assert.Null(after.Divergence.CurrentSeverity);

        // The order is no longer flagged at all, so the run has no alert decision to report and the
        // open alert is neither closed nor updated: only a human verdict closes it.
        Assert.Equal((0, 0, 0), (second.AlertsCreated, second.AlertsSkippedOpen, second.AlertsSkippedReviewed));
        Assert.Equal("OPEN", after.Status);
    }

    [Fact]
    public async Task AFailedRunWriteLeavesNoAlertBehind()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IScoringRunStore>();
        var orderId = await dbContext.Orders
            .Where(order => order.MerchantReferenceId == "ORD_DIV_TARGET")
            .Select(order => order.Id)
            .SingleAsync();

        var runId = Guid.NewGuid();
        var evaluation = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            "e3-v1",
            new(orderId, DateTimeOffset.UnixEpoch, 70, true, [new("amount_anomaly", 40, "d"), new("new_buyer_high_value", 30, "d")]),
            DateTimeOffset.UnixEpoch);
        var alert = Alert.Open(
            Guid.NewGuid(),
            orderId,
            evaluation.Id,
            70,
            evaluation.SignalsJson!,
            "e4-v1",
            supersedesAlertId: null,
            DateTimeOffset.UnixEpoch);

        // The per-order reference points at an evaluation the run never appends, so the write fails
        // after the run, the evaluation and the alert are already part of the same unit of work.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => store.SaveRunAsync(
            ScoringRun.Complete(runId, 1, "e3-v1", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 1, 1, 0, 1, 0, 0),
            [evaluation],
            [RunEvaluation.Create(runId, orderId, Guid.NewGuid())],
            [alert],
            CancellationToken.None));

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        Assert.Equal(0, await verificationContext.ScoringRuns.CountAsync());
        Assert.Equal(0, await verificationContext.RiskEvaluations.CountAsync());
        Assert.Equal(0, await verificationContext.RunEvaluations.CountAsync());
        Assert.Equal(0, await verificationContext.Alerts.CountAsync());
    }
}
