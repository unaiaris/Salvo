using Salvo.Application.External;

namespace Salvo.Infrastructure;

public sealed class SystemCallbackReceiptIdGenerator : ICallbackReceiptIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
