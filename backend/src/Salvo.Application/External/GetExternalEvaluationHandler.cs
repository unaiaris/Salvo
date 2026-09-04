namespace Salvo.Application.External;

public sealed class GetExternalEvaluationHandler(IExternalEvaluationStore store)
{
    public async Task<ExternalEvaluationView?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var evaluation = await store.FindAsync(id, cancellationToken);

        return evaluation is null ? null : ExternalEvaluationProjection.ToView(evaluation);
    }
}
