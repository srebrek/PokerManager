namespace Contracts.Api.Gameplay;

public sealed record HandActionEffect(
    int ActionNumber,
    int PotDelta,
    Street? NewStreet,
    HandActionSeatEffect Seat);

public sealed record HandActionSeatEffect(Guid ParticipantId, int ChipsDelta, SeatState? NewState);
