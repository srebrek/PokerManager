namespace Contracts.Api.Gameplay;

public sealed record GameStateResponse(
    Guid GameId,
    string JoinCode,
    bool IsFinished,
    int SmallBlind,
    int BigBlind,
    IReadOnlyList<GameStateParticipant> Participants,
    Guid? CurrentHandId);
