namespace Contracts.Api.Gameplay;

public sealed record ChangeGameRulesRequest(Guid ActingParticipantId, int SmallBlind, int BigBlind);
