namespace Salvo.Application.Orders.Seed;

public interface IDemoOrderSource
{
    Task<DemoOrderDocument> LoadAsync(CancellationToken cancellationToken);
}
