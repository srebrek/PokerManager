using Contracts.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine;

namespace Identity.Features.Register;

internal sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "identity/register",
            async (
                RegisterRequest request,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                RegisterCommand command = new(request.Email, request.Password);
                Result result = await bus.InvokeAsync<Result>(command, ct);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Identity")
            .AllowAnonymous();
    }
}
