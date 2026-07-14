using Contracts.Http;
using Microsoft.AspNetCore.Http;

namespace Shared.Presentation.Middleware;

public sealed class AntiCsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string method = context.Request.Method;
        bool isStateChanging = HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);

        if (isStateChanging
            && context.Request.Headers[HttpDefenseHeaders.AntiCsrf] != HttpDefenseHeaders.AntiCsrfValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Missing anti-CSRF header.");
            return;
        }

        await next(context);
    }
}
