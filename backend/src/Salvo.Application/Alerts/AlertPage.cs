namespace Salvo.Application.Alerts;

/// <param name="CurrentRun">
/// The run the current evaluations of the page come from, or <see langword="null"/> when the corpus
/// was never scored.
/// </param>
public sealed record AlertPage(
    IReadOnlyList<AlertContext> Items,
    int TotalCount,
    ScoringRunReference? CurrentRun);
