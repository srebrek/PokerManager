namespace Gameplay.Domain.ValueObjects;

internal sealed record HandAction(
    int SequenceNumber,
    ParticipantId ParticipantId,
    HandActionType Type,
    ChipsStack? Amount,
    Street Street);
