using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class SetParticipantSittingOutIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task SetParticipantSittingOut_HostSitsAParticipantOut_PersistsTheFlagAndKeepsTheSeat()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        SetParticipantSittingOutRequest request = new(game.ParticipantId, true);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.SetParticipantSittingOutFor(game.GameId, participant.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Select(p => p.Id).ShouldBe([game.ParticipantId, participant.ParticipantId]);
        state.Participants.Single(p => p.Id == participant.ParticipantId).IsSittingOut.ShouldBeTrue();
        state.Participants.Single(p => p.Id == game.ParticipantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public async Task SetParticipantSittingOut_GameIsFinished_ReturnsConflictAndKeepsTheParticipantSeated()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        using HttpResponseMessage finish = await Client.PostAsJsonAsync(
            GameplayRoutes.FinishGameFor(game.GameId),
            new FinishGameRequest(game.ParticipantId),
            CancellationToken);
        finish.EnsureSuccessStatusCode();

        SetParticipantSittingOutRequest request = new(game.ParticipantId, true);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.SetParticipantSittingOutFor(game.GameId, participant.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Single(p => p.Id == participant.ParticipantId).IsSittingOut.ShouldBeFalse();
    }

    [Fact]
    public async Task SetParticipantSittingOut_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        SetParticipantSittingOutRequest request = new(Guid.NewGuid(), true);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.SetParticipantSittingOutFor(Guid.NewGuid(), Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
