namespace Salvo.Domain.Orders;

public sealed record OrderCreationResult(Order? Order, IReadOnlyList<OrderValidationError> Errors)
{
    public bool IsSuccess => Order is not null && Errors.Count == 0;
}
