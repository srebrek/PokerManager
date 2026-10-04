using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Presentation.Middleware;

/// <summary>
/// Logs the client address of every request, so it is possible to find out what wakes the app up
/// from scale-to-zero. Must run after forwarded headers processing to see the real client IP.
/// </summary>
public sealed partial class RequestSourceLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestSourceLoggingMiddleware> logger)
{
    private const int MaxValueLength = 200;

    public async Task InvokeAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string forwardedFor = Sanitize(request.Headers["X-Forwarded-For"].ToString());
        string host = Sanitize(request.Host.Value);
        string path = Sanitize(request.Path.Value);
        string userAgent = Sanitize(request.Headers.UserAgent.ToString());

        LogRequestSource(ip, forwardedFor, request.Method, host, path, userAgent);

        await next(context);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "REQ ip={Ip} xff={ForwardedFor} {Method} host={Host} path={Path} ua={UserAgent}")]
    private partial void LogRequestSource(
        string ip,
        string forwardedFor,
        string method,
        string host,
        string path,
        string userAgent);

    // Request values are attacker-controlled: strip control characters so they cannot forge log lines.
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        string trimmed = value.Length > MaxValueLength ? value[..MaxValueLength] : value;

        return string.Create(trimmed.Length, trimmed, static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                span[i] = char.IsControl(source[i]) ? '_' : source[i];
            }
        });
    }
}
