using Salvo.Domain.Evaluation;

namespace Salvo.Application.Risk;

public interface IEvaluationLabelReader
{
    Task<IReadOnlyDictionary<Guid, OrderEvaluationLabel>> GetByOrderIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken);
}
