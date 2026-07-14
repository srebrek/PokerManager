using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct GameId(Guid Value) : IStronglyTypedId<GameId>
{
    public static GameId New() => new(Guid.NewGuid());

    public static GameId From(Guid value) => new(value);
}
