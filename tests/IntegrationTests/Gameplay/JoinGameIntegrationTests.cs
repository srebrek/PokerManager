using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class JoinGameIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task JoinGame_TwoParticipantsJoin_AppendsThemInJoiningOrder()
    {
        // Arrange
        CreateGameResponse createGameResponse = await CreateGameAsync("TestHostName");
        string joinCode = await GetJoinCodeAsync(createGameResponse.GameId);

        // Act
        using HttpResponseMessage join1HttpResponse = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            new JoinGameRequest("TestParticipant1Name", joinCode),
            CancellationToken);

        using HttpResponseMessage join2HttpResponse = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            new JoinGameRequest("TestParticipant2Name", joinCode),
            CancellationToken);

        // Assert
        JoinGameResponse? joinGame1Response = await join1HttpResponse.Content
            .ReadFromJsonAsync<JoinGameResponse>(CancellationToken);
        JoinGameResponse? joinGame2Response = await join2HttpResponse.Content
            .ReadFromJsonAsync<JoinGameResponse>(CancellationToken);

        joinGame1Response.ShouldNotBeNull();
        joinGame2Response.ShouldNotBeNull();
        joinGame1Response.GameId.ShouldBe(createGameResponse.GameId);
        joinGame2Response.GameId.ShouldBe(createGameResponse.GameId);
        joinGame1Response.ParticipantId.ShouldNotBe(Guid.Empty);
        joinGame2Response.ParticipantId.ShouldNotBe(Guid.Empty);

        GameStateResponse state = await GetGameStateAsync(createGameResponse.GameId);

        state.Participants.Count.ShouldBe(3);

        GameStateParticipant participant0 = state.Participants[0];
        GameStateParticipant participant1 = state.Participants[1];
        GameStateParticipant participant2 = state.Participants[2];

        participant0.ShouldBe(new GameStateParticipant(
            createGameResponse.ParticipantId,
            "TestHostName",
            1000,
            true));

        participant1.ShouldBe(new GameStateParticipant(
            joinGame1Response.ParticipantId,
            "TestParticipant1Name",
            1000,
            false));

        participant2.ShouldBe(new GameStateParticipant(
            joinGame2Response.ParticipantId,
            "TestParticipant2Name",
            1000,
            false));
    }

    [Fact]
    public async Task JoinGame_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        JoinGameRequest request = new("TestParticipantName", "123456");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
