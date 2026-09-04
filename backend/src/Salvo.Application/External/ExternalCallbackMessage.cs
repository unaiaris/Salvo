using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// One callback, reduced to what this system acts on.
/// </summary>
/// <remarks>
/// Whatever else the provider sent is deliberately not here. Section 7 of the Blueprint forbids
/// persisting a foreign payload, and a message is only ever used to correlate a row and move it, so
/// nothing beyond these five values could change the outcome.
/// </remarks>
/// <param name="ExternalEvaluationId">
/// The identifier the provider assigned. Absent for a provider that only echoes the reference, and
/// absent for the message that arrives before this system has been told the identifier at all.
/// </param>
/// <param name="ReferenceId">
/// The order reference, echoed back. It is the half of the correlation that exists from the moment
/// the row is reserved, which is why a real adapter is required to send it.
/// </param>
/// <param name="ProviderInstant">
/// When the provider says it decided. Part of the deduplication key, and never replaced by the
/// instant of reception: that one would make every redelivery look like a new message.
/// </param>
public sealed record ExternalCallbackMessage(
    string? ExternalEvaluationId,
    string? ReferenceId,
    ExternalEvaluationStatus Status,
    int? Score,
    DateTimeOffset? ProviderInstant);
