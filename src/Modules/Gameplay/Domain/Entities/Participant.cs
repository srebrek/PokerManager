using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Participant : Entity<ParticipantId>
{
    public string Name { get; private set; }
    public ChipsStack Chips { get; private set; }
    public ChipsStack TotalBuyIn { get; private set; }

    private Participant(ParticipantId id, string name, ChipsStack chips)
        : base(id)
    {
        Name = name;
        Chips = chips;
        TotalBuyIn = chips;
    }

    public static Result<Participant> Create(string name, ChipsStack chips)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ParticipantErrors.InvalidName;
        }

        return new Participant(ParticipantId.New(), name, chips);
    }

    public Result AddChips(int amount)
    {
        if (!ChipsStack.Create(Chips + amount).TryGetValue(out ChipsStack chips, out Error? error))
        {
            return error;
        }

        Chips = chips;
        return Result.Success();
    }

    public Result Rebuy(int amount)
    {
        if (!ChipsStack.Create(Chips + amount).TryGetValue(out ChipsStack chips, out Error? _))
        {
            return ParticipantErrors.RebuyLeavesNegativeChips;
        }

        if (!ChipsStack.Create(TotalBuyIn + amount).TryGetValue(out ChipsStack totalBuyIn, out Error? _))
        {
            return ParticipantErrors.RebuyLeavesNegativeTotalBuyIn;
        }

        Chips = chips;
        TotalBuyIn = totalBuyIn;
        return Result.Success();
    }
}
