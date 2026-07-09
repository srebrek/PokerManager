namespace Shared.Domain;

public abstract class Entity<TId>(TId id)
    where TId : IStronglyTypedId<TId>
{
    public TId Id { get; } = id;
}
