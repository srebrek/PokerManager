using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shared.Presentation.Infrastructure;

public sealed class TelemetryFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        object? result = await next(context);

        if (result is ProblemHttpResult problem
            && problem.ProblemDetails.Extensions.TryGetValue("errorCode", out object? code)
            && code is string errorCode)
        {
            Activity.Current?.SetTag("error.code", errorCode);
        }

        return result;
    }
}
