using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class RebuyIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task Rebuy_HostRebuysForParticipant_IncreasesParticipantChips()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        RebuyRequest request = new(game.ParticipantId, 500);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RebuyFor(game.GameId, participant.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Single(p => p.Id == participant.ParticipantId).Chips.ShouldBe(500);
    }

    [Fact]
    public async Task Rebuy_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        RebuyRequest request = new(Guid.NewGuid(), 500);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RebuyFor(Guid.NewGuid(), Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rebuy_NegativeAmountBelowParticipantChips_ReturnsConflictAndKeepsChips()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        RebuyRequest request = new(game.ParticipantId, -1);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RebuyFor(game.GameId, participant.ParticipantId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Single(p => p.Id == participant.ParticipantId).Chips.ShouldBe(0);
    }
}
