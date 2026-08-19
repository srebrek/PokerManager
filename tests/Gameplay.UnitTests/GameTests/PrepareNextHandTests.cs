using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class PrepareNextHandTests
{
    private const int BigBlind = 20;

    private static Game CreateStartableGame(out ParticipantId second, out ParticipantId third)
    {
        Game game = Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)BigBlind).Value;
        second = game.AddParticipant("TestSecondName").Value;
        third = game.AddParticipant("TestThirdName").Value;

        foreach (ParticipantId participantId in game.SeatingOrder)
        {
            game.Rebuy(game.HostParticipantId, participantId, 1000);
        }

        return game;
    }

    private static ParticipantId AddFundedParticipant(Game game, string name)
    {
        ParticipantId participantId = game.AddParticipant(name).Value;
        game.Rebuy(game.HostParticipantId, participantId, 1000);
        return participantId;
    }

    [Fact]
    public void PrepareNextHand_ParticipantSittingOut_ExcludesItFromTheSeatsAndKeepsTheOrder()
    {
        // Arrange
        Game game = CreateStartableGame(out ParticipantId second, out ParticipantId third);
        ParticipantId fourth = AddFundedParticipant(game, "TestFourthName");
        game.SetParticipantSittingOut(game.HostParticipantId, second, true);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(s => s.ParticipantId).ShouldBe([third, fourth, game.HostParticipantId]);
        result.Value.Select(s => s.Position).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void PrepareNextHand_TooFewParticipantsLeftSeated_ReturnsNotEnoughParticipantsFailure()
    {
        // Arrange
        Game game = CreateStartableGame(out ParticipantId second, out _);
        game.SetParticipantSittingOut(game.HostParticipantId, second, true);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotEnoughParticipants);
    }

    [Fact]
    public void PrepareNextHand_ParticipantSittingOutBelowBigBlind_DoesNotBlockTheHand()
    {
        // Arrange
        Game game = CreateStartableGame(out ParticipantId second, out _);
        AddFundedParticipant(game, "TestFourthName");
        game.Rebuy(game.HostParticipantId, second, -1000 + BigBlind - 1);
        game.SetParticipantSittingOut(game.HostParticipantId, second, true);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(s => s.ParticipantId).ShouldNotContain(second);
    }
}
