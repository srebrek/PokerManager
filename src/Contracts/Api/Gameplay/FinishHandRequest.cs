namespace Contracts.Api.Gameplay;

public sealed record FinishHandRequest(Guid ActingParticipantId, Guid WinnerParticipantId);
