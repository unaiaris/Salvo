using System.Globalization;
using System.Text;

namespace Salvo.Domain.Explanations;

/// <summary>
/// Renders a number the way the console writes one: thousands with a point, decimals with a comma.
/// </summary>
/// <remarks>
/// Written by hand rather than taken from a culture on purpose. Everything the domain produces has
/// to be byte-identical on every machine, and culture data is neither frozen nor identical across
/// runtimes; the golden test of the summary would then be a test of the host's ICU version. The
/// grouping rules of <c>es-UY</c> are simple enough that stating them is cheaper than depending on
/// them.
/// </remarks>
public static class SpanishNumberFormat
{
    private const int ThousandsGroupLength = 3;

    public static string Format(decimal value, int decimals)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);

        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        var text = Math.Abs(rounded).ToString(
            "F" + decimals.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);

        var separator = text.IndexOf('.', StringComparison.Ordinal);
        var integerPart = separator < 0 ? text : text[..separator];
        var fractionPart = separator < 0 ? string.Empty : text[(separator + 1)..];

        var builder = new StringBuilder();
        if (rounded < 0)
        {
            builder.Append('-');
        }

        builder.Append(Group(integerPart));
        if (fractionPart.Length > 0)
        {
            builder.Append(',').Append(fractionPart);
        }

        return builder.ToString();
    }

    private static string Group(string digits)
    {
        if (digits.Length <= ThousandsGroupLength)
        {
            return digits;
        }

        var builder = new StringBuilder(digits.Length + (digits.Length / ThousandsGroupLength));
        var leading = digits.Length % ThousandsGroupLength;
        if (leading == 0)
        {
            leading = ThousandsGroupLength;
        }

        builder.Append(digits[..leading]);
        for (var index = leading; index < digits.Length; index += ThousandsGroupLength)
        {
            builder.Append('.').Append(digits.AsSpan(index, ThousandsGroupLength));
        }

        return builder.ToString();
    }
}
