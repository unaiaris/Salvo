using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExplanationProviderConverter()
    : ValueConverter<ExplanationProvider, string>(
        value => ExplanationWireNames.ToWire(value),
        value => ExplanationWireNames.ParseProvider(value));
