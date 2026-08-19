using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class SetParticipantSittingOutTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    [Fact]
    public void SetParticipantSittingOut_SittingOut_MarksTheParticipant()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.Participants.Single(p => p.Id == participantId).IsSittingOut.ShouldBeTrue();
        game.Events.OfType<ParticipantSittingOutChangedDomainEvent>().ShouldHaveSingleItem()
            .ShouldBe(new ParticipantSittingOutChangedDomainEvent(game.Id.Value, participantId.Value));
    }

    [Fact]
    public void SetParticipantSittingOut_SittingBackIn_ClearsTheFlag()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.Participants.Single(p => p.Id == participantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public void SetParticipantSittingOut_AlreadySittingOut_ReturnsAlreadySittingOutFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.AlreadySittingOut);
        game.Events.OfType<ParticipantSittingOutChangedDomainEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void SetParticipantSittingOut_AlreadySittingIn_ReturnsAlreadySittingInFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, false);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.AlreadySittingIn);
        game.Events.OfType<ParticipantSittingOutChangedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void SetParticipantSittingOut_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.SetParticipantSittingOut(participantId, participantId, true);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
        game.Participants.Single(p => p.Id == participantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public void SetParticipantSittingOut_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.Finish(game.HostParticipantId);

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
        game.Participants.Single(p => p.Id == participantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public void SetParticipantSittingOut_HandIsRunning_ReturnsHandIsRunningFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.AttachHand(HandId.New());

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, participantId, true);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.HandIsRunning);
        game.Participants.Single(p => p.Id == participantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public void SetParticipantSittingOut_TargetParticipantIsNotInGame_ReturnsParticipantNotFoundFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.SetParticipantSittingOut(game.HostParticipantId, ParticipantId.New(), true);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.ParticipantNotFound);
    }
}
