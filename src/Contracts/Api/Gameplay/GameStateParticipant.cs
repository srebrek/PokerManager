namespace Contracts.Api.Gameplay;

public sealed record GameStateParticipant(Guid Id, string Name, int Chips, int SeatIndex, bool IsHost);
