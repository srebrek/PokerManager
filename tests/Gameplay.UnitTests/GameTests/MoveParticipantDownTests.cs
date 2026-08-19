using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class MoveParticipantDownTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    private static Game CreateGameWithParticipants(out ParticipantId first, out ParticipantId last)
    {
        Game game = CreateGame();
        first = game.HostParticipantId;
        game.AddParticipant("TestMiddleName");
        last = game.AddParticipant("TestLastName").Value;
        return game;
    }

    [Fact]
    public void MoveParticipantDown_ParticipantAboveAnother_SwapsThem()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out ParticipantId first, out ParticipantId last);
        ParticipantId middle = game.SeatingOrder[1];

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, first);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.SeatingOrder.ShouldBe([middle, first, last]);
        game.Events.OfType<SeatingOrderChangedDomainEvent>().ShouldHaveSingleItem()
            .ShouldBe(new SeatingOrderChangedDomainEvent(game.Id.Value, first.Value));
    }

    [Fact]
    public void MoveParticipantDown_LastParticipant_WrapsToTheFirstSeat()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out ParticipantId first, out ParticipantId last);
        ParticipantId middle = game.SeatingOrder[1];

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, last);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.SeatingOrder.ShouldBe([last, middle, first]);
    }

    [Fact]
    public void MoveParticipantDown_OnlyParticipant_KeepsTheSeatingOrder()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, game.HostParticipantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        game.SeatingOrder.ShouldBe([game.HostParticipantId]);
    }

    [Fact]
    public void MoveParticipantDown_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out ParticipantId first, out ParticipantId last);

        // Act
        Result result = game.MoveParticipantDown(last, first);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
        game.SeatingOrder[0].ShouldBe(first);
    }

    [Fact]
    public void MoveParticipantDown_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out ParticipantId first, out _);
        game.Finish(game.HostParticipantId);

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, first);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
        game.SeatingOrder[0].ShouldBe(first);
    }

    [Fact]
    public void MoveParticipantDown_HandIsRunning_ReturnsHandIsRunningFailure()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out ParticipantId first, out _);
        game.AttachHand(HandId.New());

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, first);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.HandIsRunning);
        game.SeatingOrder[0].ShouldBe(first);
    }

    [Fact]
    public void MoveParticipantDown_TargetParticipantIsNotInGame_ReturnsParticipantNotFoundFailure()
    {
        // Arrange
        Game game = CreateGameWithParticipants(out _, out _);

        // Act
        Result result = game.MoveParticipantDown(game.HostParticipantId, ParticipantId.New());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.ParticipantNotFound);
    }
}
