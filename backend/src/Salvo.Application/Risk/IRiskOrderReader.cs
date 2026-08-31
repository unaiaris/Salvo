using Salvo.Domain.Orders;

namespace Salvo.Application.Risk;

public interface IRiskOrderReader
{
    Task<IReadOnlyList<Order>> GetAllChronologicallyAsync(CancellationToken cancellationToken);
}
