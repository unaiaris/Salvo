using Salvo.Application.Dashboard;

namespace Salvo.Api;

public static class DashboardEndpoints
{
    /// <summary>
    /// The operational dashboard. Always available and always answerable: an unscored or empty
    /// corpus is described, not rejected.
    /// </summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", GetDashboardAsync)
            .WithName("GetDashboard")
            .WithTags("Dashboard")
            .WithDescription(
                "Operational state of the current scoring run. Amounts are reported per currency "
                + "and never summed across them; reported fraud is aggregated by distinct order. "
                + "No field is derived from ground-truth labels, which do not exist outside a demo "
                + "corpus.")
            .Produces<DashboardResult>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetDashboardAsync(
        GetDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
    }
}
