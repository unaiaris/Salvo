namespace Salvo.Application.Metrics;

public sealed record ConfusionMatrixView(
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    int TrueNegatives);

public sealed record EvaluationMetricsView(
    ConfusionMatrixView Matrix,
    decimal? Precision,
    decimal? Recall,
    decimal? F1,
    decimal? FalsePositiveRate,
    decimal? FlagRate);

public sealed record ThresholdMetricsView(int Threshold, EvaluationMetricsView Metrics);

/// <summary>
/// Quality of the detection criterion, measured against the ground truth of a demo corpus.
/// </summary>
/// <remarks>
/// This is not the operational dashboard and it is not available unless the deployment declares
/// itself a demo: outside one, no merchant knows which orders really were fraud. The figures below
/// prove that the evaluation pipeline is honest — the split is temporal, the threshold is chosen on
/// the calibration side and applied unchanged to the holdout — not that the rules would generalize
/// to a corpus they were not built alongside.
/// </remarks>
/// <param name="ScoredOrders">Orders the current run scored.</param>
/// <param name="LabeledOrders">Of those, the ones that carry ground truth.</param>
/// <param name="UnlabeledOrders">
/// Of those, the ones that do not. Imported orders never carry a label, so this is normally
/// non-zero as soon as anybody imports a file; the metrics simply exclude them and say so.
/// </param>
/// <param name="CalibrationSweep">
/// The threshold sweep of the calibration cohort, collapsed to the thresholds where the confusion
/// matrix actually changes. A hundred and one near-identical rows say nothing the boundaries do
/// not.
/// </param>
/// <param name="SelectedThreshold">
/// The threshold the sweep selected on the calibration cohort. It is always present in
/// <paramref name="CalibrationSweep"/>.
/// </param>
/// <param name="Holdout">
/// The selected threshold applied, without retuning, to the cohort that was held out.
/// </param>
public sealed record EvaluationMetricsResult(
    long ScoringRunSequence,
    DateTimeOffset ScoringRunCompletedAt,
    string RuleConfigVersion,
    int ScoredOrders,
    int LabeledOrders,
    int UnlabeledOrders,
    int CalibrationOrders,
    int HoldoutOrders,
    IReadOnlyList<ThresholdMetricsView> CalibrationSweep,
    ThresholdMetricsView SelectedThreshold,
    EvaluationMetricsView Holdout);
