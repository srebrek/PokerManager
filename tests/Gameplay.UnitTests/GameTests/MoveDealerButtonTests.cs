using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class MoveDealerButtonTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    [Fact]
    public void MoveDealerButton_TargetIsAnotherSeatedParticipant_MovesTheButton()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, participantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.DealerButtonParticipantId.ShouldBe(participantId);
        game.Events.OfType<DealerButtonMovedDomainEvent>().ShouldHaveSingleItem()
            .ShouldBe(new DealerButtonMovedDomainEvent(game.Id.Value, participantId.Value));
    }

    [Fact]
    public void MoveDealerButton_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.MoveDealerButton(participantId, participantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }

    [Fact]
    public void MoveDealerButton_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.Finish(game.HostParticipantId);

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, participantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }

    [Fact]
    public void MoveDealerButton_HandIsRunning_ReturnsHandIsRunningFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.AttachHand(HandId.New());

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, participantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.HandIsRunning);
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }

    [Fact]
    public void MoveDealerButton_TargetParticipantIsNotInGame_ReturnsParticipantNotFoundFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, ParticipantId.New());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.ParticipantNotFound);
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }

    [Fact]
    public void MoveDealerButton_TargetAlreadyHoldsTheButton_ReturnsAlreadyDealerFailure()
    {
        // Arrange
        Game game = CreateGame();
        game.AddParticipant("TestParticipantName");

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, game.DealerButtonParticipantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.AlreadyDealer);
        game.Events.OfType<DealerButtonMovedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MoveDealerButton_TargetIsSittingOut_ReturnsDealerCannotSitOutFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Act
        Result result = game.MoveDealerButton(game.HostParticipantId, participantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.DealerCannotSitOut);
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }
}
