namespace Contracts.Api.Gameplay;

public sealed record MoveDealerButtonRequest(Guid ActingParticipantId, Guid DealerParticipantId);
