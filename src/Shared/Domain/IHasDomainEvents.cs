namespace Shared.Domain;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> Events { get; }

    void ClearDomainEvents();
}
