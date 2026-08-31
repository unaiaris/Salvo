namespace Salvo.Domain.Evaluation;

public sealed class OrderEvaluationLabel
{
    private OrderEvaluationLabel()
    {
    }

    public OrderEvaluationLabel(Guid orderId, bool isFraudLabel, DateTimeOffset createdAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("orderId must be a non-empty GUID.", nameof(orderId));
        }

        OrderId = orderId;
        IsFraudLabel = isFraudLabel;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid OrderId { get; private set; }

    public bool IsFraudLabel { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
