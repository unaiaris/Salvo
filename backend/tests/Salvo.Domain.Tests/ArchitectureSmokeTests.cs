using Salvo.Domain;
using Salvo.Domain.Alerts;
using Salvo.Domain.Evaluation;
using Salvo.Domain.External;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class ArchitectureSmokeTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "Microsoft.Extensions",
        "CsvHelper",
        "Anthropic",
    ];

    [Fact]
    public void DomainAssemblyCanBeLoadedWithoutFrameworkDependencies()
    {
        var assembly = typeof(DomainAssemblyMarker).Assembly;

        Assert.Equal("Salvo.Domain", assembly.GetName().Name);
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference => ForbiddenAssemblyPrefixes.Any(prefix =>
                reference.Name?.StartsWith(prefix, StringComparison.Ordinal) == true));
    }

    [Fact]
    public void ScoringBoundaryCannotReceiveGroundTruthLabels()
    {
        var scoreMethod = typeof(TemporalRiskEngine).GetMethod(nameof(TemporalRiskEngine.Score));

        Assert.NotNull(scoreMethod);
        Assert.DoesNotContain(
            scoreMethod.GetParameters(),
            parameter => parameter.ParameterType == typeof(OrderEvaluationLabel)
                || parameter.ParameterType.GenericTypeArguments.Contains(typeof(OrderEvaluationLabel)));
    }

    [Fact]
    public void PersistedRiskEntitiesCarryNoGroundTruthLabel()
    {
        foreach (var type in new[]
        {
            typeof(RiskEvaluation),
            typeof(ScoringRun),
            typeof(RunEvaluation),
            typeof(ExternalEvaluation),
            typeof(CallbackReceipt),
            typeof(Alert),
            typeof(AlertReview),
        })
        {
            Assert.DoesNotContain(
                type.GetProperties(),
                property => property.PropertyType == typeof(OrderEvaluationLabel)
                    || property.Name.Contains("Fraud", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Contains("Label", StringComparison.OrdinalIgnoreCase));
        }
    }
}
