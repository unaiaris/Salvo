using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExplanationFailureCodeConverter()
    : ValueConverter<ExplanationFailureCode, string>(
        value => ExplanationWireNames.ToWire(value),
        value => ExplanationWireNames.ParseFailureCode(value));
