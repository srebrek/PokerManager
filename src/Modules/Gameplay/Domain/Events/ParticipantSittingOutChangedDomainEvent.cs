namespace Gameplay.Domain.Events;

public sealed record ParticipantSittingOutChangedDomainEvent(Guid GameId, Guid ParticipantId)
    : IGameActivityDomainEvent;
