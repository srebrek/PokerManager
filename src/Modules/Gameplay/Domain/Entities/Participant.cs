using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Participant : Entity<ParticipantId>
{
    public string Name { get; private set; }
    public ChipsStack Chips { get; private set; }

    private Participant(ParticipantId id, string name, ChipsStack chips)
        : base(id)
    {
        Name = name;
        Chips = chips;
    }

    public static Result<Participant> Create(string name, ChipsStack chips)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Participant>(ParticipantErrors.InvalidName);
        }

        return new Participant(ParticipantId.New(), name, chips);
    }
}
