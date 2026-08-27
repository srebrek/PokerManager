namespace Contracts.IntegrationEvents.Gameplay;

public sealed record HandStartedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid HandId,
    Guid GameId,
    IReadOnlyList<HandStartedSeat> Seats,
    IReadOnlyList<HandStartedBlind> Blinds) : IIntegrationEvent;

public sealed record HandStartedSeat(Guid ParticipantId, string Name, int Position, int StartingChips);

public sealed record HandStartedBlind(int SequenceNumber, Guid ParticipantId, HandActionType Type, int AmountTo);
