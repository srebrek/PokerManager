using Shared.Domain;

namespace Gameplay.Domain.Events;

public sealed record HandActionUndoneDomainEvent(Guid HandId) : IDomainEvent;
