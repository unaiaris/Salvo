using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Salvo.Domain.Risk;

/// <summary>
/// Canonical, culture-independent serialization of the signals of a local risk evaluation.
/// </summary>
/// <remarks>
/// The produced string is both what is persisted and what is hashed into the evaluation
/// fingerprint: it is never re-serialized. Signals are emitted in the rule order of
/// <see cref="RiskRuleNames.CanonicalOrder"/>, which is the order
/// <see cref="TemporalRiskEngine"/> itself uses, so that rescoring a stored evaluation reproduces
/// the same fingerprint regardless of the order in which the caller supplies the signals.
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
                writer.WriteString("detail", signal.Detail);
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
