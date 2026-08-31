namespace Salvo.Domain.Orders;

public sealed record OrderValidationError(string Field, string Code, string Message);
