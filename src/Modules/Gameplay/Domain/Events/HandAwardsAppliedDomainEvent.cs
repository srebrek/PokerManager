namespace Gameplay.Domain.Events;

public sealed record HandAwardsAppliedDomainEvent(Guid GameId, Guid HandId) : IGameActivityDomainEvent;
