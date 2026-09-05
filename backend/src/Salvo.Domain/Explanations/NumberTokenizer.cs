using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Salvo.Domain.Explanations;

/// <summary>
/// One way of reading a numeric token. A token can have more than one when the separators are
/// genuinely ambiguous.
/// </summary>
/// <param name="Decimals">
/// How many digits the token wrote after the decimal mark. It is what decides how coarsely a fact
/// may be rounded before it counts as backing this token.
/// </param>
public sealed record NumberReading(decimal Value, int Decimals);

/// <param name="Text">The token exactly as it appeared, kept so a rejection can name it.</param>
/// <param name="Readings">
/// Empty when the token is not a number under any reading. Such a token is never grounded, which
/// is the safe answer: an unparseable figure is not a figure anybody can check.
/// </param>
public sealed record NumberToken(string Text, IReadOnlyList<NumberReading> Readings);

/// <summary>
/// The single, declared way this system turns text into numbers.
/// </summary>
/// <remarks>
/// <para>
/// It runs on both sides of the grounding check: over the English <c>detail</c> the engine writes,
/// to build the facts, and over the Spanish summary a provider returns, to read what it claimed.
/// One tokenizer rather than two is the point — two would disagree, and the disagreement would
/// surface as a rejection of correct text.
/// </para>
/// <para>
/// The two sides format numbers differently, so a token is read into every interpretation that is
/// defensible rather than into one. <c>56.0</c> is fifty-six point zero, because a thousands group
/// has exactly three digits and <c>0</c> does not. <c>8.900,00</c> is eight thousand nine hundred,
/// because the last mark is followed by two digits. <c>1.279</c> is genuinely ambiguous and yields
/// both readings. Being generous here is safe: a fabricated number does not become real by having
/// two spellings, and the alternative is rejecting <c>8.900,00</c> for an amount of 890000 cents,
/// which is one of the false rejections this design exists to prevent.
/// </para>
/// <para>
/// Numbers written as words are not extracted and therefore not validated. That is a stated
/// limitation, not an oversight: rejecting word-numbers would mean rejecting «una regla».
/// </para>
/// </remarks>
public static partial class NumberTokenizer
{
    /// <summary>
    /// The coarsest rounding a fact may undergo to back a token. Beyond two decimals only an exact
    /// match counts.
    /// </summary>
    public const int MaximumRoundedDecimals = 2;

    /// <summary>
    /// A thousands group, everywhere it is not the leading one.
    /// </summary>
    private const int ThousandsGroupLength = 3;

    private static readonly char[] Separators = ['.', ','];

    /// <summary>
    /// Version identifiers are struck out before tokenizing. <c>e3-v1</c> would otherwise donate a
    /// 3 and a 1 to the fact set on the engine side, and would have to be explained away on the
    /// summary side.
    /// </summary>
    private static readonly string[] VersionStrings = ["e3-v1", "e4-v1", "e7-v1"];

    /// <summary>
    /// A run of digits with interior separators. Colons are not separators, so <c>00:00-06:00</c>
    /// yields four tokens rather than one unreadable one.
    /// </summary>
    [GeneratedRegex(@"\d+(?:[.,]\d+)*", RegexOptions.CultureInvariant)]
    private static partial Regex CandidatePattern();

    public static IReadOnlyList<NumberToken> Extract(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalized = text.Normalize(NormalizationForm.FormC);
        foreach (var version in VersionStrings)
        {
            normalized = normalized.Replace(version, " ", StringComparison.OrdinalIgnoreCase);
        }

        var tokens = new List<NumberToken>();
        foreach (var match in CandidatePattern().EnumerateMatches(normalized))
        {
            var candidate = normalized.Substring(match.Index, match.Length);
            tokens.Add(new(candidate, Read(candidate)));
        }

        return tokens;
    }

    /// <summary>
    /// Every value the token can defensibly denote.
    /// </summary>
    private static List<NumberReading> Read(string token)
    {
        var groups = token.Split(Separators);
        if (groups.Length == 1)
        {
            return TryParse(token, 0, out var whole) ? [whole] : [];
        }

        var readings = new List<NumberReading>();

        // The last mark is the decimal one. Everything before it has to be a plain integer or a
        // properly grouped one: the leading group may be any length, the rest exactly three.
        if (IsGroupedInteger(groups[..^1]))
        {
            var last = groups[^1];
            if (TryParse(
                    string.Concat(groups[..^1]) + "." + last,
                    last.Length,
                    out var fractional))
            {
                readings.Add(fractional);
            }
        }

        // Every mark groups thousands, so the token is an integer. Only possible when the leading
        // group is short enough and every other group is exactly three digits.
        if (groups[0].Length <= ThousandsGroupLength && IsGroupedInteger(groups))
        {
            if (TryParse(string.Concat(groups), 0, out var integral)
                && readings.TrueForAll(reading => reading.Value != integral.Value))
            {
                readings.Add(integral);
            }
        }

        return readings;
    }

    /// <summary>
    /// Whether the groups form the integer part of a number: the first of any length, the rest
    /// exactly a thousands group.
    /// </summary>
    private static bool IsGroupedInteger(string[] groups)
    {
        if (groups.Length == 0 || groups[0].Length == 0)
        {
            return false;
        }

        for (var index = 1; index < groups.Length; index++)
        {
            if (groups[index].Length != ThousandsGroupLength)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParse(string invariant, int decimals, out NumberReading reading)
    {
        if (decimal.TryParse(
                invariant,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var value))
        {
            reading = new(value, decimals);

            return true;
        }

        reading = new(0m, 0);

        return false;
    }
}
