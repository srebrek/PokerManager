namespace Contracts.IntegrationEvents.Gameplay;

public sealed record HandActionUndoneIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid HandId,
    int SequenceNumber) : IIntegrationEvent;
