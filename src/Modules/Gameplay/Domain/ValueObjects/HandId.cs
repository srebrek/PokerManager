using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct HandId(Guid Value) : IStronglyTypedId<HandId>
{
    public static HandId New() => new(Guid.NewGuid());
    public static HandId From(Guid value) => new(value);
}
