namespace Shared.Domain;

public abstract class Entity
{
    private readonly List<object> _events = [];

    public IReadOnlyList<object> Events => _events.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);
}
