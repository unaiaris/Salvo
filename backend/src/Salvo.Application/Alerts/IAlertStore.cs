using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

public interface IAlertStore
{
    /// <summary>
    /// Reads one page of alerts, newest first, optionally narrowed by status and by severity.
    /// Severity is not a column, so the filter is resolved through the score bands of every known
    /// <see cref="AlertPolicy"/>.
    /// </summary>
    Task<AlertPage> GetPageAsync(
        AlertStatus? status,
        AlertSeverity? severity,
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
