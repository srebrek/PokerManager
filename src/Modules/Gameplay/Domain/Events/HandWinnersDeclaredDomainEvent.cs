using Shared.Domain;

namespace Gameplay.Domain.Events;

public sealed record HandWinnersDeclaredDomainEvent(Guid HandId) : IDomainEvent;
