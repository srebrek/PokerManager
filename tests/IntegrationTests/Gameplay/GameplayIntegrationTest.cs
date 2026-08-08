using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public abstract class GameplayIntegrationTest(ApiFactory factory) : BaseIntegrationTest(factory)
{
    protected async Task<CreateGameResponse> CreateGameAsync(string hostName)
    {
        CreateGameRequest request = new(hostName);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.CreateGame,
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        CreateGameResponse? body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<JoinGameResponse> JoinGameAsync(string participantName, string joinCode)
    {
        JoinGameRequest request = new(participantName, joinCode);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        JoinGameResponse? body = await response.Content.ReadFromJsonAsync<JoinGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<string> GetJoinCodeAsync(Guid gameId) => (await GetGameStateAsync(gameId)).JoinCode;

    protected async Task<GameStateResponse> GetGameStateAsync(Guid gameId)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            GameplayRoutes.GameStateFor(gameId),
            CancellationToken);
        response.EnsureSuccessStatusCode();

        GameStateResponse? body = await response.Content.ReadFromJsonAsync<GameStateResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }
}
