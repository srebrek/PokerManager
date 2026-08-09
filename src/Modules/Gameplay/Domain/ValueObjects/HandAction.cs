using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal sealed class HandAction
{
    public int SequenceNumber { get; }
    public ParticipantId ParticipantId { get; }
    public HandActionType Type { get; }
    public ChipsStack? AmountTo { get; }

    private HandAction(int sequenceNumber, ParticipantId participantId, HandActionType type, ChipsStack? amountTo)
    {
        SequenceNumber = sequenceNumber;
        ParticipantId = participantId;
        Type = type;
        AmountTo = amountTo;
    }

    public static Result<HandAction> Create(
        int sequenceNumber,
        ParticipantId participantId,
        HandActionType type,
        ChipsStack? amountTo)
    {
        if ((type is HandActionType.Bet or HandActionType.Raise) && !(amountTo is { Value: > 0 }))
        {
            return Result.Failure<HandAction>(EmptyStackRaiseOrBet);
        }

        if ((type is HandActionType.Check or HandActionType.Call or HandActionType.Fold) && amountTo is not null)
        {
            return Result.Failure<HandAction>(NotEmptyStackCheckCallOrFold);
        }

        return new HandAction(sequenceNumber, participantId, type, amountTo);
    }

    public static readonly Error EmptyStackRaiseOrBet =
        Error.Conflict("Gameplay.HandAction.EmptyStackRaiseOrBet", "Cannot raise or bet with empty stack.");

    public static readonly Error NotEmptyStackCheckCallOrFold =
        Error.Conflict(
            "Gameplay.HandAction.NotEmptyStackCheckCallOrFold",
            "Cannot check, call or fold without empty stack.");
}
