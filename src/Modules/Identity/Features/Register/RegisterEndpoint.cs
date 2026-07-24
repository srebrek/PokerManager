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
                IEnumerable<IValidator<RegisterCommand>> validators,
                CancellationToken ct) =>
            {
                RegisterCommand command = new(request.Email, request.Password);
                Result result = await bus.InvokeValidatedAsync(command, validators, ct);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Identity")
            .AllowAnonymous();
    }
}
