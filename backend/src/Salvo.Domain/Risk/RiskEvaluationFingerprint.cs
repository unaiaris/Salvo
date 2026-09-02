using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Salvo.Domain.Risk;

/// <summary>
/// Content identity of a local risk evaluation.
/// </summary>
/// <remarks>
/// The fingerprint is <c>SHA-256(orderId | source | ruleConfigVersion | score | signalsCanonical)</c>
/// over UTF-8 bytes, rendered as lowercase hexadecimal. Only the last component can contain the
/// separator, so the encoding is unambiguous. The canonical signal string is supplied by the caller
/// and must be exactly the string that gets persisted; it is never re-serialized here.
/// </remarks>
public static class RiskEvaluationFingerprint
{
    public const int Length = 64;

    public static string Compute(
        Guid orderId,
        RiskEvaluationSource source,
        string ruleConfigVersion,
        int score,
        string signalsCanonical)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleConfigVersion);
        ArgumentNullException.ThrowIfNull(signalsCanonical);

        var material = string.Create(
            CultureInfo.InvariantCulture,
            $"{orderId:D}|{RiskEvaluationWireNames.ToWire(source)}|{ruleConfigVersion}|{score}|{signalsCanonical}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        return Convert.ToHexStringLower(hash);
    }
}
