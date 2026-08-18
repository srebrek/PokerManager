namespace Gameplay.Domain.Events;

public sealed record SeatingOrderChangedDomainEvent(Guid GameId, Guid ParticipantId) : IGameActivityDomainEvent;
