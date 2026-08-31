using Salvo.Infrastructure;

namespace Salvo.Api;

public sealed class Program
{
    private Program()
    {
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.MapOpenApi();

        app.MapGet(
                "/health",
                () => TypedResults.Ok(new HealthResponse("ok", "salvo-api")))
            .WithName("GetHealth")
            .WithTags("System");

        app.Run();
    }
}

public sealed record HealthResponse(string Status, string Service);
