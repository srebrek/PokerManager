using Microsoft.AspNetCore.Builder;
using Shared.Presentation.Middleware;

namespace Shared.Presentation.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseAntiCsrf(this IApplicationBuilder app)
    {
        app.UseMiddleware<AntiCsrfMiddleware>();
        return app;
    }
}
