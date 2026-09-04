using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// What the migration actually built, read back from <c>sqlite_master</c>.
/// </summary>
/// <remarks>
/// The EF model configuration and the shipped schema are two different artifacts, and only the
/// second one is what a deployed database has. These assertions are about the second.
/// </remarks>
public sealed class ExternalEvaluationSchemaTests
{
    /// <summary>
    /// The index that makes the two-phase request work. It has to be partial: an order legitimately
    /// accumulates external evaluations over time, and a total index would forbid the second one.
    /// </summary>
    [Fact]
    public async Task OnlyOneExternalEvaluationOfAnOrderCanBeWaitingForAnAnswer()
    {
        var definition = await ReadDefinitionAsync("ux_external_evaluations_pending_order");

        Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
        Assert.Matches(@"WHERE\s+""?status""?\s*=\s*'PENDING'", definition);
    }

    /// <summary>
    /// The other half of the double correlation. The identifier does not exist during the
    /// reservation and for some failures it never arrives, so the uniqueness has to be partial too.
    /// </summary>
    [Fact]
    public async Task AProviderIdentifierIsUniqueOnlyOnceItExists()
    {
        var definition = await ReadDefinitionAsync("ux_external_evaluations_provider_identifier");

        Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
        Assert.Matches(@"WHERE\s+""?external_evaluation_id""?\s+IS\s+NOT\s+NULL", definition);
    }

    [Fact]
    public async Task TheTableRefusesRowsThatContradictThemselves()
    {
        var definition = await ReadDefinitionAsync("external_evaluations");

        // Settled and not settled cannot disagree with the instant, an error code belongs only to an
        // error, and provenance exists exactly when a verdict does.
        Assert.Contains("ck_external_evaluations_settlement", definition, StringComparison.Ordinal);
        Assert.Contains("ck_external_evaluations_error_status", definition, StringComparison.Ordinal);
        Assert.Contains("ck_external_evaluations_settled_by_consistency", definition, StringComparison.Ordinal);
    }

    /// <summary>
    /// The narrowing the migration performs. The external lifecycle left this table, so a row here
    /// can only be a local evaluation, and a local evaluation always has an answer.
    /// </summary>
    [Fact]
    public async Task TheLocalEvaluationTableNoLongerCarriesTheExternalLifecycle()
    {
        var definition = await ReadDefinitionAsync("risk_evaluations");

        Assert.DoesNotContain("external_evaluation_id", definition, StringComparison.Ordinal);
        Assert.DoesNotContain("error_code", definition, StringComparison.Ordinal);
        Assert.Matches(@"CHECK\s*\(\s*source\s*=\s*'LOCAL'\s*\)", definition);
        Assert.Matches(@"CHECK\s*\(\s*status\s+IN\s*\(\s*'APPROVED'\s*,\s*'DENIED'\s*\)\s*\)", definition);
    }

    private static async Task<string> ReadDefinitionAsync(string name)
    {
        await using var factory = new SalvoApiFactory();
        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE name = $name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }
}
