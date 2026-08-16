namespace Gameplay.Domain.Events;

public sealed record HandActionRecordedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
