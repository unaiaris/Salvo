using System.Globalization;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// Every number an explanation writes, formatted once and identically in every language.
/// </summary>
/// <remarks>
/// The split this class marks is the one that matters when a template speaks two languages: the
/// <em>figures</em> are a claim about the evaluation and must not move, while the <em>words</em>
/// around them are a matter of who is reading. A figure that changed with the language would be a
/// different claim, and the grounding check — which knows nothing about language — would be right
/// to reject it.
/// </remarks>
internal static class ExplanationFigures
{
    /// <summary>
    /// The amount in the units a sentence about money uses. The cents are what is stored; nobody
    /// writes them.
    /// </summary>
    public static string Money(ExplanationInput input)
    {
        return ExplanationNumberFormat.Format(input.AmountCents / 100m, 2);
    }

    /// <summary>
    /// Minutes, without decimals when there are none to write. The engine states them with up to
    /// two, and carrying a trailing zero into prose reads like precision nobody measured.
    /// </summary>
    public static string Minutes(decimal value) => Trimmed(value, 2);

    /// <summary>
    /// How many times the median an amount is, with one decimal and only when it says something.
    /// «15,0 veces» claims a measurement to the tenth that the ratio does not have; «23,2 veces» is
    /// a different number and keeps its decimal. The token stays grounded either way: a fact of
    /// <c>15.0</c> rounded to zero decimals is the <c>15</c> the sentence writes.
    /// </summary>
    public static string Ratio(decimal value) => Trimmed(value, 1);

    public static string Number(int value) => ExplanationNumberFormat.Format(value, 0);

    public static string Number(decimal value, int decimals)
    {
        return ExplanationNumberFormat.Format(value, decimals);
    }

    /// <summary>An hour as two digits, so a clock reads like a clock.</summary>
    public static string Clock(int hour) => hour < 10 ? $"0{hour}" : Number(hour);

    /// <summary>
    /// A year, ungrouped. It is a label rather than a quantity, and «2.026» is not how anybody
    /// writes one — the grounding check accepts it, which is exactly why the golden text is what
    /// catches it.
    /// </summary>
    public static string Year(int year) => year.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A number with at most <paramref name="decimals"/> decimals, and with none at all when every
    /// one of them would be a zero.
    /// </summary>
    private static string Trimmed(decimal value, int decimals)
    {
        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        return ExplanationNumberFormat.Format(
            rounded,
            rounded == Math.Truncate(rounded) ? 0 : decimals);
    }
}
