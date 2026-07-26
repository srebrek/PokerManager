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
    public void DomainLayer_DoesNotDependOnApplicationLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_applicationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void DomainLayer_DoesNotDependOnInfrastructureLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_infrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void DomainLayer_DoesNotDependOnPresentationLayer()
    {
        Types()
            .That()
            .Are(s_domainLayer)
            .Should()
            .NotDependOnAny(s_presentationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_DoesNotDependOnInfrastructureLayer()
    {
        Types()
            .That()
            .Are(s_applicationLayer)
            .Should()
            .NotDependOnAny(s_infrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_DoesNotDependOnPresentationLayer()
    {
        Types()
            .That()
            .Are(s_applicationLayer)
            .Should()
            .NotDependOnAny(s_presentationLayer)
            .Check(Architecture);
    }
}
