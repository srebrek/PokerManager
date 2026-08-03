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

namespace Identity.Features.Register;

internal sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "identity/register",
            async (
                RegisterRequest request,
                RegisterCommandHandler handler,
                IEnumerable<IValidator<RegisterCommand>> validators,
                CancellationToken ct) =>
            {
                RegisterCommand command = new(request.Email, request.Password);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Identity")
            .AllowAnonymous();
    }
}
