using Salvo.Application.Explanations;

namespace Salvo.Application.Alerts;

/// <remarks>
/// The provider is here for one string: which template explanations are written with today. Reading
/// it from the port rather than from a constant is what keeps the console and the writer agreeing
/// about which explanation is the current one — the disagreement that made a wording fix invisible
/// once already.
/// </remarks>
public sealed class GetAlertHandler(IAlertStore store, IExplanationProvider explanationProvider)
{
    /// <summary>
    /// The alert together with the current evaluation of its order, or <see langword="null"/> when
    /// no alert carries that identifier.
    /// </summary>
    public async Task<AlertDetail?> HandleAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var context = await store.FindAsync(alertId, cancellationToken);

        return context is null
            ? null
            : AlertProjection.ToDetail(context, explanationProvider.TemplateVersion);
    }
}
