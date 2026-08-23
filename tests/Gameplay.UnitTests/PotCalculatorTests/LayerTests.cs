using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;

namespace Gameplay.UnitTests.PotCalculatorTests;

public sealed class LayerTests
{
    [Fact]
    public void Layer_NobodyContributed_ReturnsNoPots()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(0), Seats.With(0)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        pots.ShouldBeEmpty();
    }

    [Fact]
    public void Layer_EverybodyContributedTheSame_ReturnsOnePotForEverybody()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(20), Seats.With(20), Seats.With(20)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Index.ShouldBe(0);
        pot.Amount.Value.ShouldBe(60);
        pot.EligibleParticipantIds.ShouldBe(seatStates.Select(ss => ss.ParticipantId));
    }

    [Fact]
    public void Layer_OneSeatIsAllInForLess_SplitsIntoAMainAndASidePot()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(50, SeatState.AllIn), Seats.With(200), Seats.With(200)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        pots.Count.ShouldBe(2);
        pots[0].Amount.Value.ShouldBe(150);
        pots[0].EligibleParticipantIds.ShouldBe(seatStates.Select(ss => ss.ParticipantId));
        pots[1].Index.ShouldBe(1);
        pots[1].Amount.Value.ShouldBe(300);
        pots[1].EligibleParticipantIds.ShouldBe(seatStates.Skip(1).Select(ss => ss.ParticipantId));
    }

    [Fact]
    public void Layer_TwoSeatsAreAllInForDifferentAmounts_ReturnsAPotPerLayer()
    {
        // Arrange
        List<HandSeatState> seatStates =
        [
            Seats.With(50, SeatState.AllIn),
            Seats.With(100, SeatState.AllIn),
            Seats.With(200),
            Seats.With(200),
        ];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        pots.Select(pot => pot.Amount.Value).ShouldBe([200, 150, 200]);
        pots.Select(pot => pot.EligibleParticipantIds.Count).ShouldBe([4, 3, 2]);
    }

    [Fact]
    public void Layer_ASeatFolded_LeavesItsChipsInThePotButNotInTheEligibleList()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(50, SeatState.Folded), Seats.With(50), Seats.With(50)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(150);
        pot.EligibleParticipantIds.ShouldBe(seatStates.Skip(1).Select(ss => ss.ParticipantId));
    }

    [Fact]
    public void Layer_OnlyOneSeatIsEligible_KeepsThePotInTheList()
    {
        // Arrange
        List<HandSeatState> seatStates =
        [
            Seats.With(10, SeatState.Folded),
            Seats.With(20, SeatState.Folded),
            Seats.With(20),
        ];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(50);
        pot.EligibleParticipantIds.ShouldBe([seatStates[2].ParticipantId]);
    }

    [Fact]
    public void Layer_ActiveSeatsHaveUnequalContributionsButNobodyIsAllIn_KeepsThemInOnePot()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(60), Seats.With(60), Seats.With(10)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(130);
        pot.EligibleParticipantIds.ShouldBe(seatStates.Select(ss => ss.ParticipantId));
    }

    [Fact]
    public void Layer_ASeatHasNotActedYet_StillCountsAsEligibleForTheOpenPot()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(10), Seats.With(20), Seats.With(0)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(30);
        pot.EligibleParticipantIds.ShouldBe(seatStates.Select(ss => ss.ParticipantId));
    }

    [Fact]
    public void Layer_AllInHasNotBeenCalledYet_DoesNotAddAnEmptyPotAboveIt()
    {
        // Arrange
        List<HandSeatState> seatStates = [Seats.With(500, SeatState.AllIn), Seats.With(100), Seats.With(100)];

        // Act
        List<HandPot> pots = PotCalculator.Layer(seatStates);

        // Assert
        HandPot pot = pots.ShouldHaveSingleItem();
        pot.Amount.Value.ShouldBe(700);
        pot.EligibleParticipantIds.ShouldBe([seatStates[0].ParticipantId]);
    }
}
