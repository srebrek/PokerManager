using Contracts.Api.Gameplay;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Application.Validation;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;

namespace Gameplay.Features.JoinGame;

internal sealed class JoinGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.JoinGame,
            async (
                JoinGameRequest request,
                JoinGameCommandHandler handler,
                IEnumerable<IValidator<JoinGameCommand>> validators,
                CancellationToken ct) =>
            {
                JoinGameCommand command = new(request.ParticipantName, request.JoinCode);
                Result<JoinGameResponse> result =
                    await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags("Gameplay")
            .AllowAnonymous();
    }
}
