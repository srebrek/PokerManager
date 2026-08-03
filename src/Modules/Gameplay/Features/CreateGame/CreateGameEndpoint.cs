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

namespace Gameplay.Features.CreateGame;

internal sealed class CreateGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.CreateGame,
            async (
                CreateGameRequest request,
                CreateGameCommandHandler handler,
                IEnumerable<IValidator<CreateGameCommand>> validators,
                CancellationToken ct) =>
            {
                CreateGameCommand command = new(request.HostName);
                Result<CreateGameResponse> result =
                    await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(
                    value => Results.Created($"gameplay/games/{value.GameId}", value), // TODO: use LinkGenerator
                    CustomResults.Problem);
            })
            .WithTags("Gameplay")
            .AllowAnonymous();
    }
}
