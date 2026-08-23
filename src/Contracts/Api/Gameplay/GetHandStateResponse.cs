namespace Contracts.Api.Gameplay;

public sealed record GetHandStateResponse(
    Guid HandId,
    HandStatus Status,
    Street Street,
    IReadOnlyList<HandPotState> Pots,
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

public sealed record HandPotState(
    int Index,
    int Amount,
    IReadOnlyList<Guid> EligibleParticipantIds,
    IReadOnlyList<Guid> WinnerParticipantIds);

public sealed record HandStateSeat(
    Guid ParticipantId,
    int StreetContribution,
    int RemainingStack,
    SeatState State);

public enum SeatState
{
    Active,
    Folded,
    AllIn,
}
