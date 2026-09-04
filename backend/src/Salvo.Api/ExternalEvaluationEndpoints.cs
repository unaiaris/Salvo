using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Api;

public static class ExternalEvaluationEndpoints
{
    public static IEndpointRouteBuilder MapExternalEvaluationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{orderId:guid}/external-evaluations", RequestAsync)
            .WithName("RequestExternalEvaluation")
            .WithTags("External evaluations")
            .WithDescription(
                "Asks an external provider to evaluate the order. Idempotent: when the order "
                + "already has an evaluation the existing one is returned with 'applied' false, so "
                + "a repeated request neither creates a second evaluation on the provider side nor "
                + "changes anything here. 'requestNew' asks for another evaluation anyway, and is "
                + "allowed only when the current one ended in ERROR. A provider failure is not a "
                + "server error for the console: the row settles in ERROR or stays PENDING and the "
                + "response is 200 with that row.")
            .Produces<RequestExternalEvaluationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/api/orders/{orderId:guid}/external-evaluations", ListAsync)
            .WithName("ListOrderExternalEvaluations")
            .WithTags("External evaluations")
            .WithDescription(
                "The external evaluation history of the order, oldest first. An order may accumulate "
                + "several over time; only one of them is ever waiting for an answer.")
            .Produces<OrderExternalEvaluationsResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/external-evaluations/{id:guid}", GetAsync)
            .WithName("GetExternalEvaluation")
            .WithTags("External evaluations")
            .Produces<ExternalEvaluationView>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/external-evaluations:reconcile", ReconcileAsync)
            .WithName("ReconcileExternalEvaluations")
            .WithTags("External evaluations")
            .WithDescription(
                "Asks the provider again about every evaluation still waiting for an answer, and "
                + "applies whatever it says. Explicit and manual, like the scoring run: this system "
                + "has no background jobs and no automatic retries. Each evaluation is saved on its "
                + "own, so one that another writer moved first is reported in 'conflicted' without "
                + "rolling back the rest.")
            .Produces<ReconciliationSummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> RequestAsync(
        Guid orderId,
        RequestExternalEvaluationRequest? request,
        RequestExternalEvaluationHandler handler,
        CancellationToken cancellationToken)
    {
        var provider = ExternalProvider.ExternalMock;
        if (request?.Provider is { } requested
            && !ExternalEvaluationWireNames.TryParseProvider(requested, out provider))
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_PROVIDER",
                $"provider must be {ExternalEvaluationWireNames.ExternalMock} or "
                + $"{ExternalEvaluationWireNames.KoinSandbox}.");
        }

        try
        {
            var result = await handler.HandleAsync(
                orderId,
                provider,
                request?.RequestNew ?? false,
                cancellationToken);

            return result is null ? OrderNotFound(orderId) : TypedResults.Ok(result);
        }
        catch (ExternalProviderNotRegisteredException exception)
        {
            return CreateProblem(
                StatusCodes.Status404NotFound,
                "PROVIDER_NOT_REGISTERED",
                exception.Message);
        }
        catch (ExternalEvaluationConflictException exception)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                ToCode(exception.Reason),
                exception.Message);
        }
    }

    private static async Task<IResult> ListAsync(
        Guid orderId,
        ListOrderExternalEvaluationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(orderId, cancellationToken);

        return result is null ? OrderNotFound(orderId) : TypedResults.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        GetExternalEvaluationHandler handler,
        CancellationToken cancellationToken)
    {
        var evaluation = await handler.HandleAsync(id, cancellationToken);

        return evaluation is null
            ? CreateProblem(
                StatusCodes.Status404NotFound,
                "EXTERNAL_EVALUATION_NOT_FOUND",
                $"No external evaluation exists with identifier {id}.")
            : TypedResults.Ok(evaluation);
    }

    private static async Task<IResult> ReconcileAsync(
        ReconcileExternalEvaluationsHandler handler,
        CancellationToken cancellationToken)
    {
        var summary = await handler.HandleAsync(cancellationToken);

        // A sweep that found work and lost every single row to another writer achieved nothing, and
        // saying so is more useful than a summary of zeros: the recovery is to run it again, once
        // the other writer is done. A partial loss is normal and reported in the summary.
        return summary.Examined > 0 && summary.Conflicted == summary.Examined
            ? CreateProblem(
                StatusCodes.Status409Conflict,
                "RECONCILIATION_CONFLICT",
                "Every pending evaluation this sweep examined was moved by a concurrent writer. "
                + "Run the reconciliation again.")
            : TypedResults.Ok(summary);
    }

    private static string ToCode(ExternalEvaluationConflictReason reason)
    {
        return reason switch
        {
            ExternalEvaluationConflictReason.PendingEvaluationExists => "EXTERNAL_EVALUATION_PENDING",
            ExternalEvaluationConflictReason.AlreadySettled => "EXTERNAL_EVALUATION_SETTLED",
            _ => "EXTERNAL_EVALUATION_CONFLICT",
        };
    }

    private static IResult OrderNotFound(Guid orderId)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "ORDER_NOT_FOUND",
            $"No order exists with identifier {orderId}.");
    }

    private static IResult CreateProblem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "External evaluation request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}

/// <param name="Provider">
/// Which provider to ask. Defaults to the mock, the only one this build registers.
/// </param>
/// <param name="RequestNew">
/// Ask for another evaluation even though the order already has one. Allowed only when the current
/// evaluation ended in ERROR: asking again after a verdict would create a second evaluation on the
/// provider side for a question already answered.
/// </param>
public sealed record RequestExternalEvaluationRequest(string? Provider, bool? RequestNew);
