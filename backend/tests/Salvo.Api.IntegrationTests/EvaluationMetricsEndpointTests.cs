using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Salvo.Application.Metrics;

namespace Salvo.Api.IntegrationTests;

public sealed class EvaluationMetricsEndpointTests
{
    /// <summary>
    /// Importing a file writes no ground truth, and the console invites importing files. An order
    /// without a label is therefore an ordinary state of the corpus, reported as a count rather
    /// than raised as a failure.
    /// </summary>
    [Fact]
    public async Task OrdersWithoutGroundTruthAreCountedInsteadOfFailingTheRequest()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        var run = await AlertTestCorpus.RunScoringAsync(client);

        var metrics = await GetMetricsAsync(client);

        Assert.Equal(run.Sequence, metrics.ScoringRunSequence);
        Assert.Equal(304, metrics.ScoredOrders);
        Assert.Equal(300, metrics.LabeledOrders);
        Assert.Equal(4, metrics.UnlabeledOrders);
        Assert.Equal(metrics.LabeledOrders, metrics.CalibrationOrders + metrics.HoldoutOrders);
    }

    /// <summary>
    /// Without a run there is nothing current to measure. The answer is a conflict with the state,
    /// with a code the interface can turn into "run the scoring first".
    /// </summary>
    [Fact]
    public async Task WithoutAScoringRunTheMetricsAreUnavailableRatherThanAServerError()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/evaluation-metrics");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("METRICS_UNAVAILABLE", await ReadCodeAsync(response));
    }

    /// <summary>
    /// A corpus with a run but no ground truth at all cannot be split in time either, and fails the
    /// same way rather than through the exception the domain splitter raises.
    /// </summary>
    [Fact]
    public async Task WithoutGroundTruthAtAllTheMetricsAreUnavailable()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        var response = await client.GetAsync("/api/evaluation-metrics");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("METRICS_UNAVAILABLE", await ReadCodeAsync(response));
    }

    /// <summary>
    /// The sweep is reported at the thresholds where the confusion matrix moves. A hundred and one
    /// rows that mostly repeat each other say nothing the boundaries do not.
    /// </summary>
    [Fact]
    public async Task TheSweepIsCollapsedToTheThresholdsWhereTheMatrixChanges()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var metrics = await GetMetricsAsync(client);
        var sweep = metrics.CalibrationSweep;

        Assert.InRange(sweep.Count, 2, 100);
        Assert.Equal(
            sweep.Select(row => row.Threshold).OrderBy(threshold => threshold),
            sweep.Select(row => row.Threshold));
        Assert.Equal(sweep.Count, sweep.Select(row => row.Threshold).Distinct().Count());
        Assert.Equal(sweep.Count, sweep.Select(row => row.Metrics.Matrix).Distinct().Count());
        Assert.Equal(100, sweep[^1].Threshold);

        // The selected threshold has to be one of the rows the client receives, or the response
        // would name a threshold it does not describe.
        Assert.Contains(metrics.SelectedThreshold, sweep);

        // The holdout is the selected threshold applied to the cohort that was held out, not a
        // second calibration.
        Assert.Equal(
            metrics.LabeledOrders - metrics.CalibrationOrders,
            metrics.Holdout.Matrix.TruePositives
                + metrics.Holdout.Matrix.FalsePositives
                + metrics.Holdout.Matrix.FalseNegatives
                + metrics.Holdout.Matrix.TrueNegatives);
    }

    private static async Task<EvaluationMetricsResult> GetMetricsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/evaluation-metrics");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<EvaluationMetricsResult>(
            await response.Content.ReadFromJsonAsync<EvaluationMetricsResult>());
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("code").GetString();
    }
}
