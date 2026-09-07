using Salvo.Application.Alerts;
using Salvo.Domain.Alerts;
using Salvo.Domain.Risk;

namespace Salvo.Application.Risk;

/// <summary>
/// Scores the whole corpus and persists the result as one auditable run, including the alerts the
/// run opens.
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
    IAlertIdGenerator alertIdGenerator,
    TimeProvider timeProvider)
{
    public async Task<ScoringRunSummary> HandleAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var orders = await orderReader.GetAllChronologicallyAsync(cancellationToken);
        var config = RuleConfig.Current;
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
        var flagged = new List<FlaggedOrder>();
        var reused = 0;

        for (var index = 0; index < candidates.Length; index++)
        {
            var candidate = candidates[index];
            Guid evaluationId;
            if (existing.TryGetValue(fingerprints[index], out var existingId))
            {
                evaluationId = existingId;
                reused++;
            }
            else
            {
                evaluationId = candidate.Id;
                evaluationsToAppend.Add(candidate);
            }

            runEvaluations.Add(RunEvaluation.Create(runId, candidate.OrderId, evaluationId));

            if (candidate.IsFlagged)
            {
                // A reused evaluation has the same content as the candidate by construction: the
                // fingerprint they matched on covers the score and the canonical signal string.
                flagged.Add(new(
                    candidate.OrderId,
                    evaluationId,
                    candidate.Score ?? throw new InvalidOperationException("A local evaluation must carry a score."),
                    candidate.SignalsJson ?? throw new InvalidOperationException("A local evaluation must carry signals.")));
            }
        }

        var outcome = await OpenAlertsAsync(flagged, startedAt, cancellationToken);

        var sequence = await store.GetLastRunSequenceAsync(cancellationToken) + 1;
        var run = ScoringRun.Complete(
            runId,
            sequence,
            config.Version,
            startedAt,
            timeProvider.GetUtcNow(),
            candidates.Length,
            evaluationsToAppend.Count,
            reused,
            outcome.Alerts.Count,
            outcome.SkippedOpen,
            outcome.SkippedReviewed);

        await store.SaveRunAsync(run, evaluationsToAppend, runEvaluations, outcome.Alerts, cancellationToken);

        return new(
            run.Id,
            run.Sequence,
            run.RuleConfigVersion,
            run.StartedAt,
            run.CompletedAt,
            run.OrderCount,
            run.EvaluationsCreated,
            run.EvaluationsReused,
            run.AlertsCreated,
            run.AlertsSkippedOpen,
            run.AlertsSkippedReviewed);
    }

    /// <summary>
    /// Decides which flagged orders deserve an alert, reading the alert history from the database
    /// rather than from the delta of this run.
    /// </summary>
    /// <remarks>
    /// An order gets an alert when it has no open one and either has never been alerted, or its
    /// current severity band is higher than the band of its last alert. Requiring a band change
    /// rather than more points keeps noise out of the queue while still resurfacing an order whose
    /// nature of risk changed because a retroactive import completed its history.
    /// </remarks>
    private async Task<AlertOutcome> OpenAlertsAsync(
        IReadOnlyList<FlaggedOrder> flagged,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (flagged.Count == 0)
        {
            return new([], 0, 0);
        }

        var policy = AlertPolicy.E4V1;
        var states = await store.GetAlertStatesAsync(
            flagged.Select(entry => entry.OrderId).ToArray(),
            cancellationToken);

        var alerts = new List<Alert>();
        var skippedOpen = 0;
        var skippedReviewed = 0;

        foreach (var entry in flagged)
        {
            states.TryGetValue(entry.OrderId, out var state);

            if (state?.HasOpenAlert == true)
            {
                skippedOpen++;
                continue;
            }

            var severity = policy.SeverityFor(entry.Score);
            if (state?.LatestSeverity is { } latest && severity <= latest)
            {
                skippedReviewed++;
                continue;
            }

            alerts.Add(Alert.Open(
                alertIdGenerator.Create(),
                entry.OrderId,
                entry.EvaluationId,
                entry.Score,
                entry.SignalsJson,
                policy.Version,
                state?.LatestAlertId,
                createdAt));
        }

        return new(alerts, skippedOpen, skippedReviewed);
    }

    private sealed record FlaggedOrder(Guid OrderId, Guid EvaluationId, int Score, string SignalsJson);

    private sealed record AlertOutcome(IReadOnlyList<Alert> Alerts, int SkippedOpen, int SkippedReviewed);
}
