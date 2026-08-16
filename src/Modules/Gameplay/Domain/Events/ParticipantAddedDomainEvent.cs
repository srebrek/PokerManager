namespace Gameplay.Domain.Events;

public sealed record ParticipantAddedDomainEvent(Guid GameId, Guid ParticipantId) : IGameActivityDomainEvent;
