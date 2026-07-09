using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Shared.Presentation.Middleware;

public sealed class RequestContextMiddleware(
    RequestDelegate next,
    ILogger<RequestContextMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetCorrelationId(context);

        Dictionary<string, object> logScope = new()
        {
            ["CorrelationId"] = correlationId
        };

        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            Activity.Current?.SetTag("user.id", userId);
            logScope["UserId"] = userId;
        }

        using (logger.BeginScope(logScope))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName,
            out StringValues correlationId);

        return correlationId.FirstOrDefault()
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;
    }
}
