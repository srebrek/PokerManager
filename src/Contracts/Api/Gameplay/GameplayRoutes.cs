namespace Contracts.Api.Gameplay;

public static class GameplayRoutes
{
    public const string Tag = "Gameplay";

    public const string Games = "gameplay/games";
    public static Uri GamesByJoinCode(string joinCode) =>
        new($"{Games}?joinCode={Uri.EscapeDataString(joinCode)}", UriKind.Relative);
    public const string Game = $"{Games}/{{gameId:guid}}";
    public static Uri GameFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
    public const string GameRouteName = "Game";
    public const string GameParticipants = $"{Game}/participants";
    public static Uri GameParticipantsFor(Guid gameId) => new($"{Games}/{gameId}/participants", UriKind.Relative);
    public const string GameHands = $"{Game}/hands";
    public static Uri GameHandsFor(Guid gameId) => new($"{Games}/{gameId}/hands", UriKind.Relative);
    public const string FinishGame = $"{Game}/finish";
    public static Uri FinishGameFor(Guid gameId) => new($"{Games}/{gameId}/finish", UriKind.Relative);

    private const string Hands = "gameplay/hands";
    public const string Hand = $"{Hands}/{{handId:guid}}";
    public static Uri HandFor(Guid handId) => new($"{Hands}/{handId}", UriKind.Relative);
    public const string HandRouteName = "Hand";
    public const string HandActions = $"{Hand}/actions";
    public static Uri HandActionsFor(Guid handId) => new($"{Hands}/{handId}/actions", UriKind.Relative);
    public const string FinishHand = $"{Hand}/finish";
    public static Uri FinishHandFor(Guid handId) => new($"{Hands}/{handId}/finish", UriKind.Relative);
    public const string AbortHand = $"{Hand}/abort";
    public static Uri AbortHandFor(Guid handId) => new($"{Hands}/{handId}/abort", UriKind.Relative);

    public const string Hub = "gameplay/hub";
    public const string HubJoinGameMethod = "JoinGame";
    public const string HubLeaveGameMethod = "LeaveGame";
    public const string HubGameUpdatedMethod = "GameUpdated";
    public const string HubJoinHandMethod = "JoinHand";
    public const string HubLeaveHandMethod = "LeaveHand";
    public const string HubApplyHandActionEffectMethod = "ApplyHandActionEffect";
}
