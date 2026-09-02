using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The design-time factory used to hard-code its own database file. Migrations then landed in a file
/// the API never opened, and the mismatch only surfaced at runtime as a missing table.
/// </summary>
public sealed class SalvoDbContextFactoryTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"salvo-settings-{Guid.NewGuid():N}");

    public SalvoDbContextFactoryTests()
    {
        Directory.CreateDirectory(directory);
    }

    [Fact]
    public void TheEnvironmentWinsOverEverySettingsFile()
    {
        WriteSettings("appsettings.json", "Data Source=from-base.db");
        WriteSettings("appsettings.Development.json", "Data Source=from-development.db");

        Assert.Equal(
            "Data Source=from-environment.db",
            SalvoDbContextFactory.ResolveConnectionString(
                "Data Source=from-environment.db",
                directory,
                "Development"));
    }

    [Fact]
    public void TheEnvironmentSpecificFileWinsOverTheBaseFile()
    {
        WriteSettings("appsettings.json", "Data Source=from-base.db");
        WriteSettings("appsettings.Development.json", "Data Source=from-development.db");

        Assert.Equal(
            "Data Source=from-development.db",
            SalvoDbContextFactory.ResolveConnectionString(null, directory, "Development"));
    }

    [Fact]
    public void TheBaseFileIsUsedWhenTheEnvironmentSpecificOneDefinesNothing()
    {
        WriteSettings("appsettings.json", "Data Source=from-base.db");
        File.WriteAllText(Path.Combine(directory, "appsettings.Development.json"), """{ "DemoData": { "Enabled": true } }""");

        Assert.Equal(
            "Data Source=from-base.db",
            SalvoDbContextFactory.ResolveConnectionString(null, directory, "Development"));
        Assert.Equal(
            "Data Source=from-base.db",
            SalvoDbContextFactory.ResolveConnectionString(null, directory, environmentName: null));
    }

    [Fact]
    public void TheDesignDatabaseIsOnlyTheLastResort()
    {
        Assert.Equal(
            SalvoDbContextFactory.FallbackConnectionString,
            SalvoDbContextFactory.ResolveConnectionString(null, directory, "Development"));
        Assert.Equal("Data Source=salvo.design.db", SalvoDbContextFactory.FallbackConnectionString);
        Assert.Equal("ConnectionStrings__SalvoDb", SalvoDbContextFactory.ConnectionStringVariable);
    }

    [Fact]
    public void TheRealApiSettingsResolveToTheDatabaseTheApplicationOpens()
    {
        var apiDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Salvo.Api");

        Assert.Equal(
            "Data Source=salvo.db",
            SalvoDbContextFactory.ResolveConnectionString(null, apiDirectory, environmentName: null));
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    private void WriteSettings(string fileName, string connectionString)
    {
        File.WriteAllText(
            Path.Combine(directory, fileName),
            $$"""{ "ConnectionStrings": { "SalvoDb": "{{connectionString}}" } }""");
    }
}
