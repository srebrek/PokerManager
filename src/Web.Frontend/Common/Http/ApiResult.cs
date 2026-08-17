using System.Diagnostics.CodeAnalysis;

namespace Web.Frontend.Common.Http;

internal class ApiResult
{
    protected ApiResult(bool isSuccess, ApiError error)
    {
        if ((isSuccess && error != ApiError.None) || (!isSuccess && error == ApiError.None))
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ApiError Error { get; }

    public static ApiResult Success() => new(true, ApiError.None);

    public static ApiResult<TValue> Success<TValue>(TValue value) => new(value, true, ApiError.None);

    public static ApiResult Failure(ApiError error) => new(false, error);

    public static ApiResult<TValue> Failure<TValue>(ApiError error) => new(default, false, error);
}

internal sealed class ApiResult<TValue>(TValue? value, bool isSuccess, ApiError error) : ApiResult(isSuccess, error)
{
    [NotNull]
    public TValue Value
    {
        get => IsSuccess
            ? field!
            : throw new InvalidOperationException($"The value of a failed result can't be accessed: {Error.Code}.");
    } = value;
}

internal sealed record ApiError(string Code, string Message, ApiErrorKind Kind)
{
    public static readonly ApiError None = new(string.Empty, string.Empty, ApiErrorKind.None);

    /// <summary>The request never reached a response: network failure or timeout.</summary>
    public static ApiError Connection(string message) => new("Api.ConnectionFailed", message, ApiErrorKind.Connection);

    /// <summary>An error response whose body carried no <c>errorCode</c> - an unhandled exception, or a
    /// failure raised by the pipeline rather than by a domain rule.</summary>
    public static ApiError Uncoded(string message, ApiErrorKind kind) => new("Api.Unknown", message, kind);
}

internal enum ApiErrorKind
{
    None,
    Validation,
    NotFound,
    Conflict,
    Server,
    Connection
}
