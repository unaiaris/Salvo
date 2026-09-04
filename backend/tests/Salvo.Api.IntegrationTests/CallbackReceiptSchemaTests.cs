using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// What the migration actually built for the receipts, read back from <c>sqlite_master</c>.
/// </summary>
/// <remarks>
/// The model configuration and the shipped schema are two artifacts, and only the second is what a
/// deployed database has. These assertions are about the second.
/// </remarks>
public sealed class CallbackReceiptSchemaTests
{
    /// <summary>
    /// The index the whole deduplication rests on. Scoped by provider, not by key alone: two
    /// providers may perfectly well mint the same identifier, and a global uniqueness would make one
    /// of them silently discard the other's callbacks.
    /// </summary>
    [Fact]
    public async Task ACallbackIsUniqueWithinItsProvider()
    {
        var definition = await ReadDefinitionAsync("ux_callback_receipts_provider_key");

        Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
        Assert.Matches(@"""?provider""?\s*,\s*""?deduplication_key""?", definition);
    }

    /// <summary>
    /// The state list has no <c>DUPLICATE</c>. It cannot: a second row with the same key would
    /// violate the very index that detects the repetition.
    /// </summary>
    [Fact]
    public async Task TheReceiptStatesAreTheFiveOfTheTransitionTable()
    {
        var definition = await ReadDefinitionAsync("callback_receipts");

        Assert.Contains(
            "status IN ('APPLIED', 'NO_OP', 'SUPERSEDED', 'CONFLICTING', 'UNMATCHED')",
            definition,
            StringComparison.Ordinal);
        Assert.DoesNotContain("DUPLICATE", definition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheTableRefusesRowsThatContradictThemselves()
    {
        var definition = await ReadDefinitionAsync("callback_receipts");

        // A message with nothing to correlate by could never find its evaluation; unmatched and
        // unprocessed are the same fact and can never disagree.
        Assert.Contains("ck_callback_receipts_correlation", definition, StringComparison.Ordinal);
        Assert.Contains("ck_callback_receipts_processed", definition, StringComparison.Ordinal);
    }

    /// <summary>
    /// Late linking searches by either half of the correlation, so both have to be indexed.
    /// </summary>
    [Fact]
    public async Task BothHalvesOfTheCorrelationAreIndexed()
    {
        Assert.Contains(
            "CREATE INDEX",
            await ReadDefinitionAsync("ix_callback_receipts_provider_identifier"),
            StringComparison.Ordinal);
        Assert.Contains(
            "CREATE INDEX",
            await ReadDefinitionAsync("ix_callback_receipts_provider_reference"),
            StringComparison.Ordinal);
    }

    private static async Task<string> ReadDefinitionAsync(string name)
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE name = $name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        var sql = await command.ExecuteScalarAsync();

        return Assert.IsType<string>(sql);
    }
}
