using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;
using Salvo.Infrastructure;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api;

public sealed class Program
{
    private Program()
    {
    }

    public static void Main(string[] args)
    {
        // The alert policy is a total function over the alertable score range only as long as its
        // lowest band starts exactly at the flag threshold. Checking it here makes a future rule
        // configuration that lowers the threshold fail at startup instead of leaving flagged orders
        // without a severity band. Every known configuration is checked, not the current one: a
        // stored evaluation is read under the version that wrote it, so a band gap in a version
        // still on disk is just as unserviceable as one in the version being written.
        foreach (var ruleConfig in RuleConfig.Known)
        {
            AlertPolicy.E4V1.Validate(ruleConfig);
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = OrderEndpoints.MaximumFileSizeBytes + (64 * 1024);
        });
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        MigrateIfAsked(app);

        app.MapOpenApi();

        app.MapGet(
                "/health",
                () => TypedResults.Ok(new HealthResponse("ok", "salvo-api")))
            .WithName("GetHealth")
            .WithTags("System");

        app.MapSystemEndpoints(builder.Configuration);
        app.MapOrderEndpoints(builder.Configuration);
        app.MapRiskEvaluationEndpoints();
        app.MapAlertEndpoints();
        app.MapExplanationEndpoints();
        app.MapExternalEvaluationEndpoints();
        app.MapExternalCallbackEndpoints(builder.Configuration);
        app.MapExternalDemoEndpoints(builder.Configuration);
        app.MapDashboardEndpoints();
        app.MapEvaluationMetricsEndpoints(builder.Configuration);

        app.Run();
    }

    /// <summary>
    /// Applies the pending migrations, and only when the deployment asked for it by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Off by default, and that is the whole point of the flag.</strong> Every other way of
    /// running this API — <c>dotnet run</c>, <c>scripts/demo.sh</c>, <c>scripts/smoke-ui.sh</c>,
    /// <c>scripts/capturas.sh</c> — migrates from the outside with <c>dotnet ef database update</c>
    /// and expects the process to leave the file alone; <c>Salvo-Getting-Started.md</c> says the
    /// application never touches a database it did not create. Migrating on startup is right for a
    /// container that boots with an empty volume and wrong for a machine that keeps its data, so
    /// the difference is declared rather than inherited.
    /// </para>
    /// <para>
    /// <strong>Why <c>Migrate()</c> and not a SQL script generated at build time.</strong> Both
    /// avoid the SDK in the runtime image — the migrations are compiled into
    /// <c>Salvo.Infrastructure</c>, so nothing here needs <c>dotnet ef</c>. The deciding argument is
    /// which one the gate already watches: <c>scripts/check.sh</c> runs
    /// <c>dotnet ef migrations has-pending-model-changes</c>, so a model that drifts from its
    /// migrations fails the build. A generated script would be a second artefact of the same truth,
    /// and nothing in this repository would notice it going stale.
    /// </para>
    /// <para>
    /// It runs before the first request is served, and a failure stops the process instead of
    /// leaving an API answering over a database that lacks its tables.
    /// </para>
    /// </remarks>
    private static void MigrateIfAsked(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        StartupLog.Migrating(app.Logger);
        dbContext.Database.Migrate();
    }
}

/// <summary>
/// The startup messages, as compiled delegates.
/// </summary>
/// <remarks>
/// Generated rather than written by hand because <c>CA1848</c> is an error in this build: the
/// analyzers are on and warnings are errors, so <c>LogInformation</c> with a plain string does not
/// compile. The source generator produces the same message without allocating on every call.
/// </remarks>
internal static partial class StartupLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database:MigrateOnStartup is on: applying pending migrations before serving.")]
    public static partial void Migrating(ILogger logger);
}

public sealed record HealthResponse(string Status, string Service);
