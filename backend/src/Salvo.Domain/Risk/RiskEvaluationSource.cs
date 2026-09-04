namespace Salvo.Domain.Risk;

/// <summary>
/// Origin of a risk evaluation. Only the deterministic engine produces one.
/// </summary>
/// <remarks>
/// A single member on purpose. The external provider has its own entity, its own table and its own
/// <c>ExternalProvider</c> enumeration; sharing this one would put a mutable lifecycle back inside
/// an append-only type. The column survives because the wire name is hashed into the fingerprint of
/// every existing evaluation.
/// </remarks>
public enum RiskEvaluationSource
{
    Local = 1,
}
