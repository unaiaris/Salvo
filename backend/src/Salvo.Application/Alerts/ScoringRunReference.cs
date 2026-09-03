namespace Salvo.Application.Alerts;

/// <summary>
/// The scoring run a read is current as of.
/// </summary>
/// <remarks>
/// It travels with the alert contract because the instant of an evaluation is not the instant of
/// the run that made it current: an unchanged evaluation is reused by later runs and keeps the
/// timestamp of the run that first inserted it. Without this, a screen labelling the current block
/// with <c>evaluatedAt</c> would present a weeks-old moment as if it were now.
/// </remarks>
public sealed record ScoringRunReference(long Sequence, DateTimeOffset CompletedAt);
