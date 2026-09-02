using Salvo.Domain.Risk;

namespace Salvo.Application.Risk;

/// <summary>
/// Scores the whole corpus and persists the result as one auditable run.
/// </summary>
/// <remarks>
/// The run invokes <see cref="TemporalRiskEngine"/> directly rather than the metrics handler, which
/// requires a ground-truth label for every order; imports never produce labels, so routing the run
/// through it would break the main flow.
/// </remarks>
public sealed class RunScoringHandler(
    IRiskOrderReader orderReader,
    IScoringRunStore store,
    IRiskIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    public async Task<ScoringRunSummary> HandleAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var orders = await orderReader.GetAllChronologicallyAsync(cancellationToken);
        var config = RuleConfig.E3V1;
        var assessments = TemporalRiskEngine.Score(orders, config);

        var runId = idGenerator.Create();
        var candidates = assessments
            .Select(assessment => RiskEvaluation.ForLocal(
                idGenerator.Create(),
                config.Version,
                assessment,
                startedAt))
            .ToArray();

        var fingerprints = candidates
            .Select(candidate => candidate.EvaluationFingerprint ?? throw new InvalidOperationException(
                "A local evaluation must carry a fingerprint."))
            .ToArray();
        var existing = await store.GetLocalEvaluationIdsByFingerprintAsync(fingerprints, cancellationToken);

        var evaluationsToAppend = new List<RiskEvaluation>();
        var runEvaluations = new List<RunEvaluation>(candidates.Length);
        var reused = 0;

        for (var index = 0; index < candidates.Length; index++)
        {
            var candidate = candidates[index];
            if (existing.TryGetValue(fingerprints[index], out var existingId))
            {
                runEvaluations.Add(RunEvaluation.Create(runId, candidate.OrderId, existingId));
                reused++;
                continue;
            }

            evaluationsToAppend.Add(candidate);
            runEvaluations.Add(RunEvaluation.Create(runId, candidate.OrderId, candidate.Id));
        }

        var sequence = await store.GetLastRunSequenceAsync(cancellationToken) + 1;
        var run = ScoringRun.Complete(
            runId,
            sequence,
            config.Version,
            startedAt,
            timeProvider.GetUtcNow(),
            candidates.Length,
            evaluationsToAppend.Count,
            reused);

        await store.SaveRunAsync(run, evaluationsToAppend, runEvaluations, cancellationToken);

        return new(
            run.Id,
            run.Sequence,
            run.RuleConfigVersion,
            run.StartedAt,
            run.CompletedAt,
            run.OrderCount,
            run.EvaluationsCreated,
            run.EvaluationsReused);
    }
}
