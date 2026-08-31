using Salvo.Domain;

namespace Salvo.Domain.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void DomainAssemblyCanBeLoadedWithoutFrameworkDependencies()
    {
        var assembly = typeof(DomainAssemblyMarker).Assembly;

        Assert.Equal("Salvo.Domain", assembly.GetName().Name);
    }
}
