using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

public sealed class RiskEvaluationStatusConverter()
    : ValueConverter<RiskEvaluationStatus, string>(
        value => RiskEvaluationWireNames.ToWire(value),
        value => RiskEvaluationWireNames.ParseStatus(value));
