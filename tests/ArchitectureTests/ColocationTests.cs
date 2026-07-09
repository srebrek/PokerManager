using FluentValidation;
using Shouldly;

namespace ArchitectureTests;

public sealed class ColocationTests : BaseArchitectureTest
{
    [Fact]
    public void FeatureComponents_ShouldResideInSameNamespace()
    {
        // Lookups are scoped per module assembly: two modules can legitimately contain
        // identically named handlers/endpoints, which must not match across modules.
        foreach (System.Reflection.Assembly moduleAssembly in ModuleAssemblies)
        {
            AssertFeatureComponentsColocated(moduleAssembly);
        }
    }

    private static void AssertFeatureComponentsColocated(System.Reflection.Assembly moduleAssembly)
    {
        Type[] allTypes = [.. moduleAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, DeclaringType: null })];

        List<Type> commands = [.. allTypes
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))];

        foreach (Type command in commands)
        {
            string expectedNamespace = command.Namespace!;
            string expectedHandlerName = command.Name[..^"Command".Length] + "CommandHandler";

            Type handler = allTypes.SingleOrDefault(t => t.Name == expectedHandlerName)
                .ShouldNotBeNull($"Command '{command.Name}' has no handler. " +
                    $"Expected '{expectedHandlerName}' in the same assembly.");

            handler.Namespace
                .ShouldBe(expectedNamespace,
                    $"Handler '{handler.Name}' should be in namespace " +
                    $"'{expectedNamespace}' (same as '{command.Name}')");

            Type? validator = allTypes.SingleOrDefault(t =>
                t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>)
                && t.BaseType.GetGenericArguments().Contains(command));

            validator?.Namespace
                .ShouldBe(expectedNamespace,
                    $"Validator '{validator.Name}' should be in namespace " +
                    $"'{expectedNamespace}' (same as '{command.Name}')");

            string featureName = expectedNamespace[(expectedNamespace.LastIndexOf('.') + 1)..];
            Type? endpoint = allTypes.SingleOrDefault(t => t.Name == $"{featureName}Endpoint");

            endpoint?.Namespace
                .ShouldBe(expectedNamespace,
                    $"Endpoint '{endpoint.Name}' should be in namespace " +
                    $"'{expectedNamespace}' (same as '{command.Name}')");
        }
    }

    [Fact]
    public void RequestAndResponse_ShouldResideInSameNamespace()
    {
        Type[] allTypes = [.. ContractsAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, DeclaringType: null })];

        List<Type> requests = [.. allTypes.Where(t => t.Name
            .EndsWith("Request", StringComparison.Ordinal))];
        List<Type> responses = [.. allTypes.Where(t => t.Name
            .EndsWith("Response", StringComparison.Ordinal))];

        foreach (Type request in requests)
        {
            string baseFeatureName = request.Name[..^"Request".Length];
            Type? matchingResponse = responses.SingleOrDefault(r => r.Name == $"{baseFeatureName}Response");

            if (matchingResponse is null)
            {
                continue;
            }

            request.Namespace.ShouldNotBeNull();
            request.Namespace.ShouldBe(
                matchingResponse.Namespace,
                $"'{request.Name}' and '{matchingResponse.Name}' should be in the same namespace.");
        }
    }
}
