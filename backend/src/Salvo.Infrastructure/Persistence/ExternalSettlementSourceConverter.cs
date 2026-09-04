using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExternalSettlementSourceConverter()
    : ValueConverter<ExternalSettlementSource, string>(
        value => ExternalEvaluationWireNames.ToWire(value),
        value => ExternalEvaluationWireNames.ParseSettlementSource(value));
