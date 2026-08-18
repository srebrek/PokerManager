using Contracts.Api.Gameplay;
using Shared.Domain;

namespace Gameplay.Domain.Events;

public sealed record HandActionRecordedDomainEvent(
    Guid HandId,
    int ActionNumber,
    int PotDelta,
    Street? NewStreet,
    HandActionSeatEffect Seat) : IDomainEvent;

public sealed record HandActionSeatEffect(Guid ParticipantId, int ChipsDelta, SeatState? NewState);
