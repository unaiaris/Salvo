namespace Salvo.Application.Alerts;

public sealed record AlertPage(IReadOnlyList<AlertContext> Items, int TotalCount);
