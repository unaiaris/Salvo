using System.Globalization;

namespace Salvo.Domain.External;

/// <summary>
/// The canonical text two implementations have to agree on to recognise the same callback twice.
/// </summary>
/// <remarks>
/// <para>
/// The shape is fixed by the design: <c>provider|externalEvaluationId|status|providerInstant</c>. It
/// never includes the instant of reception, which is a fact about this process rather than about the
/// message, and would make every redelivery look new.
/// </para>
/// <para>
/// When the provider sends no identifier of its own the correlating slot falls back to the order
/// reference, tagged so the two can never be confused. Leaving the slot empty instead would give
/// every identifier-less message of a given status and instant the same key, and a callback about
/// one order would be discarded as a duplicate of a callback about another.
/// </para>
/// <para>
/// A message with no instant collapses to provider, identifier and status, which is harmless: the
/// second copy would be a no-op anyway.
/// </para>
/// </remarks>
public static class CallbackDeduplicationKey
{
    public const char Separator = '|';

    public const string ReferencePrefix = "ref:";

    public const int MaximumLength = 512;

    /// <summary>
    /// The instant format the key uses. Fixed and culture-invariant, because the same message must
    /// produce the same key on any machine.
    /// </summary>
    public const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    public static string From(
        ExternalProvider provider,
        string? externalEvaluationId,
        string? referenceId,
        ExternalEvaluationStatus status,
        DateTimeOffset? providerInstant)
    {
        var correlator = Correlator(externalEvaluationId, referenceId);
        var instant = providerInstant is { } at
            ? at.ToUniversalTime().ToString(InstantFormat, CultureInfo.InvariantCulture)
            : string.Empty;

        return string.Join(
            Separator,
            ExternalEvaluationWireNames.ToWire(provider),
            correlator,
            ExternalEvaluationWireNames.ToWire(status),
            instant);
    }

    private static string Correlator(string? externalEvaluationId, string? referenceId)
    {
        if (!string.IsNullOrWhiteSpace(externalEvaluationId))
        {
            return externalEvaluationId.Trim();
        }

        return string.IsNullOrWhiteSpace(referenceId)
            ? string.Empty
            : ReferencePrefix + referenceId.Trim();
    }
}
