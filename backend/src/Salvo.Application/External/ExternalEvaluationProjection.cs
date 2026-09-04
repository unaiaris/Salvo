using Salvo.Domain.External;

namespace Salvo.Application.External;

public static class ExternalEvaluationProjection
{
    public static ExternalEvaluationView ToView(ExternalEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        return new(
            evaluation.Id,
            evaluation.OrderId,
            ExternalEvaluationWireNames.ToWire(evaluation.Provider),
            evaluation.ReferenceId,
            evaluation.ExternalEvaluationId,
            ExternalEvaluationWireNames.ToWire(evaluation.Status),
            evaluation.Score,
            evaluation.ErrorCode is { } errorCode ? ExternalEvaluationWireNames.ToWire(errorCode) : null,
            evaluation.LastErrorCode is { } lastErrorCode ? ExternalEvaluationWireNames.ToWire(lastErrorCode) : null,
            evaluation.AttemptCount,
            evaluation.SettledBy is { } settledBy ? ExternalEvaluationWireNames.ToWire(settledBy) : null,
            evaluation.RequestedAt,
            evaluation.UpdatedAt,
            evaluation.SettledAt);
    }
}
