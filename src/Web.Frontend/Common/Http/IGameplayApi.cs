using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Gameplay;

namespace Web.Frontend.Common.Http;

internal interface IGameplayApi
{
    Task<GameplayResult<CreateGameResponse>> CreateGameAsync(CreateGameRequest request, CancellationToken ct = default);
    Task<GameplayResult<JoinGameResponse>> JoinGameAsync(JoinGameRequest request, CancellationToken ct = default);
    Task<GameplayResult<GameStateResponse>> GetGameStateAsync(Guid gameId, CancellationToken ct = default);
    Task<GameplayResult<GetHandStateResponse>> GetHandStateAsync(Guid handId, CancellationToken ct = default);
    Task<GameplayResult<StartHandResponse>> StartHandAsync(
      Guid gameId, StartHandRequest request, CancellationToken ct = default);
    Task<GameplayResult> RecordActionAsync(Guid handId, RecordActionRequest request, CancellationToken ct = default);
    Task<GameplayResult> FinishHandAsync(Guid handId, FinishHandRequest request, CancellationToken ct = default);
}

internal sealed partial class GameplayApi(HttpClient httpClient, ILogger<GameplayApi> logger) : IGameplayApi
{
    public Task<GameplayResult<CreateGameResponse>> CreateGameAsync(
        CreateGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<CreateGameResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.CreateGame, request, ct),
            ct);

    public Task<GameplayResult<JoinGameResponse>> JoinGameAsync(
        JoinGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<JoinGameResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.JoinGame, request, ct),
            ct);

    public Task<GameplayResult<GameStateResponse>> GetGameStateAsync(
        Guid gameId,
        CancellationToken ct = default) =>
        ExecuteAsync<GameStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.GameStateFor(gameId), ct),
            ct);

    public Task<GameplayResult<GetHandStateResponse>> GetHandStateAsync(
        Guid handId,
        CancellationToken ct = default) =>
        ExecuteAsync<GetHandStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.HandStateFor(handId), ct),
            ct);

    public Task<GameplayResult<StartHandResponse>> StartHandAsync(
        Guid gameId,
        StartHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<StartHandResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.StartHandFor(gameId), request, ct),
            ct);

    public Task<GameplayResult> RecordActionAsync(
        Guid handId,
        RecordActionRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.RecordActionFor(handId), request, ct),
            ct);

    public Task<GameplayResult> FinishHandAsync(
        Guid handId,
        FinishHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.FinishHandFor(handId), request, ct),
            ct);

    private async Task<GameplayResult> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);
        if (!outcome.Success)
        {
            return GameplayResult.Failure(outcome.Error!, outcome.ErrorKind);
        }

        return GameplayResult.Success();
    }

    private async Task<GameplayResult<TValue>> ExecuteAsync<TValue>(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);
        if (!outcome.Success)
        {
            return GameplayResult.Failure<TValue>(outcome.Error!, outcome.ErrorKind);
        }

        TValue? value = await outcome.Response!.Content.ReadFromJsonAsync<TValue>(ct);
        return value is not null
            ? GameplayResult.Success(value)
            : throw new InvalidOperationException(
                $"Gameplay API returned success without expected body: {typeof(TValue).Name}.");
    }

    private readonly record struct RequestOutcome(
      bool Success, HttpResponseMessage? Response, string? Error, GameplayErrorKind ErrorKind);

    private async Task<RequestOutcome> HandleHttpCallAsync(
      Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
      CancellationToken ct,
      string genericErrorMessage = "Server error. Try again.")
    {
        try
        {
            HttpResponseMessage response = await sendAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                string? message = await ApiErrorReader.ReadMessageAsync(response, ct);

                if (message is null)
                {
                    LogUnparsableErrorBody(logger, (int)response.StatusCode);
                }

                GameplayErrorKind errorKind = response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => GameplayErrorKind.Validation,
                    HttpStatusCode.NotFound => GameplayErrorKind.NotFound,
                    _ => GameplayErrorKind.Server
                };

                return new RequestOutcome(false, response, message ?? genericErrorMessage, errorKind);
            }

            return new RequestOutcome(true, response, null, GameplayErrorKind.None);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            LogRequestTimedOut(logger, ex);
            return new RequestOutcome(false, null, "Connection error. Try again.", GameplayErrorKind.Connection);
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(logger, ex);
            return new RequestOutcome(false, null, "Connection error. Try again.", GameplayErrorKind.Connection);
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

// TODO: make it imposible to create invalid result e.g. value and error notnull
internal class GameplayResult(bool isSuccess, string? error, GameplayErrorKind errorKind)
{
    public bool IsSuccess { get; } = isSuccess;
    public string? Error { get; } = error;
    public GameplayErrorKind ErrorKind { get; } = errorKind;

    public static GameplayResult Success() =>
        new(true, null, GameplayErrorKind.None);

    public static GameplayResult<TValue> Success<TValue>(TValue value) =>
        new(true, value, null, GameplayErrorKind.None);

    public static GameplayResult Failure(string error, GameplayErrorKind errorKind) =>
        new(false, error, errorKind);

    public static GameplayResult<TValue> Failure<TValue>(string error, GameplayErrorKind errorKind) =>
        new(false, default, error, errorKind);
}

internal sealed class GameplayResult<TValue>(bool isSuccess, TValue? value, string? error, GameplayErrorKind errorKind)
    : GameplayResult(isSuccess, error, errorKind)
{
    public TValue? Value { get; } = value;
}

internal enum GameplayErrorKind
{
    None,
    Validation,
    NotFound,
    Server,
    Connection
}
