using Salvo.Domain.Evaluation;
using Salvo.Domain.Risk;

namespace Salvo.Application.Risk;

public sealed class EvaluateLocalRiskHandler(
    IRiskOrderReader orderReader,
    IEvaluationLabelReader labelReader)
{
    public async Task<LocalRiskEvaluationResult> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await orderReader.GetAllChronologicallyAsync(cancellationToken);
        var config = RuleConfig.E3V1;
        var assessments = TemporalRiskEngine.Score(orders, config);

        var orderIds = assessments.Select(assessment => assessment.OrderId).ToArray();
        var labels = await labelReader.GetByOrderIdsAsync(orderIds, cancellationToken);
        if (labels.Count != orderIds.Length || orderIds.Any(orderId => !labels.ContainsKey(orderId)))
        {
            throw new InvalidOperationException(
                "Evaluation requires exactly one ground-truth label for every scored order.");
        }

        var labeledScores = assessments
            .Select(assessment => new LabeledRiskScore(
                assessment.OrderId,
                assessment.OccurredAt,
                assessment.Score,
                labels[assessment.OrderId].IsFraudLabel))
            .ToArray();
        var split = RiskMetricsEvaluator.SplitByTime(labeledScores);
        var sweep = RiskMetricsEvaluator.Sweep(split.Calibration);
        var selected = RiskMetricsEvaluator.SelectBest(sweep);
        var holdout = RiskMetricsEvaluator.Evaluate(split.Holdout, selected.Threshold);

        return new(
            config.Version,
            assessments,
            sweep,
            selected,
            holdout);
    }
}
