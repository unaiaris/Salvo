using Salvo.Domain.Risk;

namespace Salvo.Application.Risk;

public interface IScoringRunStore
{
    /// <summary>
    /// Highest run sequence already persisted, or zero when no run exists yet.
    /// </summary>
    Task<long> GetLastRunSequenceAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Identifiers of the local evaluations that already exist for the given fingerprints. Querying
    /// them first is what keeps a run from failing wholesale against the unique index when part of
    /// the corpus is unchanged.
    /// </summary>
    Task<IReadOnlyDictionary<string, Guid>> GetLocalEvaluationIdsByFingerprintAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the run, the evaluations it appends and its per-order references as a single unit of
    /// work. A failure leaves none of the three behind.
    /// </summary>
    /// <exception cref="ScoringRunConflictException">
    /// A concurrent run already persisted conflicting state.
    /// </exception>
    Task SaveRunAsync(
        ScoringRun run,
        IReadOnlyCollection<RiskEvaluation> evaluationsToAppend,
        IReadOnlyCollection<RunEvaluation> runEvaluations,
        CancellationToken cancellationToken);

    /// <summary>
    /// The evaluation the latest run referenced for each order it covered, keyed by order. This is
    /// the current evaluation of an order: it does not depend on insertion timestamps, so a score
    /// that returns to an earlier value still resolves to the right row.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, RiskEvaluation>> GetCurrentEvaluationsAsync(
        CancellationToken cancellationToken);
}
