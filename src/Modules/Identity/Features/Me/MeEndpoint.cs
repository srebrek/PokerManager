using System.Security.Claims;
using Contracts.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Presentation;

namespace Identity.Features.Me;

internal sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "identity/me",
            (ClaimsPrincipal user) =>
            {
                string? idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                string? email = user.FindFirst(ClaimTypes.Name)?.Value;

                if (!Guid.TryParse(idStr, out Guid id) || email is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new CurrentUserResponse(id, email));
            })
            .WithTags("Identity")
            .RequireAuthorization();
    }
}
