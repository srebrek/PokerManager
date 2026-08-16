namespace Gameplay.Domain.Events;

public sealed record HandDetachedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
