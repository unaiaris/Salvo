using Salvo.Application.Alerts;

namespace Salvo.Infrastructure;

public sealed class SystemAlertIdGenerator : IAlertIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
