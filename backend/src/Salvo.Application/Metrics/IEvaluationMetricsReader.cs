namespace Salvo.Application.Metrics;

/// <summary>
/// The scoring run the quality metrics describe.
/// </summary>
public sealed record MetricsRun(
    Guid Id,
    long Sequence,
    string RuleConfigVersion,
    DateTimeOffset CompletedAt);

/// <summary>
/// One order the run covered, with the score its current evaluation carries.
/// </summary>
/// <param name="Score">
/// <see langword="null"/> when the current evaluation carries no score, which no local evaluation
/// ever does.
/// </param>
public sealed record ScoredOrderRow(Guid OrderId, DateTimeOffset OccurredAt, int? Score);

/// <summary>
/// Reads the persisted scores of the current run for the quality surface. Ground-truth labels are
/// not part of this port: they are read separately through
/// <see cref="Salvo.Application.Risk.IEvaluationLabelReader"/>, and only for the orders this run
/// covered.
/// </summary>
public interface IEvaluationMetricsReader
{
    Task<MetricsRun?> GetCurrentRunAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ScoredOrderRow>> GetScoredOrdersAsync(
        Guid runId,
        CancellationToken cancellationToken);
}
