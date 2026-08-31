namespace Salvo.Domain.Evaluation;

public sealed record TemporalEvaluationSplit(
    IReadOnlyList<LabeledRiskScore> Calibration,
    IReadOnlyList<LabeledRiskScore> Holdout);
