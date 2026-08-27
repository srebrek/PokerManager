namespace Contracts.IntegrationEvents.Gameplay;

public sealed record HandAbortedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid HandId) : IIntegrationEvent;
