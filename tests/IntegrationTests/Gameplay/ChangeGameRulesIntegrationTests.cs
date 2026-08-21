using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class ChangeGameRulesIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task ChangeGameRules_HostSetsNewBlinds_PersistsTheNewBlinds()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");

        ChangeGameRulesRequest request = new(game.ParticipantId, 25, 50);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RulesFor(game.GameId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.SmallBlind.ShouldBe(25);
        state.BigBlind.ShouldBe(50);
    }

    [Fact]
    public async Task ChangeGameRules_ActingParticipantIsNotHost_ReturnsConflictAndKeepsTheBlinds()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");
        GameStateResponse before = await GetGameStateAsync(game.GameId);

        ChangeGameRulesRequest request = new(participant.ParticipantId, 25, 50);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RulesFor(game.GameId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.SmallBlind.ShouldBe(before.SmallBlind);
        state.BigBlind.ShouldBe(before.BigBlind);
    }

    [Fact]
    public async Task ChangeGameRules_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        ChangeGameRulesRequest request = new(Guid.NewGuid(), 25, 50);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RulesFor(Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
