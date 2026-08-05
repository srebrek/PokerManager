namespace Contracts.Api.Gameplay;

public static class GameplayRoutes
{
    private const string Games = "gameplay/games";

    public const string Tag = "Gameplay";
    public const string CreateGame = Games;
    public const string JoinGame = $"{Games}/join";
    public const string GetGameState = $"{Games}/{{gameId:guid}}";
    public const string GetGameStateEndpointName = "GetGameState";

    public static Uri GetGameStateFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
}
