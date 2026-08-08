namespace Contracts.Api.Gameplay;

public sealed record HandStateResponse(
    Guid HandId,
    Guid GameId,
    string Status,
    string Street,
    int Pot,
    IReadOnlyList<HandStateSeat> Seats);
