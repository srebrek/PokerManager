using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;
using static Gameplay.UnitTests.HandTests.Hands;

namespace Gameplay.UnitTests.HandTests;

public sealed class FinishTests
{
    [Fact]
    public void Finish_EverybodyElseFolded_PaysTheUncontestedPotAndKeepsTheUncalledChips()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();
        DeclareWinner(hand, 0, 1);

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.Status.ShouldBe(HandStatus.Finished);
        Net(result.Value, hand, 0).ShouldBe(-SmallBlind);
        Net(result.Value, hand, 1).ShouldBe(SmallBlind);
        Net(result.Value, hand, 2).ShouldBe(0);
    }

    [Fact]
    public void Finish_SidePotWasFormed_PaysEveryPotToItsOwnWinner()
    {
        // Arrange
        Hand hand = WithSidePot();
        hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 0)), new HandPotWinner(1, Seat(hand, 1))]);

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Net(result.Value, hand, 0).ShouldBe(100);
        Net(result.Value, hand, 1).ShouldBe(0);
        Net(result.Value, hand, 2).ShouldBe(-100);
    }

    [Fact]
    public void Finish_PotIsChopped_SplitsItAndGivesTheOddChipToTheFirstSeat()
    {
        // Arrange
        Hand hand = Start(1000, 1000, 1000);
        Act(hand, 2, HandActionType.Raise, 25);
        Act(hand, 0, HandActionType.Call);
        Act(hand, 1, HandActionType.Call);
        CheckDownToTheRiver(hand, 0, 1, 2);
        hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 1)), new HandPotWinner(0, Seat(hand, 0))]);

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Net(result.Value, hand, 0).ShouldBe(13);
        Net(result.Value, hand, 1).ShouldBe(12);
        Net(result.Value, hand, 2).ShouldBe(-25);
    }

    [Fact]
    public void Finish_WinnersWereNotDeclared_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        ShouldFailWith(result, HandErrors.WinnersNotDeclared);
        hand.Status.ShouldBe(HandStatus.InProgress);
    }

    [Fact]
    public void Finish_HandIsStillInPlay_Fails()
    {
        // Arrange
        Hand hand = Start(1000, 1000, 1000);

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        ShouldFailWith(result, HandErrors.NotFinishedStreetFinish);
    }

    [Fact]
    public void Finish_HandIsAlreadyFinished_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();
        DeclareWinner(hand, 0, 1);
        hand.Finish();

        // Act
        Result<List<HandAward>> result = hand.Finish();

        // Assert
        ShouldFailWith(result, HandErrors.NotInProgressHandFinish);
    }

    private static void ShouldFailWith(Result<List<HandAward>> result, Error expected)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expected.Code);
    }

    private static int Net(List<HandAward> awards, Hand hand, int position) =>
        awards.Single(award => award.ParticipantId == Seat(hand, position)).Net;
}
