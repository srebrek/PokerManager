using ArchUnitNET.xUnitV3;
using FluentValidation;
using Shared.Presentation;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class ModifierTests : BaseArchitectureTest
{
    [Fact]
    public void Commands_ShouldBeSealed()
    {
        Classes()
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .BeSealed()
            .Check(Architecture);
    }

    [Fact]
    public void Queries_ShouldBeSealed()
    {
        Classes()
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .BeSealed()
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Commands_ShouldBeRecords()
    {
        IEnumerable<Type> commandTypes = ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal));

        foreach (Type type in commandTypes)
        {
            type.GetMethod("<Clone>$")
                .ShouldNotBeNull($"'{type.Name}' is a Command but is not a record. " +
                    $"Commands should be immutable records.");
        }
    }

    [Fact]
    public void Queries_ShouldBeRecords()
    {
        IEnumerable<Type> queryTypes = ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Query", StringComparison.Ordinal));

        foreach (Type type in queryTypes)
        {
            type.GetMethod("<Clone>$")
                .ShouldNotBeNull($"'{type.Name}' is a Query but is not a record. " +
                    $"Queries should be immutable records.");
        }
    }

    [Fact]
    public void Contracts_RequestsAndResponses_ShouldBeRecords()
    {
        IEnumerable<Type> contractTypes = ContractsAssembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal)
                    || t.Name.EndsWith("Response", StringComparison.Ordinal));

        foreach (Type type in contractTypes)
        {
            type.GetMethod("<Clone>$")
                .ShouldNotBeNull($"Contract type '{type.Name}' should be a record to ensure immutability.");
        }
    }

    [Fact]
    public void Commands_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void Queries_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .BePublic()
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void CommandHandlers_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void QueryHandlers_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .BePublic()
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Endpoints_ShouldBeInternal()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .BeInternal()
            .Check(Architecture);
    }

    [Fact]
    public void Validators_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .AreAssignableTo(typeof(AbstractValidator<>))
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void EventHandlers_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("EventHandler")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void IntegrationEvents_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Shared.Domain.IIntegrationEvent))
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void DomainEvents_ShouldBePublic() // Wolverine requirement
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Shared.Domain.IDomainEvent))
            .And()
            .DoNotImplementInterface(typeof(Shared.Domain.IIntegrationEvent))
            .Should()
            .BePublic()
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void DbContexts_ShouldBePublic() // Wolverine requirement
    {
        Type[] dbContextTypes = [.. ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.IsSubclassOf(typeof(Microsoft.EntityFrameworkCore.DbContext)))];

        dbContextTypes.ShouldNotBeEmpty();
        foreach (Type type in dbContextTypes)
        {
            type.IsPublic.ShouldBeTrue(
                $"'{type.FullName}' is injected into handlers — Wolverine codegen needs it public.");
        }
    }
}
