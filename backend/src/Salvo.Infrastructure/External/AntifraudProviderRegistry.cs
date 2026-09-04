using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.External;

public sealed class AntifraudProviderRegistry(IEnumerable<IAntifraudProvider> providers) : IAntifraudProviderRegistry
{
    private readonly Dictionary<ExternalProvider, IAntifraudProvider> byProvider =
        providers.ToDictionary(provider => provider.Provider);

    public IAntifraudProvider? Find(ExternalProvider provider)
    {
        return byProvider.GetValueOrDefault(provider);
    }
}
