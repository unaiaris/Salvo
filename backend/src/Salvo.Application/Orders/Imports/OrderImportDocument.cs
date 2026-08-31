namespace Salvo.Application.Orders.Importing;

public sealed record OrderImportDocument(IReadOnlyList<RawOrderImportRecord> Records);
