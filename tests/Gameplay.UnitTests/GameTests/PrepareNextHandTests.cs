using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.GameTests;

public sealed class PrepareNextHandTests
{
    private static Game CreateGame(out ParticipantId second, out ParticipantId third, out ParticipantId fourth)
    {
        Game game = Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;
        second = AddParticipant(game, "TestSecondName", 800);
        third = AddParticipant(game, "TestThirdName", 600);
        fourth = AddParticipant(game, "TestFourthName", 400);
        game.Rebuy(game.HostParticipantId, game.HostParticipantId, 1000);

        return game;
    }

    private static ParticipantId AddParticipant(Game game, string name, int chips)
    {
        ParticipantId participantId = game.AddParticipant(name).Value;
        game.Rebuy(game.HostParticipantId, participantId, chips);
        return participantId;
    }

    [Fact]
    public void PrepareNextHand_ParticipantSittingOut_SeatsTheRemainingParticipantsAfterTheDealer()
    {
        // Arrange
        Game game = CreateGame(out ParticipantId second, out ParticipantId third, out ParticipantId fourth);
        game.SetParticipantSittingOut(game.HostParticipantId, third, true);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([
            new HandSeat(second, (ChipsStack)800, 0),
            new HandSeat(fourth, (ChipsStack)400, 1),
            new HandSeat(game.HostParticipantId, (ChipsStack)1000, 2),
        ]);
    }

    [Fact]
    public void PrepareNextHand_ActingParticipantIsNotHost_ReturnsNotHostFailure()
    {
        // Arrange
        Game game = CreateGame(out ParticipantId second, out _, out _);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(second);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.NotHost);
    }

    [Fact]
    public void PrepareNextHand_GameFinished_ReturnsGameFinishedFailure()
    {
        // Arrange
        Game game = CreateGame(out _, out _, out _);
        game.Finish(game.HostParticipantId);

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.GameFinished);
    }

    [Fact]
    public void PrepareNextHand_HandIsRunning_ReturnsHandIsRunningFailure()
    {
        // Arrange
        Game game = CreateGame(out _, out _, out _);
        game.AttachHand(HandId.New());

        // Act
        Result<IReadOnlyList<HandSeat>> result = game.PrepareNextHand(game.HostParticipantId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(GameErrors.HandIsRunning);
    }
}
