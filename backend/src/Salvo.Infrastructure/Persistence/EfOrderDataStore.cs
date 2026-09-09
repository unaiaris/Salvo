using Microsoft.EntityFrameworkCore;
using Salvo.Application.Orders;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfOrderDataStore(SalvoDbContext dbContext) : IOrderDataStore
{
    public Task<int> CountOrdersAsync(CancellationToken cancellationToken)
    {
        return dbContext.Orders.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<OrderReference, Order>> GetOrdersByReferencesAsync(
        IReadOnlyCollection<OrderReference> references,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return new Dictionary<OrderReference, Order>();
        }

        var referenceSet = references.ToHashSet();
        var merchantIds = referenceSet.Select(reference => reference.MerchantId).Distinct().ToArray();
        var merchantReferenceIds = referenceSet
            .Select(reference => reference.MerchantReferenceId)
            .Distinct()
            .ToArray();
        var orders = await dbContext.Orders
            .Where(order =>
                merchantIds.Contains(order.MerchantId)
                && merchantReferenceIds.Contains(order.MerchantReferenceId))
            .ToListAsync(cancellationToken);

        return orders
            .Where(order => referenceSet.Contains(order.Reference))
            .ToDictionary(order => order.Reference);
    }

    public async Task<IReadOnlyDictionary<Guid, OrderEvaluationLabel>> GetLabelsByOrderIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return new Dictionary<Guid, OrderEvaluationLabel>();
        }

        var ids = orderIds.Distinct().ToArray();
        return await dbContext.OrderEvaluationLabels
            .Where(label => ids.Contains(label.OrderId))
            .ToDictionaryAsync(label => label.OrderId, cancellationToken);
    }

    public async Task AddOrdersAsync(
        IReadOnlyCollection<Order> orders,
        CancellationToken cancellationToken)
    {
        dbContext.Orders.AddRange(orders);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSeedDataAsync(
        IReadOnlyCollection<Order> orders,
        IReadOnlyCollection<OrderEvaluationLabel> labels,
        CancellationToken cancellationToken)
    {
        dbContext.Orders.AddRange(orders);
        dbContext.OrderEvaluationLabels.AddRange(labels);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
