using Microsoft.AspNetCore.SignalR;

namespace Gameplay.Features.SubscribeGameStateChanges;

// Public: pulled into the public GameUpdatePushEventHandler's IHubContext<GameHub> signature.
public sealed class GameHub : Hub
{
    public Task JoinGame(Guid gameId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));
    internal static string GroupName(Guid gameId) => $"game-{gameId}";

    public Task JoinHand(Guid handId) => Groups.AddToGroupAsync(Context.ConnectionId, HandGroupName(handId));
    public Task LeaveHand(Guid handId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, HandGroupName(handId));
    internal static string HandGroupName(Guid handId) => $"hand-{handId}";
}
