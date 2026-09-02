using Salvo.Application.Risk;

namespace Salvo.Infrastructure;

public sealed class SystemRiskIdGenerator : IRiskIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
