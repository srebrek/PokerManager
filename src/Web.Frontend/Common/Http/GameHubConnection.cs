using Contracts.Api.Gameplay;
using Contracts.Api.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Web.Frontend.Common.Http;

internal sealed partial class GameHubConnection : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<GameHubConnection> _logger;

    public GameHubConnection(NavigationManager navigationManager, ILogger<GameHubConnection> logger)
    {
        _logger = logger;
        _connection = new HubConnectionBuilder()
            .WithUrl(
                navigationManager.ToAbsoluteUri($"api/{GameplayRoutes.Hub}"),
                options => options.Headers.Add(HttpDefenseHeaders.AntiCsrf, HttpDefenseHeaders.AntiCsrfValue))
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += LogConnectionClosedAsync;
    }

    public IDisposable OnGameUpdated(Func<Guid, Task> onGameUpdated) =>
        _connection.On(GameplayRoutes.HubGameUpdatedMethod, onGameUpdated);

    public async Task JoinGameAsync(Guid gameId, CancellationToken ct)
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(ct);
        }

        await _connection.InvokeAsync(GameplayRoutes.HubJoinGameMethod, gameId, ct);
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private Task LogConnectionClosedAsync(Exception? exception)
    {
        LogConnectionClosed(_logger, exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Game hub connection closed.")]
    private static partial void LogConnectionClosed(ILogger logger, Exception? exception);
}
