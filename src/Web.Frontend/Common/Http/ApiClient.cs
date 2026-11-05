using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Frontend.Common.Http;

internal abstract partial class ApiClient(ILogger logger)
{
    protected async Task<ApiResult> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);
        outcome.Response?.Dispose();

        return outcome.Error is { } error
            ? ApiResult.Failure(error)
            : ApiResult.Success();
    }

    protected async Task<ApiResult<TValue>> ExecuteAsync<TValue>(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken ct,
        string genericErrorMessage = "Server error. Try again.")
    {
        RequestOutcome outcome = await HandleHttpCallAsync(sendAsync, ct, genericErrorMessage);
        if (outcome.Error is { } error)
        {
            return ApiResult.Failure<TValue>(error);
        }

        using HttpResponseMessage response = outcome.Response!;

        TValue? value;
        try
        {
            value = await response.Content.ReadFromJsonAsync<TValue>(ct);
        }
        catch (JsonException ex)
        {
            LogUnparsableSuccessBody(logger, ex);
            return ApiResult.Failure<TValue>(ApiError.Uncoded(genericErrorMessage, ApiErrorKind.Server));
        }

        return value is not null
            ? ApiResult.Success(value)
            : throw new InvalidOperationException(
                $"API returned success without expected body: {typeof(TValue).Name}.");
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

            using HttpResponseMessage failedResponse = response;
            ApiErrorBody body = await ApiErrorReader.ReadAsync(response, ct);

            if (body.Message is null)
            {
                LogUnparsableErrorBody(logger, (int)response.StatusCode);
            }

            ApiErrorKind kind = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => ApiErrorKind.Validation,
                HttpStatusCode.Unauthorized => ApiErrorKind.Unauthorized,
                HttpStatusCode.NotFound => ApiErrorKind.NotFound,
                HttpStatusCode.Conflict => ApiErrorKind.Conflict,
                _ => ApiErrorKind.Server
            };

            string message = body.Message ?? genericErrorMessage;
            ApiError error = body.Code is { } code
                ? new ApiError(code, message, kind)
                : ApiError.Uncoded(message, kind);

            return new RequestOutcome(null, error);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "API request failed.")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "API request timed-out.")]
    private static partial void LogRequestTimedOut(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "API success response body could not be parsed.")]
    private static partial void LogUnparsableSuccessBody(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "API error response body could not be parsed: status {StatusCode}.")]
    private static partial void LogUnparsableErrorBody(ILogger logger, int statusCode);
}
