using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Gameplay;

namespace Web.Frontend.Common.Http;

internal interface IGameplayApi
{
    Task<ApiResult<CreateGameResponse>> CreateGameAsync(CreateGameRequest request, CancellationToken ct = default);
    Task<ApiResult<GameLookupResponse>> GetGameByJoinCodeAsync(string joinCode, CancellationToken ct = default);
    Task<ApiResult<AddParticipantResponse>> AddParticipantAsync(
      Guid gameId, AddParticipantRequest request, CancellationToken ct = default);
    Task<ApiResult<GameStateResponse>> GetGameStateAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResult<GetHandStateResponse>> GetHandStateAsync(Guid handId, CancellationToken ct = default);
    Task<ApiResult<StartHandResponse>> StartHandAsync(
      Guid gameId, StartHandRequest request, CancellationToken ct = default);
    Task<ApiResult> RecordActionAsync(Guid handId, RecordActionRequest request, CancellationToken ct = default);
    Task<ApiResult> FinishHandAsync(Guid handId, FinishHandRequest request, CancellationToken ct = default);
    Task<ApiResult> AbortHandAsync(Guid handId, AbortHandRequest request, CancellationToken ct = default);
    Task<ApiResult> FinishGameAsync(Guid gameId, FinishGameRequest request, CancellationToken ct = default);
    Task<ApiResult> RebuyAsync(
      Guid gameId, Guid participantId, RebuyRequest request, CancellationToken ct = default);
    Task<ApiResult> MoveParticipantDownAsync(
      Guid gameId, Guid participantId, MoveParticipantDownRequest request, CancellationToken ct = default);
    Task<ApiResult> SetParticipantSittingOutAsync(
      Guid gameId, Guid participantId, SetParticipantSittingOutRequest request, CancellationToken ct = default);
    Task<ApiResult> MoveDealerButtonAsync(
      Guid gameId, MoveDealerButtonRequest request, CancellationToken ct = default);
    Task<ApiResult> ChangeGameRulesAsync(
      Guid gameId, ChangeGameRulesRequest request, CancellationToken ct = default);
}

internal sealed partial class GameplayApi(HttpClient httpClient, ILogger<GameplayApi> logger) : IGameplayApi
{
    public Task<ApiResult<CreateGameResponse>> CreateGameAsync(
        CreateGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<CreateGameResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.Games, request, ct),
            ct);

    public Task<ApiResult<GameLookupResponse>> GetGameByJoinCodeAsync(
        string joinCode,
        CancellationToken ct = default) =>
        ExecuteAsync<GameLookupResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.GamesByJoinCode(joinCode), ct),
            ct);

    public Task<ApiResult<AddParticipantResponse>> AddParticipantAsync(
        Guid gameId,
        AddParticipantRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<AddParticipantResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.GameParticipantsFor(gameId), request, ct),
            ct);

    public Task<ApiResult<GameStateResponse>> GetGameStateAsync(
        Guid gameId,
        CancellationToken ct = default) =>
        ExecuteAsync<GameStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.GameFor(gameId), ct),
            ct);

    public Task<ApiResult<GetHandStateResponse>> GetHandStateAsync(
        Guid handId,
        CancellationToken ct = default) =>
        ExecuteAsync<GetHandStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.HandFor(handId), ct),
            ct);

    public Task<ApiResult<StartHandResponse>> StartHandAsync(
        Guid gameId,
        StartHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<StartHandResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.GameHandsFor(gameId), request, ct),
            ct);

    public Task<ApiResult> RecordActionAsync(
        Guid handId,
        RecordActionRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.HandActionsFor(handId), request, ct),
            ct);

    public Task<ApiResult> FinishHandAsync(
        Guid handId,
        FinishHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.FinishHandFor(handId), request, ct),
            ct);

    public Task<ApiResult> AbortHandAsync(
        Guid handId,
        AbortHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.AbortHandFor(handId), request, ct),
            ct);

    public Task<ApiResult> FinishGameAsync(
        Guid gameId,
        FinishGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.FinishGameFor(gameId), request, ct),
            ct);

    public Task<ApiResult> RebuyAsync(
        Guid gameId,
        Guid participantId,
        RebuyRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.RebuyFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> MoveParticipantDownAsync(
        Guid gameId,
        Guid participantId,
        MoveParticipantDownRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(
                GameplayRoutes.MoveParticipantDownFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> SetParticipantSittingOutAsync(
        Guid gameId,
        Guid participantId,
        SetParticipantSittingOutRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(
                GameplayRoutes.SetParticipantSittingOutFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> MoveDealerButtonAsync(
        Guid gameId,
        MoveDealerButtonRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.DealerButtonFor(gameId), request, ct),
            ct);

    public Task<ApiResult> ChangeGameRulesAsync(
        Guid gameId,
        ChangeGameRulesRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.RulesFor(gameId), request, ct),
            ct);

    private async Task<ApiResult> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);

        return outcome.Error is { } error
            ? ApiResult.Failure(error)
            : ApiResult.Success();
    }

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
                $"Gameplay API returned success without expected body: {typeof(TValue).Name}.");
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Gameplay API request failed.")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Gameplay API request timed-out.")]
    private static partial void LogRequestTimedOut(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Gameplay API error response body could not be parsed: status {StatusCode}.")]
    private static partial void LogUnparsableErrorBody(ILogger logger, int statusCode);
}
