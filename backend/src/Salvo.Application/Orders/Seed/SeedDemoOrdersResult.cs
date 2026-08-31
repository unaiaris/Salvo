namespace Salvo.Application.Orders.Seed;

public sealed record SeedDemoOrdersResult(
    string DatasetVersion,
    int TotalOrders,
    int InsertedOrders,
    int DuplicateOrders,
    int TotalLabels,
    int InsertedLabels,
    int FraudLabelCount);
