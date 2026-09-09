using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Explanations;

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

        var demoDataEnabled = DemoDataSwitches.Enabled(configuration);
        var demoSeedEnabled = DemoDataSwitches.SeedEnabled(configuration);

        // Resolved rather than re-read from configuration: composition already parsed
        // SALVO_LANGUAGE and refused to start on a value this build cannot write, so what is
        // published here is the same value the explanations are stamped with. A second parse would
        // be a second chance to disagree.
        var language = endpoints.ServiceProvider.GetRequiredService<DeploymentLanguage>().Wire;

        endpoints.MapGet(
                "/api/system/capabilities",
                () => TypedResults.Ok(
                    new CapabilitiesResponse(
                        demoDataEnabled,
                        demoSeedEnabled,
                        demoDataEnabled,
                        language)))
            .WithName("GetCapabilities")
            .WithTags("System")
            .Produces<CapabilitiesResponse>(StatusCodes.Status200OK);

        return endpoints;
    }
}

/// <param name="DemoDataEnabled">
/// Whether this deployment declares itself a demonstration: <c>GET /api/evaluation-metrics</c> and
/// the provider triggers are registered.
/// </param>
/// <param name="DemoSeedEnabled">
/// Whether <c>POST /api/demo-data/seed</c> and its preview are registered. Asked separately from
/// <see cref="DemoDataEnabled"/> because the public instance turns this one off on its own: its
/// database arrives baked into the image, so nobody there needs to seed, and the seed route was the
/// only one with which a visitor could leave the console unusable for the next one.
/// </param>
/// <param name="ExternalCallbackTriggerEnabled">
/// Whether the console may ask this API to deliver a provider callback on its own, through
/// <c>POST /api/demo-data/external-callbacks:deliver</c>. It rides on the same switch today and is
/// still asked separately: the console needs to know whether it can offer that button, and that is
/// not the same question as whether a demo corpus can be seeded.
/// </param>
/// <param name="Language">
/// The language this deployment writes explanations in and the console composes itself in,
/// <c>es</c> or <c>pt</c>.
/// <para>
/// It is published here rather than read from the environment by the console because there must be
/// exactly one reader of <c>SALVO_LANGUAGE</c>. Two independent readers of one variable is a
/// deployment where a misconfigured console renders Portuguese around a Spanish paragraph and
/// nothing anywhere reports a problem. Taking it from this response makes that disagreement
/// impossible to represent.
/// </para>
/// <para>
/// Not negotiated per request. <c>Accept-Language</c> would put the identity of a stored
/// explanation at the mercy of whoever asked first.
/// </para>
/// </param>
public sealed record CapabilitiesResponse(
    bool DemoDataEnabled,
    bool DemoSeedEnabled,
    bool ExternalCallbackTriggerEnabled,
    string Language);
