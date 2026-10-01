using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <summary>
/// Who writes explanations in this deployment: a provider and the version it writes with.
/// </summary>
/// <remarks>
/// Taken from the registered port on every read rather than from configuration or a constant, so
/// the console and the writer cannot disagree about which writer is current — the disagreement that
/// made a wording fix invisible once already (<c>E7D</c>). The language is not here because every
/// row a read considers is already in the language of the deployment.
/// </remarks>
public sealed record ExplanationWriter(ExplanationProvider Provider, string TemplateVersion)
{
    public static ExplanationWriter Of(IExplanationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return new(provider.Provider, provider.TemplateVersion);
    }

    /// <summary>Whether this writer wrote the row.</summary>
    public bool Wrote(AlertExplanation explanation)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        return explanation.Provider == Provider
            && string.Equals(explanation.TemplateVersion, TemplateVersion, StringComparison.Ordinal);
    }
}

/// <param name="Shown">The explanation the console shows, and the one a review may cite.</param>
/// <param name="CurrentWriterAttempt">
/// The row of the current writer, when it is not ready and the text shown is somebody else's. Shown
/// beside that text, with its status, its code and its detail; never cited by a review, because it
/// is not what anybody read.
/// </param>
public sealed record ExplanationChoice(AlertExplanation Shown, AlertExplanation? CurrentWriterAttempt);

/// <summary>
/// Which of the rows of one evaluation the console shows (decision 79).
/// </summary>
/// <remarks>
/// <para>
/// Before a writer could fail, the answer was «the most recently requested row», and it did not
/// matter: the template does not fail. With a model it hides a correct paragraph that is still in
/// the database behind an attempt that failed. The rule, in order:
/// </para>
/// <list type="number">
/// <item><description>The <c>READY</c> row of the current writer, if there is one.</description></item>
/// <item><description>
/// Otherwise the <c>READY</c> row most recently settled by any other writer, labelled with who wrote
/// it — and beside it the row of the current writer, if it has one, which is then not ready.
/// </description></item>
/// <item><description>Otherwise, with nothing ready, the row of the current writer.</description></item>
/// <item><description>
/// Otherwise the most recently requested row of any writer, which is what this always did.
/// </description></item>
/// </list>
/// <para>
/// <strong>An accepted text is never hidden behind a failed attempt.</strong> Pure and in Application,
/// so the store that selects rows and the tests that pin the five cases share one definition.
/// </para>
/// </remarks>
public static class ExplanationSelection
{
    /// <param name="rows">
    /// The rows of one evaluation, under one policy and in the language of the deployment.
    /// </param>
    /// <returns><see langword="null"/> when nobody has asked for an explanation of it.</returns>
    public static ExplanationChoice? Choose(IEnumerable<AlertExplanation> rows, ExplanationWriter writer)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(writer);

        var candidates = rows.ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var current = candidates
            .Where(writer.Wrote)
            .OrderByDescending(row => row.RequestedAt)
            .ThenByDescending(row => row.Id)
            .FirstOrDefault();

        if (current is { Status: ExplanationStatus.Ready })
        {
            return new(current, null);
        }

        var otherReady = candidates
            .Where(row => row.Status == ExplanationStatus.Ready && !writer.Wrote(row))
            .OrderByDescending(row => row.SettledAt)
            .ThenByDescending(row => row.Id)
            .FirstOrDefault();

        if (otherReady is not null)
        {
            return new(otherReady, current);
        }

        if (current is not null)
        {
            return new(current, null);
        }

        return new(
            candidates
                .OrderByDescending(row => row.RequestedAt)
                .ThenByDescending(row => row.Id)
                .First(),
            null);
    }
}
