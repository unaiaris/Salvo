namespace Salvo.Domain.External;

/// <summary>
/// Which path settled an external evaluation, or probed it while it stayed pending. Provenance is
/// part of the record: a verdict that arrived by callback and one this API went looking for are not
/// the same fact.
/// </summary>
public enum ExternalSettlementSource
{
    Sync = 1,
    Callback = 2,
    Reconciliation = 3,
}
