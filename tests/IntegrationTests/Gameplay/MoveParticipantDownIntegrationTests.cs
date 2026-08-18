using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class MoveParticipantDownIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task MoveParticipantDown_HostMovesItself_PersistsTheNewSeatingOrder()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        MoveParticipantDownRequest request = new(game.ParticipantId);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.MoveParticipantDownFor(game.GameId, game.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Select(p => p.Id).ShouldBe([participant.ParticipantId, game.ParticipantId]);
    }

    [Fact]
    public async Task MoveParticipantDown_GameIsFinished_ReturnsConflictAndKeepsSeatingOrder()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        using HttpResponseMessage finish = await Client.PostAsJsonAsync(
            GameplayRoutes.FinishGameFor(game.GameId),
            new FinishGameRequest(game.ParticipantId),
            CancellationToken);
        finish.EnsureSuccessStatusCode();

        MoveParticipantDownRequest request = new(game.ParticipantId);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.MoveParticipantDownFor(game.GameId, game.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Select(p => p.Id).ShouldBe([game.ParticipantId, participant.ParticipantId]);
    }

    [Fact]
    public async Task MoveParticipantDown_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        MoveParticipantDownRequest request = new(Guid.NewGuid());

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.MoveParticipantDownFor(Guid.NewGuid(), Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
