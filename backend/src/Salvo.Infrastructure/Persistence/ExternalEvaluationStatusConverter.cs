using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExternalEvaluationStatusConverter()
    : ValueConverter<ExternalEvaluationStatus, string>(
        value => ExternalEvaluationWireNames.ToWire(value),
        value => ExternalEvaluationWireNames.ParseStatus(value));
