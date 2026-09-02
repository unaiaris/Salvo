using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Salvo.Infrastructure.Persistence;

/// <summary>
/// Builds a context for design-time tooling such as <c>dotnet ef</c>.
/// </summary>
/// <remarks>
/// The factory resolves the same connection string the application uses. A hard-coded design-time
/// database silently migrates a file the API never opens, which surfaces much later as
/// <c>no such table</c> against the real one.
/// </remarks>
public sealed class SalvoDbContextFactory : IDesignTimeDbContextFactory<SalvoDbContext>
{
    /// <summary>
    /// Environment variable of the connection string, in the double-underscore form the
    /// configuration system uses for <c>ConnectionStrings:SalvoDb</c>.
    /// </summary>
    public const string ConnectionStringVariable = "ConnectionStrings__SalvoDb";

    /// <summary>
    /// Last resort, used only when neither the environment nor an application settings file defines
    /// a connection string.
    /// </summary>
    public const string FallbackConnectionString = "Data Source=salvo.design.db";

    public SalvoDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var connectionString = ResolveConnectionString(
            Environment.GetEnvironmentVariable(ConnectionStringVariable),
            Directory.GetCurrentDirectory(),
            environmentName);

        var options = new DbContextOptionsBuilder<SalvoDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new(options);
    }

    /// <summary>
    /// Resolves the connection string from the environment first, then from the application settings
    /// files under <paramref name="basePath"/>, and only then from
    /// <see cref="FallbackConnectionString"/>.
    /// </summary>
    public static string ResolveConnectionString(
        string? fromEnvironment,
        string basePath,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        if (!string.IsNullOrWhiteSpace(environmentName)
            && ReadFromSettings(Path.Combine(basePath, $"appsettings.{environmentName}.json")) is { } scoped)
        {
            return scoped;
        }

        return ReadFromSettings(Path.Combine(basePath, "appsettings.json")) ?? FallbackConnectionString;
    }

    private static string? ReadFromSettings(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
            || connectionStrings.ValueKind != JsonValueKind.Object
            || !connectionStrings.TryGetProperty("SalvoDb", out var salvoDb)
            || salvoDb.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = salvoDb.GetString();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
