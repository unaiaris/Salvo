using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Api;

public static class ExternalDemoEndpoints
{
    /// <summary>
    /// The two demo triggers, registered only when the deployment declares itself a demo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// They exist because the console cannot press the real button. Calling the authenticated
    /// callback endpoint through the Next rewrite would put the shared secret in the browser, and
    /// calling it from a server action would put it in the Next process; section 10 of the Blueprint
    /// forbids both. And if the client composed the message, anyone with the console open could
    /// close any pending evaluation in whatever state they chose.
    /// </para>
    /// <para>
    /// So the client picks <em>which</em> evaluation and never <em>what</em> it says: neither
    /// contract has a field for a status. The verdict is asked of the provider adapter and injected
    /// through the same use case a real callback goes through.
    /// </para>
    /// </remarks>
    public static IEndpointRouteBuilder MapExternalDemoEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.GetValue<bool>("DemoData:Enabled"))
        {
            return endpoints;
        }

        endpoints.MapPost("/api/demo-data/external-callbacks:deliver", DeliverAsync)
            .WithName("DeliverExternalCallbacks")
            .WithTags("Demo data")
            .WithDescription(
                "Asks the mock provider what it would say about one pending external evaluation, or "
                + "about every pending one, and feeds that answer back in as a callback through the "
                + "same use case an external provider reaches. The caller chooses which evaluation "
                + "and never what it reports. The provider's instant is the moment the evaluation "
                + "was requested, so delivering the same one twice produces the same deduplication "
                + "key and is recognised as the replay it is.")
            .Produces<CallbackDeliverySummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/demo-data/external-evaluations:request", RequestCorpusAsync)
            .WithName("RequestCorpusExternalEvaluations")
            .WithTags("Demo data")
            .WithDescription(
                "Requests an external evaluation for every order this provider has never been asked "
                + "about. It covers the orders that never produced an alert, which the alert detail "
                + "cannot reach, without adding an orders screen. Each order goes through the "
                + "ordinary request use case, so the reservation, the idempotence and the failure "
                + "taxonomy are the same ones a single request gets.")
            .Produces<CorpusExternalEvaluationSummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> DeliverAsync(
        DeliverExternalCallbacksRequest? request,
        DeliverPendingCallbacksHandler handler,
        CancellationToken cancellationToken)
    {
        var summary = await handler.HandleAsync(request?.ExternalEvaluationId, cancellationToken);

        return summary is null
            ? CreateProblem(
                StatusCodes.Status404NotFound,
                "EXTERNAL_EVALUATION_NOT_FOUND",
                $"No external evaluation exists with identifier {request?.ExternalEvaluationId}.")
            : TypedResults.Ok(summary);
    }

    private static async Task<IResult> RequestCorpusAsync(
        RequestCorpusExternalEvaluationsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(
                await handler.HandleAsync(ExternalProvider.ExternalMock, cancellationToken));
        }
        catch (ExternalProviderNotRegisteredException exception)
        {
            return CreateProblem(
                StatusCodes.Status404NotFound,
                "PROVIDER_NOT_REGISTERED",
                exception.Message);
        }
    }

    private static IResult CreateProblem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "Demo external evaluation request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}

/// <param name="ExternalEvaluationId">
/// Which pending evaluation to deliver the callback of. Omitted means every pending one. There is
/// deliberately no field for the verdict: that comes from the provider, never from the caller.
/// </param>
public sealed record DeliverExternalCallbacksRequest(Guid? ExternalEvaluationId);
