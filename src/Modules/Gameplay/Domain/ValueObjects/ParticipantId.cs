using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct ParticipantId(Guid Value) : IStronglyTypedId<ParticipantId>
{
    public static ParticipantId New() => new(Guid.NewGuid());

    public static ParticipantId From(Guid value) => new(value);
}
