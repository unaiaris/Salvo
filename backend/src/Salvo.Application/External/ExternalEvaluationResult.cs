using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <param name="ErrorCode">
/// Only consulted for <see cref="ExternalProviderOutcome.Transient"/>, where it distinguishes a
/// timeout from a server error from an unreadable answer. The two settling failures name themselves.
/// </param>
public sealed record ExternalEvaluationResult(
    ExternalProviderOutcome Outcome,
    string? ExternalEvaluationId = null,
    int? Score = null,
    ExternalEvaluationErrorCode? ErrorCode = null);
