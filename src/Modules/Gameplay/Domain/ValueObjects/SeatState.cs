using Api = Contracts.Api.Gameplay;

namespace Gameplay.Domain.ValueObjects;

internal enum SeatState
{
    Active,
    Folded,
}

internal static class SeatStateMappings
{
    public static Api.SeatState ToContract(this SeatState state) => state switch
    {
        SeatState.Active => Api.SeatState.Active,
        SeatState.Folded => Api.SeatState.Folded,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
