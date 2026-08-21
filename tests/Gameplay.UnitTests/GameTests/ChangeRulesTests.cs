using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class ChangeRulesTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    [Fact]
    public void ChangeRules_HostSetsNewBlinds_UpdatesTheBlinds()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 25, 50);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.SmallBlind.Value.ShouldBe(25);
        game.BigBlind.Value.ShouldBe(50);
        game.Events.OfType<GameRulesChangedDomainEvent>().ShouldHaveSingleItem()
            .ShouldBe(new GameRulesChangedDomainEvent(game.Id.Value));
    }

    [Fact]
    public void ChangeRules_BlindsAreEqual_UpdatesTheBlinds()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 30, 30);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.SmallBlind.Value.ShouldBe(30);
        game.BigBlind.Value.ShouldBe(30);
    }

    [Fact]
    public void ChangeRules_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.ChangeRules(participantId, 25, 50);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
    }

    [Fact]
    public void ChangeRules_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGame();
        game.Finish(game.HostParticipantId);

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 25, 50);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
    }

    [Fact]
    public void ChangeRules_HandIsRunning_ReturnsHandIsRunningFailure()
    {
        // Arrange
        Game game = CreateGame();
        game.AttachHand(HandId.New());

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 25, 50);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.HandIsRunning);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
    }

    [Fact]
    public void ChangeRules_SmallBlindIsNegative_ReturnsNegativeValueFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, -1, 50);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ChipsStack.NegativeValueError);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
    }

    [Fact]
    public void ChangeRules_BigBlindIsNegative_ReturnsNegativeValueFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 25, -1);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ChipsStack.NegativeValueError);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
    }

    [Fact]
    public void ChangeRules_BigBlindIsLessThanSmallBlind_ReturnsBigBlindLessThanSmallBlindFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.ChangeRules(game.HostParticipantId, 50, 25);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.BigBlindLessThanSmallBlind);
        game.SmallBlind.Value.ShouldBe(10);
        game.BigBlind.Value.ShouldBe(20);
        game.Events.OfType<GameRulesChangedDomainEvent>().ShouldBeEmpty();
    }
}
