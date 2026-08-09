namespace Contracts.Api.Gameplay;

public sealed record HandStateSeat(
    Guid ParticipantId,
    int RemainingStack,
    int Contribution,
    SeatState State);

public enum SeatState
{
    Active,
    Folded,
    AllIn
}

