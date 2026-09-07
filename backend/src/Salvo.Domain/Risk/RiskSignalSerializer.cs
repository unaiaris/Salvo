using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Salvo.Domain.Risk;

/// <summary>
/// Canonical, culture-independent serialization of the signals of a local risk evaluation.
/// </summary>
/// <remarks>
/// <para>
/// The produced string is both what is persisted and what is hashed into the evaluation
/// fingerprint: it is never re-serialized. Signals are emitted in the rule order of
/// <see cref="RiskRuleNames.CanonicalOrder"/>, which is the order
/// <see cref="TemporalRiskEngine"/> itself uses, so that rescoring a stored evaluation reproduces
/// the same fingerprint regardless of the order in which the caller supplies the signals.
/// </para>
/// <para>
/// <strong>Within a signal the field order is declared here and never changes.</strong> It is
/// <c>rule</c>, <c>weight</c>, then the fields of that rule in the order the <c>E9B</c> table
/// writes them, omitting the ones that are absent, and <c>detail</c> last when there is one — which
/// happens only on an <c>e3-v1</c> row read back, because the engine does not write prose any more.
/// The order is per rule and cannot be a single global one: <c>amount_anomaly</c> states its ratio
/// before the median it was measured against and <c>new_buyer_high_value</c> states it after, so no
/// one sequence satisfies both.
/// </para>
/// <para>
/// A decimal is written with the fixed number of decimals its field declares, as text, which is not
/// the same as rounding it. <c>Math.Round(4m, 1)</c> is <c>4</c>, not <c>4.0</c>: rounding sets the
/// value and this sets how the value is written. Both matter, because the fingerprint hashes these
/// characters.
/// </para>
/// </remarks>
public static class RiskSignalSerializer
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(IReadOnlyList<RiskSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var ordered = Order(signals);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartArray();
            foreach (var signal in ordered)
            {
                writer.WriteStartObject();
                writer.WriteString("rule", signal.Rule);
                writer.WriteNumber("weight", signal.Weight);
                WriteFields(writer, signal);

                if (signal.Detail is { } detail)
                {
                    writer.WriteString("detail", detail);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Reads back a canonical signal string. Used by read models that present a stored evaluation;
    /// it is never used to recompute a fingerprint, which is always derived from the stored text.
    /// </summary>
    public static IReadOnlyList<RiskSignal> Deserialize(string canonical)
    {
        ArgumentNullException.ThrowIfNull(canonical);

        var signals = JsonSerializer.Deserialize<RiskSignal[]>(canonical, ReadOptions)
            ?? throw new ArgumentException("The canonical signal string must be a JSON array.", nameof(canonical));

        return signals.AsReadOnly();
    }

    /// <summary>
    /// The fields of one rule, in the declared order, skipping the ones the signal does not carry.
    /// </summary>
    /// <remarks>
    /// A signal read back from an <c>e3-v1</c> row carries none of them and writes none, which is
    /// what reproduces its original canonical string exactly.
    /// </remarks>
    private static void WriteFields(Utf8JsonWriter writer, RiskSignal signal)
    {
        switch (signal.Rule)
        {
            case RiskRuleNames.AmountAnomaly:
                WriteAmount(writer, signal);
                WriteDecimal(writer, "ratio", signal.Ratio, RiskSignal.RatioDecimals);
                WriteScope(writer, signal.Scope);
                WriteMedian(writer, signal);
                WriteInteger(writer, "windowDays", signal.WindowDays);
                break;

            case RiskRuleNames.Velocity:
                WriteInteger(writer, "orderCount", signal.OrderCount);
                WriteInteger(writer, "windowMinutes", signal.WindowMinutes);
                WriteInteger(writer, "threshold", signal.Threshold);
                break;

            case RiskRuleNames.CrossBorderVelocity:
                WriteText(writer, "fromCountry", signal.FromCountry);
                WriteText(writer, "toCountry", signal.ToCountry);
                WriteDecimal(
                    writer,
                    "elapsedMinutes",
                    signal.ElapsedMinutes,
                    RiskSignal.ElapsedMinutesDecimals);
                break;

            case RiskRuleNames.UnusualHour:
                WriteInteger(writer, "bucketStartHour", signal.BucketStartHour);
                WriteInteger(writer, "bucketEndHour", signal.BucketEndHour);
                WriteText(writer, "timeZoneId", signal.TimeZoneId);
                WriteObservation(writer, signal);
                break;

            case RiskRuleNames.NewBuyerHighValue:
                WriteAmount(writer, signal);
                WriteMedian(writer, signal);
                WriteDecimal(writer, "ratio", signal.Ratio, RiskSignal.RatioDecimals);
                break;

            case RiskRuleNames.ForeignCountry:
                WriteText(writer, "country", signal.Country);
                WriteText(writer, "habitualCountry", signal.HabitualCountry);
                WriteObservation(writer, signal);
                break;

            default:
                throw new ArgumentException($"Unknown risk rule '{signal.Rule}'.", nameof(signal));
        }
    }

    private static void WriteAmount(Utf8JsonWriter writer, RiskSignal signal)
    {
        WriteLong(writer, "amountCents", signal.AmountCents);
        WriteText(writer, "currencyCode", signal.CurrencyCode);
    }

    private static void WriteMedian(Utf8JsonWriter writer, RiskSignal signal)
    {
        WriteLong(writer, "medianCents", signal.MedianCents);
        WriteInteger(writer, "historyCount", signal.HistoryCount);
    }

    private static void WriteObservation(Utf8JsonWriter writer, RiskSignal signal)
    {
        WriteInteger(writer, "observedCount", signal.ObservedCount);
        WriteInteger(writer, "totalCount", signal.TotalCount);
        WriteDecimal(writer, "sharePercent", signal.SharePercent, RiskSignal.SharePercentDecimals);
    }

    private static void WriteText(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteInteger(Utf8JsonWriter writer, string name, int? value)
    {
        if (value is { } present)
        {
            writer.WriteNumber(name, present);
        }
    }

    private static void WriteLong(Utf8JsonWriter writer, string name, long? value)
    {
        if (value is { } present)
        {
            writer.WriteNumber(name, present);
        }
    }

    private static void WriteScope(Utf8JsonWriter writer, AmountMedianScope? scope)
    {
        if (scope is { } present)
        {
            writer.WriteString("scope", present == AmountMedianScope.Buyer ? "buyer" : "merchant");
        }
    }

    /// <summary>
    /// A decimal at the fixed width its field declares, written as the raw characters of the
    /// number so that the trailing zeros survive into the fingerprint.
    /// </summary>
    private static void WriteDecimal(Utf8JsonWriter writer, string name, decimal? value, int decimals)
    {
        if (value is not { } present)
        {
            return;
        }

        writer.WritePropertyName(name);
        writer.WriteRawValue(
            present.ToString($"F{decimals.ToString(CultureInfo.InvariantCulture)}", CultureInfo.InvariantCulture));
    }

    private static RiskSignal[] Order(IReadOnlyList<RiskSignal> signals)
    {
        var indexed = new (int Position, RiskSignal Signal)[signals.Count];
        for (var index = 0; index < signals.Count; index++)
        {
            var signal = signals[index];
            ArgumentNullException.ThrowIfNull(signal);
            indexed[index] = (RiskRuleNames.CanonicalIndexOf(signal.Rule), signal);
        }

        Array.Sort(indexed, static (left, right) => left.Position.CompareTo(right.Position));

        for (var index = 1; index < indexed.Length; index++)
        {
            if (indexed[index].Position == indexed[index - 1].Position)
            {
                throw new ArgumentException(
                    $"A risk evaluation must not repeat rule '{indexed[index].Signal.Rule}'.",
                    nameof(signals));
            }
        }

        return Array.ConvertAll(indexed, entry => entry.Signal);
    }
}
