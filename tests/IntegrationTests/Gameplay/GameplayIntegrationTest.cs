using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public abstract class GameplayIntegrationTest(ApiFactory factory) : BaseIntegrationTest(factory)
{
    protected async Task<CreateGameResponse> CreateGameAsync(string hostName)
    {
        CreateGameRequest request = new(hostName);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.Games,
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        CreateGameResponse? body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<AddParticipantResponse> AddParticipantAsync(Guid gameId, string participantName)
    {
        AddParticipantRequest request = new(participantName);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.GameParticipantsFor(gameId),
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? body =
            await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<string> GetJoinCodeAsync(Guid gameId) => (await GetGameStateAsync(gameId)).JoinCode;

    protected async Task<GameStateResponse> GetGameStateAsync(Guid gameId)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            GameplayRoutes.GameFor(gameId),
            CancellationToken);
        response.EnsureSuccessStatusCode();

        GameStateResponse? body = await response.Content.ReadFromJsonAsync<GameStateResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }
}
