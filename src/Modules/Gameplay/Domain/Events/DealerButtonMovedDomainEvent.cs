namespace Gameplay.Domain.Events;

public sealed record DealerButtonMovedDomainEvent(Guid GameId, Guid ParticipantId) : IGameActivityDomainEvent;
