namespace Gameplay.Features.AddParticipant;

internal sealed record AddParticipantCommand(Guid GameId, string Name);
