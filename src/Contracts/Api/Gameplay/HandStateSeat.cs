namespace Contracts.Api.Gameplay;

public sealed record HandStateSeat(
    Guid ParticipantId,
    int RemainingStack,
    int Contribution,
    string State);
