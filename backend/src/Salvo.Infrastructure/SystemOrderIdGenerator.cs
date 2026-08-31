using Salvo.Application.Orders;

namespace Salvo.Infrastructure;

public sealed class SystemOrderIdGenerator : IOrderIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
