using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExternalProviderConverter()
    : ValueConverter<ExternalProvider, string>(
        value => ExternalEvaluationWireNames.ToWire(value),
        value => ExternalEvaluationWireNames.ParseProvider(value));
