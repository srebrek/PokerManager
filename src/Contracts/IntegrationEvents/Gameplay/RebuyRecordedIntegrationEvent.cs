namespace Contracts.IntegrationEvents.Gameplay;

public sealed record RebuyRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid GameId,
    Guid ParticipantId,
    int Amount) : IIntegrationEvent;
