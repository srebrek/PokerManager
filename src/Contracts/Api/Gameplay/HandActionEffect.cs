namespace Contracts.Api.Gameplay;

public sealed record HandActionEffect(
    int ActionNumber,
    IReadOnlyList<HandPotState> Pots,
    Street? NewStreet,
    HandActionSeatEffect Seat);

public sealed record HandActionSeatEffect(Guid ParticipantId, int ChipsDelta, SeatState? NewState);
