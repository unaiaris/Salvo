using System.Globalization;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;

namespace Salvo.Api;

public static class OrderEndpoints
{
    public const long MaximumFileSizeBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        endpoints.MapPost("/api/order-imports", ImportOrdersAsync)
            .WithName("ImportOrders")
            .WithTags("Orders")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportOrdersResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .DisableAntiforgery();

        endpoints.MapGet("/api/orders", ListOrdersAsync)
            .WithName("ListOrders")
            .WithTags("Orders")
            .Produces<ListOrdersResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        if (DemoDataSwitches.SeedEnabled(configuration))
        {
            endpoints.MapPost("/api/demo-data/seed", SeedDemoOrdersAsync)
                .WithName("SeedDemoOrders")
                .WithTags("Demo data")
                .WithDescription(
                    "Loads the demo corpus. Idempotent: an order that is already there with the "
                    + "same facts is left alone. It refuses without writing anything when the "
                    + "database holds orders with these merchant references and different facts, "
                    + "and says which of the two causes it is.")
                .Produces<SeedDemoOrdersResult>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status409Conflict);

            endpoints.MapGet("/api/demo-data/seed-preview", PreviewDemoSeedAsync)
                .WithName("GetDemoSeedPreview")
                .WithTags("Demo data")
                .WithDescription(
                    "What loading the demo corpus would do, without doing it. The console reads it "
                    + "when the import screen opens so that a database holding the previous corpus "
                    + "is announced before the button is pressed rather than discovered by "
                    + "pressing it. It runs the very same comparison the load runs, and writes "
                    + "nothing.")
                .Produces<DemoSeedPreviewResult>(StatusCodes.Status200OK);
        }

        return endpoints;
    }

    private static async Task<IResult> ImportOrdersAsync(
        HttpRequest request,
        ImportOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return CreateProblem(
                StatusCodes.Status415UnsupportedMediaType,
                "UNSUPPORTED_MEDIA_TYPE",
                "The request must use multipart/form-data.");
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return CreateProblem(
                StatusCodes.Status413PayloadTooLarge,
                "FILE_TOO_LARGE",
                $"The import file must not exceed {MaximumFileSizeBytes.ToString(CultureInfo.InvariantCulture)} bytes.");
        }

        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "FILE_REQUIRED",
                "A non-empty file field is required.");
        }

        if (file.Length > MaximumFileSizeBytes)
        {
            return CreateProblem(
                StatusCodes.Status413PayloadTooLarge,
                "FILE_TOO_LARGE",
                $"The import file must not exceed {MaximumFileSizeBytes.ToString(CultureInfo.InvariantCulture)} bytes.");
        }

        if (!TryParseFormat(form["format"], out var format))
        {
            return CreateProblem(
                StatusCodes.Status415UnsupportedMediaType,
                "UNSUPPORTED_FORMAT",
                "format must be CSV or JSON.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await handler.HandleAsync(stream, format, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (OrderImportDocumentException exception)
        {
            var statusCode = exception.Failure switch
            {
                OrderImportDocumentFailure.TooManyRecords => StatusCodes.Status413PayloadTooLarge,
                OrderImportDocumentFailure.UnsupportedFormat => StatusCodes.Status415UnsupportedMediaType,
                OrderImportDocumentFailure.CapacityReached => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };

            return CreateProblem(statusCode, exception.Code, exception.Message);
        }
    }

    private static async Task<IResult> ListOrdersAsync(
        ListOrdersHandler handler,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        var requestedPage = page ?? 1;
        var requestedPageSize = pageSize ?? ListOrdersHandler.DefaultPageSize;

        if (requestedPage < 1)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_PAGE",
                "page must be greater than or equal to 1.");
        }

        if (requestedPageSize < 1 || requestedPageSize > ListOrdersHandler.MaximumPageSize)
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_PAGE_SIZE",
                $"pageSize must be between 1 and {ListOrdersHandler.MaximumPageSize.ToString(CultureInfo.InvariantCulture)}.");
        }

        return TypedResults.Ok(
            await handler.HandleAsync(requestedPage, requestedPageSize, cancellationToken));
    }

    private static async Task<IResult> SeedDemoOrdersAsync(
        SeedDemoOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
        }
        catch (DemoSeedConflictException exception)
        {
            // Two codes rather than one. "The corpus you have is the previous version of this
            // corpus" and "somebody imported orders that collide" lead to different next steps,
            // and the earlier single code said neither.
            return CreateProblem(
                StatusCodes.Status409Conflict,
                exception.Reason == DemoSeedConflictReason.PreviousCorpus
                    ? "DEMO_DATA_PREVIOUS_CORPUS"
                    : "DEMO_DATA_CONFLICT",
                exception.Message);
        }
    }

    private static async Task<IResult> PreviewDemoSeedAsync(
        SeedDemoOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handler.PreviewAsync(cancellationToken));
    }

    private static bool TryParseFormat(string? value, out OrderImportFormat format)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "CSV":
                format = OrderImportFormat.Csv;
                return true;
            case "JSON":
                format = OrderImportFormat.Json;
                return true;
            default:
                format = default;
                return false;
        }
    }

    private static IResult CreateProblem(
        int statusCode,
        string code,
        string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "Order request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}
