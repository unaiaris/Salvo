using Salvo.Domain.External;

namespace Salvo.Application.External;

/// <summary>
/// The requested provider has no adapter in this deployment.
/// </summary>
public sealed class ExternalProviderNotRegisteredException : Exception
{
    public ExternalProviderNotRegisteredException()
        : base("No adapter is registered for that external provider.")
    {
    }

    public ExternalProviderNotRegisteredException(string message)
        : base(message)
    {
    }

    public ExternalProviderNotRegisteredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ExternalProviderNotRegisteredException(ExternalProvider provider)
        : base($"No adapter is registered for external provider '{ExternalEvaluationWireNames.ToWire(provider)}'.")
    {
    }
}
