namespace Gameplay.Domain.Events;

public sealed record HandAttachedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
