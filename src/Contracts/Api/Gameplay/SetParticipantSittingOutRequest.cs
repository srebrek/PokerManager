namespace Contracts.Api.Gameplay;

public sealed record SetParticipantSittingOutRequest(Guid ActingParticipantId, bool IsSittingOut);
