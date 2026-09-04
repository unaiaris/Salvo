using Salvo.Application.External;

namespace Salvo.Infrastructure;

public sealed class SystemExternalEvaluationIdGenerator : IExternalEvaluationIdGenerator
{
    public Guid Create() => Guid.NewGuid();
}
