namespace Gameplay.Domain.Events;

public sealed record HandAbortedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
