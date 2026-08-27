namespace Contracts.IntegrationEvents.Gameplay;

public sealed record HandActionRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid HandId,
    int SequenceNumber,
    Guid ParticipantId,
    HandActionType Type,
    int? AmountTo) : IIntegrationEvent;
