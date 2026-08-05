using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;

namespace Integration.Tests.Gameplay;

public sealed class GetGameStateIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task GetGameState_GameExists_ReturnsGameState()
    {
        // Arrange
        string testHostName = "TestHostName";
        CreateGameResponse createGameResponse = await CreateGameAsync(testHostName);

        // Act
        using HttpResponseMessage response = await Client.GetAsync(
            GameplayRoutes.GetGameStateFor(createGameResponse.GameId),
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        GameStateResponse? body = await response.Content.ReadFromJsonAsync<GameStateResponse>(CancellationToken);
        body.ShouldNotBeNull();
        body.GameId.ShouldBe(createGameResponse.GameId);
        body.JoinCode.ShouldNotBe(string.Empty);
        body.IsFinished.ShouldBeFalse();
        body.SmallBlind.ShouldBe(5);
        body.BigBlind.ShouldBe(10);

        GameStateParticipant host = body.Participants.ShouldHaveSingleItem();
        host.Id.ShouldBe(createGameResponse.ParticipantId);
        host.Name.ShouldBe("TestHostName");
        host.Chips.ShouldBe(Game.DefaultStartingStack);
        host.SeatIndex.ShouldBe(0);
        host.IsHost.ShouldBeTrue();
    }

    [Fact]
    public async Task GetGameState_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid unknownGameId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await Client.GetAsync(
            GameplayRoutes.GetGameStateFor(unknownGameId),
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
