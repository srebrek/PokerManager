using ArchUnitNET.xUnitV3;
using FluentValidation;
using Shared.Presentation;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class ModifierTests : BaseArchitectureTest
{
    [Fact]
    public void Command_IsSealed()
    {
        Classes()
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .BeSealed()
            .Check(Architecture);
    }

    [Fact]
    public void Query_IsSealed()
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
    public void Command_IsRecord()
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
    public void Query_IsRecord()
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
    public void RequestAndResponse_AreRecords()
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
    public void Command_IsPublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void Query_IsPublic() // Wolverine requirement
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
    public void CommandHandler_IsPublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void QueryHandler_IsPublic() // Wolverine requirement
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
    public void Endpoint_IsInternal()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .BeInternal()
            .Check(Architecture);
    }

    [Fact]
    public void Validator_IsInternal()
    {
        Classes()
            .That()
            .AreAssignableTo(typeof(AbstractValidator<>))
            .Should()
            .BeInternal()
            .Check(Architecture);
    }

    [Fact]
    public void EventHandler_IsPublic() // Wolverine requirement
    {
        Classes()
            .That()
            .HaveNameEndingWith("EventHandler")
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void IntegrationEvent_IsPublic() // Wolverine requirement
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Contracts.IntegrationEvents.IIntegrationEvent))
            .Should()
            .BePublic()
            .Check(Architecture);
    }

    [Fact]
    public void DomainEvent_IsPublic() // Wolverine requirement
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Shared.Domain.IDomainEvent))
            .And()
            .DoNotImplementInterface(typeof(Contracts.IntegrationEvents.IIntegrationEvent))
            .Should()
            .BePublic()
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void DbContext_IsPublic() // Wolverine requirement
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
