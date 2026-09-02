using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
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
    /// The persisted alert history of the given orders, for the orders that have one. Reading it
    /// from the database rather than from the delta of the run is what keeps the creation predicate
    /// honest when several runs happen over a growing corpus.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, OrderAlertState>> GetAlertStatesAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the run, the evaluations it appends, its per-order references and the alerts it
    /// opens as a single unit of work. A failure leaves none of the four behind.
    /// </summary>
    /// <exception cref="ScoringRunConflictException">
    /// A concurrent run already persisted conflicting state, or another run already opened an alert
    /// for one of these orders.
    /// </exception>
    Task SaveRunAsync(
        ScoringRun run,
        IReadOnlyCollection<RiskEvaluation> evaluationsToAppend,
        IReadOnlyCollection<RunEvaluation> runEvaluations,
        IReadOnlyCollection<Alert> alertsToOpen,
        CancellationToken cancellationToken);

    /// <summary>
    /// The evaluation the latest run referenced for each order it covered, keyed by order. This is
    /// the current evaluation of an order: it does not depend on insertion timestamps, so a score
    /// that returns to an earlier value still resolves to the right row.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, RiskEvaluation>> GetCurrentEvaluationsAsync(
        CancellationToken cancellationToken);
}
