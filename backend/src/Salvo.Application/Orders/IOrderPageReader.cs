namespace Salvo.Application.Orders;

public interface IOrderPageReader
{
    /// <summary>
    /// Reads one chronological page of persisted orders.
    /// </summary>
    Task<OrderPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
}
