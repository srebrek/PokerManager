using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Services;

internal static class HandStateCalculator
{
    public static Result<HandState> Calculate(IReadOnlyList<HandSeat> seats, IReadOnlyList<HandAction> actions)
    {
        List<HandSeatState> seatStates = [.. seats
            .OrderBy(hs => hs.Position)
            .Select(hs => new HandSeatState(
                hs.ParticipantId,
                ChipsStack.Zero,
                ChipsStack.Zero,
                hs.StartingStack,
                false,
                SeatState.Active))];

        ChipsStack currentBet = ChipsStack.Zero;
        int actingSeatIndex = 0;
        int seatCount = seats.Count;
        Street currentStreet = Street.PreFlop;

        ParticipantId lastActionParticipantId = default;
        int lastActionParticipantChipsDelta = default;
        SeatState? lastActionNewSeatState = default;
        Street? lastActionNewStreet = default;

        foreach ((int index, HandAction action) in actions.Index())
        {
            bool isLastAction = index == actions.Count - 1;
            if (currentStreet is Street.Finished)
            {
                return AdjustErrorType(HandFinished, isLastAction);
            }

            // skip not active
            while (seatStates[actingSeatIndex].State is not SeatState.Active)
            {
                actingSeatIndex = (actingSeatIndex + 1) % seatCount;
            }

            HandSeatState actor = seatStates[actingSeatIndex];
            int toCall = currentBet - actor.StreetContribution;
            int remaining = actor.RemainingStack;

            // rules
            if (action.SequenceNumber != index)
            {
                return AdjustErrorType(SequenceNumberMismatch, isLastAction);
            }

            if (action.ParticipantId != actor.ParticipantId)
            {
                return AdjustErrorType(NotYourTurn, isLastAction);
            }

            if (action.Type is HandActionType.Check or HandActionType.Bet && currentBet != 0)
            {
                return AdjustErrorType(CheckOrBetWhenBetIsNotZero, isLastAction);
            }

            if (action.Type is HandActionType.Call && currentBet == 0)
            {
                return AdjustErrorType(ZeroBetCall, isLastAction);
            }

            if (action.Type is HandActionType.Call && toCall > remaining)
            {
                return AdjustErrorType(CallExceedsStack, isLastAction);
            }

            if (action.Type is HandActionType.Call && toCall == remaining)
            {
                return AdjustErrorType(CallForEntireStack, isLastAction);
            }

            if (action.Type is HandActionType.Bet or HandActionType.Raise)
            {
                int pushed = action.AmountTo!.Value - actor.StreetContribution;

                if (pushed > remaining)
                {
                    return AdjustErrorType(InsufficientStack, isLastAction);
                }

                if (action.Type is HandActionType.Raise && action.AmountTo <= currentBet)
                {
                    return AdjustErrorType(RaiseBellowCurrentBet, isLastAction);
                }

                if (pushed == remaining)
                {
                    return AdjustErrorType(BetOrRaiseForEntireStack, isLastAction);
                }
            }

            // act
            bool isBlindPost = action.Type is HandActionType.PostSmallBlind or HandActionType.PostBigBlind;
            ChipsStack betBeforeAction = currentBet;

            lastActionParticipantId = action.ParticipantId;
            lastActionParticipantChipsDelta = 0;
            lastActionNewSeatState = null;
            lastActionNewStreet = null;

            if (action.Type is HandActionType.Fold)
            {
                seatStates[actingSeatIndex] = actor with { State = SeatState.Folded, HasActedThisStreet = true };
                lastActionNewSeatState = SeatState.Folded;
            }
            else
            {
                int chipsToPush = action.Type switch
                {
                    HandActionType.Check => 0,
                    HandActionType.Call => toCall,
                    HandActionType.AllIn => remaining,
                    _ => action.AmountTo!.Value - actor.StreetContribution,
                };

                currentBet = (ChipsStack)Math.Max(currentBet, actor.StreetContribution + chipsToPush);

                SeatState newState = action.Type is HandActionType.AllIn ? SeatState.AllIn : SeatState.Active;
                seatStates[actingSeatIndex] = actor with
                {
                    StreetContribution = (ChipsStack)(actor.StreetContribution + chipsToPush),
                    TotalContribution = (ChipsStack)(actor.TotalContribution + chipsToPush),
                    RemainingStack = (ChipsStack)(remaining - chipsToPush),
                    HasActedThisStreet = !isBlindPost,
                    State = newState,
                };

                lastActionParticipantChipsDelta = -chipsToPush;
                if (newState != actor.State)
                {
                    lastActionNewSeatState = newState;
                }
            }

            if (!isBlindPost && currentBet > betBeforeAction)
            {
                for (int i = 0; i < seatStates.Count; i++)
                {
                    seatStates[i] = seatStates[i] with { HasActedThisStreet = i == actingSeatIndex };
                }
            }

            // advance
            bool everyoneElseFolded = seatStates.Count(ss => ss.State is not SeatState.Folded) is 1;
            bool roundClosed = seatStates.All(ss => ss.State is not SeatState.Active
                || (ss.HasActedThisStreet && ss.StreetContribution == currentBet));

            if (!everyoneElseFolded && !roundClosed)
            {
                actingSeatIndex = (actingSeatIndex + 1) % seatCount;
                continue;
            }

            currentStreet = everyoneElseFolded
                || currentStreet is Street.River
                || seatStates.Count(ss => ss.State is SeatState.Active) <= 1
                    ? Street.Finished
                    : currentStreet + 1;

            currentBet = ChipsStack.Zero;
            actingSeatIndex = 0;
            for (int i = 0; i < seatStates.Count; i++)
            {
                seatStates[i] = seatStates[i] with
                {
                    StreetContribution = ChipsStack.Zero,
                    HasActedThisStreet = false,
                };
            }

            lastActionNewStreet = currentStreet;
        }

        return new HandState(
            PotCalculator.Layer(seatStates),
            currentStreet,
            seatStates,
            new(
                new(lastActionParticipantId, lastActionParticipantChipsDelta, lastActionNewSeatState),
                lastActionNewStreet));
    }

    private static Result<HandState> AdjustErrorType(Error error, bool isLastAction)
    {
        return isLastAction
            ? Error.Conflict(error.Code, error.Description)
            : error;
    }

    public static readonly Error SequenceNumberMismatch =
        Error.Failure("Gameplay.HandStateCalculator.SequenceNumberMismatch", "Action sequence number mismatch.");

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

    public static readonly Error RaiseBellowCurrentBet =
        Error.Failure("Gameplay.HandStateCalculator.RaiseBellowCurrentBet", "Raise cannot be under current bet.");

    public static readonly Error ZeroBetCall =
        Error.Failure("Gameplay.HandStateCalculator.ZeroBetCall", "Cannot call when the current bet is zero");

    public static readonly Error CallExceedsStack =
        Error.Failure(
            "Gameplay.HandStateCalculator.CallExceedsStack",
            "Cannot call for more than the remaining stack. Go all-in instead.");

    public static readonly Error CallForEntireStack =
        Error.Failure(
            "Gameplay.HandStateCalculator.CallForEntireStack",
            "Cannot call for the entire remaining stack. Go all-in instead.");

    public static readonly Error BetOrRaiseForEntireStack =
        Error.Failure(
            "Gameplay.HandStateCalculator.BetOrRaiseForEntireStack",
            "Cannot bet or raise the entire remaining stack. Go all-in instead.");
}

internal readonly record struct HandState(
    IReadOnlyList<HandPot> Pots,
    Street Street,
    IReadOnlyList<HandSeatState> SeatStates,
    HandActionEffect LastActionEffect);

internal readonly record struct HandSeatState(
    ParticipantId ParticipantId,
    ChipsStack StreetContribution,
    ChipsStack TotalContribution,
    ChipsStack RemainingStack,
    bool HasActedThisStreet,
    SeatState State);

internal readonly record struct HandActionEffect(HandSeatEffect SeatEffect, Street? NewStreet);

internal readonly record struct HandSeatEffect(ParticipantId ParticipantId, int ChipsDelta, SeatState? NewState);
