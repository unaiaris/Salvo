namespace Salvo.Application.External;

public sealed class ListOrderExternalEvaluationsHandler(IExternalEvaluationStore store)
{
    /// <summary>
    /// The external evaluation history of an order, or <see langword="null"/> when no order carries
    /// that identifier. An empty history and a missing order are different answers.
    /// </summary>
    public async Task<OrderExternalEvaluationsResult?> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (await store.FindOrderAsync(orderId, cancellationToken) is null)
        {
            return null;
        }

        var evaluations = await store.ListByOrderAsync(orderId, cancellationToken);

        return new(orderId, [.. evaluations.Select(ExternalEvaluationProjection.ToView)]);
    }
}
