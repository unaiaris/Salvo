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

    private static async Task<CapabilitiesResponse> GetCapabilitiesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/system/capabilities");
        response.EnsureSuccessStatusCode();

        return Assert.IsType<CapabilitiesResponse>(
            await response.Content.ReadFromJsonAsync<CapabilitiesResponse>());
    }
}
