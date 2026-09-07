using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Application.Explanations;

/// <summary>
/// Asks a provider to explain the evaluation an alert was opened on, and refuses to store anything
/// it cannot check.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The verification lives here, between the port and the store, and that placement is the
/// design.</strong> Inside an adapter the deterministic template would pass because it is polite
/// and a future model would pass because somebody remembered; here there is simply no route from a
/// provider to the database that does not go through
/// <see cref="ExplanationGrounding.Verify"/>. A test asserts it by removing the call and watching
/// a rejection turn into a stored summary.
/// </para>
/// <para>
/// The lifecycle is the one stage 6 arrived at, applied again. The row is reserved and committed
/// before anything is asked, so two requests cannot both pay. Settling uses
/// <see cref="CancellationToken.None"/>, so a browser that closes cannot leave a reservation
/// behind. A failure is retried on the same row, so the unique index over the identity refuses
/// nothing. A reservation older than twice the timeout is taken over, which is what replaces a
/// reconciliation sweep in a stage that has none. And three attempts is where it stops.
/// </para>
/// </remarks>
public sealed class RequestExplanationHandler(
    IExplanationStore store,
    IExplanationProvider provider,
    IExplanationIdGenerator idGenerator,
    ExplanationOptions options,
    DeploymentLanguage language,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Generates the explanation, or returns what is already there.
    /// </summary>
    /// <returns><see langword="null"/> when no alert carries that identifier.</returns>
    /// <exception cref="ExplanationConflictException">
    /// Something new was asked for that cannot be given: a provider is answering right now, the
    /// explanation is already written, or the attempts are spent.
    /// </exception>
    public async Task<RequestExplanationResult?> HandleAsync(
        Guid alertId,
        bool regenerate,
        CancellationToken cancellationToken)
    {
        var target = await store.FindTargetAsync(alertId, cancellationToken);
        if (target is null)
        {
            return null;
        }

        var outdated = target.CurrentRiskEvaluationId is { } current
            && current != target.RiskEvaluationId;

        var existing = await store.FindAsync(
            target.RiskEvaluationId,
            provider.Provider,
            provider.TemplateVersion,
            target.AlertPolicyVersion,
            language.Value,
            cancellationToken);

        var reserved = await ReserveAsync(target, existing, regenerate, cancellationToken);
        if (reserved is null)
        {
            // The row already answers the question. Saying so is what makes repeating the request
            // free rather than merely harmless.
            return new(
                false,
                ExplanationProjection.ToView(existing!, outdated, provider.TemplateVersion));
        }

        await GenerateAsync(reserved, target, cancellationToken);

        return new(
            true,
            ExplanationProjection.ToView(reserved, outdated, provider.TemplateVersion));
    }

    /// <summary>
    /// Phase one: decide whether anything is to be asked, and if so claim the right to ask it.
    /// </summary>
    /// <returns>
    /// The row to generate into, or <see langword="null"/> when the request changes nothing.
    /// </returns>
    private async Task<AlertExplanation?> ReserveAsync(
        ExplanationTarget target,
        AlertExplanation? existing,
        bool regenerate,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        if (existing is null)
        {
            var reserved = AlertExplanation.Reserve(
                idGenerator.Create(),
                target.RiskEvaluationId,
                provider.Provider,
                provider.TemplateVersion,
                target.AlertPolicyVersion,
                language.Value,
                target.AlertId,
                now);

            // Committed before the provider is called. Losing this race costs nothing, because
            // nothing has been asked of anybody yet.
            await store.ReserveAsync(reserved, cancellationToken);

            return reserved;
        }

        var abandoned = existing.IsAbandonedAt(now - options.AbandonedAfter);

        if (existing.Status == ExplanationStatus.Pending && !abandoned)
        {
            return regenerate
                ? throw new ExplanationConflictException(
                    ExplanationConflictReason.PendingInFlight,
                    $"Alert {target.AlertId} is already having its evaluation explained. Wait for "
                    + "that request to finish before asking for another.")
                : null;
        }

        if (existing.Status == ExplanationStatus.Ready)
        {
            return regenerate
                ? throw new ExplanationConflictException(
                    ExplanationConflictReason.AlreadyReady,
                    $"Alert {target.AlertId} already has an explanation of this evaluation.")
                : null;
        }

        if (existing.AttemptsExhausted)
        {
            if (abandoned)
            {
                // Out of budget and nothing is coming back for it. Closing the row is what keeps
                // the partial unique index from defending a reservation that nobody owns.
                existing.Fail(ExplanationFailureCode.AttemptLimitReached, null, now);
                await store.SaveAsync(existing, cancellationToken);
            }

            return regenerate
                ? throw new ExplanationConflictException(
                    ExplanationConflictReason.AttemptsExhausted,
                    $"The evaluation of alert {target.AlertId} was sent to a provider "
                    + $"{AlertExplanation.MaximumAttempts} times without a usable answer.")
                : null;
        }

        // What is left is a failure to retry, or a reservation nobody is waiting on. An abandoned
        // one is taken over whether or not this request asked to regenerate: leaving it would jam
        // the evaluation for good.
        if (!abandoned && !regenerate)
        {
            return null;
        }

        existing.Retake(target.AlertId, now);
        await store.SaveAsync(existing, cancellationToken);

        return existing;
    }

    /// <summary>
    /// Phase two: ask, verify, and write down what happened either way.
    /// </summary>
    private async Task GenerateAsync(
        AlertExplanation explanation,
        ExplanationTarget target,
        CancellationToken cancellationToken)
    {
        // The configuration of the row being explained, never the current one. The thresholds
        // of the two live versions are equal today, which is exactly why choosing wrong here would
        // stay invisible until the day they are not: a paragraph that is correct about the wrong
        // evaluation.
        var config = RuleConfig.ForVersion(target.RuleConfigVersion);
        var input = ExplanationInputFactory.For(target, language.Value);

        ExplanationFacts facts;
        try
        {
            facts = ExplanationFacts.For(input, SignalFacts.ForAll(input.Signals), config);
        }
        catch (SignalDetailNotRecognizedException exception)
        {
            // The evaluation states its signals as `e3-v1` prose, which nothing reads any more. No
            // provider can be asked about it and no summary could be checked if one arrived, so the
            // attempt is closed rather than left reserved. The row itself is intact and the console
            // still shows it exactly as it was written; only the grounding facts cannot be built.
            //
            // The code says that, and it is the tenth of the enumeration. `E9B` had to settle for
            // `PROVIDER_UNAVAILABLE` — the nearest of the nine that existed — because a tenth value
            // means touching the check constraint that lists them, and that stage forbade
            // migrations. This one has one, so the compromise ends here: «the provider failed before
            // answering» was false about an attempt in which no provider was ever called, and it
            // sent whoever read it to debug a provider that had done nothing.
            await SettleAsync(explanation, ExplanationFailureCode.LegacySignalFormat, exception.Message);

            return;
        }

        var attempt = await ExplanationExchange.CallAsync(
            provider,
            input,
            options.RequestTimeout,
            cancellationToken);

        if (attempt.FailureCode is { } failure)
        {
            await SettleAsync(explanation, failure, null);

            return;
        }

        var draft = attempt.Draft!;
        var verdict = ExplanationGrounding.Verify(draft.Summary!, draft.ReferencedRules, input, facts);
        if (!verdict.IsGrounded)
        {
            // The offending token, never the sentence that carried it.
            await SettleAsync(explanation, verdict.FailureCode!.Value, verdict.Offender);

            return;
        }

        explanation.Complete(
            draft.Summary!,
            draft.ReferencedRules,
            draft.ProviderVersion,
            draft.InputTokens,
            draft.OutputTokens,
            timeProvider.GetUtcNow());

        await store.SaveAsync(explanation, CancellationToken.None);
    }

    /// <summary>
    /// Closes a failed attempt. Deliberately not cancellable: the reservation exists, and whether
    /// the caller is still listening has nothing to do with whether the row may stay open.
    /// </summary>
    private async Task SettleAsync(
        AlertExplanation explanation,
        ExplanationFailureCode code,
        string? detail)
    {
        explanation.Fail(code, detail, timeProvider.GetUtcNow());

        await store.SaveAsync(explanation, CancellationToken.None);
    }
}
