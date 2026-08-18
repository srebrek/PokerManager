using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class AddParticipantIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task AddParticipant_TwoParticipantsAdded_AppendsThemInJoiningOrder()
    {
        // Arrange
        CreateGameResponse createGameResponse = await CreateGameAsync("TestHostName");

        // Act
        AddParticipantResponse participant1Response =
            await AddParticipantAsync(createGameResponse.GameId, "TestParticipant1Name");

        AddParticipantResponse participant2Response =
            await AddParticipantAsync(createGameResponse.GameId, "TestParticipant2Name");

        // Assert
        participant1Response.ParticipantId.ShouldNotBe(Guid.Empty);
        participant2Response.ParticipantId.ShouldNotBe(Guid.Empty);

        GameStateResponse state = await GetGameStateAsync(createGameResponse.GameId);

        state.Participants.Count.ShouldBe(3);

        GameStateParticipant participant0 = state.Participants[0];
        GameStateParticipant participant1 = state.Participants[1];
        GameStateParticipant participant2 = state.Participants[2];

        participant0.ShouldBe(new GameStateParticipant(
            createGameResponse.ParticipantId,
            "TestHostName",
            0,
            true));

        participant1.ShouldBe(new GameStateParticipant(
            participant1Response.ParticipantId,
            "TestParticipant1Name",
            0,
            false));

        participant2.ShouldBe(new GameStateParticipant(
            participant2Response.ParticipantId,
            "TestParticipant2Name",
            0,
            false));
    }

    [Fact]
    public async Task AddParticipant_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        AddParticipantRequest request = new("TestParticipantName");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.GameParticipantsFor(Guid.NewGuid()),
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
