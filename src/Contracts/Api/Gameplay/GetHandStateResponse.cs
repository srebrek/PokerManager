using System.Text.Json.Serialization;

namespace Contracts.Api.Gameplay;

public sealed record GetHandStateResponse(
    Guid HandId,
    HandStatus Status,
    Street Street,
    IReadOnlyList<HandPotState> Pots,
    IReadOnlyList<HandStateSeat> Seats,
    int LastActionNumber);

[JsonConverter(typeof(JsonStringEnumConverter<HandStatus>))]
public enum HandStatus
{
    InProgress,
    Finished,
    Aborted,
}

[JsonConverter(typeof(JsonStringEnumConverter<Street>))]
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

[JsonConverter(typeof(JsonStringEnumConverter<SeatState>))]
public enum SeatState
{
    Active,
    Folded,
    AllIn,
}
