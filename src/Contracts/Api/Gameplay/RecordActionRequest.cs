namespace Contracts.Api.Gameplay;

public sealed record RecordActionRequest(Guid ActingParticipantId, HandActionType Type, int? AmountTo = null);

public enum HandActionType
{
    Check,
    Call,
    Bet,
    Raise,
    Fold,
}
