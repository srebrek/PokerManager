using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;
using static Gameplay.UnitTests.HandTests.Hands;

namespace Gameplay.UnitTests.HandTests;

public sealed class UndoLastActionTests
{
    [Fact]
    public void UndoLastAction_ActionWasRecorded_RemovesItAndKeepsTheEarlierOnes()
    {
        // Arrange
        Hand hand = Start(1000, 1000, 1000);
        Act(hand, 2, HandActionType.Raise, 100);

        // Act
        Result result = hand.UndoLastAction();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.Actions.Select(a => a.Type).ShouldBe([HandActionType.PostSmallBlind, HandActionType.PostBigBlind]);
    }

    [Fact]
    public void UndoLastAction_CalledRepeatedly_UnwindsEveryActionDownToTheBlinds()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();

        // Act
        hand.UndoLastAction().IsSuccess.ShouldBeTrue();
        hand.UndoLastAction().IsSuccess.ShouldBeTrue();
        Result result = hand.UndoLastAction();

        // Assert
        result.Error.ShouldBe(HandErrors.NothingToUndo);
        hand.Actions.Count.ShouldBe(2);
    }

    [Fact]
    public void UndoLastAction_WinnersWereDeclared_ClearsThem()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();
        DeclareWinner(hand, 0, 1);

        // Act
        Result result = hand.UndoLastAction();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        hand.PotWinners.ShouldBeEmpty();
    }

    [Fact]
    public void UndoLastAction_HandIsNotInProgress_Fails()
    {
        // Arrange
        Hand hand = FoldedToTheBigBlind();
        DeclareWinner(hand, 0, 1);
        hand.Finish().IsSuccess.ShouldBeTrue();

        // Act
        Result result = hand.UndoLastAction();

        // Assert
        result.Error.ShouldBe(HandErrors.NotInProgressHandUndo);
    }
}
