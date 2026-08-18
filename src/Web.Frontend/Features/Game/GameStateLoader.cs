using Contracts.Api.Gameplay;
using Web.Frontend.Common.Http;

namespace Web.Frontend.Features.Game;

internal sealed class GameStateLoader(IGameplayApi api, GameHubConnection hub) : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Guid _gameId;
    private Guid? _currentGameId;
    private Guid? _currentHandId;
    private bool _reloadRequested;
    private bool _forceHandReload;

    public IPageState State { get; private set; } = new Loading();

    public bool IsLoading { get; private set; }

    public Task EnterGameAsync(Guid gameId)
    {
        _gameId = gameId;

        return LoadAsync();
    }

    public Task ResubscribeAsync()
    {
        _currentGameId = null;
        _currentHandId = null;

        return LoadAsync();
    }

    public async Task OnHandActionEffectAsync(HandActionEffect effect)
    {
        if (State is Loaded { Hand: { } hand } loaded && effect.ActionNumber == hand.LastActionNumber + 1)
        {
            State = loaded with { Hand = HandEffectProjection.Apply(hand, effect) };
            return;
        }

        await LoadAsync();
    }

    public Task LoadAsync()
    {
        _forceHandReload = true;
        return LoadCoreAsync();
    }

    public Task OnGameUpdatedAsync() => LoadCoreAsync();

    private async Task LoadCoreAsync()
    {
        if (IsLoading)
        {
            _reloadRequested = true;
            return;
        }

        IsLoading = true;
        try
        {
            do
            {
                _reloadRequested = false;
                bool forceHandReload = _forceHandReload;
                _forceHandReload = false;

                await ReconcileGameSubscriptionAsync(_gameId, _cts.Token);
                State = await LoadStateAsync(forceHandReload);
                await ReconcileHandSubscriptionAsync(
                    State is Loaded { Hand: { } hand } ? hand.HandId : null, _cts.Token);
            }
            while (_reloadRequested);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<IPageState> LoadStateAsync(bool forceHandReload)
    {
        ApiResult<GameStateResponse> gameResult = await api.GetGameStateAsync(_gameId, _cts.Token);
        if (!gameResult.IsSuccess)
        {
            return new Failed(gameResult.Error.Message);
        }

        if (gameResult.Value.CurrentHandId is not { } currentHandId)
        {
            return new Loaded(gameResult.Value, null);
        }

        if (!forceHandReload && State is Loaded { Hand: { } loadedHand } && loadedHand.HandId == currentHandId)
        {
            return new Loaded(gameResult.Value, loadedHand);
        }

        ApiResult<GetHandStateResponse> handResult = await api.GetHandStateAsync(currentHandId, _cts.Token);

        return handResult.IsSuccess
            ? new Loaded(gameResult.Value, handResult.Value)
            : new Failed(handResult.Error.Message);
    }

    private async Task ReconcileGameSubscriptionAsync(Guid? gameId, CancellationToken ct)
    {
        if (_currentGameId == gameId)
        {
            return;
        }

        if (_currentGameId is { } previousGameId)
        {
            await hub.LeaveGameAsync(previousGameId, ct);
            _currentGameId = null;
        }

        if (gameId is { } currentGameId)
        {
            await hub.JoinGameAsync(currentGameId, ct);
            _currentGameId = currentGameId;
        }
    }

    private async Task ReconcileHandSubscriptionAsync(Guid? handId, CancellationToken ct)
    {
        if (_currentHandId == handId)
        {
            return;
        }

        if (_currentHandId is { } previousHandId)
        {
            await hub.LeaveHandAsync(previousHandId, ct);
            _currentHandId = null;
        }

        if (handId is { } currentHandId)
        {
            await hub.JoinHandAsync(currentHandId, ct);
            _currentHandId = currentHandId;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        await ReconcileHandSubscriptionAsync(null, CancellationToken.None);
        await ReconcileGameSubscriptionAsync(null, CancellationToken.None);

        _cts.Dispose();
    }

    internal interface IPageState;

    internal sealed record Loading : IPageState;

    internal sealed record Loaded(GameStateResponse Game, GetHandStateResponse? Hand) : IPageState;

    internal sealed record Failed(string Message) : IPageState;
}
