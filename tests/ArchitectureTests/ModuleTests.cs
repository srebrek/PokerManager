using ArchUnitNET.xUnitV3;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ReflectionAssembly = System.Reflection.Assembly;

namespace ArchitectureTests;

public sealed class ModuleTests : BaseArchitectureTest
{
    // Code-coverage instrumentation (e.g. "run tests with coverage" in VS Code) injects a
    // Microsoft.CodeCoverage.Instrumentation.Static.Tracker.* type into every instrumented
    // assembly. ArchUnitNET then sees that type name in both module assemblies and flags a
    // false cross-module dependency. Excluded here since it's a test-run artifact, not code.
    private const string CoverageInstrumentationNamespace = @"^Microsoft\.CodeCoverage\..*";

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
                    .And()
                    .DoNotHaveFullNameMatching(CoverageInstrumentationNamespace)
                    .Should()
                    .NotDependOnAnyTypesThat()
                    .ResideInAssembly(other)
                    .AndShould()
                    .NotHaveFullNameMatching(CoverageInstrumentationNamespace)
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

    private static HashSet<string> PublicInfrastructureExceptions { get; } =
    [
        "Identity.Infrastructure.User", // Arg of public UserAccountService
    ];

    [Fact]
    public void ModuleInfrastructure_ShouldBeInternal_ExceptWolverineCodegenSurface()
    {
        foreach (ReflectionAssembly module in ModuleAssemblies)
        {
            string moduleName = module.GetName().Name!;
            IEnumerable<Type> infrastructureTypes = module
                .GetTypes()
                .Where(t => !t.IsNested
                    && t.Namespace is not null
                    && t.Namespace.StartsWith($"{moduleName}.Infrastructure", StringComparison.Ordinal)
                    && !t.Namespace.StartsWith($"{moduleName}.Infrastructure.Data.Migrations", StringComparison.Ordinal));

            foreach (Type type in infrastructureTypes)
            {
                bool isDbContext = type.IsSubclassOf(typeof(DbContext));
                bool isAbstractionsImplementation = type.GetInterfaces()
                    .Any(i => i.Namespace == $"{moduleName}.Abstractions");
                bool isListedException = PublicInfrastructureExceptions.Contains(type.FullName!);

                if (isDbContext || isAbstractionsImplementation)
                {
                    type.IsPublic.ShouldBeTrue(
                        $"'{type.FullName}' is part of the Wolverine codegen surface and must be public.");
                }
                else if (!isListedException)
                {
                    type.IsPublic.ShouldBeFalse($"'{type.FullName}' is infrastructure model and should be internal.");
                }
            }
        }
    }

    [Fact]
    public void ModuleDomain_ShouldBeInternal_ExceptEventTypes()
    {
        foreach (ReflectionAssembly module in ModuleAssemblies)
        {
            string moduleName = module.GetName().Name!;
            IEnumerable<Type> domainTypes = module
                .GetTypes()
                .Where(t => !t.IsNested
                    && t.Namespace is not null
                    && t.Namespace.StartsWith($"{moduleName}.Domain", StringComparison.Ordinal)
                    && !typeof(Shared.Domain.IDomainEvent).IsAssignableFrom(t)
                    && !typeof(Contracts.IntegrationEvents.IIntegrationEvent).IsAssignableFrom(t));

            foreach (Type type in domainTypes)
            {
                type.IsPublic.ShouldBeFalse($"'{type.FullName}' is domain model and should be internal.");
            }
        }
    }

    private static IEnumerable<string> ModuleNames() =>
        ModuleAssemblies.Select(a => a.GetName().Name!);
}
