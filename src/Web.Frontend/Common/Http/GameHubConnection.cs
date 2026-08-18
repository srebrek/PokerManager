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

    public IDisposable OnReconnected(Func<Task> onReconnected)
    {
        Task handler(string? _) => onReconnected();
        _connection.Reconnected += handler;

        return new Unsubscriber(() => _connection.Reconnected -= handler);
    }

    public async Task JoinGameAsync(Guid gameId, CancellationToken ct)
    {
        if (_connection.State is HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(ct);
        }

        await _connection.InvokeAsync(GameplayRoutes.HubJoinGameMethod, gameId, ct);
    }

    public Task LeaveGameAsync(Guid gameId, CancellationToken ct) =>
        LeaveGroupAsync(GameplayRoutes.HubLeaveGameMethod, gameId, ct);

    public async Task JoinHandAsync(Guid handId, CancellationToken ct)
    {
        if (_connection.State is HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(ct);
        }

        await _connection.InvokeAsync(GameplayRoutes.HubJoinHandMethod, handId, ct);
    }

    public Task LeaveHandAsync(Guid handId, CancellationToken ct) =>
        LeaveGroupAsync(GameplayRoutes.HubLeaveHandMethod, handId, ct);

    private Task LeaveGroupAsync(string hubMethod, Guid id, CancellationToken ct) =>
        _connection.State is HubConnectionState.Connected
            ? _connection.InvokeAsync(hubMethod, id, ct)
            : Task.CompletedTask;

    public IDisposable OnHandActionEffectReceived(Func<HandActionEffect, Task> onHandActionEffectReceived) =>
        _connection.On(GameplayRoutes.HubApplyHandActionEffectMethod, onHandActionEffectReceived);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private Task LogConnectionClosedAsync(Exception? exception)
    {
        LogConnectionClosed(_logger, exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Game hub connection closed.")]
    private static partial void LogConnectionClosed(ILogger logger, Exception? exception);

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
