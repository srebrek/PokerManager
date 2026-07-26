using ArchUnitNET.xUnitV3;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class EventContractTests : BaseArchitectureTest
{
    [Fact]
    public void IntegrationEvent_ResidesInContractsIntegrationEventsNamespace()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(Contracts.IntegrationEvents.IIntegrationEvent))
            .Should()
            .ResideInNamespaceMatching(@"^Contracts\.IntegrationEvents")
            .Check(Architecture);
    }

    [Fact]
    public void Event_DoesNotImplementBothDomainAndIntegrationEvent()
    {
        IEnumerable<Type> offenders = ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Shared.Domain.IDomainEvent).IsAssignableFrom(t)
                && typeof(Contracts.IntegrationEvents.IIntegrationEvent).IsAssignableFrom(t));

        offenders.ShouldBeEmpty(
            "a type must be either a domain event or an integration event, never both.");
    }
}
