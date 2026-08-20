using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class MoveDealerButtonIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task MoveDealerButton_HostMovesTheButtonToAnotherParticipant_PersistsTheNewDealer()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse participant = await AddParticipantAsync(game.GameId, "TestParticipantName");

        MoveDealerButtonRequest request = new(game.ParticipantId, participant.ParticipantId);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.DealerButtonFor(game.GameId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Single(p => p.Id == participant.ParticipantId).IsDealer.ShouldBeTrue();
        state.Participants.Single(p => p.Id == game.ParticipantId).IsDealer.ShouldBeFalse();
    }

    [Fact]
    public async Task MoveDealerButton_TargetAlreadyHoldsTheButton_ReturnsConflictAndKeepsTheDealer()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        await AddParticipantAsync(game.GameId, "TestParticipantName");

        MoveDealerButtonRequest request = new(game.ParticipantId, game.ParticipantId);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.DealerButtonFor(game.GameId),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse state = await GetGameStateAsync(game.GameId);
        state.Participants.Single(p => p.IsDealer).Id.ShouldBe(game.ParticipantId);
    }

    [Fact]
    public async Task MoveDealerButton_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        MoveDealerButtonRequest request = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.DealerButtonFor(Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
