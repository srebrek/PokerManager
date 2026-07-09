using ArchUnitNET.Domain;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class SharedArchitectureTests : BaseArchitectureTest
{
    private static readonly IObjectProvider<IType> s_domainLayer =
        Types().That().ResideInNamespaceMatching(@"^Shared\.Domain").As("Shared.Domain");

    private static readonly IObjectProvider<IType> s_applicationLayer =
        Types().That().ResideInNamespaceMatching(@"^Shared\.Application").As("Shared.Application");

    private static readonly IObjectProvider<IType> s_infrastructureLayer =
        Types().That().ResideInNamespaceMatching(@"^Shared\.Infrastructure").As("Shared.Infrastructure");

    private static readonly IObjectProvider<IType> s_presentationLayer =
        Types().That().ResideInNamespaceMatching(@"^Shared\.Presentation").As("Shared.Presentation");

    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_applicationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_infrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_PresentationLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_presentationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        Types()
            .That()
            .Are(s_applicationLayer)
            .Should()
            .NotDependOnAny(s_infrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_PresentationLayer()
    {
        Types()
            .That()
            .Are(s_applicationLayer)
            .Should()
            .NotDependOnAny(s_presentationLayer)
            .Check(Architecture);
    }
}
