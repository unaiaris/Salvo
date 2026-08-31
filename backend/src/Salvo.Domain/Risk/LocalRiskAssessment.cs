namespace Salvo.Domain.Risk;

public sealed record LocalRiskAssessment(
    Guid OrderId,
    DateTimeOffset OccurredAt,
    int Score,
    bool IsFlagged,
    IReadOnlyList<RiskSignal> Signals);
