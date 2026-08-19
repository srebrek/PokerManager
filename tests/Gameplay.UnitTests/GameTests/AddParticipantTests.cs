using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class AddParticipantTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    [Fact]
    public void AddParticipant_ValidInput_ReturnsParticipantId()
    {
        // Arrange
        Game game = CreateGame();
        string participantName = "TestParticipantName";

        // Act
        Result<ParticipantId> result = game.AddParticipant(participantName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ParticipantId participantId = result.Value;
        game.Participants.ShouldAllBe(p => p.Chips.Value == 0);
        game.Participants.Count.ShouldBe(2);
        game.SeatingOrder.ShouldBe([game.HostParticipantId, participantId]);
    }

    [Fact]
    public void AddParticipant_EmptyParticipantName_ReturnsInvalidNameFailure()
    {
        // Arrange
        Game game = CreateGame();
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
        Game game = CreateGame();

        // Act
        ParticipantId participant1Id = game.AddParticipant("Participant1Name").Value;
        ParticipantId participant2Id = game.AddParticipant("Participant2Name").Value;

        // Assert
        game.Participants.Count.ShouldBe(3);
        game.SeatingOrder.ShouldBe([game.HostParticipantId, participant1Id, participant2Id]);
    }
}
