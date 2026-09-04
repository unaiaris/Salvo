namespace Salvo.Domain.External;

/// <summary>
/// Lifecycle of an external evaluation. Unlike the local one, it is mutable on purpose: the row is
/// reserved before the provider is called and only later learns what the provider decided.
/// </summary>
public enum ExternalEvaluationStatus
{
    Pending = 1,
    Approved = 2,
    Denied = 3,
    Error = 4,
}
