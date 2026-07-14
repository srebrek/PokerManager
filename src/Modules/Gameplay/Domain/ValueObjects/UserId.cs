using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct UserId(Guid Value) : IStronglyTypedId<UserId>
{
    public static UserId From(Guid value) => new(value);
}
