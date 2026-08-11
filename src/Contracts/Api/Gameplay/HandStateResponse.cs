namespace Contracts.Api.Gameplay;

public sealed record GetHandStateResponse(
    Guid HandId,
    Guid GameId,
    HandStatus Status,
    Street Street,
    int Pot,
    IReadOnlyList<HandStateSeat> Seats);

public enum HandStatus
{
    InProgress,
    Finished,
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
