using Shared.Domain;

namespace Gameplay.Domain.Events;

public sealed record HandActionRecordedDomainEvent(
    Guid HandId,
    int ActionNumber,
    int PotDelta,
    int? Street,
    HandActionSeatEffect Seat) : IDomainEvent;

public sealed record HandActionSeatEffect(Guid ParticipantId, int ChipsDelta, int? NewState);
