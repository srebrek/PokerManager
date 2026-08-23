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
            return EmptyStackRaiseOrBet;
        }

        if ((type is HandActionType.Check
                or HandActionType.Call
                or HandActionType.Fold
                or HandActionType.AllIn)
            && amountTo is not null)
        {
            return AmountNotAllowed;
        }

        return new HandAction(sequenceNumber, participantId, type, amountTo);
    }

    public static readonly Error EmptyStackRaiseOrBet =
        Error.Conflict("Gameplay.HandAction.EmptyStackRaiseOrBet", "Cannot raise or bet with empty stack.");

    public static readonly Error AmountNotAllowed =
        Error.Conflict(
            "Gameplay.HandAction.AmountNotAllowed",
            "Only bet and raise carry an amount.");
}
