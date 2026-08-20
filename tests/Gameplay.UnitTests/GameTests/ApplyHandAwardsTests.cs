using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;

namespace Gameplay.UnitTests.GameTests;

public sealed class ApplyHandAwardsTests
{
    private static Game CreateGame(params string[] participantNames)
    {
        Game game = Game.Create("TestHostName", (ChipsStack)10, (ChipsStack)20).Value;

        foreach (string participantName in participantNames)
        {
            game.AddParticipant(participantName);
        }

        return game;
    }

    private static void FinishHand(Game game)
    {
        HandId handId = HandId.New();
        game.AttachHand(handId);
        game.ApplyHandAwards([], game.HostParticipantId, handId);
    }

    [Fact]
    public void ApplyHandAwards_NextSeatIsSeated_MovesTheDealerButtonToIt()
    {
        // Arrange
        Game game = CreateGame("TestSecondName", "TestThirdName");

        // Act
        FinishHand(game);

        // Assert
        game.DealerButtonParticipantId.ShouldBe(game.SeatingOrder[1]);
    }

    [Fact]
    public void ApplyHandAwards_NextSeatIsSittingOut_SkipsIt()
    {
        // Arrange
        Game game = CreateGame("TestSecondName", "TestThirdName");
        game.SetParticipantSittingOut(game.HostParticipantId, game.SeatingOrder[1], true);

        // Act
        FinishHand(game);

        // Assert
        game.DealerButtonParticipantId.ShouldBe(game.SeatingOrder[2]);
    }

    [Fact]
    public void ApplyHandAwards_DealerIsTheLastSeat_WrapsAroundToTheFirstSeat()
    {
        // Arrange
        Game game = CreateGame("TestSecondName", "TestThirdName");
        FinishHand(game);
        FinishHand(game);

        // Act
        FinishHand(game);

        // Assert
        game.DealerButtonParticipantId.ShouldBe(game.SeatingOrder[0]);
    }

    [Fact]
    public void ApplyHandAwards_EveryOtherParticipantIsSittingOut_KeepsTheDealerButtonInPlace()
    {
        // Arrange
        Game game = CreateGame("TestSecondName", "TestThirdName");
        game.SetParticipantSittingOut(game.HostParticipantId, game.SeatingOrder[1], true);
        game.SetParticipantSittingOut(game.HostParticipantId, game.SeatingOrder[2], true);

        // Act
        FinishHand(game);

        // Assert
        game.DealerButtonParticipantId.ShouldBe(game.HostParticipantId);
    }
}
