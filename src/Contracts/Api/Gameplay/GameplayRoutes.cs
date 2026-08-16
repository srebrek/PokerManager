namespace Contracts.Api.Gameplay;

public static class GameplayRoutes
{
    // TODO: refactor names eg we dont need CreateGame
    private const string Games = "gameplay/games";
    public const string Tag = "Gameplay";
    public const string CreateGame = Games;

    public const string GetGameByJoinCode = Games;
    public static Uri GetGameByJoinCodeFor(string joinCode) =>
        new($"{Games}?joinCode={Uri.EscapeDataString(joinCode)}", UriKind.Relative);
    public const string GameState = $"{Games}/{{gameId:guid}}";
    public static Uri GameStateFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
    public const string GameStateEndpointName = "GameState";
    public const string StartHand = $"{Games}/{{gameId:guid}}/hands";
    public static Uri StartHandFor(Guid gameId) => new($"{Games}/{gameId}/hands", UriKind.Relative);
    public const string AddParticipant = $"{Games}/{{gameId:guid}}/participants";
    public static Uri AddParticipantFor(Guid gameId) => new($"{Games}/{gameId}/participants", UriKind.Relative);
    public const string FinishGame = $"{Games}/{{gameId:guid}}/finish";
    public static Uri FinishGameFor(Guid gameId) => new($"{Games}/{gameId}/finish", UriKind.Relative);

    private const string Hands = "gameplay/hands";
    public const string HandState = $"{Hands}/{{handId:guid}}";
    public static Uri HandStateFor(Guid handId) => new($"{Hands}/{handId}", UriKind.Relative);
    public const string HandStateEndpointName = "HandState";
    public const string RecordAction = $"{Hands}/{{handId:guid}}/actions";
    public static Uri RecordActionFor(Guid handId) => new($"{Hands}/{handId}/actions", UriKind.Relative);
    public const string FinishHand = $"{Hands}/{{handId:guid}}/finish";
    public static Uri FinishHandFor(Guid handId) => new($"{Hands}/{handId}/finish", UriKind.Relative);
    public const string AbortHand = $"{Hands}/{{handId:guid}}/abort";
    public static Uri AbortHandFor(Guid handId) => new($"{Hands}/{handId}/abort", UriKind.Relative);

    public const string Hub = "gameplay/hub";
    public const string HubJoinGameMethod = "JoinGame";
    public const string HubGameUpdatedMethod = "GameUpdated";
}
