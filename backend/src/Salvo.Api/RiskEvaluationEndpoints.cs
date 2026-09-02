using Salvo.Application.Risk;

namespace Salvo.Api;

public static class RiskEvaluationEndpoints
{
    public static IEndpointRouteBuilder MapRiskEvaluationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/risk-evaluations:run", RunScoringAsync)
            .WithName("RunScoring")
            .WithTags("Risk evaluations")
            .Produces<ScoringRunSummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> RunScoringAsync(
        RunScoringHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
        }
        catch (ScoringRunConflictException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Scoring run rejected",
                detail: "A concurrent scoring run already persisted conflicting state. Retry the run.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "SCORING_RUN_CONFLICT",
                });
        }
    }
}
