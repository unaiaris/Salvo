namespace Salvo.Application.Orders;

public interface IOrderIdGenerator
{
    Guid Create();
}
