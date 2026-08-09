using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Services;

internal static class HandStateCalculator
{
    public static Result<HandState> Calculate(IReadOnlyList<HandSeat> seats, IReadOnlyList<HandAction> actions)
    {
        const int BlindNumber = 2;

        List<HandSeatState> seatStates = [.. seats
                .OrderBy(hs => hs.Position)
                .Select(hs => new HandSeatState(hs.ParticipantId, 0, hs.StartingStack, ValueObjects.SeatState.Active))];

        ChipsStack pot = 0;

        ChipsStack currentBet = 0;
        int actingParticipantIndex = 0;
        int participantCount = seats.Count;
        int newStreetCounter = participantCount + BlindNumber;
        Street currentStreet = Street.PreFlop;
        int foldCounter = 0;

        foreach ((int index, HandAction action) in actions.Index())
        {
            bool isLastAction = index == actions.Count - 1;
            if (currentStreet is Street.Finished)
            {
                return AdjustErrorType(HandFinished, isLastAction);
            }

            // skip folded participants
            while (seatStates[actingParticipantIndex].State is SeatState.Folded)
            {
                actingParticipantIndex = (actingParticipantIndex + 1) % participantCount;
            }

            // rules
            if (action.SequenceNumber != index)
            {
                return AdjustErrorType(SequenceNumberMismatch, isLastAction);
            }

            if (action.ParticipantId != seatStates[actingParticipantIndex].ParticipantId)
            {
                return AdjustErrorType(NotYourTurn, isLastAction);
            }

            if (seatStates[actingParticipantIndex].State is not ValueObjects.SeatState.Active)
            {
                return AdjustErrorType(InactiveSeatActs, isLastAction);
            }

            if (action.Type is HandActionType.Check or HandActionType.Bet && currentBet != 0)
            {
                return AdjustErrorType(CheckOrBetWhenBetIsNotZero, isLastAction);
            }

            if (action.Type is HandActionType.Call or HandActionType.Bet or HandActionType.Raise)
            {
                ChipsStack chipsToPush = action.AmountTo is not null
                    ? action.AmountTo.Value - seatStates[actingParticipantIndex].StreetContribution
                    : currentBet - seatStates[actingParticipantIndex].StreetContribution;

                if (chipsToPush > seatStates[actingParticipantIndex].RemainingStack)
                {
                    return AdjustErrorType(InsufficientStack, isLastAction);
                }
            }

            // act

            if (action.Type is HandActionType.Fold)
            {
                seatStates[actingParticipantIndex] =
                    seatStates[actingParticipantIndex] with { State = SeatState.Folded };
                foldCounter++;
            }

            if (action.AmountTo is not null || action.Type is HandActionType.Call)
            {
                ChipsStack chipsToPush;
                if (action.AmountTo is not null)
                {
                    chipsToPush = action.AmountTo.Value - seatStates[actingParticipantIndex].StreetContribution;
                    currentBet = action.AmountTo.Value;
                }
                else
                {
                    chipsToPush = currentBet - seatStates[actingParticipantIndex].StreetContribution;
                }

                HandSeatState current = seatStates[actingParticipantIndex];
                seatStates[actingParticipantIndex] = seatStates[actingParticipantIndex] with
                {
                    StreetContribution = current.StreetContribution + chipsToPush,
                    RemainingStack = current.RemainingStack - chipsToPush,
                };

                pot += chipsToPush;
            }

            // advance
            if (action.Type is HandActionType.Bet or HandActionType.Raise)
            {
                newStreetCounter = participantCount - foldCounter;
            }

            if (--newStreetCounter is 0)
            {
                for (int i = 0; i < seatStates.Count; i++)
                {
                    seatStates[i] = seatStates[i] with { StreetContribution = 0 };
                }

                currentStreet++;
                currentBet = 0;
                actingParticipantIndex = 0;
                newStreetCounter = participantCount - foldCounter;

                // last participant
                if (newStreetCounter is 1)
                {
                    currentStreet = Street.Finished;
                }
            }

            actingParticipantIndex = (actingParticipantIndex + 1) % participantCount;
        }

        return new HandState(pot, currentStreet, seatStates);
    }

    private static Result<HandState> AdjustErrorType(Error error, bool isLastAction)
    {
        return isLastAction
            ? Result.Failure<HandState>(Error.Conflict(error.Code, error.Description))
            : Result.Failure<HandState>(error);
    }

    public static readonly Error SequenceNumberMismatch =
        Error.Failure("Gameplay.HandStateCalculator.SequenceNumberMismatch", "Action sequence number mismatch.");

    public static readonly Error InactiveSeatActs =
        Error.Failure("Gameplay.HandStateCalculator.InactiveSeatActs", "Inactive participant cannot act.");

    public static readonly Error CheckOrBetWhenBetIsNotZero =
        Error.Failure(
            "Gameplay.HandStateCalculator.CheckOrBetWhenBetIsNotZero",
            "Cannot check or bet when the active bet is not zero.");

    public static readonly Error InsufficientStack =
        Error.Failure("Gameplay.HandStateCalculator.InsufficientStack", "Insufficient stack.");

    public static readonly Error NotYourTurn =
        Error.Failure("Gameplay.HandStateCalculator.NotYourTurn", "Not your turn.");

    public static readonly Error HandFinished =
        Error.Failure("Gameplay.HandStateCalculator.HandFinished", "Hand is already finished.");
}

internal readonly record struct HandState(
    ChipsStack Pot,
    Street Street,
    IReadOnlyList<HandSeatState> SeatStates);

internal readonly record struct HandSeatState(
    ParticipantId ParticipantId,
    ChipsStack StreetContribution,
    ChipsStack RemainingStack,
    SeatState State);
