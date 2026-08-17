namespace Contracts.Api.Gameplay;

public sealed record GetHandStateResponse(
    Guid HandId,
    HandStatus Status,
    Street Street,
    int Pot,
    IReadOnlyList<HandStateSeat> Seats,
    int LastActionNumber);

public enum HandStatus
{
    InProgress,
    Finished,
    Aborted,
}

public enum Street
{
    PreFlop,
    Flop,
    Turn,
    River,
    Finished,
}

public sealed record HandStateSeat(
    Guid ParticipantId,
    int StreetContribution,
    int RemainingStack,
    SeatState State);

public enum SeatState
{
    Active,
    Folded,
}
