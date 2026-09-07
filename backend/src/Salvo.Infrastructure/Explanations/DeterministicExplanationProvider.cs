using System.Text;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using static Salvo.Infrastructure.Explanations.ExplanationFigures;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// The explanation provider of the MVP: a template, with no network and no model.
/// </summary>
/// <remarks>
/// <para>
/// It composes prose from <see cref="SignalFacts"/> rather than from the raw sentences the engine
/// wrote, which is why the parsing seam is where it is: when the engine emits typed fields, this
/// class keeps working and the extractor disappears.
/// </para>
/// <para>
/// It passes exactly the verification a model would, because the verification is not here. That is
/// the point of the arrangement, and the reason the template is worth having: it exercises the same
/// path on every run, so the check is proved by ordinary use rather than only by a test.
/// </para>
/// <para>
/// Every figure it writes comes from the evaluation. The wording is deliberately plain and states
/// what fired and by how much; it never recommends, never concludes and never mentions fraud.
/// </para>
/// <para>
/// <strong>The language comes from the input and not from this class.</strong> Writing stays a pure
/// function of what it was given, so the same evaluation produces the same paragraph wherever it
/// runs — which is what a golden text is worth pinning for. The words themselves live in
/// <see cref="ExplanationVocabulary"/> and the figures in <see cref="ExplanationFigures"/>: nothing
/// below chooses a word or formats a number, it only decides which sentences a paragraph has.
/// </para>
/// </remarks>
public sealed class DeterministicExplanationProvider : IExplanationProvider
{
    /// <summary>
    /// The template version, which is part of the identity of every row it produces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is bumped whenever the words change, and that is what makes a wording fix reach an
    /// evaluation that was already explained: the store looks a row up by this version among
    /// others, finds none, and writes a new one beside the old. Leaving it alone would keep the
    /// previous paragraph on screen for ever, because a written explanation is never regenerated.
    /// <c>e7-v2</c> is the polish of <c>E7C</c>: rules that fire rather than coincide, and a whole
    /// ratio written without the zero it used to drag.
    /// </para>
    /// <para>
    /// <strong>Portuguese did not bump it, and that is decision 64 rather than an oversight.</strong>
    /// The Spanish text this version writes is unchanged to the byte, so a new version would be two
    /// versions with identical output — which turns «write this with the current template» into a
    /// button that silently offers to change language. The language is a column of the identity
    /// instead, and the two rows sit side by side.
    /// </para>
    /// </remarks>
    public const string Version = "e7-v2";

    public ExplanationProvider Provider => ExplanationProvider.Mock;

    public string TemplateVersion => Version;

    public Task<ExplanationDraft> ExplainAsync(
        ExplanationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var config = RuleConfig.ForVersion(input.RuleConfigVersion);
        var words = ExplanationVocabulary.For(input.Language);
        var signals = SignalFacts.ForAll(input.Signals);
        var builder = new StringBuilder();

        Open(builder, words, input, signals, config);
        foreach (var signal in signals)
        {
            builder.Append(' ').Append(words.Describe(signal, input));
        }

        Close(builder, words, input, config);

        return Task.FromResult(new ExplanationDraft(
            builder.ToString(),
            [.. signals.Select(signal => signal.Rule)]));
    }

    private static void Open(
        StringBuilder builder,
        ExplanationVocabulary words,
        ExplanationInput input,
        IReadOnlyList<SignalFacts> signals,
        RuleConfig config)
    {
        builder.Append(words.Opening(
            Number(input.Score),
            Number(config.FlagThreshold),
            words.SeverityWord(input.Severity)));

        builder.Append(' ').Append(words.RulesFired(signals.Count, Number(signals.Count)));

        var total = signals.Sum(signal => signal.Weight);
        if (total > config.ScoreCap)
        {
            builder.Append(' ').Append(words.Capped(Number(total), Number(config.ScoreCap)));
        }
    }

    private static void Close(
        StringBuilder builder,
        ExplanationVocabulary words,
        ExplanationInput input,
        RuleConfig config)
    {
        // Business time, the same zone the rules read a day in and the console renders an instant
        // in. Writing the stored UTC instead would put the console and this paragraph on different
        // days for half of every evening.
        var local = TimeZoneInfo.ConvertTime(input.OccurredAt, config.BusinessTimeZone);

        builder.Append(' ').Append(words.Closing(
            Number(local.Day),
            words.MonthName(local.Month),
            Year(local.Year),
            Clock(local.Hour),
            Clock(local.Minute)));
    }
}
