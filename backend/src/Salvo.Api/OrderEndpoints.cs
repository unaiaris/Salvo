using System.Globalization;
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
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .DisableAntiforgery();

        if (configuration.GetValue<bool>("DemoData:Enabled"))
        {
            endpoints.MapPost("/api/demo-data/seed", SeedDemoOrdersAsync)
                .WithName("SeedDemoOrders")
                .WithTags("Demo data")
                .Produces<SeedDemoOrdersResult>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status409Conflict);
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
                _ => StatusCodes.Status400BadRequest,
            };

            return CreateProblem(statusCode, exception.Code, exception.Message);
        }
    }

    private static async Task<IResult> SeedDemoOrdersAsync(
        SeedDemoOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
        }
        catch (DemoSeedConflictException)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                "DEMO_DATA_CONFLICT",
                "The demo dataset conflicts with existing immutable order data.");
        }
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
            title: "Order import request rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}
