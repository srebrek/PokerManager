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
    public void Validators_ShouldHaveNameEndingWith_Validator()
    {
        Classes()
            .That()
            .AreAssignableTo(typeof(AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .Check(Architecture);
    }

    [Fact]
    public void Endpoints_ShouldHaveNameEndingWith_Endpoint()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .HaveNameEndingWith("Endpoint")
            .Check(Architecture);
    }

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
            .DoNotImplementInterface(typeof(Shared.Domain.IIntegrationEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .WithoutRequiringPositiveResults() // TODO: Remove when there is at least one domain event
            .Check(Architecture);
    }

    [Fact]
    public void IntegrationEvents_ShouldHaveNameEndingWith_IntegrationEvent()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Shared.Domain.IIntegrationEvent))
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
    public void CommandValidators_ShouldBeNamedAfter_FeatureNamespace()
    {
        Type[] validatorTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("CommandValidator", StringComparison.Ordinal))];

        foreach (Type type in validatorTypes)
        {
            string ns = type.Namespace!;
            string featureName = ns[(ns.LastIndexOf('.') + 1)..];
            string expectedName = $"{featureName}CommandValidator";
            type.Name.ShouldBe(expectedName,
                $"CommandValidator in namespace '{ns}' should be named '{expectedName}'.");
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
