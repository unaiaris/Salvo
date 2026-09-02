namespace Salvo.Domain.Risk;

/// <summary>
/// One execution of the deterministic engine over the whole corpus. A run is append-only and, once
/// persisted, defines which evaluation is current for each order it covered.
/// </summary>
/// <remarks>
/// <see cref="Sequence"/> gives runs a total order that does not depend on timestamp resolution and
/// is unique in the database, so two concurrent runs collide on a unique constraint instead of
/// producing an ambiguous "latest run".
/// </remarks>
public sealed class ScoringRun
{
    private ScoringRun()
    {
        RuleConfigVersion = string.Empty;
    }

    private ScoringRun(
        Guid id,
        long sequence,
        string ruleConfigVersion,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        int orderCount,
        int evaluationsCreated,
        int evaluationsReused)
    {
        Id = id;
        Sequence = sequence;
        RuleConfigVersion = ruleConfigVersion;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        OrderCount = orderCount;
        EvaluationsCreated = evaluationsCreated;
        EvaluationsReused = evaluationsReused;
    }

    public Guid Id { get; private set; }

    public long Sequence { get; private set; }

    public string RuleConfigVersion { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }

    public int OrderCount { get; private set; }

    public int EvaluationsCreated { get; private set; }

    public int EvaluationsReused { get; private set; }

    public static ScoringRun Complete(
        Guid id,
        long sequence,
        string ruleConfigVersion,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        int orderCount,
        int evaluationsCreated,
        int evaluationsReused)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleConfigVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(orderCount);
        ArgumentOutOfRangeException.ThrowIfNegative(evaluationsCreated);
        ArgumentOutOfRangeException.ThrowIfNegative(evaluationsReused);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("id must be a non-empty GUID.", nameof(id));
        }

        var startedAtUtc = startedAt.ToUniversalTime();
        var completedAtUtc = completedAt.ToUniversalTime();
        if (completedAtUtc < startedAtUtc)
        {
            throw new ArgumentException(
                "completedAt must not precede startedAt.",
                nameof(completedAt));
        }

        if (evaluationsCreated + evaluationsReused != orderCount)
        {
            throw new ArgumentException(
                "Every scored order must be accounted for exactly once as created or reused.",
                nameof(orderCount));
        }

        return new(
            id,
            sequence,
            ruleConfigVersion,
            startedAtUtc,
            completedAtUtc,
            orderCount,
            evaluationsCreated,
            evaluationsReused);
    }
}
