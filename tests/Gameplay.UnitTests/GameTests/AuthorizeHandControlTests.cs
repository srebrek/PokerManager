using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class AuthorizeHandControlTests
{
    [Fact]
    public void AuthorizeHandControl_HostActsOnTheCurrentHand_Succeeds()
    {
        // Arrange
        Game game = CreateGameWithHand(out HandId handId);

        // Act
        Result result = game.AuthorizeHandControl(game.HostParticipantId, handId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AuthorizeHandControl_ActingParticipantIsNotTheHost_Fails()
    {
        // Arrange
        Game game = CreateGameWithHand(out HandId handId);

        // Act
        Result result = game.AuthorizeHandControl(ParticipantId.New(), handId);

        // Assert
        result.Error.ShouldBe(GameErrors.NotHost);
    }

    [Fact]
    public void AuthorizeHandControl_NoHandIsRunning_Fails()
    {
        // Arrange
        Game game = Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

        // Act
        Result result = game.AuthorizeHandControl(game.HostParticipantId, HandId.New());

        // Assert
        result.Error.ShouldBe(GameErrors.NoCurrentHand);
    }

    [Fact]
    public void AuthorizeHandControl_AnotherHandIsRunning_Fails()
    {
        // Arrange
        Game game = CreateGameWithHand(out _);

        // Act
        Result result = game.AuthorizeHandControl(game.HostParticipantId, HandId.New());

        // Assert
        result.Error.ShouldBe(GameErrors.InvalidHandId);
    }

    private static Game CreateGameWithHand(out HandId handId)
    {
        Game game = Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;
        handId = HandId.New();
        game.AttachHand(handId);

        return game;
    }
}
