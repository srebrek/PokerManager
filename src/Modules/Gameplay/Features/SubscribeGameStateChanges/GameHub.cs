using Microsoft.AspNetCore.SignalR;

namespace Gameplay.Features.SubscribeGameStateChanges;

// Public: pulled into the public GameUpdatePushEventHandler's IHubContext<GameHub> signature.
public sealed class GameHub : Hub
{
    public Task JoinGame(Guid gameId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));

    internal static string GroupName(Guid gameId) => $"game-{gameId}";
}
