using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Presentation.Infrastructure;

public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IWebHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DbUpdateConcurrencyException concurrencyException)
        {
            LogConcurrencyConflict(logger, concurrencyException);

            ProblemDetails conflictProblem = new()
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflict",
                Detail = "The resource was modified by another request. Reload and try again."
            };

            httpContext.Response.StatusCode = conflictProblem.Status.Value;

            return await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = conflictProblem,
                    Exception = exception
                });
        }

        LogUnhandledException(logger, exception, exception.Message);

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            if (exception.InnerException is not null)
            {
                problemDetails.Extensions["innerException"] = exception.InnerException.Message;
            }

            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred: {message}",
        EventId = 1001)]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string message);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Optimistic concurrency conflict",
        EventId = 1002)]
    private static partial void LogConcurrencyConflict(ILogger logger, Exception exception);
}
