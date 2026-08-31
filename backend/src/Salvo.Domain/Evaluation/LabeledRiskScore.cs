namespace Salvo.Domain.Evaluation;

public sealed record LabeledRiskScore(
    Guid OrderId,
    DateTimeOffset OccurredAt,
    int Score,
    bool IsFraudLabel);
