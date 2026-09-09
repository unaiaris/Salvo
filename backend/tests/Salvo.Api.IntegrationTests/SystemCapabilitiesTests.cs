using System.Net;
using System.Net.Http.Json;

namespace Salvo.Api.IntegrationTests;

public sealed class SystemCapabilitiesTests
{
    /// <summary>
    /// A client cannot infer whether the demo routes exist: the build is identical either way and a
    /// 404 is indistinguishable from a typo or a backend that is down. It asks instead.
    /// </summary>
    [Fact]
    public async Task CapabilitiesAnnounceThatDemoDataIsAvailable()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = await GetCapabilitiesAsync(client);
        var seed = await client.PostAsync("/api/demo-data/seed", null);

        Assert.True(capabilities.DemoDataEnabled);
        // Sin declarar nada, el sembrado sigue encendido: el interruptor nuevo no cambia ningun
        // modo de correr esta API que ya existiera.
        Assert.True(capabilities.DemoSeedEnabled);
        Assert.Equal(HttpStatusCode.OK, seed.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/evaluation-metrics")).StatusCode);
    }

    [Fact]
    public async Task WithoutDemoDataTheSeedAndTheMetricsDoNotExist()
    {
        await using var factory = new SalvoApiFactory { DemoDataEnabled = false };
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = await GetCapabilitiesAsync(client);

        Assert.False(capabilities.DemoDataEnabled);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/demo-data/seed", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/evaluation-metrics")).StatusCode);

        // The operational surface does not depend on the demo corpus and stays available.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/alerts")).StatusCode);
    }

    /// <summary>
    /// The public instance turns off the seed route and keeps everything else, and that split is
    /// the whole reason the second switch exists.
    /// </summary>
    /// <remarks>
    /// Turning off <c>DemoData:Enabled</c> to protect a public instance would take the quality
    /// surface with it — F1, the confusion matrix, the sweep — which is half of what this project
    /// argues. What the public instance actually needs gone is the one route with which a visitor
    /// could leave the console unusable for the next one, and its corpus arrives baked into the
    /// image so nobody there needs it.
    /// </remarks>
    [Fact]
    public async Task TurningOffOnlyTheSeedLeavesTheQualitySurfaceAndTheTriggersStanding()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["DemoData:SeedEnabled"] = "false";
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = await GetCapabilitiesAsync(client);

        Assert.True(capabilities.DemoDataEnabled);
        Assert.False(capabilities.DemoSeedEnabled);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/demo-data/seed", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/demo-data/seed-preview")).StatusCode);

        // The two that have to survive, and the trigger that rides on the other switch.
        Assert.NotEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/evaluation-metrics")).StatusCode);
        Assert.NotEqual(
            HttpStatusCode.NotFound,
            (await client.PostAsync("/api/demo-data/external-evaluations:request", null)).StatusCode);
    }

    /// <summary>
    /// The seed switch narrows and never widens: it cannot bring the seed back where the
    /// deployment does not declare itself a demonstration at all.
    /// </summary>
    [Fact]
    public async Task TheSeedSwitchCannotTurnTheSeedOnWithoutTheDemonstration()
    {
        await using var factory = new SalvoApiFactory { DemoDataEnabled = false };
        factory.Settings["DemoData:SeedEnabled"] = "true";
        using var client = await factory.CreateMigratedClientAsync();

        var capabilities = await GetCapabilitiesAsync(client);

        Assert.False(capabilities.DemoDataEnabled);
        Assert.False(capabilities.DemoSeedEnabled);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/demo-data/seed", null)).StatusCode);
    }

    private static async Task<CapabilitiesResponse> GetCapabilitiesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/system/capabilities");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<CapabilitiesResponse>(
            await response.Content.ReadFromJsonAsync<CapabilitiesResponse>());
    }
}
