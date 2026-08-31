namespace Salvo.Domain.Evaluation;

public readonly record struct ConfusionMatrix(
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    int TrueNegatives)
{
    public int Total => TruePositives + FalsePositives + FalseNegatives + TrueNegatives;
}
