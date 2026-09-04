using Salvo.Domain.External;

namespace Salvo.Application.External;

public interface IAntifraudProviderRegistry
{
    /// <summary>
    /// The adapter registered for <paramref name="provider"/>, or <see langword="null"/> when this
    /// deployment has none. Which providers exist is runtime configuration, so a caller has to be
    /// able to ask rather than assume.
    /// </summary>
    IAntifraudProvider? Find(ExternalProvider provider);
}
