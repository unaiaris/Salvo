using Salvo.Domain.Evaluation;
using Salvo.Domain.Risk;

namespace Salvo.Application.Risk;

public sealed record LocalRiskEvaluationResult(
    string ConfigVersion,
    IReadOnlyList<LocalRiskAssessment> Assessments,
    IReadOnlyList<ThresholdEvaluation> CalibrationSweep,
    ThresholdEvaluation SelectedThreshold,
    EvaluationMetrics HoldoutMetrics);
