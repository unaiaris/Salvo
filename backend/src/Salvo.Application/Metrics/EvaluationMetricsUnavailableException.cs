namespace Salvo.Application.Metrics;

/// <summary>
/// The quality metrics cannot be computed from the state the database is in: either the corpus was
/// never scored, or the labelled part of it does not span two distinct instants and no temporal
/// split exists.
/// </summary>
/// <remarks>
/// This is a conflict with the current state, not a server fault. Both situations are reachable
/// from the flow the console itself invites — importing a file writes no labels — so the endpoint
/// answers them with a code the interface can explain instead of with a stack trace.
/// </remarks>
public sealed class EvaluationMetricsUnavailableException(string message) : Exception(message);
