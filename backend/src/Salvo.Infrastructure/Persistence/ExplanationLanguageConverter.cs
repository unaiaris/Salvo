using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Salvo.Domain.Explanations;

namespace Salvo.Infrastructure.Persistence;

public sealed class ExplanationLanguageConverter()
    : ValueConverter<ExplanationLanguage, string>(
        value => ExplanationWireNames.ToWire(value),
        value => ExplanationWireNames.ParseLanguage(value));
