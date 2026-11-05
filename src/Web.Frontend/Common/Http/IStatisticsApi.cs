using Contracts.Api.Statistics;

namespace Web.Frontend.Common.Http;

internal interface IStatisticsApi
{
    Task<ApiResult<GameSummaryResponse>> GetGameSummaryAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResult<HandSummaryResponse>> GetHandSummaryAsync(Guid handId, CancellationToken ct = default);
}

internal sealed class StatisticsApi(HttpClient httpClient, ILogger<StatisticsApi> logger)
    : ApiClient(logger), IStatisticsApi
{
    public Task<ApiResult<GameSummaryResponse>> GetGameSummaryAsync(
        Guid gameId,
        CancellationToken ct = default) =>
        ExecuteAsync<GameSummaryResponse>(
            ct => httpClient.GetAsync(StatisticsRoutes.GameSummaryFor(gameId), ct),
            ct);

    public Task<ApiResult<HandSummaryResponse>> GetHandSummaryAsync(
        Guid handId,
        CancellationToken ct = default) =>
        ExecuteAsync<HandSummaryResponse>(
            ct => httpClient.GetAsync(StatisticsRoutes.HandSummaryFor(handId), ct),
            ct);
}
