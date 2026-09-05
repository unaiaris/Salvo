using Salvo.Domain.Risk;

namespace Salvo.Domain.Explanations;

/// <summary>
/// Every figure a correct sentence about an evaluation may legitimately contain.
/// </summary>
/// <remarks>
/// <para>
/// This is not «the data that was supplied». Checking a summary against the supplied fields alone
/// rejects correct text, systematically, and for five reasons that were verified against real
/// rows: an amount stated in units rather than cents, a thousands separator, a rounded percentage,
/// an instant converted to business time, and a true number that is nowhere in the input because
/// it is a property of the configuration — the threshold, the cap, the number of rules that
/// matched, the sum of their weights before the cap applies.
/// </para>
/// <para>
/// So the set is built here, in the domain, from the evaluation and the order, and it names each
/// of those cases. Removing any entry brings its false rejection back.
/// </para>
/// <para>
/// The set is permissive by design and it is not what makes the check strong. What makes it strong
/// is that an <em>invented</em> figure — a ratio nobody computed, a count nobody observed — is not
/// in it, and cannot be reached from it by rounding.
/// </para>
/// </remarks>
public sealed class ExplanationFacts
{
    private readonly decimal[] values;

    private ExplanationFacts(decimal[] values)
    {
        this.values = values;
    }

    /// <summary>The facts themselves, ascending and distinct.</summary>
    public IReadOnlyList<decimal> Values => values;

    /// <summary>
    /// Builds the fact set of one evaluation.
    /// </summary>
    public static ExplanationFacts For(
        ExplanationInput input,
        IReadOnlyList<SignalFacts> signals,
        RuleConfig config)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(signals);
        ArgumentNullException.ThrowIfNull(config);

        var facts = new HashSet<decimal>
        {
            input.Score,
            config.FlagThreshold,
            config.ScoreCap,

            // How many rules matched. A summary that counts them is stating a fact about the
            // evaluation that appears in no single field of it.
            signals.Count,
        };

        // Each weight, and their sum before the cap. The sum can exceed the score — four rules of
        // the demo corpus add up to 130 against a score of 100 — and saying so is true.
        var total = 0;
        foreach (var signal in signals)
        {
            facts.Add(signal.Weight);
            total += signal.Weight;

            // Every figure the engine wrote into this signal's prose, read by the one tokenizer.
            foreach (var token in signal.Numbers)
            {
                foreach (var reading in token.Readings)
                {
                    facts.Add(reading.Value);
                }
            }
        }

        facts.Add(total);

        // The amount in cents, which is how it is stored, and in units, which is how anybody
        // writing a sentence about money states it.
        facts.Add(input.AmountCents);
        var units = input.AmountCents / 100m;
        for (var decimals = 0; decimals <= NumberTokenizer.MaximumRoundedDecimals; decimals++)
        {
            facts.Add(Math.Round(units, decimals, MidpointRounding.AwayFromZero));
        }

        // The instant, in both zones. The engine records UTC and the console shows business time,
        // so both readings of the same moment are true.
        AddInstant(facts, input.OccurredAt.ToUniversalTime());
        AddInstant(facts, TimeZoneInfo.ConvertTime(input.OccurredAt, config.BusinessTimeZone));

        var ordered = facts.ToArray();
        Array.Sort(ordered);

        return new(ordered);
    }

    /// <summary>
    /// Whether some fact backs this reading.
    /// </summary>
    /// <remarks>
    /// A reading with <c>d</c> decimals is backed by a fact it equals, or by a fact that becomes it
    /// once written with <c>d</c> decimals. Rounding is accepted in both directions and truncation
    /// as well, because all three are ways of writing a true number shorter, and none of them
    /// introduces information the evaluation did not have. Beyond
    /// <see cref="NumberTokenizer.MaximumRoundedDecimals"/> only an exact match counts.
    /// </remarks>
    public bool IsGrounded(NumberReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var rounds = reading.Decimals <= NumberTokenizer.MaximumRoundedDecimals;
        foreach (var fact in values)
        {
            if (fact == reading.Value)
            {
                return true;
            }

            if (!rounds)
            {
                continue;
            }

            if (Math.Round(fact, reading.Decimals, MidpointRounding.ToEven) == reading.Value
                || Math.Round(fact, reading.Decimals, MidpointRounding.AwayFromZero) == reading.Value
                || Truncate(fact, reading.Decimals) == reading.Value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether some reading of this token is backed. A token with no readings at all is not: an
    /// unparseable figure is one nobody can check.
    /// </summary>
    public bool IsGrounded(NumberToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        foreach (var reading in token.Readings)
        {
            if (IsGrounded(reading))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a plain integer is backed.
    /// </summary>
    /// <remarks>
    /// Exists so that a falsification test can pick a figure that is invented <em>by
    /// construction</em> — the smallest positive integer this returns false for — instead of
    /// hard-coding one that stops being invented the day the corpus changes. It answers with the
    /// same rule the real check uses, which is the only way the test proves anything.
    /// </remarks>
    public bool IsGrounded(int value) => IsGrounded(new NumberReading(value, 0));

    private static void AddInstant(HashSet<decimal> facts, DateTimeOffset instant)
    {
        facts.Add(instant.Year);
        facts.Add(instant.Month);
        facts.Add(instant.Day);
        facts.Add(instant.Hour);
        facts.Add(instant.Minute);
    }

    private static decimal Truncate(decimal value, int decimals)
    {
        var scale = 1m;
        for (var index = 0; index < decimals; index++)
        {
            scale *= 10m;
        }

        return Math.Truncate(value * scale) / scale;
    }
}
