namespace Contracts.Api.Statistics;

public static class StatisticsRoutes
{
    public const string Tag = "Statistics";

    private const string Hands = "statistics/hands";
    public const string HandSummary = $"{Hands}/{{handId:guid}}";
    public static Uri HandSummaryFor(Guid handId) => new($"{Hands}/{handId}", UriKind.Relative);

    private const string Games = "statistics/games";
    public const string GameSummary = $"{Games}/{{gameId:guid}}";
    public static Uri GameSummaryFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
}
