using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Salvo.Api;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Hosts the API against a private SQLite database.
/// </summary>
/// <remarks>
/// By default every scope shares one in-memory connection, which is fast and isolated. That shape
/// cannot express a race: a single connection serializes everything, so a test of concurrent
/// requests would pass while the bug it targets still shipped. <see cref="WithFileDatabase"/> gives
/// each scope its own connection to a temporary file, which is what the review concurrency test
/// needs. The parameterless constructor stays the only public one because xUnit class fixtures
/// require exactly one.
/// </remarks>
public sealed class SalvoApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? sharedConnection;
    private readonly string? databaseFilePath;
    private readonly string? connectionString;

    public SalvoApiFactory()
    {
        sharedConnection = new("Data Source=:memory:");
    }

    private SalvoApiFactory(string databaseFilePath)
    {
        this.databaseFilePath = databaseFilePath;
        connectionString = $"Data Source={databaseFilePath}";
    }

    /// <summary>
    /// A factory backed by a temporary database file, so that concurrent requests really do use
    /// separate connections.
    /// </summary>
    public static SalvoApiFactory WithFileDatabase()
    {
        return new(Path.Combine(Path.GetTempPath(), $"salvo-tests-{Guid.NewGuid():N}.db"));
    }

    /// <summary>
    /// Optional service overrides, applied after the test database is registered. Set it before the
    /// first client or service is resolved.
    /// </summary>
    public Action<IServiceCollection>? ConfigureTestServices { get; set; }

    public async Task<HttpClient> CreateMigratedClientAsync()
    {
        var client = CreateClient();
        await InitializeDatabaseAsync();
        return client;
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        sharedConnection?.Open();
        builder.UseSetting("DemoData:Enabled", "true");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SalvoDbContext>>();
            services.RemoveAll<SalvoDbContext>();
            if (sharedConnection is not null)
            {
                services.AddDbContext<SalvoDbContext>(options => options.UseSqlite(sharedConnection));
            }
            else
            {
                services.AddDbContext<SalvoDbContext>(options => options.UseSqlite(connectionString));
            }

            ConfigureTestServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        sharedConnection?.Dispose();

        if (databaseFilePath is null)
        {
            return;
        }

        // Pooled connections keep the file open, so the pool has to be drained before the temporary
        // file can be removed.
        SqliteConnection.ClearAllPools();
        File.Delete(databaseFilePath);
    }
}
