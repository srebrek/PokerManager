using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ReflectionAssembly = System.Reflection.Assembly;

namespace ArchitectureTests;

public abstract class BaseArchitectureTest
{
    protected static readonly ReflectionAssembly IdentityAssembly = typeof(Identity.IdentityModule).Assembly;
    protected static readonly ReflectionAssembly SharedAssembly = typeof(Shared.Domain.Entity).Assembly;
    protected static readonly ReflectionAssembly ContractsAssembly =
        typeof(Contracts.Authentication.RegisterRequest).Assembly;

    // IMPORTANT: register every new module assembly here (and reference its project from
    // this test project), otherwise it is invisible to ALL architecture rules.
    protected static readonly ReflectionAssembly[] ModuleAssemblies = [IdentityAssembly];

    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies([.. ModuleAssemblies, SharedAssembly, ContractsAssembly])
        .Build();
}
