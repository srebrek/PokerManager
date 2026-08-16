namespace Gameplay.Domain.Events;

public sealed record HandStartedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
