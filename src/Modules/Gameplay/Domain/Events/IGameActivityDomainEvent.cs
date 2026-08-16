using Shared.Domain;

namespace Gameplay.Domain.Events;

internal interface IGameActivityDomainEvent : IDomainEvent
{
    Guid GameId { get; }
}
