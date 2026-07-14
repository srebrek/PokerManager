using Gameplay.Domain.ValueObjects;

namespace Gameplay.Domain.Services;

internal static class HandStateCalculator
{
    public static HandState Calculate(IReadOnlyList<HandSeat> seats, IReadOnlyList<HandAction> actions)
    {
        Dictionary<ParticipantId, SeatState> seatStates = seats
            .ToDictionary(s => s.ParticipantId, _ => SeatState.Active);

        Dictionary<ParticipantId, ChipsStack> contributions = seats
            .ToDictionary(s => s.ParticipantId, _ => ChipsStack.Create(0).Value);

        foreach (HandAction action in actions)
        {
            seatStates[action.ParticipantId] = action.Type switch
            {
                HandActionType.AllIn => SeatState.AllIn,
                HandActionType.Fold => SeatState.Folded,
                _ => SeatState.Active,
            };

            contributions[action.ParticipantId] = contributions[action.ParticipantId].Add(action.Amount?.Value ?? 0);
        }

        ChipsStack pot = ChipsStack.Create(contributions.Values.Sum(c => c.Value)).Value;

        Dictionary<ParticipantId, ChipsStack> remainingStacks = seats
            .ToDictionary(
                s => s.ParticipantId,
                s => s.StartingStack.Subtract(contributions[s.ParticipantId].Value).Value);

        return new(pot, seatStates, contributions, remainingStacks);
    }
}

internal readonly record struct HandState(
    ChipsStack Pot,
    IReadOnlyDictionary<ParticipantId, SeatState> SeatStates,
    IReadOnlyDictionary<ParticipantId, ChipsStack> Contributions,
    IReadOnlyDictionary<ParticipantId, ChipsStack> RemainingStacks);
