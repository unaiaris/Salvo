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

    /// <summary>
    /// How many orders the store holds. Only asked for where the deployment sets a ceiling.
    /// </summary>
    Task<int> CountOrdersAsync(CancellationToken cancellationToken);

    Task AddOrdersAsync(
        IReadOnlyCollection<Order> orders,
        CancellationToken cancellationToken);

    Task AddSeedDataAsync(
        IReadOnlyCollection<Order> orders,
        IReadOnlyCollection<OrderEvaluationLabel> labels,
        CancellationToken cancellationToken);
}
