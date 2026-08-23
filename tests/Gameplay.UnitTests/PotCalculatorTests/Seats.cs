using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;

namespace Gameplay.UnitTests.PotCalculatorTests;

internal static class Seats
{
    public static HandSeatState With(int contribution, SeatState state = SeatState.Active) => new(
        ParticipantId.New(),
        ChipsStack.Zero,
        (ChipsStack)contribution,
        ChipsStack.Zero,
        false,
        state);
}
