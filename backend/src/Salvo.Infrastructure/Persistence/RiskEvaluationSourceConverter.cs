using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

public sealed class RiskEvaluationSourceConverter()
    : ValueConverter<RiskEvaluationSource, string>(
        value => RiskEvaluationWireNames.ToWire(value),
        value => RiskEvaluationWireNames.ParseSource(value));
