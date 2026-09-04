using Salvo.Domain.Alerts;
using Salvo.Domain.External;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Application.Alerts;

/// <summary>
/// Maps persisted alerts to their public projections. The severity of both the snapshot and the
/// current evaluation is resolved through the policy version the alert was opened with, so the two
/// are always compared on the same scale.
/// </summary>
public static class AlertProjection
{
    public static AlertListItem ToListItem(AlertContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var alert = context.Alert;
        var order = context.Order;
        var divergence = ToDivergence(alert, context.CurrentEvaluation);

        return new(
            alert.Id,
            alert.OrderId,
            order.MerchantReferenceId,
            order.BuyerReferenceId,
            order.OccurredAt,
            order.AmountCents,
            order.CurrencyCode,
            order.CountryCode,
            AlertWireNames.ToWire(alert.Status),
            AlertWireNames.ToWire(alert.Severity),
            alert.RiskScoreSnapshot,
            divergence.CurrentScore,
            divergence.CurrentSeverity,
            divergence.HasBandDivergence,
            alert.AlertPolicyVersion,
            alert.SupersedesAlertId,
            alert.CreatedAt,
            alert.ReviewedAt);
    }

    public static AlertDetail ToDetail(AlertContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var alert = context.Alert;

        return new(
            alert.Id,
            alert.OrderId,
            AlertWireNames.ToWire(alert.Status),
            AlertWireNames.ToWire(alert.Severity),
            alert.AlertPolicyVersion,
            alert.SupersedesAlertId,
            alert.CreatedAt,
            alert.ReviewedAt,
            ToOrderView(context.Order),
            new(
                alert.RiskEvaluationId,
                alert.RiskScoreSnapshot,
                AlertWireNames.ToWire(alert.Severity),
                ToSignalViews(alert.SignalsSnapshotJson)),
            ToEvaluationView(alert, context.CurrentEvaluation),
            context.CurrentRun,
            ToDivergence(alert, context.CurrentEvaluation),
            ToExternalView(context.ExternalEvaluation, context.HasContradictoryCallback),
            ToReviewView(context.Review));
    }

    /// <summary>
    /// Compares the band of the current evaluation with the band of the snapshot.
    /// </summary>
    /// <remarks>
    /// When no evaluation is current for the order there is nothing that contradicts the snapshot,
    /// so the alert is not reported as divergent; the null score makes the absence visible instead.
    /// </remarks>
    public static AlertDivergenceView ToDivergence(Alert alert, RiskEvaluation? currentEvaluation)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var policy = AlertPolicy.ForVersion(alert.AlertPolicyVersion);
        var snapshotSeverity = policy.SeverityFor(alert.RiskScoreSnapshot);
        var currentScore = currentEvaluation?.Score;
        var currentSeverity = currentScore is null ? null : policy.SeverityForOrNull(currentScore.Value);

        return new(
            currentScore is not null && currentSeverity != snapshotSeverity,
            alert.RiskScoreSnapshot,
            AlertWireNames.ToWire(snapshotSeverity),
            currentScore,
            currentSeverity is null ? null : AlertWireNames.ToWire(currentSeverity.Value));
    }

    private static AlertEvaluationView? ToEvaluationView(Alert alert, RiskEvaluation? evaluation)
    {
        if (evaluation?.Score is not { } score)
        {
            return null;
        }

        var severity = AlertPolicy.ForVersion(alert.AlertPolicyVersion).SeverityForOrNull(score);

        return new(
            evaluation.Id,
            score,
            severity is null ? null : AlertWireNames.ToWire(severity.Value),
            evaluation.IsFlagged,
            ToSignalViews(evaluation.SignalsJson),
            evaluation.CreatedAt);
    }

    private static AlertExternalEvaluationView? ToExternalView(
        ExternalEvaluation? evaluation,
        bool hasContradictoryCallback)
    {
        return evaluation is null
            ? null
            : new(
                evaluation.Id,
                ExternalEvaluationWireNames.ToWire(evaluation.Provider),
                ExternalEvaluationWireNames.ToWire(evaluation.Status),
                evaluation.Score,
                evaluation.ErrorCode is { } errorCode
                    ? ExternalEvaluationWireNames.ToWire(errorCode)
                    : null,
                evaluation.LastErrorCode is { } lastErrorCode
                    ? ExternalEvaluationWireNames.ToWire(lastErrorCode)
                    : null,
                evaluation.SettledBy is { } settledBy
                    ? ExternalEvaluationWireNames.ToWire(settledBy)
                    : null,
                evaluation.RequestedAt,
                evaluation.SettledAt,
                hasContradictoryCallback);
    }

    private static AlertOrderView ToOrderView(Order order)
    {
        return new(
            order.Id,
            order.MerchantId,
            order.MerchantReferenceId,
            order.BuyerReferenceId,
            order.OccurredAt,
            order.AmountCents,
            order.CurrencyCode,
            order.CountryCode,
            order.City,
            order.DeviceSessionId);
    }

    private static AlertReviewView? ToReviewView(AlertReview? review)
    {
        return review is null
            ? null
            : new(
                review.Id,
                AlertWireNames.ToWire(review.PreviousStatus),
                AlertWireNames.ToWire(review.NewStatus),
                review.Note,
                review.ReviewedAt);
    }

    private static AlertSignalView[] ToSignalViews(string? signalsJson)
    {
        if (string.IsNullOrEmpty(signalsJson))
        {
            return [];
        }

        return RiskSignalSerializer.Deserialize(signalsJson)
            .Select(signal => new AlertSignalView(signal.Rule, signal.Weight, signal.Detail))
            .ToArray();
    }
}
