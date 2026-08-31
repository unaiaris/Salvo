using Salvo.Application.Orders.Importing;

namespace Salvo.Infrastructure.Importing;

public sealed class OrderImportParser : IOrderImportParser
{
    public Task<OrderImportDocument> ParseAsync(
        Stream stream,
        OrderImportFormat format,
        CancellationToken cancellationToken)
    {
        return format switch
        {
            OrderImportFormat.Csv => CsvOrderImportParser.ParseAsync(stream, cancellationToken),
            OrderImportFormat.Json => JsonOrderImportParser.ParseAsync(stream, cancellationToken),
            _ => throw new OrderImportDocumentException(
                "UNSUPPORTED_FORMAT",
                "The declared import format is not supported.",
                OrderImportDocumentFailure.UnsupportedFormat),
        };
    }
}
