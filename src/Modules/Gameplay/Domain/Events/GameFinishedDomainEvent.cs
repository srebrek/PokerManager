namespace Gameplay.Domain.Events;

public sealed record GameFinishedDomainEvent(Guid GameId) : IGameActivityDomainEvent;
