using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ReflectionAssembly = System.Reflection.Assembly;

namespace ArchitectureTests;

public sealed class ModuleTests : BaseArchitectureTest
{
    [Fact]
    public void Modules_ShouldNotDependOn_OtherModules()
    {
        foreach (ReflectionAssembly module in ModuleAssemblies)
        {
            foreach (ReflectionAssembly other in ModuleAssemblies.Where(a => a != module))
            {
                Types()
                    .That()
                    .ResideInAssembly(module)
                    .Should()
                    .NotDependOnAnyTypesThat()
                    .ResideInAssembly(other)
                    .Check(Architecture);
            }
        }
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_FeaturesLayer()
    {
        foreach (string moduleName in ModuleNames())
        {
            Types()
                .That()
                .ResideInNamespaceMatching($@"^{moduleName}\.Infrastructure")
                .Should()
                .NotDependOnAnyTypesThat()
                .ResideInNamespaceMatching($@"^{moduleName}\.Features")
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }
    }

    [Fact]
    public void ModuleInfrastructure_ShouldBeInternal()
    {
        foreach (string moduleName in ModuleNames())
        {
            Classes()
                .That()
                .ResideInNamespaceMatching($@"^{moduleName}\.Infrastructure")
                .And()
                .DoNotResideInNamespaceMatching($@"^{moduleName}\.Infrastructure\.Data\.Migrations")
                .Should()
                .BeInternal()
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }
    }

    private static IEnumerable<string> ModuleNames() =>
        ModuleAssemblies.Select(a => a.GetName().Name!);
}
