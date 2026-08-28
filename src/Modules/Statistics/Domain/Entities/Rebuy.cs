namespace Statistics.Domain.Entities;

internal sealed class Rebuy(Guid eventId, Guid gameId, Guid participantId, int amount, DateTimeOffset occurredAt)
{
    public Guid EventId { get; private init; } = eventId;
    public Guid GameId { get; private init; } = gameId;
    public Guid ParticipantId { get; private init; } = participantId;
    public int Amount { get; private init; } = amount;
    public DateTimeOffset OccurredAt { get; private init; } = occurredAt;
}
