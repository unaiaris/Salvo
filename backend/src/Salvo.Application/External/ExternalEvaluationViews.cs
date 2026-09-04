namespace Salvo.Application.External;

/// <summary>
/// The public projection of an external evaluation. Fields are enumerated explicitly, so nothing a
/// provider might start sending can reach a client without a decision here first.
/// </summary>
/// <param name="Score">
/// On the scale of the provider, whatever that is. It is never placed beside the local 0–100 score
/// as if the two were comparable.
/// </param>
/// <param name="LastErrorCode">
/// The failure a still-pending evaluation is carrying, which is not the same fact as
/// <paramref name="ErrorCode"/>: one describes an attempt, the other an outcome.
/// </param>
public sealed record ExternalEvaluationView(
    Guid Id,
    Guid OrderId,
    string Provider,
    string ReferenceId,
    string? ExternalEvaluationId,
    string Status,
    int? Score,
    string? ErrorCode,
    string? LastErrorCode,
    int AttemptCount,
    string? SettledBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SettledAt);

/// <param name="Applied">
/// <see langword="false"/> when an evaluation of this order already existed and the request changed
/// nothing, which is what makes a double click harmless.
/// </param>
public sealed record RequestExternalEvaluationResult(bool Applied, ExternalEvaluationView Evaluation);

public sealed record OrderExternalEvaluationsResult(
    Guid OrderId,
    IReadOnlyList<ExternalEvaluationView> Items);

/// <param name="Examined">Pending evaluations the sweep looked at.</param>
/// <param name="Settled">Evaluations that reached a verdict or a definitive failure.</param>
/// <param name="StillPending">
/// Evaluations the provider has still not decided, including the ones whose probe failed.
/// </param>
/// <param name="Failed">Probes that failed. A subset of <paramref name="StillPending"/>.</param>
/// <param name="Conflicted">
/// Evaluations another writer moved while the sweep was working on them. They are left alone: the
/// other writer had newer information.
/// </param>
/// <param name="Linked">
/// Callbacks that had arrived before their evaluation existed and were attached to it here. They are
/// counted apart from <paramref name="Settled"/> because they are a different event: not something
/// the provider was asked, but something it had already said and nothing had picked up.
/// </param>
public sealed record ReconciliationSummary(
    int Examined,
    int Settled,
    int StillPending,
    int Failed,
    int Conflicted,
    int Linked,
    DateTimeOffset ReconciledAt);
