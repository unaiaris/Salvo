using Microsoft.EntityFrameworkCore;
using Salvo.Application.Risk;
using Salvo.Domain.Evaluation;

namespace Salvo.Infrastructure.Persistence;

public sealed class EfEvaluationLabelReader(SalvoDbContext dbContext) : IEvaluationLabelReader
{
    public async Task<IReadOnlyDictionary<Guid, OrderEvaluationLabel>> GetByOrderIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return new Dictionary<Guid, OrderEvaluationLabel>();
        }

        var ids = orderIds.Distinct().ToArray();
        return await dbContext.OrderEvaluationLabels
            .AsNoTracking()
            .Where(label => ids.Contains(label.OrderId))
            .ToDictionaryAsync(label => label.OrderId, cancellationToken);
    }
}
