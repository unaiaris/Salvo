namespace Salvo.Application.Orders.Importing;

public interface IOrderImportParser
{
    Task<OrderImportDocument> ParseAsync(
        Stream stream,
        OrderImportFormat format,
        CancellationToken cancellationToken);
}
