namespace Salvo.Application.Risk;

public sealed record ScoringRunSummary(
    Guid RunId,
    long Sequence,
    string RuleConfigVersion,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int OrderCount,
    int EvaluationsCreated,
    int EvaluationsReused);
