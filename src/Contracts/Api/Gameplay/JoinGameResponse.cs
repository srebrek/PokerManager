namespace Contracts.Api.Gameplay;

public sealed record JoinGameResponse(Guid GameId, Guid ParticipantId);
