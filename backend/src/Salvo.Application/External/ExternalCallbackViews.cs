namespace Salvo.Application.External;

/// <summary>
/// What a caller is told about the callback it just sent.
/// </summary>
/// <param name="ReceiptStatus">
/// What became of the message: applied, no-op, superseded, conflicting or unmatched. It is a fact
/// about the message, not about the evaluation.
/// </param>
/// <param name="IsReplay">
/// Whether this exact message had already been recorded. A provider that retries reads this as
/// confirmation rather than as an error.
/// </param>
/// <param name="ExternalEvaluationId">
/// The evaluation the message correlated with, when it correlated with one. Absent for an unmatched
/// message, which is the whole reason late linking exists.
/// </param>
public sealed record ExternalCallbackOutcome(
    string ReceiptStatus,
    bool IsReplay,
    int ReplayCount,
    Guid? ExternalEvaluationId,
    string? EvaluationStatus);

/// <param name="Linked">Unmatched receipts that found their evaluation and were applied to it.</param>
/// <param name="Examined">Unmatched receipts that correlated with the evaluation.</param>
public sealed record CallbackLinkSummary(int Examined, int Linked);

/// <param name="Delivered">Pending evaluations a callback was injected for.</param>
/// <param name="Settled">Evaluations that carry a verdict afterwards.</param>
/// <param name="Replayed">
/// Messages the store had already recorded. Delivering the same callback twice is deliberately
/// harmless, so this counts rather than fails.
/// </param>
public sealed record CallbackDeliverySummary(
    int Examined,
    int Delivered,
    int Settled,
    int Replayed,
    int Unavailable);

/// <param name="Requested">Orders an external evaluation was asked for.</param>
/// <param name="Skipped">Orders that already had one, or that refused a second.</param>
public sealed record CorpusExternalEvaluationSummary(
    int Examined,
    int Requested,
    int Settled,
    int StillPending,
    int Skipped);
