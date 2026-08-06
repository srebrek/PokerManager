namespace Contracts.Api.Gameplay;

// TODO: remove seat index as the GameStateResponse list is already ordered
public sealed record GameStateParticipant(Guid Id, string Name, int Chips, int SeatIndex, bool IsHost);
