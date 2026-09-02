namespace Salvo.Application.Alerts;

public sealed class GetAlertHandler(IAlertStore store)
{
    /// <summary>
    /// The alert together with the current evaluation of its order, or <see langword="null"/> when
    /// no alert carries that identifier.
    /// </summary>
    public async Task<AlertDetail?> HandleAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var context = await store.FindAsync(alertId, cancellationToken);

        return context is null ? null : AlertProjection.ToDetail(context);
    }
}
