namespace Salvo.Api;

public static class SystemEndpoints
{
    /// <summary>
    /// What this backend can do, for a client that has to decide what to render.
    /// </summary>
    /// <remarks>
    /// Whether the demo data routes exist is runtime configuration of the API, and a client build
    /// is identical either way. Without this route the only signal would be a 404, which is
    /// indistinguishable from a misspelled path or an API that is down.
    /// </remarks>
    public static IEndpointRouteBuilder MapSystemEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var demoDataEnabled = configuration.GetValue<bool>("DemoData:Enabled");

        endpoints.MapGet(
                "/api/system/capabilities",
                () => TypedResults.Ok(new CapabilitiesResponse(demoDataEnabled, demoDataEnabled)))
            .WithName("GetCapabilities")
            .WithTags("System")
            .Produces<CapabilitiesResponse>(StatusCodes.Status200OK);

        return endpoints;
    }
}

/// <param name="DemoDataEnabled">
/// Whether <c>POST /api/demo-data/seed</c> and <c>GET /api/evaluation-metrics</c> are registered.
/// </param>
/// <param name="ExternalCallbackTriggerEnabled">
/// Whether the console may ask this API to deliver a provider callback on its own, through
/// <c>POST /api/demo-data/external-callbacks:deliver</c>. It rides on the same switch today and is
/// still asked separately: the console needs to know whether it can offer that button, and that is
/// not the same question as whether a demo corpus can be seeded.
/// </param>
public sealed record CapabilitiesResponse(bool DemoDataEnabled, bool ExternalCallbackTriggerEnabled);
