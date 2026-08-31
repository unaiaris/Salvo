namespace Salvo.Domain.Evaluation;

public sealed record EvaluationMetrics(
    ConfusionMatrix Matrix,
    decimal? Precision,
    decimal? Recall,
    decimal? F1,
    decimal? FalsePositiveRate,
    decimal? FlagRate)
{
    public static EvaluationMetrics From(ConfusionMatrix matrix)
    {
        var precision = Divide(matrix.TruePositives, matrix.TruePositives + matrix.FalsePositives);
        var recall = Divide(matrix.TruePositives, matrix.TruePositives + matrix.FalseNegatives);
        var f1 = precision is not null && recall is not null && precision + recall > 0
            ? 2 * precision * recall / (precision + recall)
            : null;

        return new(
            matrix,
            precision,
            recall,
            f1,
            Divide(matrix.FalsePositives, matrix.FalsePositives + matrix.TrueNegatives),
            Divide(matrix.TruePositives + matrix.FalsePositives, matrix.Total));
    }

    private static decimal? Divide(int numerator, int denominator)
    {
        return denominator == 0 ? null : (decimal)numerator / denominator;
    }
}
