using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The transition table of section 4.5 of the Blueprint, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The distinction this table exists to preserve is <em>out of order</em> against
/// <em>contradiction</em>. A <c>PENDING</c> after a verdict is the network delivering messages in
/// the wrong order, and the right answer is to ignore it. A <em>different</em> verdict after a
/// verdict is the provider saying two things, and the right answer is to keep it and show it.
/// Collapsing both into "discarded" would swallow the second in silence.
/// </para>
/// <para>
/// A resend of the same verdict with a different instant is not a duplicate — the key differs — and
/// must be a no-op rather than a conflict.
/// </para>
/// </remarks>
internal static class ExternalCallbackTransition
{
    public static CallbackReceiptStatus Classify(
        ExternalEvaluation? evaluation,
        ExternalEvaluationStatus reported)
    {
        if (evaluation is null)
        {
            return CallbackReceiptStatus.Unmatched;
        }

        if (!evaluation.IsSettled)
        {
            return reported == ExternalEvaluationStatus.Pending
                ? CallbackReceiptStatus.NoOp
                : CallbackReceiptStatus.Applied;
        }

        if (reported == ExternalEvaluationStatus.Pending)
        {
            return CallbackReceiptStatus.Superseded;
        }

        return reported == evaluation.Status
            ? CallbackReceiptStatus.NoOp
            : CallbackReceiptStatus.Conflicting;
    }

    /// <summary>
    /// Applies the message, when the classification says it changes something.
    /// </summary>
    /// <returns>
    /// The classification, possibly downgraded to <see cref="CallbackReceiptStatus.Conflicting"/>:
    /// a message that correlates with a row but contradicts an invariant of it — a second, different
    /// provider identifier, say — is the provider disagreeing with itself, not a server error.
    /// </returns>
    public static CallbackReceiptStatus Apply(
        ExternalEvaluation? evaluation,
        ExternalCallbackMessage message,
        CallbackReceiptStatus classification,
        DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (evaluation is null
            || classification is CallbackReceiptStatus.Superseded or CallbackReceiptStatus.Conflicting)
        {
            return classification;
        }

        // A no-op on a settled evaluation really is nothing; on a pending one it still carries the
        // provider's latest word, which the exchange records as an attempt.
        if (classification == CallbackReceiptStatus.NoOp && evaluation.IsSettled)
        {
            return classification;
        }

        try
        {
            // Through the exchange, so that the callback and the synchronous request cannot end up
            // with two different ideas of what a given answer means.
            ExternalProviderExchange.Apply(
                evaluation,
                ToResult(message),
                ExternalSettlementSource.Callback,
                at);
        }
        catch (ExternalEvaluationTransitionException)
        {
            return CallbackReceiptStatus.Conflicting;
        }

        return classification;
    }

    /// <summary>
    /// A reported status as the outcome the exchange understands.
    /// </summary>
    /// <remarks>
    /// <c>ERROR</c> maps to a definitive refusal rather than to a transient failure. A provider that
    /// bothers to send a callback saying the evaluation failed has decided; that is not the silence
    /// of a timeout, which is the only case the pending path exists for.
    /// </remarks>
    private static ExternalEvaluationResult ToResult(ExternalCallbackMessage message)
    {
        return message.Status switch
        {
            ExternalEvaluationStatus.Approved => new(
                ExternalProviderOutcome.Approved,
                message.ExternalEvaluationId,
                message.Score),
            ExternalEvaluationStatus.Denied => new(
                ExternalProviderOutcome.Denied,
                message.ExternalEvaluationId,
                message.Score),
            ExternalEvaluationStatus.Error => new(
                ExternalProviderOutcome.Rejected,
                message.ExternalEvaluationId),
            ExternalEvaluationStatus.Pending => new(
                ExternalProviderOutcome.Pending,
                message.ExternalEvaluationId),
            _ => throw new ArgumentOutOfRangeException(
                nameof(message),
                message.Status,
                "Unsupported reported status."),
        };
    }
}
