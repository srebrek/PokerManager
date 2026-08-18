namespace Gameplay.Domain.Events;

public sealed record RebuyRecordedDomainEvent(Guid GameId, Guid ParticipantId) : IGameActivityDomainEvent;
