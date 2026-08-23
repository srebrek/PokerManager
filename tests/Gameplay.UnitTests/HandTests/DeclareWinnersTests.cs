using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;
using static Gameplay.UnitTests.HandTests.Hands;

namespace Gameplay.UnitTests.HandTests;

public sealed class DeclareWinnersTests
{
    [Fact]
    public void DeclareWinners_SinglePot_StoresTheWinnerAndRaisesTheEvent()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 1))]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.PotWinners.ShouldBe([new HandPotWinner(0, Seat(hand, 1))]);
        hand.Events.OfType<HandWinnersDeclaredDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void DeclareWinners_ChoppedPot_StoresEveryWinner()
    {
        // Arrange
        Hand hand = WithSidePot();

        // Act
        Result result = hand.DeclareWinners(
        [
            new HandPotWinner(0, Seat(hand, 0)),
            new HandPotWinner(0, Seat(hand, 1)),
            new HandPotWinner(1, Seat(hand, 2)),
        ]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.PotWinners.Count.ShouldBe(3);
    }

    [Fact]
    public void DeclareWinners_DeclaredAgain_ReplacesThePreviousDeclaration()
    {
        // Arrange
        Hand hand = WithSidePot();
        hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 0)), new HandPotWinner(1, Seat(hand, 1))]);

        // Act
        Result result = hand.DeclareWinners(
            [new HandPotWinner(0, Seat(hand, 1)), new HandPotWinner(1, Seat(hand, 2))]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.PotWinners.ShouldBe([new HandPotWinner(0, Seat(hand, 1)), new HandPotWinner(1, Seat(hand, 2))]);
    }

    [Fact]
    public void DeclareWinners_NothingIsClaimed_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result result = hand.DeclareWinners([]);

        // Assert
        ShouldFailWith(result, HandErrors.WinnersNotDeclared);
    }

    [Fact]
    public void DeclareWinners_SidePotIsNotClaimed_Fails()
    {
        // Arrange
        Hand hand = WithSidePot();

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 0))]);

        // Assert
        ShouldFailWith(result, HandErrors.PotWinnersMismatch);
    }

    [Fact]
    public void DeclareWinners_PotIndexIsUnknown_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(1, Seat(hand, 1))]);

        // Assert
        ShouldFailWith(result, HandErrors.PotWinnersMismatch);
    }

    [Fact]
    public void DeclareWinners_SameWinnerIsListedTwiceForThePot_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result result = hand.DeclareWinners(
            [new HandPotWinner(0, Seat(hand, 1)), new HandPotWinner(0, Seat(hand, 1))]);

        // Assert
        ShouldFailWith(result, HandErrors.DuplicatedPotWinners);
    }

    [Fact]
    public void DeclareWinners_WinnerIsNotEligibleForThePot_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 0))]);

        // Assert
        ShouldFailWith(result, HandErrors.IneligibleWinner);
    }

    [Fact]
    public void DeclareWinners_SidePotWinnerIsOnlyEligibleForTheMainPot_Fails()
    {
        // Arrange
        Hand hand = WithSidePot();

        // Act
        Result result = hand.DeclareWinners(
            [new HandPotWinner(0, Seat(hand, 0)), new HandPotWinner(1, Seat(hand, 0))]);

        // Assert
        ShouldFailWith(result, HandErrors.IneligibleWinner);
    }

    [Fact]
    public void DeclareWinners_HandIsStillInPlay_Fails()
    {
        // Arrange
        Hand hand = Start(1000, 1000, 1000);

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 1))]);

        // Assert
        ShouldFailWith(result, HandErrors.NotFinishedStreetWinnersDeclaration);
    }

    [Fact]
    public void DeclareWinners_HandIsAlreadyFinished_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();
        DeclareWinner(hand, 0, 1);
        hand.Finish();

        // Act
        Result result = hand.DeclareWinners([new HandPotWinner(0, Seat(hand, 1))]);

        // Assert
        ShouldFailWith(result, HandErrors.NotInProgressWinnersDeclaration);
    }

    private static void ShouldFailWith(Result result, Error expected)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expected.Code);
    }
}
