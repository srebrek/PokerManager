using Contracts.Api.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Domain;
using Shared.Infrastructure.Messaging;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine;

namespace Identity.Features.Login;

internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "identity/login",
            async (
                LoginRequest request,
                IMessageBus bus,
                IEnumerable<IValidator<LoginCommand>> validators,
                CancellationToken ct) =>
            {
                LoginCommand command = new(request.Email, request.Password, request.RememberMe);
                Result result = await bus.InvokeValidatedAsync(command, validators, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Identity")
            .AllowAnonymous();
    }
}
