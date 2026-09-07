namespace Salvo.Application.Orders.Seed;

public sealed record SeedDemoOrdersResult(
    string DatasetVersion,
    int TotalOrders,
    int InsertedOrders,
    int DuplicateOrders,
    int TotalLabels,
    int InsertedLabels,
    int FraudLabelCount);

/// <summary>
/// What loading the demo corpus would do, without doing it.
/// </summary>
/// <remarks>
/// <para>
/// The console asks for this when <c>/import</c> opens, so that a database holding the previous
/// corpus is announced <strong>before</strong> the button is pressed rather than discovered by
/// pressing it. It runs the same comparison the seed runs and writes nothing.
/// </para>
/// <para>
/// It is deliberately the same code path: a preview that reimplemented the comparison would be a
/// second opinion about the same question, and the two would drift.
/// </para>
/// </remarks>
/// <param name="Conflict"><see langword="null"/> when the corpus can be loaded as it is.</param>
public sealed record DemoSeedPreviewResult(
    string DatasetVersion,
    int TotalOrders,
    int OrdersToInsert,
    int DuplicateOrders,
    int LabelsToInsert,
    string? Conflict);
