using Salvo.Application.Explanations;

namespace Salvo.Api;

public static class ExplanationEndpoints
{
    public static IEndpointRouteBuilder MapExplanationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/alerts/{alertId:guid}/explanation", RequestAsync)
            .WithName("RequestAlertExplanation")
            .WithTags("Explanations")
            .WithDescription(
                "Explains, in words, the evaluation this alert was opened on. Idempotent: when an "
                + "explanation already exists the existing one is returned with 'applied' false, so "
                + "a repeated request neither asks a provider twice nor changes anything. "
                + "'regenerate' asks for another attempt, and is allowed only over an explanation "
                + "that failed and still has attempts left. Every figure of the text is verified "
                + "against the evaluation before it is stored, so a provider that invents one "
                + "produces a failed explanation rather than a paragraph: that is a 200 with the row "
                + "in FAILED, never a server error. There is no separate GET — the alert detail "
                + "carries the explanation, and the absence of one is an ordinary state rather than "
                + "a 404.")
            .Produces<RequestExplanationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> RequestAsync(
        Guid alertId,
        RequestExplanationRequest? request,
        RequestExplanationHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(
                alertId,
                request?.Regenerate ?? false,
                cancellationToken);

            return result is null
                ? CreateProblem(
                    StatusCodes.Status404NotFound,
                    "ALERT_NOT_FOUND",
                    $"No alert exists with identifier {alertId}.")
                : TypedResults.Ok(result);
        }
        catch (ExplanationConflictException exception)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                ToCode(exception.Reason),
                exception.Message);
        }
    }

    private static string ToCode(ExplanationConflictReason reason)
    {
        return reason switch
        {
            ExplanationConflictReason.PendingInFlight => "EXPLANATION_PENDING",
            ExplanationConflictReason.AlreadyReady => "EXPLANATION_ALREADY_READY",
            ExplanationConflictReason.AttemptsExhausted => "EXPLANATION_ATTEMPTS_EXHAUSTED",
            _ => "EXPLANATION_CONFLICT",
        };
    }

    private static IResult CreateProblem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "Explanation request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}

/// <param name="Regenerate">
/// Ask for another attempt even though an explanation exists. Allowed only over one that failed and
/// has attempts left: regenerating a written explanation would replace a record somebody may have
/// formed a verdict on, and asking again while a provider is answering would pay twice.
/// </param>
public sealed record RequestExplanationRequest(bool? Regenerate);
