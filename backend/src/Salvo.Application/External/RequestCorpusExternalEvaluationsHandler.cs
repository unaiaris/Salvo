using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// Asks the provider about every order that has never been sent to it.
/// </summary>
/// <remarks>
/// <para>
/// The console can request an evaluation from the detail of an alert, and most orders never get an
/// alert. Rather than adding a whole orders screen to give those a button — a screen this stage does
/// not budget for — the demo surface asks for the corpus in one go.
/// </para>
/// <para>
/// It goes through the same request use case, one order at a time, so the reservation, the
/// idempotence and the failure taxonomy are the same ones a single request gets. An order that
/// already has an evaluation is skipped rather than refused: repeating this is meant to be harmless.
/// </para>
/// </remarks>
public sealed class RequestCorpusExternalEvaluationsHandler(
    IExternalEvaluationStore store,
    RequestExternalEvaluationHandler requests)
{
    /// <summary>
    /// How many orders one call will take on. Well above the three hundred of the demo fixture, and
    /// low enough that this cannot become an unbounded sweep by accident.
    /// </summary>
    public const int MaximumOrders = 1000;

    public async Task<CorpusExternalEvaluationSummary> HandleAsync(
        ExternalProvider provider,
        CancellationToken cancellationToken)
    {
        var orderIds = await store.ListOrdersWithoutEvaluationAsync(
            provider,
            MaximumOrders,
            cancellationToken);

        var requested = 0;
        var settled = 0;
        var stillPending = 0;
        var skipped = 0;

        foreach (var orderId in orderIds)
        {
            RequestExternalEvaluationResult? result;
            try
            {
                result = await requests.HandleAsync(orderId, provider, requestNew: false, cancellationToken);
            }
            catch (ExternalEvaluationConflictException)
            {
                // Another writer got to this order first. Nothing to do about it here, and the
                // orders after it are unaffected.
                skipped++;
                continue;
            }

            if (result is null || !result.Applied)
            {
                skipped++;
                continue;
            }

            requested++;

            if (result.Evaluation.Status == ExternalEvaluationWireNames.Pending)
            {
                stillPending++;
            }
            else
            {
                settled++;
            }
        }

        return new(orderIds.Count, requested, settled, stillPending, skipped);
    }
}
