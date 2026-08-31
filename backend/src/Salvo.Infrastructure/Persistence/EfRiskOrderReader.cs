using Microsoft.EntityFrameworkCore;
using Salvo.Application.Risk;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfRiskOrderReader(SalvoDbContext dbContext) : IRiskOrderReader
{
    public async Task<IReadOnlyList<Order>> GetAllChronologicallyAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .OrderBy(order => order.OccurredAt)
            .ThenBy(order => order.MerchantId)
            .ThenBy(order => order.MerchantReferenceId)
            .ToListAsync(cancellationToken);
    }
}
