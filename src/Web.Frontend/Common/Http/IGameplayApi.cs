using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Gameplay;

namespace Web.Frontend.Common.Http;

internal interface IGameplayApi
{
    Task<GameplayResult<CreateGameResponse>> CreateGameAsync(CreateGameRequest request, CancellationToken ct = default);
    Task<GameplayResult<JoinGameResponse>> JoinGameAsync(JoinGameRequest request, CancellationToken ct = default);
    Task<GameplayResult<GameStateResponse>> GetGameStateAsync(Guid gameId, CancellationToken ct = default);
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

    private async Task<GameplayResult<TValue>> ExecuteAsync<TValue>(
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

                return GameplayResult.Failure<TValue>(message ?? genericErrorMessage, errorKind);
            }

            TValue? value = await response.Content.ReadFromJsonAsync<TValue>(ct);

            return value is not null
                ? GameplayResult.Success(value)
                : throw new InvalidOperationException(
                    $"Gameplay API returned success without expected body: {typeof(TValue).Name}.");
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            LogRequestTimedOut(logger, ex);
            return GameplayResult.Failure<TValue>("Connection error. Try again.", GameplayErrorKind.Connection);
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(logger, ex);
            return GameplayResult.Failure<TValue>("Connection error. Try again.", GameplayErrorKind.Connection);
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

internal static class GameplayResult
{
    public static GameplayResult<TValue> Success<TValue>(TValue value) =>
        new(true, value, null, GameplayErrorKind.None);

    public static GameplayResult<TValue> Failure<TValue>(string error, GameplayErrorKind errorKind) =>
        new(false, default, error, errorKind);
}

// TODO: make it imposible to create invalid result e.g. value and error notnull
internal sealed record GameplayResult<TValue>
{
    internal GameplayResult(bool isSuccess, TValue? value, string? error, GameplayErrorKind errorKind)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess { get; }
    public TValue? Value { get; }
    public string? Error { get; }
    public GameplayErrorKind ErrorKind { get; }
}

internal enum GameplayErrorKind
{
    None,
    Validation,
    NotFound,
    Server,
    Connection
}
