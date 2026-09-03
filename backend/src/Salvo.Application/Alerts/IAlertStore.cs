using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

public interface IAlertStore
{
    /// <summary>
    /// Reads one page of alerts, ordered as <paramref name="sort"/> asks and optionally narrowed by
    /// status and by severity.
    /// </summary>
    /// <remarks>
    /// Severity is not a column, so the filter is resolved through the score bands of every known
    /// <see cref="AlertPolicy"/>, applied to the frozen snapshot of each alert.
    /// <see cref="AlertSortOrder.LocalScoreDesc"/>, in contrast, orders by the evaluation that is
    /// current now: filtering by <c>MEDIUM</c> and sorting by score are questions about two
    /// different moments, and the contract says so rather than pretending they agree.
    /// </remarks>
    Task<AlertPage> GetPageAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        AlertSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads one alert for presentation. The returned entities are detached.
    /// </summary>
    Task<AlertContext?> FindAsync(Guid alertId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads one alert for review. The returned <see cref="AlertContext.Alert"/> is tracked, so the
    /// verdict recorded on it is what <see cref="SaveReviewAsync"/> writes.
    /// </summary>
    Task<AlertContext?> FindForReviewAsync(Guid alertId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists the verdict and its audit record as a single unit of work.
    /// </summary>
    /// <exception cref="AlertReviewConflictException">
    /// Another review already closed the alert. The status of an alert is a concurrency token, so a
    /// check-then-act race loses here instead of silently overwriting the other verdict.
    /// </exception>
    Task SaveReviewAsync(Alert alert, AlertReview review, CancellationToken cancellationToken);
}
