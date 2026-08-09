namespace Contracts.Api.Gameplay;

public static class GameplayRoutes
{
    private const string Games = "gameplay/games";
    public const string Tag = "Gameplay";
    public const string CreateGame = Games;
    public const string JoinGame = $"{Games}/join";
    public const string GameState = $"{Games}/{{gameId:guid}}";
    public static Uri GameStateFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
    public const string GameStateEndpointName = "GameState";
    public const string StartHand = $"{Games}/{{gameId:guid}}/hands";
    public static Uri StartHandFor(Guid gameId) => new($"{Games}/{gameId}/hands", UriKind.Relative);

    private const string Hands = "gameplay/hands";
    public const string HandState = $"{Hands}/{{handId:guid}}";
    public static Uri HandStateFor(Guid handId) => new($"{Hands}/{handId}", UriKind.Relative);
    public const string HandStateEndpointName = "HandState";
}
