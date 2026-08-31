using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Salvo.Infrastructure.Persistence;

public sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, string>(
        value => value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture),
        value => DateTimeOffset.ParseExact(
            value,
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal))
{
    public const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
}
