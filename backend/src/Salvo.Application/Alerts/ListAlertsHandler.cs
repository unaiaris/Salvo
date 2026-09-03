using Salvo.Domain.Alerts;

namespace Salvo.Application.Alerts;

public sealed class ListAlertsHandler(IAlertStore store)
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 200;

    public async Task<ListAlertsResult> HandleAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        AlertSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaximumPageSize);

        var result = await store.GetPageAsync(status, severity, sort, page, pageSize, cancellationToken);

        return new(
            result.Items.Select(AlertProjection.ToListItem).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            result.CurrentRun?.Sequence,
            result.CurrentRun);
    }
}
