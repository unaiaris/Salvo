using System.Globalization;
using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;

namespace Salvo.Api;

public static class AlertEndpoints
{
    private const int MaximumNoteLength = 2000;

    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/alerts", ListAlertsAsync)
            .WithName("ListAlerts")
            .WithTags("Alerts")
            .WithDescription(
                "One page of alerts. 'severity' filters on the frozen snapshot each alert was "
                + "opened with, while 'sort=SCORE_DESC' orders by the local score of the evaluation "
                + "the current run made current: the two describe different moments. An alert with "
                + "no current evaluation has no score to compare and sorts last. 'scoringRunSequence' "
                + "identifies the state the page was read from; a client that pages restarts when it "
                + "changes.")
            .Produces<ListAlertsResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/alerts/{id:guid}", GetAlertAsync)
            .WithName("GetAlert")
            .WithTags("Alerts")
            .Produces<AlertDetail>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/alerts/{id:guid}/review", ReviewAlertAsync)
            .WithName("ReviewAlert")
            .WithTags("Alerts")
            .Produces<AlertReviewResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> ListAlertsAsync(
        ListAlertsHandler handler,
        CancellationToken cancellationToken,
        string? status = null,
        string? severity = null,
        string? sort = null,
        int? page = null,
        int? pageSize = null)
    {
        AlertStatus? requestedStatus = null;
        if (status is not null)
        {
            if (!AlertWireNames.TryParseStatus(status, out var parsedStatus))
            {
                return CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "INVALID_STATUS",
                    "status must be OPEN, CONFIRMED_SAFE or REPORTED_FRAUD.");
            }

            requestedStatus = parsedStatus;
        }

        AlertSeverity? requestedSeverity = null;
        if (severity is not null)
        {
            if (!AlertWireNames.TryParseSeverity(severity, out var parsedSeverity))
            {
                return CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "INVALID_SEVERITY",
                    "severity must be MEDIUM, HIGH or CRITICAL.");
            }

            requestedSeverity = parsedSeverity;
        }

        var requestedSort = AlertSortOrder.CreatedDesc;
        if (sort is not null && !AlertSortWireNames.TryParse(sort, out requestedSort))
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_SORT",
                $"sort must be {AlertSortWireNames.CreatedDesc} or {AlertSortWireNames.ScoreDesc}.");
        }

        var requestedPage = page ?? 1;
        var requestedPageSize = pageSize ?? ListAlertsHandler.DefaultPageSize;

        if (requestedPage < 1)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_PAGE",
                "page must be greater than or equal to 1.");
        }

        if (requestedPageSize < 1 || requestedPageSize > ListAlertsHandler.MaximumPageSize)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_PAGE_SIZE",
                $"pageSize must be between 1 and {ListAlertsHandler.MaximumPageSize.ToString(CultureInfo.InvariantCulture)}.");
        }

        return TypedResults.Ok(await handler.HandleAsync(
            requestedStatus,
            requestedSeverity,
            requestedSort,
            requestedPage,
            requestedPageSize,
            cancellationToken));
    }

    private static async Task<IResult> GetAlertAsync(
        Guid id,
        GetAlertHandler handler,
        CancellationToken cancellationToken)
    {
        var detail = await handler.HandleAsync(id, cancellationToken);

        return detail is null ? NotFound(id) : TypedResults.Ok(detail);
    }

    private static async Task<IResult> ReviewAlertAsync(
        Guid id,
        ReviewAlertRequest? request,
        ReviewAlertHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || !AlertWireNames.TryParseStatus(request.NewStatus, out var newStatus))
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_STATUS",
                "newStatus must be CONFIRMED_SAFE or REPORTED_FRAUD.");
        }

        if (request.Note is { Length: > MaximumNoteLength })
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "NOTE_TOO_LONG",
                $"note must not exceed {MaximumNoteLength.ToString(CultureInfo.InvariantCulture)} characters.");
        }

        try
        {
            var result = await handler.HandleAsync(
                id,
                new(newStatus, request.Note, request.AcknowledgedDivergence ?? false),
                cancellationToken);

            return result is null ? NotFound(id) : TypedResults.Ok(result);
        }
        catch (AlertTransitionException exception)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_STATUS",
                exception.Message);
        }
        catch (AlertReviewConflictException exception)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                ToCode(exception.Reason),
                exception.Message);
        }
    }

    private static string ToCode(AlertReviewConflictReason reason)
    {
        return reason switch
        {
            AlertReviewConflictReason.AlreadyReviewedWithDifferentStatus => "ALERT_ALREADY_REVIEWED",
            AlertReviewConflictReason.AlreadyReviewedWithDifferentNote => "ALERT_REVIEW_NOTE_CONFLICT",
            AlertReviewConflictReason.DivergenceNotAcknowledged => "ALERT_DIVERGENCE_NOT_ACKNOWLEDGED",
            _ => "ALERT_REVIEW_CONFLICT",
        };
    }

    private static IResult NotFound(Guid id)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "ALERT_NOT_FOUND",
            $"No alert exists with identifier {id}.");
    }

    private static IResult CreateProblem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "Alert request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}

/// <param name="AcknowledgedDivergence">
/// Must be <see langword="true"/> to review an alert whose current evaluation sits in another
/// severity band than the snapshot it was opened with.
/// </param>
public sealed record ReviewAlertRequest(
    string? NewStatus,
    string? Note,
    bool? AcknowledgedDivergence);
