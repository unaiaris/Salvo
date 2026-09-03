using System.Net;
using System.Text.Json;
using Salvo.Application.Alerts;

namespace Salvo.Api.IntegrationTests;

public sealed class AlertFeedOrderTests
{
    /// <summary>
    /// The feed can be ordered by the local score of the evaluation that is current now, which is
    /// the order an analyst works in.
    /// </summary>
    [Fact]
    public async Task ScoreDescOrdersByTheCurrentLocalScoreAndIsDeterministic()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var sorted = await AlertTestCorpus.ListAlertsAsync(client, "?sort=SCORE_DESC&pageSize=200");
        var repeated = await AlertTestCorpus.ListAlertsAsync(client, "?sort=SCORE_DESC&pageSize=200");
        var byCreation = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200");

        Assert.Equal(18, sorted.Items.Count);
        Assert.Equal(
            sorted.Items.Select(alert => alert.CurrentRiskScore).OrderByDescending(score => score),
            sorted.Items.Select(alert => alert.CurrentRiskScore));

        // Ties are broken by creation instant and then by identifier, so two identical requests
        // return the same sequence rather than whatever the database happened to scan first.
        Assert.Equal(
            sorted.Items.Select(alert => alert.Id),
            repeated.Items.Select(alert => alert.Id));
        Assert.Equal(
            sorted.Items
                .OrderByDescending(alert => alert.CurrentRiskScore)
                .ThenByDescending(alert => alert.CreatedAt)
                .ThenBy(alert => alert.Id)
                .Select(alert => alert.Id),
            sorted.Items.Select(alert => alert.Id));

        // The default is unchanged, and it is a different order: every alert of this corpus was
        // created by the same run, so creation order falls back to the identifier.
        Assert.Equal(
            byCreation.Items.Select(alert => alert.Id),
            (await AlertTestCorpus.ListAlertsAsync(client, "?sort=CREATED_DESC&pageSize=200"))
                .Items.Select(alert => alert.Id));
    }

    /// <summary>
    /// Paging is what proves the join happens inside the query: composing the current evaluation
    /// after <c>Skip</c>/<c>Take</c> would order each page against a score the database never saw,
    /// so pages would overlap and the concatenation would not be sorted.
    /// </summary>
    [Fact]
    public async Task ScoreDescOrdersTheWholeFeedBeforeItIsPaged()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var whole = await AlertTestCorpus.ListAlertsAsync(client, "?sort=SCORE_DESC&pageSize=200");
        var pages = new List<AlertListItem>();
        for (var page = 1; page <= 4; page++)
        {
            var slice = await AlertTestCorpus.ListAlertsAsync(
                client,
                $"?sort=SCORE_DESC&page={page}&pageSize=5");
            pages.AddRange(slice.Items);
        }

        Assert.Equal(18, pages.Count);
        Assert.Equal(pages.Count, pages.Select(alert => alert.Id).Distinct().Count());
        Assert.Equal(whole.Items.Select(alert => alert.Id), pages.Select(alert => alert.Id));
    }

    [Fact]
    public async Task AnUnknownSortIsRejected()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var response = await client.GetAsync("/api/alerts?sort=SCORE_ASC");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_SORT", document.RootElement.GetProperty("code").GetString());
    }

    /// <summary>
    /// Every read states the run it is current as of. Without it a screen would date the current
    /// evaluation by the instant it was first computed, which a later run reuses unchanged.
    /// </summary>
    [Fact]
    public async Task TheFeedAndTheDetailStateTheRunTheyAreCurrentAsOf()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        var first = await AlertTestCorpus.RunScoringAsync(client);

        var afterFirst = await AlertTestCorpus.ListAlertsAsync(client);
        var alert = Assert.Single(afterFirst.Items);
        var detailAfterFirst = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

        Assert.Equal(first.Sequence, afterFirst.ScoringRunSequence);
        Assert.Equal(first.Sequence, afterFirst.CurrentRun?.Sequence);
        Assert.Equal(first.Sequence, detailAfterFirst.CurrentRun?.Sequence);

        // The evaluation is reused by the second run and keeps its own timestamp; only the run
        // reference moves, which is exactly the distinction the contract has to carry.
        var second = await AlertTestCorpus.RunScoringAsync(client);
        var afterSecond = await AlertTestCorpus.ListAlertsAsync(client);
        var detailAfterSecond = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

        Assert.Equal(second.Sequence, afterSecond.ScoringRunSequence);
        Assert.Equal(second.Sequence, afterSecond.CurrentRun?.Sequence);
        Assert.Equal(second.Sequence, detailAfterSecond.CurrentRun?.Sequence);
        Assert.NotEqual(first.Sequence, second.Sequence);
        Assert.Equal(
            detailAfterFirst.CurrentEvaluation?.EvaluatedAt,
            detailAfterSecond.CurrentEvaluation?.EvaluatedAt);
        Assert.NotEqual(
            detailAfterFirst.CurrentRun?.CompletedAt,
            detailAfterSecond.CurrentRun?.CompletedAt);
    }

    /// <summary>
    /// An alert whose order the current run did not cover has no score to be compared with, and
    /// sorts last instead of first.
    /// </summary>
    [Fact]
    public async Task AlertsWithoutACurrentEvaluationSortLast()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.EscalationBase());
        await AlertTestCorpus.RunScoringAsync(client);

        var sorted = await AlertTestCorpus.ListAlertsAsync(client, "?sort=SCORE_DESC&pageSize=200");

        Assert.Equal(2, sorted.Items.Count);
        Assert.All(sorted.Items, item => Assert.NotNull(item.CurrentRiskScore));
        Assert.Equal(
            sorted.Items.Select(alert => alert.CurrentRiskScore).OrderByDescending(score => score),
            sorted.Items.Select(alert => alert.CurrentRiskScore));
    }
}
