using Salvo.Domain.Orders;

namespace Salvo.Application.Orders;

public sealed record OrderPage(
    IReadOnlyList<Order> Items,
    int TotalCount);
