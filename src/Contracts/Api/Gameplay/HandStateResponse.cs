namespace Contracts.Api.Gameplay;

public sealed record HandStateResponse(
    Guid HandId,
    Guid GameId,
    HandStatus Status,
    Street Street,
    int Pot,
    IReadOnlyList<HandStateSeat> Seats);

public enum HandStatus
{
    InProgress,
    Completed
}

public enum Street
{
    PreFlop,
    Flop,
    Turn,
    River
}

