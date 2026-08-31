namespace Salvo.Application.Orders.Importing;

public sealed record RawOrderImportRecord(
    int RecordNumber,
    int? LineNumber,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<ImportRecordError> Errors);
