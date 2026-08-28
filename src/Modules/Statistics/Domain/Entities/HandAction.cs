using Statistics.Domain.ValueObjects;

namespace Statistics.Domain.Entities;

internal sealed class HandAction(
    Guid eventId,
    int sequenceNumber,
    Guid participantId,
    HandActionType type,
    int? amountTo,
    DateTimeOffset occurredAt)
{
    public Guid EventId { get; private init; } = eventId;
    public int SequenceNumber { get; private init; } = sequenceNumber;
    public Guid ParticipantId { get; private init; } = participantId;
    public HandActionType Type { get; private init; } = type;
    public int? AmountTo { get; private init; } = amountTo;
    public DateTimeOffset OccurredAt { get; private init; } = occurredAt;
    public DateTimeOffset? UndoneAt { get; private set; }

    public void MarkUndone(DateTimeOffset undoneAt) => UndoneAt = undoneAt;
}
