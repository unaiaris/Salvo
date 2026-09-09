using Salvo.Application.Metrics;

namespace Salvo.Api;

public static class EvaluationMetricsEndpoints
{
    /// <summary>
    /// The quality surface, registered only when the deployment declares itself a demo.
    /// </summary>
    /// <remarks>
    /// It is gated exactly like the demo seed: measuring a criterion requires ground truth, and
    /// ground truth only exists because this corpus is synthetic. A client discovers whether the
    /// route exists through <c>GET /api/system/capabilities</c> rather than by probing for a 404.
    /// </remarks>
    public static IEndpointRouteBuilder MapEvaluationMetricsEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!DemoDataSwitches.Enabled(configuration))
        {
            return endpoints;
        }

        endpoints.MapGet("/api/evaluation-metrics", GetEvaluationMetricsAsync)
            .WithName("GetEvaluationMetrics")
            .WithTags("Demo data")
            .WithDescription(
                "Quality of the deterministic criterion over the current scoring run, measured "
                + "against the ground truth of the demo corpus. Orders without a label are counted "
                + "and excluded, never a reason to fail.")
            .Produces<EvaluationMetricsResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetEvaluationMetricsAsync(
        GetEvaluationMetricsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
        }
        catch (EvaluationMetricsUnavailableException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Evaluation metrics unavailable",
                detail: exception.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "METRICS_UNAVAILABLE",
                });
        }
    }
}
