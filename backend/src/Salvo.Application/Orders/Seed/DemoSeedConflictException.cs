namespace Salvo.Application.Orders.Seed;

/// <summary>
/// Why the demo corpus cannot be loaded into this database.
/// </summary>
/// <remarks>
/// The two cases need different words on screen, and telling them apart is not a guess: only the
/// seed writes an <see cref="Salvo.Domain.Evaluation.OrderEvaluationLabel"/>, so a colliding order
/// that carries one was written by an earlier version of this very corpus, and one that does not
/// was imported by somebody.
/// </remarks>
public enum DemoSeedConflictReason
{
    /// <summary>The database holds an earlier version of the demo corpus.</summary>
    PreviousCorpus = 1,

    /// <summary>The database holds imported orders that use the same merchant references.</summary>
    ImportedOrders = 2,
}

public sealed class DemoSeedConflictException(DemoSeedConflictReason reason, string message)
    : Exception(message)
{
    public DemoSeedConflictReason Reason { get; } = reason;
}
