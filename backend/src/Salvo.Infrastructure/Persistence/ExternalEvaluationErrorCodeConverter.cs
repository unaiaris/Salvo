using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExternalEvaluationErrorCodeConverter()
    : ValueConverter<ExternalEvaluationErrorCode, string>(
        value => ExternalEvaluationWireNames.ToWire(value),
        value => ExternalEvaluationWireNames.ParseErrorCode(value));
