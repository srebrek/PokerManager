using Identity.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Presentation;

namespace Identity.Features.Logout;

internal sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "identity/logout",
            async (IUserAccountService userAccountService) =>
            {
                await userAccountService.LogoutAsync();
                return Results.NoContent();
            })
            .WithTags("Identity")
            .RequireAuthorization();
    }
}
