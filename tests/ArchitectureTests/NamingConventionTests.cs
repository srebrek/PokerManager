using System.Diagnostics;
using ArchUnitNET.xUnitV3;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Presentation;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class NamingConventionTests : BaseArchitectureTest
{
    [Fact]
    public void DbContexts_ShouldHaveNameEndingWith_DbContext()
    {
        Type[] dbContextTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(DbContext)))];

        dbContextTypes.ShouldNotBeEmpty();
        foreach (Type t in dbContextTypes)
        {
            t.Name.ShouldEndWith("DbContext");
        }
    }

    [Fact]
    public void DomainEvents_ShouldHaveNameEndingWith_DomainEvent()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Shared.Domain.IDomainEvent))
            .And()
            .DoNotImplementInterface(typeof(Contracts.IntegrationEvents.IIntegrationEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void IntegrationEvents_ShouldHaveNameEndingWith_IntegrationEvent()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Contracts.IntegrationEvents.IIntegrationEvent))
            .Should()
            .HaveNameEndingWith("IntegrationEvent")
            .Check(Architecture);
    }

    [Fact]
    public void Commands_ShouldBeNamedAfter_FeatureNamespace()
    {
        Type[] commandTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))];

        foreach (Type type in commandTypes)
        {
            string ns = type.Namespace!;
            string featureName = ns[(ns.LastIndexOf('.') + 1)..];
            string expectedName = $"{featureName}Command";
            type.Name.ShouldBe(expectedName,
                $"Command in namespace '{ns}' should be named '{expectedName}'.");
        }
    }

    [Fact]
    public void CommandHandlers_ShouldBeNamedAfter_FeatureNamespace()
    {
        Type[] handlerTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("CommandHandler", StringComparison.Ordinal))];

        foreach (Type type in handlerTypes)
        {
            string ns = type.Namespace!;
            string featureName = ns[(ns.LastIndexOf('.') + 1)..];
            string expectedName = $"{featureName}CommandHandler";
            type.Name.ShouldBe(expectedName,
                $"CommandHandler in namespace '{ns}' should be named '{expectedName}'.");
        }
    }

    [Fact]
    public void Validators_ShouldBeNamedAfter_FeatureNamespaceAndValidatedMessage()
    {
        static bool IsValidator(Type t) =>
            t is { IsClass: true, IsAbstract: false }
            && t.BaseType is { IsGenericType: true } baseType
            && baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>);

        Type[] validatorTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(IsValidator)];

        validatorTypes.ShouldNotBeEmpty();

        foreach (Type type in validatorTypes)
        {
            Type validatedType = type.BaseType!.GetGenericArguments()[0];
            string ns = type.Namespace!;
            string featureName = ns[(ns.LastIndexOf('.') + 1)..];
            string suffix = validatedType.Name switch
            {
                var n when n.EndsWith("Command", StringComparison.Ordinal) => "CommandValidator",
                var n when n.EndsWith("Query", StringComparison.Ordinal) => "QueryValidator",
                _ => throw new UnreachableException(
                    $"'{type.Name}' validates '{validatedType.Name}', which is neither Command nor " +
                    "Query. Extend the naming convention (and this test) or fix the validated type."),
            };
            string expectedName = $"{featureName}{suffix}";
            type.Name.ShouldBe(expectedName,
                $"Validator in namespace '{ns}' should be named '{expectedName}'.");
        }
    }

    [Fact]
    public void Endpoints_ShouldBeNamedAfter_FeatureNamespace()
    {
        Type[] endpointTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })];

        foreach (Type type in endpointTypes)
        {
            string ns = type.Namespace!;
            string featureName = ns[(ns.LastIndexOf('.') + 1)..];
            string expectedName = $"{featureName}Endpoint";
            type.Name.ShouldBe(expectedName,
                $"Endpoint in namespace '{ns}' should be named '{expectedName}'.");
        }
    }
}
