using Microsoft.AspNetCore.Http.Features;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;
using Salvo.Infrastructure;

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
        // without a severity band.
        AlertPolicy.E4V1.Validate(RuleConfig.E3V1);

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = OrderEndpoints.MaximumFileSizeBytes + (64 * 1024);
        });
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

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
        app.MapDashboardEndpoints();
        app.MapEvaluationMetricsEndpoints(builder.Configuration);

        app.Run();
    }
}

public sealed record HealthResponse(string Status, string Service);
