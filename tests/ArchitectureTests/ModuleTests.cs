using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ReflectionAssembly = System.Reflection.Assembly;

namespace ArchitectureTests;

/// <summary>
/// Generic per-module rules. They apply automatically to every assembly registered in
/// <see cref="BaseArchitectureTest.ModuleAssemblies"/> — no per-module copies needed.
/// </summary>
public sealed class ModuleTests : BaseArchitectureTest
{
    [Fact]
    public void Modules_ShouldNotDependOn_OtherModules()
    {
        // Modules may only communicate via Contracts (public types) and Wolverine
        // messages — never by referencing each other directly.
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
    public void Features_ShouldNotDependOn_InfrastructureLayer()
    {
        foreach (string moduleName in ModuleNames())
        {
            Types()
                .That()
                .ResideInNamespaceMatching($@"^{moduleName}\.Features")
                .Should()
                .NotDependOnAnyTypesThat()
                .ResideInNamespaceMatching($@"^{moduleName}\.Infrastructure")
                .Check(Architecture);
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
                .Check(Architecture);
        }
    }

    private static IEnumerable<string> ModuleNames() =>
        ModuleAssemblies.Select(a => a.GetName().Name!);
}
