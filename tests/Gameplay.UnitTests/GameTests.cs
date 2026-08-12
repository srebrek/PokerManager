using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests;

public sealed class GameTests
{
    [Fact]
    public void Create_ValidInput_ReturnsGame()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = ChipsStack.Create(10).Value;
        ChipsStack bigBlind = ChipsStack.Create(20).Value;

        // Act
        Result<Game> result = Game.Create(hostName, smallBlind, bigBlind);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Game game = result.Value;
        Participant host = game.Participants.ShouldHaveSingleItem();
        host.Name.ShouldBe(hostName);
        host.Chips.Value.ShouldBe(Game.DefaultStartingStack);
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
        ChipsStack smallBlind = ChipsStack.Create(20).Value;
        ChipsStack bigBlind = ChipsStack.Create(10).Value;

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
        ChipsStack smallBlind = ChipsStack.Create(10).Value;
        ChipsStack bigBlind = ChipsStack.Create(20).Value;

        // Act
        Result<Game> result = Game.Create(hostName, smallBlind, bigBlind);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.InvalidName);
    }

    [Fact]
    public void AddParticipant_ValidInput_ReturnsParticipantId()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = ChipsStack.Create(10).Value;
        ChipsStack bigBlind = ChipsStack.Create(20).Value;

        Game game = Game.Create(hostName, smallBlind, bigBlind).Value;

        string participantName = "TestParticipantName";

        // Act
        Result<ParticipantId> result = game.AddParticipant(participantName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ParticipantId participantId = result.Value;
        game.Participants.ShouldAllBe(p => p.Chips.Value == Game.DefaultStartingStack);
        game.Participants.Count.ShouldBe(2);
        game.SeatingOrder.ShouldBe([game.HostParticipantId, participantId]);
    }

    [Fact]
    public void AddParticipant_EmptyParticipantName_ReturnsInvalidNameFailure()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = ChipsStack.Create(10).Value;
        ChipsStack bigBlind = ChipsStack.Create(20).Value;

        Game game = Game.Create(hostName, smallBlind, bigBlind).Value;

        string participantName = string.Empty;

        // Act
        Result<ParticipantId> result = game.AddParticipant(participantName);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.InvalidName);
    }

    [Fact]
    public void AddParticipant_TwoAdded_GameHasCorrectSeatingOrder()
    {
        // Arrange
        string hostName = "TestHostName";
        ChipsStack smallBlind = ChipsStack.Create(10).Value;
        ChipsStack bigBlind = ChipsStack.Create(20).Value;

        Game game = Game.Create(hostName, smallBlind, bigBlind).Value;

        // Act
        string participant1Name = "Participant1Name";
        ParticipantId participant1Id = game.AddParticipant(participant1Name).Value;

        string participant2Name = "Participant2Name";
        ParticipantId participant2Id = game.AddParticipant(participant2Name).Value;

        // Assert
        game.Participants.Count.ShouldBe(3);
        game.SeatingOrder.ShouldBe([game.HostParticipantId, participant1Id, participant2Id]);
    }
}
