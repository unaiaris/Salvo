namespace Salvo.Application.Orders.Importing;

public sealed record ImportOrdersResult(
    int TotalRecords,
    int ImportedCount,
    int DuplicateCount,
    int InvalidRecordCount,
    bool ErrorsTruncated,
    IReadOnlyList<ImportRecordError> Errors);
