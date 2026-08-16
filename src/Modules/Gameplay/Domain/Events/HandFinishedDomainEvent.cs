namespace Gameplay.Domain.Events;

public sealed record HandFinishedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
