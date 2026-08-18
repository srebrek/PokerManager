namespace Contracts.Api.Gameplay;

public sealed record RebuyRequest(Guid ActingParticipantId, int Amount);
