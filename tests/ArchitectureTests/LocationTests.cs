using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class LocationTests : BaseArchitectureTest
{

    [Fact]
    public void DbContext_ShouldResideIn_DataNamespace()
    {
        Classes()
            .That()
            .HaveNameEndingWith("DbContext")
            .Should()
            .ResideInNamespaceMatching(@"^[^.]+\.Infrastructure\.Data$") // Matches <AssemblyName>.Data
            .Check(Architecture);
    }

    [Fact]
    public void Command_ShouldResideIn_FeaturesNamespace()
    {
        Classes()
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ResideInNamespaceMatching(@"^[^.]+\.Features\.") // Matches <AssemblyName>.Features.*
            .Check(Architecture);
    }

    [Fact]
    public void Query_ShouldResideIn_FeaturesNamespace()
    {
        Classes()
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .ResideInNamespaceMatching(@"^[^.]+\.Features\.") // Matches <AssemblyName>.Features.*
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
