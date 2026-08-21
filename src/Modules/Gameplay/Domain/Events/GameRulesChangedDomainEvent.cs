namespace Gameplay.Domain.Events;

public sealed record GameRulesChangedDomainEvent(Guid GameId) : IGameActivityDomainEvent;
