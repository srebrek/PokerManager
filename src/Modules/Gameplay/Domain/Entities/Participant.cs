using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Participant : Entity<ParticipantId>
{
    public string Name { get; private set; }
    public ChipsStack Chips { get; private set; }
    public bool SittingOut { get; private set; }
    public UserId? UserId { get; }

    private Participant(ParticipantId id, string name, ChipsStack chips)
        : base(id)
    {
        Name = name;
        Chips = chips;
        SittingOut = false;
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
