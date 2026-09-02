namespace Salvo.Domain.Risk;

/// <summary>
/// Origin of a risk evaluation. Only <see cref="Local"/> is produced by the deterministic engine;
/// the remaining sources exist so the external provider of a later stage does not require a schema
/// rewrite.
/// </summary>
public enum RiskEvaluationSource
{
    Local = 1,
    ExternalMock = 2,
    KoinSandbox = 3,
}
