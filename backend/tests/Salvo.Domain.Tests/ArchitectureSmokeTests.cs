using Salvo.Domain;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void DomainAssemblyCanBeLoadedWithoutFrameworkDependencies()
    {
        var assembly = typeof(DomainAssemblyMarker).Assembly;

        Assert.Equal("Salvo.Domain", assembly.GetName().Name);
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
}
