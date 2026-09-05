using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Salvo.Domain.Explanations;

/// <summary>
/// Canonical serialization of the rules a provider says it cited.
/// </summary>
/// <remarks>
/// Written with a writer rather than a reflection-based serializer for the same reason
/// <c>RiskSignalSerializer</c> is: the produced string is what gets persisted, and it has to be the
/// same string on every machine and every runtime.
/// </remarks>
public static class ReferencedRuleSerializer
{
    public static string Serialize(IReadOnlyList<string> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartArray();
            foreach (var rule in rules)
            {
                ArgumentNullException.ThrowIfNull(rule);
                writer.WriteStringValue(rule);
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static IReadOnlyList<string> Deserialize(string canonical)
    {
        ArgumentNullException.ThrowIfNull(canonical);

        var rules = JsonSerializer.Deserialize<string[]>(canonical)
            ?? throw new ArgumentException(
                "The canonical referenced-rule string must be a JSON array.",
                nameof(canonical));

        return rules.AsReadOnly();
    }
}
