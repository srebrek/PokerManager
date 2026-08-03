using Contracts.Api.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Application.Validation;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;

namespace Identity.Features.Login;

internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "identity/login",
            async (
                LoginRequest request,
                LoginCommandHandler handler,
                IEnumerable<IValidator<LoginCommand>> validators,
                CancellationToken ct) =>
            {
                LoginCommand command = new(request.Email, request.Password, request.RememberMe);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Identity")
            .AllowAnonymous();
    }
}
