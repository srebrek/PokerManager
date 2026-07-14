namespace Gameplay.Domain.ValueObjects;

internal sealed record HandSeat(ParticipantId ParticipantId, ChipsStack StartingStack, int Position);
