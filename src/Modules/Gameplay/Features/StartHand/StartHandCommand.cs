namespace Gameplay.Features.StartHand;

internal sealed record StartHandCommand(Guid GameId, Guid ActingParticipantId);
