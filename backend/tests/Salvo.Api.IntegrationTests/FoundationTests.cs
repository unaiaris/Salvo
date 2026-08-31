using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Api;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

public sealed class FoundationTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public FoundationTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpointReturnsExpectedContract()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.Equal("salvo-api", payload.Service);
    }

    [Fact]
    public async Task SqliteProviderCanPersistAndReadAFoundationCheckpoint()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.FoundationCheckpoints.Add(new FoundationCheckpoint("integration-ready"));
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.FoundationCheckpoints.SingleAsync();
        Assert.Equal("integration-ready", saved.Name);
    }
}
