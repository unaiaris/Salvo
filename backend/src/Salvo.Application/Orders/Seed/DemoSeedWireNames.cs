namespace Salvo.Application.Orders.Seed;

/// <summary>
/// The wire spelling of <see cref="DemoSeedConflictReason"/>. Declared rather than derived from the
/// enumeration name, so renaming a member in C# cannot silently change the contract.
/// </summary>
public static class DemoSeedWireNames
{
    public const string PreviousCorpus = "PREVIOUS_CORPUS";
    public const string ImportedOrders = "IMPORTED_ORDERS";

    public static string ToWire(DemoSeedConflictReason reason)
    {
        return reason switch
        {
            DemoSeedConflictReason.PreviousCorpus => PreviousCorpus,
            DemoSeedConflictReason.ImportedOrders => ImportedOrders,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown conflict reason."),
        };
    }
}
