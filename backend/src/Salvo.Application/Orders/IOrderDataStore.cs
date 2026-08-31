using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Application.Orders;

public interface IOrderDataStore
{
    Task<IReadOnlyDictionary<OrderReference, Order>> GetOrdersByReferencesAsync(
        IReadOnlyCollection<OrderReference> references,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, OrderEvaluationLabel>> GetLabelsByOrderIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken);

    Task AddOrdersAsync(
        IReadOnlyCollection<Order> orders,
        CancellationToken cancellationToken);

    Task AddSeedDataAsync(
        IReadOnlyCollection<Order> orders,
        IReadOnlyCollection<OrderEvaluationLabel> labels,
        CancellationToken cancellationToken);
}
