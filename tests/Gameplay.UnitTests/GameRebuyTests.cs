using Gameplay.Domain.Entities;
using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests;

public sealed class GameRebuyTests
{
    private static Game CreateGame() => Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

    [Fact]
    public void Rebuy_PositiveAmount_AddsChipsAndBuyIn()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.Rebuy(game.HostParticipantId, participantId, 500);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Participant participant = game.Participants.Single(p => p.Id == participantId);
        participant.Chips.Value.ShouldBe(500);
        participant.TotalBuyIn.Value.ShouldBe(500);
        game.Events.OfType<RebuyRecordedDomainEvent>().ShouldHaveSingleItem()
            .ShouldBe(new RebuyRecordedDomainEvent(game.Id.Value, participantId.Value));
    }

    [Fact]
    public void Rebuy_NegativeAmount_RemovesChipsAndBuyIn()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.Rebuy(game.HostParticipantId, participantId, 1000);

        // Act
        Result result = game.Rebuy(game.HostParticipantId, participantId, -400);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Participant participant = game.Participants.Single(p => p.Id == participantId);
        participant.Chips.Value.ShouldBe(600);
        participant.TotalBuyIn.Value.ShouldBe(600);
    }

    [Fact]
    public void Rebuy_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.Rebuy(participantId, participantId, 500);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
    }

    [Fact]
    public void Rebuy_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        game.Finish(game.HostParticipantId);

        // Act
        Result result = game.Rebuy(game.HostParticipantId, participantId, 500);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
    }

    [Fact]
    public void Rebuy_TargetParticipantIsNotInGame_ReturnsParticipantNotFoundFailure()
    {
        // Arrange
        Game game = CreateGame();

        // Act
        Result result = game.Rebuy(game.HostParticipantId, ParticipantId.New(), 500);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.ParticipantNotFound);
    }

    [Fact]
    public void Rebuy_NegativeAmountBelowChips_ReturnsRebuyLeavesNegativeChipsFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;

        // Act
        Result result = game.Rebuy(game.HostParticipantId, participantId, -1);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.RebuyLeavesNegativeChips);
        game.Participants.Single(p => p.Id == participantId).Chips.Value.ShouldBe(0);
    }

    [Fact]
    public void Rebuy_NegativeAmountBelowTotalBuyIn_ReturnsRebuyLeavesNegativeTotalBuyInFailure()
    {
        // Arrange
        Game game = CreateGame();
        ParticipantId participantId = game.AddParticipant("TestParticipantName").Value;
        Participant participant = game.Participants.Single(p => p.Id == participantId);
        participant.AddChips(1000);

        // Act
        Result result = game.Rebuy(game.HostParticipantId, participantId, -1);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ParticipantErrors.RebuyLeavesNegativeTotalBuyIn);
        participant.Chips.Value.ShouldBe(1000);
        participant.TotalBuyIn.Value.ShouldBe(0);
    }
}
