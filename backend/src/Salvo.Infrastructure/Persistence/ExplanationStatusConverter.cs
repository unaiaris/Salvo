using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExplanationStatusConverter()
    : ValueConverter<ExplanationStatus, string>(
        value => ExplanationWireNames.ToWire(value),
        value => ExplanationWireNames.ParseStatus(value));
