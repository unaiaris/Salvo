using Salvo.Application.Explanations;

namespace Salvo.Infrastructure;

public sealed class SystemExplanationIdGenerator : IExplanationIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
