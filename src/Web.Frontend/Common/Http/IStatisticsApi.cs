using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Statistics;

namespace Web.Frontend.Common.Http;

internal interface IStatisticsApi
{
    Task<ApiResult<GameSummaryResponse>> GetGameSummaryAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResult<HandSummaryResponse>> GetHandSummaryAsync(Guid handId, CancellationToken ct = default);
}

internal sealed partial class StatisticsApi(HttpClient httpClient, ILogger<StatisticsApi> logger) : IStatisticsApi
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

    private async Task<ApiResult<TValue>> ExecuteAsync<TValue>(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);
        if (outcome.Error is { } error)
        {
            return ApiResult.Failure<TValue>(error);
        }

        TValue? value = await outcome.Response!.Content.ReadFromJsonAsync<TValue>(ct);
        return value is not null
            ? ApiResult.Success(value)
            : throw new InvalidOperationException(
                $"Statistics API returned success without expected body: {typeof(TValue).Name}.");
    }

    private readonly record struct RequestOutcome(HttpResponseMessage? Response, ApiError? Error);

    private async Task<RequestOutcome> HandleHttpCallAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        try
        {
            HttpResponseMessage response = await sendAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                return new RequestOutcome(response, null);
            }

            ApiErrorBody body = await ApiErrorReader.ReadAsync(response, ct);

            if (body.Message is null)
            {
                LogUnparsableErrorBody(logger, (int)response.StatusCode);
            }

            ApiErrorKind kind = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => ApiErrorKind.Validation,
                HttpStatusCode.NotFound => ApiErrorKind.NotFound,
                HttpStatusCode.Conflict => ApiErrorKind.Conflict,
                _ => ApiErrorKind.Server
            };

            string message = body.Message ?? genericErrorMessage;
            ApiError error = body.Code is { } code
                ? new ApiError(code, message, kind)
                : ApiError.Uncoded(message, kind);

            return new RequestOutcome(response, error);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            LogRequestTimedOut(logger, ex);
            return new RequestOutcome(null, ApiError.Connection("Connection error. Try again."));
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(logger, ex);
            return new RequestOutcome(null, ApiError.Connection("Connection error. Try again."));
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Statistics API request failed.")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Statistics API request timed-out.")]
    private static partial void LogRequestTimedOut(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Statistics API error response body could not be parsed: status {StatusCode}.")]
    private static partial void LogUnparsableErrorBody(ILogger logger, int statusCode);
}
