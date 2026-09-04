namespace Salvo.Application.External;

/// <summary>
/// The callback could not be written because another writer keeps winning the row.
/// </summary>
/// <remarks>
/// Answered with <c>503</c> and a <c>Retry-After</c> rather than an error the provider would give up
/// on. The retry converges: whatever the other writer is doing settles the evaluation, and a message
/// about a settled evaluation is classified without contending for it.
/// </remarks>
public sealed class ExternalCallbackUnavailableException : Exception
{
    public ExternalCallbackUnavailableException()
        : base("The external evaluation is being written by another request.")
    {
    }

    public ExternalCallbackUnavailableException(string message)
        : base(message)
    {
    }

    public ExternalCallbackUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
