namespace Salvo.Domain.Risk;

/// <summary>
/// The evaluation a given scoring run assigned to a given order, whether the run appended it or
/// reused an existing one. Exactly one row per order and run.
/// </summary>
public sealed class RunEvaluation
{
    private RunEvaluation()
    {
    }

    private RunEvaluation(Guid runId, Guid orderId, Guid evaluationId)
    {
        RunId = runId;
        OrderId = orderId;
        EvaluationId = evaluationId;
    }

    public Guid RunId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid EvaluationId { get; private set; }

    public static RunEvaluation Create(Guid runId, Guid orderId, Guid evaluationId)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("runId must be a non-empty GUID.", nameof(runId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("orderId must be a non-empty GUID.", nameof(orderId));
        }

        if (evaluationId == Guid.Empty)
        {
            throw new ArgumentException("evaluationId must be a non-empty GUID.", nameof(evaluationId));
        }

        return new(runId, orderId, evaluationId);
    }
}
