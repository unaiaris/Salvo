namespace Salvo.Application.Orders.Importing;

public sealed record ImportRecordError(
    int RecordNumber,
    int? LineNumber,
    string? Field,
    string Code,
    string Message);
