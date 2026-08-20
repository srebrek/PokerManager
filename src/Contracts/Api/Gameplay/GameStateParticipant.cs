namespace Contracts.Api.Gameplay;

public sealed record GameStateParticipant(
    Guid Id,
    string Name,
    int Chips,
    bool IsHost,
    bool IsSittingOut,
    bool IsDealer);
