using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.External;

/// <summary>
/// The adapters this deployment registered, indexed by the provider they speak for.
/// </summary>
/// <remarks>
/// When two adapters claim the same provider the last registration wins, which is how replacing a
/// service works everywhere else in this container. Failing instead would turn an override into a
/// server error at the first request rather than a decision at composition time.
/// </remarks>
public sealed class AntifraudProviderRegistry : IAntifraudProviderRegistry
{
    private readonly Dictionary<ExternalProvider, IAntifraudProvider> byProvider = [];

    public AntifraudProviderRegistry(IEnumerable<IAntifraudProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        foreach (var provider in providers)
        {
            byProvider[provider.Provider] = provider;
        }
    }

    public IAntifraudProvider? Find(ExternalProvider provider)
    {
        return byProvider.GetValueOrDefault(provider);
    }
}
