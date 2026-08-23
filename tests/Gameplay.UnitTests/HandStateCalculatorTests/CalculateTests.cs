using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;
using static Gameplay.Domain.ValueObjects.HandActionType;

namespace Gameplay.UnitTests.HandStateCalculatorTests;

public sealed class CalculateTests
{
    private const int SmallBlind = 10;
    private const int BigBlind = 20;

    [Fact]
    public void Calculate_OnlyTheBlindsArePosted_LeavesTheBigBlindItsOption()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(seats);

        // Assert
        state.Street.ShouldBe(Street.PreFlop);
        state.SeatStates.Select(ss => ss.StreetContribution.Value).ShouldBe([10, 20, 0]);
        state.SeatStates.ShouldAllBe(ss => !ss.HasActedThisStreet);
    }

    [Fact]
    public void Calculate_OnlyTheBlindsArePosted_IncludesTheUnmatchedPartOfTheBigBlindInThePot()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(seats);

        // Assert
        HandPot pot = state.Pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(SmallBlind + BigBlind);
        pot.EligibleParticipantIds.ShouldBe(seats.Select(s => s.ParticipantId));
    }

    [Fact]
    public void Calculate_BigBlindHasNotActedYet_KeepsThePreFlopOpen()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(seats, Act(2, Call), Act(0, Call));

        // Assert
        state.Street.ShouldBe(Street.PreFlop);
    }

    [Fact]
    public void Calculate_BigBlindUsesItsOption_MovesToTheFlop()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(seats, Act(2, Call), Act(0, Call), Act(1, Call));

        // Assert
        state.Street.ShouldBe(Street.Flop);
        state.SeatStates.ShouldAllBe(ss => ss.StreetContribution.Value == 0);
        state.Pots.ShouldHaveSingleItem().Amount.Value.ShouldBe(60);
    }

    [Fact]
    public void Calculate_EverybodyChecksToTheRiver_FinishesTheHand()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(
            seats,
            Act(2, Call),
            Act(0, Call),
            Act(1, Call),
            Act(0, Check),
            Act(1, Check),
            Act(2, Check),
            Act(0, Check),
            Act(1, Check),
            Act(2, Check),
            Act(0, Check),
            Act(1, Check),
            Act(2, Check));

        // Assert
        state.Street.ShouldBe(Street.Finished);
        state.Pots.ShouldHaveSingleItem().Amount.Value.ShouldBe(60);
    }

    [Fact]
    public void Calculate_EverybodyElseFolds_FinishesTheHandImmediately()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        HandState state = Calculate(seats, Act(2, Fold), Act(0, Fold));

        // Assert
        state.Street.ShouldBe(Street.Finished);
        HandPot pot = state.Pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(SmallBlind + BigBlind);
        pot.EligibleParticipantIds.ShouldBe([seats[1].ParticipantId]);
    }

    [Fact]
    public void Calculate_CallForTheEntireRemainingStack_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(20, 1000, 1000);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(2, Call), Act(0, Call));

        // Assert
        ShouldFailWith(result, HandStateCalculator.CallForEntireStack);
    }

    [Fact]
    public void Calculate_ShortAllInDoesNotCoverTheCurrentBet_KeepsTheRoundClosingWithoutAnotherTurn()
    {
        // Arrange
        List<HandSeat> seats = Seats(50, 1000, 1000);

        // Act
        HandState state = Calculate(seats, Act(2, Raise, 100), Act(0, AllIn), Act(1, Call));

        // Assert
        state.Street.ShouldBe(Street.Flop);
        state.SeatStates[0].State.ShouldBe(SeatState.AllIn);
        state.Pots.Select(pot => pot.Amount.Value).ShouldBe([150, 100]);
        state.Pots[0].EligibleParticipantIds.ShouldBe(seats.Select(s => s.ParticipantId));
        state.Pots[1].EligibleParticipantIds.ShouldBe(seats.Skip(1).Select(s => s.ParticipantId));
    }

    [Fact]
    public void Calculate_AllInRaisesTheCurrentBet_ReopensTheRoundAndRunsTheHandOut()
    {
        // Arrange
        List<HandSeat> seats = Seats(200, 1000, 1000);

        // Act
        HandState state = Calculate(seats, Act(2, Raise, 60), Act(0, AllIn), Act(1, Fold), Act(2, Call));

        // Assert
        state.Street.ShouldBe(Street.Finished);
        HandPot pot = state.Pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(420);
        pot.EligibleParticipantIds.ShouldBe([seats[0].ParticipantId, seats[2].ParticipantId]);
    }

    [Fact]
    public void Calculate_AllInSeatIsNextInTheSeatingOrder_SkipsItsTurn()
    {
        // Arrange
        List<HandSeat> seats = Seats(50, 1000, 1000);

        // Act
        HandState state = Calculate(
            seats,
            Act(2, Call),
            Act(0, AllIn),
            Act(1, Call),
            Act(2, Call),
            Act(1, Check));

        // Assert
        state.Street.ShouldBe(Street.Flop);
        state.SeatStates[0].State.ShouldBe(SeatState.AllIn);
    }

    [Fact]
    public void Calculate_AllInSeatActsAgain_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(50, 1000, 1000);

        // Act
        Result<HandState> result = TryCalculate(
            seats,
            Act(2, Call),
            Act(0, AllIn),
            Act(1, Call),
            Act(2, Call),
            Act(0, Check));

        // Assert
        ShouldFailWith(result, HandStateCalculator.NotYourTurn);
    }

    [Fact]
    public void Calculate_AllInForExactlyTheAmountToCall_MarksItAllIn()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 20);

        // Act
        HandState state = Calculate(seats, Act(2, AllIn));

        // Assert
        state.SeatStates[2].State.ShouldBe(SeatState.AllIn);
        state.SeatStates[2].RemainingStack.Value.ShouldBe(0);
        state.Street.ShouldBe(Street.PreFlop);
    }

    [Fact]
    public void Calculate_CallForMoreThanTheRemainingStack_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 10);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(2, Call));

        // Assert
        ShouldFailWith(result, HandStateCalculator.CallExceedsStack);
    }

    [Fact]
    public void Calculate_RaiseForTheEntireRemainingStack_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 100);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(2, Raise, 100));

        // Assert
        ShouldFailWith(result, HandStateCalculator.BetOrRaiseForEntireStack);
    }

    [Fact]
    public void Calculate_BetForTheEntireRemainingStack_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 100);

        // Act
        Result<HandState> result = TryCalculate(
            seats,
            Act(2, Call),
            Act(0, Call),
            Act(1, Call),
            Act(0, Bet, 980));

        // Assert
        ShouldFailWith(result, HandStateCalculator.BetOrRaiseForEntireStack);
    }

    [Fact]
    public void Calculate_RaiseForMoreThanTheRemainingStack_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 100);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(2, Raise, 101));

        // Assert
        ShouldFailWith(result, HandStateCalculator.InsufficientStack);
    }

    [Fact]
    public void Calculate_ActionAfterTheHandFinished_IsRejected()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(2, Fold), Act(0, Fold), Act(1, Check));

        // Assert
        ShouldFailWith(result, HandStateCalculator.HandFinished);
    }

    [Fact]
    public void Calculate_RejectedActionIsNotTheLastOne_ReportsAFailureInsteadOfAConflict()
    {
        // Arrange
        List<HandSeat> seats = Seats(1000, 1000, 1000);

        // Act
        Result<HandState> result = TryCalculate(seats, Act(0, Call), Act(1, Call));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(HandStateCalculator.NotYourTurn.Code);
        result.Error.Type.ShouldBe(ErrorType.Failure);
    }

    private static void ShouldFailWith(Result<HandState> result, Error expected)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expected.Code);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    private static HandState Calculate(
        List<HandSeat> seats,
        params (int Seat, HandActionType Type, int? AmountTo)[] actions) =>
        TryCalculate(seats, actions).Value;

    private static Result<HandState> TryCalculate(
        List<HandSeat> seats,
        params (int Seat, HandActionType Type, int? AmountTo)[] actions) =>
        HandStateCalculator.Calculate(seats, Log(seats, actions));

    private static (int Seat, HandActionType Type, int? AmountTo) Act(
        int seat,
        HandActionType type,
        int? amountTo = null) =>
        (seat, type, amountTo);

    private static List<HandSeat> Seats(params int[] startingStacks) =>
        [.. startingStacks.Index().Select(seat => new HandSeat(
            ParticipantId.New(),
            (ChipsStack)seat.Item,
            seat.Index))];

    private static List<HandAction> Log(
        List<HandSeat> seats,
        params (int Seat, HandActionType Type, int? AmountTo)[] actions)
    {
        List<HandAction> log =
        [
            HandAction.Create(0, seats[0].ParticipantId, PostSmallBlind, (ChipsStack)SmallBlind).Value,
            HandAction.Create(1, seats[1].ParticipantId, PostBigBlind, (ChipsStack)BigBlind).Value,
        ];

        log.AddRange(actions.Select((action, index) => HandAction.Create(
            index + 2,
            seats[action.Seat].ParticipantId,
            action.Type,
            action.AmountTo is { } amountTo ? (ChipsStack)amountTo : null).Value));

        return log;
    }
}
