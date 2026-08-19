using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class CreateTests
{
    [Fact]
    public void Create_ValidInput_ReturnsGame()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = (ChipsStack)10;
        ChipsStack bigBlind = (ChipsStack)20;

        // Act
        Result<Game> result = Game.Create(hostName, smallBlind, bigBlind);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Game game = result.Value;
        Participant host = game.Participants.ShouldHaveSingleItem();
        host.Name.ShouldBe(hostName);
        host.Chips.Value.ShouldBe(0);
        game.HostParticipantId.ShouldBe(host.Id);
        game.SmallBlind.ShouldBe(smallBlind);
        game.BigBlind.ShouldBe(bigBlind);
        game.IsFinished.ShouldBeFalse();
        game.SeatingOrder.ShouldBe([host.Id]);
    }

    [Fact]
    public void Create_SmallBlindBiggerThanBigBlind_ReturnsBigBlindLessThanSmallBlindFailure()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = (ChipsStack)20;
        ChipsStack bigBlind = (ChipsStack)10;

        // Act
        Result<Game> result = Game.Create(hostName, smallBlind, bigBlind);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.BigBlindLessThanSmallBlind);
    }

    [Fact]
    public void Create_EmptyHostName_ReturnsInvalidNameFailure()
    {
        // Arrange
        string hostName = string.Empty;
        ChipsStack smallBlind = (ChipsStack)10;
        ChipsStack bigBlind = (ChipsStack)20;

        // Act
        Result<Game> result = Game.Create(hostName, smallBlind, bigBlind);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.InvalidName);
    }
}
