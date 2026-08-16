using Contracts.Api.Gameplay;
using Contracts.Api.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace IntegrationTests.Gameplay;

public sealed class GameHubIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task AddParticipant_BroadcastsGameStateChangedToJoinedGroup()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");

        await using HubConnection connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(Factory.Server.BaseAddress, $"api/{GameplayRoutes.Hub}"),
                HttpTransportType.LongPolling,
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    options.Headers.Add(HttpDefenseHeaders.AntiCsrf, HttpDefenseHeaders.AntiCsrfValue);
                })
            .Build();

        TaskCompletionSource<Guid> broadcastReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Guid>(GameplayRoutes.HubGameUpdatedMethod, gameId => broadcastReceived.TrySetResult(gameId));

        await connection.StartAsync(CancellationToken);
        await connection.InvokeAsync(GameplayRoutes.HubJoinGameMethod, game.GameId, CancellationToken);

        // Act
        await AddParticipantAsync(game.GameId, "TestParticipantName");

        // Assert
        Guid broadcastGameId = await broadcastReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
        broadcastGameId.ShouldBe(game.GameId);
    }
}
