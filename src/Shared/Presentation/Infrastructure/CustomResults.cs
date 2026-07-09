using Microsoft.AspNetCore.Http;
using Shared.Domain;

namespace Shared.Presentation.Infrastructure;

public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        return result.IsSuccess
            ? throw new InvalidOperationException()
            : Results.Problem(
            title: GetTitle(result.Error.Type),
            detail: result.Error.Description,
            type: GetType(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetExtensions(result));

        static string GetTitle(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation => "Validation error",
                ErrorType.Problem => "Bad request",
                ErrorType.NotFound => "Resource not found",
                ErrorType.Conflict => "Conflict",
                _ => "Server failure"
            };

        static string GetType(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };

        static int GetStatusCode(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

        static Dictionary<string, object?> GetExtensions(Result result)
        {
            Dictionary<string, object?> extensions = new()
            {
                ["errorCode"] = result.Error.Code
            };

            if (result.Error is ValidationError validationError)
            {
                extensions["errors"] = validationError.Errors;
            }

            return extensions;
        }
    }
}
