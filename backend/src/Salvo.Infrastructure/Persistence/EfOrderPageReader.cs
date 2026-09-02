using Microsoft.EntityFrameworkCore;
using Salvo.Application.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfOrderPageReader(SalvoDbContext dbContext) : IOrderPageReader
{
    public async Task<OrderPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .OrderBy(order => order.OccurredAt)
            .ThenBy(order => order.MerchantId)
            .ThenBy(order => order.MerchantReferenceId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new(items, totalCount);
    }
}
