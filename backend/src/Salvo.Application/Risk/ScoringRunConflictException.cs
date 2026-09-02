namespace Salvo.Application.Risk;

/// <summary>
/// Raised when a scoring run cannot be persisted because a concurrent run already claimed the same
/// unique state. Infrastructure translates provider-specific uniqueness failures into this type so
/// that no persistence detail crosses the boundary.
/// </summary>
public sealed class ScoringRunConflictException : Exception
{
    public ScoringRunConflictException()
        : base("A concurrent scoring run already persisted conflicting state.")
    {
    }

    public ScoringRunConflictException(string message)
        : base(message)
    {
    }

    public ScoringRunConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
